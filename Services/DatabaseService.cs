using System.Data.SQLite;
using System.IO;
using ExcelToSQLite.Models;

namespace ExcelToSQLite.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly string _databasePath;

        public DatabaseService()
        {
            _databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "records.db");
            _connectionString = $"Data Source={_databasePath};Version=3;";
        }

        public void InitializeDatabase()
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    string createTableQuery = @"
                        CREATE TABLE IF NOT EXISTS Records (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Phone TEXT NOT NULL UNIQUE,
                            MiniThanTai INTEGER NOT NULL DEFAULT 0,
                            BigThanTai INTEGER NOT NULL DEFAULT 0,
                            LocPhat INTEGER NOT NULL DEFAULT 0,
                            TuQuy INTEGER NOT NULL DEFAULT 0,
                            TuQuyGiua INTEGER NOT NULL DEFAULT 0,
                            TamHoaKep INTEGER NOT NULL DEFAULT 0
                        )";

                    using (var command = new SQLiteCommand(createTableQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    // Create index on Phone column for faster queries
                    string createIndexQuery = @"
                        CREATE INDEX IF NOT EXISTS idx_phone ON Records(Phone)";

                    using (var command = new SQLiteCommand(createIndexQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error initializing database: {ex.Message}", ex);
            }
        }

        public (int Imported, int Duplicates) ImportRecords(List<Record> records)
        {
            int importedCount = 0;
            int duplicateCount = 0;

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Use INSERT OR IGNORE to skip duplicates
                            string insertQuery = @"
                                INSERT OR IGNORE INTO Records (Phone, MiniThanTai, BigThanTai, LocPhat, TuQuy, TuQuyGiua, TamHoaKep)
                                VALUES (@Phone, @MiniThanTai, @BigThanTai, @LocPhat, @TuQuy, @TuQuyGiua, @TamHoaKep)";

                            foreach (var record in records)
                            {
                                using (var command = new SQLiteCommand(insertQuery, connection))
                                {
                                    command.Parameters.AddWithValue("@Phone", record.Phone);
                                    command.Parameters.AddWithValue("@MiniThanTai", record.MiniThanTai ? 1 : 0);
                                    command.Parameters.AddWithValue("@BigThanTai", record.BigThanTai ? 1 : 0);
                                    command.Parameters.AddWithValue("@LocPhat", record.LocPhat ? 1 : 0);
                                    command.Parameters.AddWithValue("@TuQuy", record.TuQuy ? 1 : 0);
                                    command.Parameters.AddWithValue("@TuQuyGiua", record.TuQuyGiua ? 1 : 0);
                                    command.Parameters.AddWithValue("@TamHoaKep", record.TamHoaKep ? 1 : 0);

                                    int rowsAffected = command.ExecuteNonQuery();

                                    if (rowsAffected > 0)
                                    {
                                        importedCount++;
                                    }
                                    else
                                    {
                                        duplicateCount++;
                                    }
                                }
                            }

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error importing records to database: {ex.Message}", ex);
            }

            return (importedCount, duplicateCount);
        }

        // Get records with pagination
        public List<Record> GetRecordsWithPagination(int pageNumber, int pageSize, string phoneFilter = "",
            bool filterMiniThanTai = false, bool filterBigThanTai = false, bool filterLocPhat = false,
            bool filterTuQuy = false, bool filterTuQuyGiua = false, bool filterTamHoaKep = false)
        {
            var records = new List<Record>();

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    int offset = (pageNumber - 1) * pageSize;

                    // Build WHERE clause
                    var whereConditions = new List<string>();

                    if (!string.IsNullOrWhiteSpace(phoneFilter))
                    {
                        whereConditions.Add("Phone LIKE @PhoneFilter");
                    }

                    if (filterMiniThanTai) whereConditions.Add("MiniThanTai = 1");
                    if (filterBigThanTai) whereConditions.Add("BigThanTai = 1");
                    if (filterLocPhat) whereConditions.Add("LocPhat = 1");
                    if (filterTuQuy) whereConditions.Add("TuQuy = 1");
                    if (filterTuQuyGiua) whereConditions.Add("TuQuyGiua = 1");
                    if (filterTamHoaKep) whereConditions.Add("TamHoaKep = 1");

                    string whereClause = whereConditions.Count > 0
                        ? "WHERE " + string.Join(" OR ", whereConditions)
                        : "";

                    string selectQuery = $@"
                        SELECT Id, Phone, MiniThanTai, BigThanTai, LocPhat, TuQuy, TuQuyGiua, TamHoaKep 
                        FROM Records 
                        {whereClause}
                        ORDER BY Id DESC 
                        LIMIT @PageSize OFFSET @Offset";

                    using (var command = new SQLiteCommand(selectQuery, connection))
                    {
                        if (!string.IsNullOrWhiteSpace(phoneFilter))
                        {
                            command.Parameters.AddWithValue("@PhoneFilter", "%" + phoneFilter + "%");
                        }

                        command.Parameters.AddWithValue("@PageSize", pageSize);
                        command.Parameters.AddWithValue("@Offset", offset);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var record = new Record
                                {
                                    Id = reader.GetInt32(0),
                                    Phone = reader.GetString(1),
                                    MiniThanTai = reader.GetInt32(2) == 1,
                                    BigThanTai = reader.GetInt32(3) == 1,
                                    LocPhat = reader.GetInt32(4) == 1,
                                    TuQuy = reader.GetInt32(5) == 1,
                                    TuQuyGiua = reader.GetInt32(6) == 1,
                                    TamHoaKep = reader.GetInt32(7) == 1
                                };
                                records.Add(record);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving records from database: {ex.Message}", ex);
            }

            return records;
        }

        public int GetRecordCount(string phoneFilter = "",
            bool filterMiniThanTai = false, bool filterBigThanTai = false, bool filterLocPhat = false,
            bool filterTuQuy = false, bool filterTuQuyGiua = false, bool filterTamHoaKep = false)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    // Build WHERE clause
                    var whereConditions = new List<string>();

                    if (!string.IsNullOrWhiteSpace(phoneFilter))
                    {
                        whereConditions.Add("Phone LIKE @PhoneFilter");
                    }

                    if (filterMiniThanTai) whereConditions.Add("MiniThanTai = 1");
                    if (filterBigThanTai) whereConditions.Add("BigThanTai = 1");
                    if (filterLocPhat) whereConditions.Add("LocPhat = 1");
                    if (filterTuQuy) whereConditions.Add("TuQuy = 1");
                    if (filterTuQuyGiua) whereConditions.Add("TuQuyGiua = 1");
                    if (filterTamHoaKep) whereConditions.Add("TamHoaKep = 1");

                    string whereClause = whereConditions.Count > 0
                        ? "WHERE " + string.Join(" OR ", whereConditions)
                        : "";

                    string countQuery = $"SELECT COUNT(*) FROM Records {whereClause}";

                    using (var command = new SQLiteCommand(countQuery, connection))
                    {
                        if (!string.IsNullOrWhiteSpace(phoneFilter))
                        {
                            command.Parameters.AddWithValue("@PhoneFilter", "%" + phoneFilter + "%");
                        }

                        return Convert.ToInt32(command.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting record count: {ex.Message}", ex);
            }
        }
    }
}
