using System.Data.SQLite;
using System.IO;
using Mobiphone.Models;

namespace Mobiphone.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly string _databasePath;

        public DatabaseService()
        {
            // Use LocalApplicationData to store database (writable location)
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SC Hoang Quoc"
            );

            // Create directory if it doesn't exist
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            _databasePath = Path.Combine(appDataPath, "records.db");
            _connectionString = $"Data Source={_databasePath};Version=3;";
        }

        public string DatabasePath => _databasePath;

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
                            TuQuy INTEGER NOT NULL DEFAULT 0,
                            Taxi2 INTEGER NOT NULL DEFAULT 0,
                            Taxi3 INTEGER NOT NULL DEFAULT 0,
                            Taxi4 INTEGER NOT NULL DEFAULT 0,
                            Taxi5 INTEGER NOT NULL DEFAULT 0,
                            TaxiDu2 INTEGER NOT NULL DEFAULT 0,
                            TaxiDu3 INTEGER NOT NULL DEFAULT 0,
                            DuoiTien INTEGER NOT NULL DEFAULT 0,
                            SanhGiua INTEGER NOT NULL DEFAULT 0,
                            TamHoa INTEGER NOT NULL DEFAULT 0,
                            SoiGuong INTEGER NOT NULL DEFAULT 0,
                            AXA_AYA INTEGER NOT NULL DEFAULT 0,
                            AXA_BXB INTEGER NOT NULL DEFAULT 0,
                            AXA_BYB INTEGER NOT NULL DEFAULT 0,
                            ABABAC INTEGER NOT NULL DEFAULT 0,
                            ABACAC INTEGER NOT NULL DEFAULT 0
                        )";

                    using (var command = new SQLiteCommand(createTableQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    // 2. Thêm cột XXXYYY nếu DB cũ chưa có (Tránh crash ứng dụng)
                    string addColumnQuery = @" ALTER TABLE Records ADD COLUMN XXXYYY INTEGER NOT NULL DEFAULT 0;";

                    try
                    {
                        using (var cmd = new SQLiteCommand(addColumnQuery, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch (SQLiteException)
                    {
                        // Bỏ qua lỗi nếu cột đã tồn tại (duplicate column name)
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
                            // Query INSERT đã cập nhật theo 16 trường mới
                            string insertQuery = @"INSERT OR IGNORE INTO Records (
                                    Phone, TuQuy, Taxi2, Taxi3, Taxi4, Taxi5, 
                                    TaxiDu2, TaxiDu3, DuoiTien, SanhGiua, TamHoa, 
                                    SoiGuong, AXA_AYA, AXA_BXB, AXA_BYB, ABABAC, ABACAC, 
                                    XXXYYY
                                ) VALUES (
                                    @Phone, @TuQuy, @Taxi2, @Taxi3, @Taxi4, @Taxi5, 
                                    @TaxiDu2, @TaxiDu3, @DuoiTien, @SanhGiua, @TamHoa, 
                                    @SoiGuong, @AXA_AYA, @AXA_BXB, @AXA_BYB, @ABABAC, @ABACAC, 
                                    @XXXYYY
                                )";

                            // Khởi tạo command một lần ngoài vòng lặp để tối ưu bộ nhớ & tốc độ
                            using (var command = new SQLiteCommand(insertQuery, connection))
                            {
                                foreach (var record in records)
                                {
                                    command.Parameters.Clear();

                                    command.Parameters.AddWithValue("@Phone", record.Phone ?? string.Empty);
                                    command.Parameters.AddWithValue("@TuQuy", record.TuQuy ? 1 : 0);
                                    command.Parameters.AddWithValue("@Taxi2", record.Taxi2 ? 1 : 0);
                                    command.Parameters.AddWithValue("@Taxi3", record.Taxi3 ? 1 : 0);
                                    command.Parameters.AddWithValue("@Taxi4", record.Taxi4 ? 1 : 0);
                                    command.Parameters.AddWithValue("@Taxi5", record.Taxi5 ? 1 : 0);
                                    command.Parameters.AddWithValue("@TaxiDu2", record.TaxiDu2 ? 1 : 0);
                                    command.Parameters.AddWithValue("@TaxiDu3", record.TaxiDu3 ? 1 : 0);
                                    command.Parameters.AddWithValue("@DuoiTien", record.DuoiTien ? 1 : 0);
                                    command.Parameters.AddWithValue("@SanhGiua", record.SanhGiua ? 1 : 0);
                                    command.Parameters.AddWithValue("@TamHoa", record.TamHoa ? 1 : 0);
                                    command.Parameters.AddWithValue("@SoiGuong", record.SoiGuong ? 1 : 0);
                                    command.Parameters.AddWithValue("@AXA_AYA", record.AXA_AYA ? 1 : 0);
                                    command.Parameters.AddWithValue("@AXA_BXB", record.AXA_BXB ? 1 : 0);
                                    command.Parameters.AddWithValue("@AXA_BYB", record.AXA_BYB ? 1 : 0);
                                    command.Parameters.AddWithValue("@ABABAC", record.ABABAC ? 1 : 0);
                                    command.Parameters.AddWithValue("@ABACAC", record.ABACAC ? 1 : 0);
                                    command.Parameters.AddWithValue("@XXXYYY", record.XXXYYY ? 1 : 0);

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
        public List<Record> GetRecordsWithPagination(int pageNumber, int pageSize, RecordFilterOptions filterOptions)
        {
            var records = new List<Record>();

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    int offset = (pageNumber - 1) * pageSize;

                    // Build WHERE clause
                    var typeConditions = new List<string>();

                    AddStoredTypeConditions(filterOptions, typeConditions);
                    AddAdvancedTypeConditions(filterOptions, typeConditions);

                    var allConditions = new List<string>();

                    // Lọc theo chuỗi Phone
                    if (!string.IsNullOrWhiteSpace(filterOptions.PhoneFilter))
                    {
                        allConditions.Add("Phone LIKE @PhoneFilter");
                    }
                    AddExclusionCondition(filterOptions, allConditions);

                    // Gộp các điều kiện loại sim theo OR
                    if (typeConditions.Count > 0)
                    {
                        allConditions.Add("(" + string.Join(" OR ", typeConditions) + ")");
                    }

                    string whereClause = allConditions.Count > 0 ? "WHERE " + string.Join(" AND ", allConditions) : "";

                    string selectQuery = $@"
                        SELECT 
                            Id, Phone, TuQuy, Taxi2, Taxi3, Taxi4, Taxi5, 
                            TaxiDu2, TaxiDu3, DuoiTien, SanhGiua, TamHoa, 
                            SoiGuong, AXA_AYA, AXA_BXB, AXA_BYB, ABABAC, ABACAC,
                            XXXYYY
                        FROM Records 
                        {whereClause}
                        ORDER BY Id DESC 
                        LIMIT @PageSize OFFSET @Offset";

                    using (var command = new SQLiteCommand(selectQuery, connection))
                    {
                        if (!string.IsNullOrWhiteSpace(filterOptions.PhoneFilter))
                        {
                            string searchPattern = "%" + filterOptions.PhoneFilter.Replace('?', '_') + "%";
                            command.Parameters.AddWithValue("@PhoneFilter", searchPattern);
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
                                    TuQuy = reader.GetInt32(2) == 1,
                                    Taxi2 = reader.GetInt32(3) == 1,
                                    Taxi3 = reader.GetInt32(4) == 1,
                                    Taxi4 = reader.GetInt32(5) == 1,
                                    Taxi5 = reader.GetInt32(6) == 1,
                                    TaxiDu2 = reader.GetInt32(7) == 1,
                                    TaxiDu3 = reader.GetInt32(8) == 1,
                                    DuoiTien = reader.GetInt32(9) == 1,
                                    SanhGiua = reader.GetInt32(10) == 1,
                                    TamHoa = reader.GetInt32(11) == 1,
                                    SoiGuong = reader.GetInt32(12) == 1,
                                    AXA_AYA = reader.GetInt32(13) == 1,
                                    AXA_BXB = reader.GetInt32(14) == 1,
                                    AXA_BYB = reader.GetInt32(15) == 1,
                                    ABABAC = reader.GetInt32(16) == 1,
                                    ABACAC = reader.GetInt32(17) == 1,
                                    XXXYYY = reader.GetInt32(18) == 1
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

        public List<Record> GetAllFilteredRecords(RecordFilterOptions filterOptions)
        {
            var records = new List<Record>();

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    // Build WHERE clause
                    var typeConditions = new List<string>();

                    AddStoredTypeConditions(filterOptions, typeConditions);
                    AddAdvancedTypeConditions(filterOptions, typeConditions);

                    var allConditions = new List<string>();

                    // Lọc theo chuỗi Phone
                    if (!string.IsNullOrWhiteSpace(filterOptions.PhoneFilter))
                    {
                        allConditions.Add("Phone LIKE @PhoneFilter");
                    }
                    AddExclusionCondition(filterOptions, allConditions);

                    // Gộp các điều kiện loại sim theo OR
                    if (typeConditions.Count > 0)
                    {
                        allConditions.Add("(" + string.Join(" OR ", typeConditions) + ")");
                    }

                    string whereClause = allConditions.Count > 0 ? "WHERE " + string.Join(" AND ", allConditions) : "";

                    string selectQuery = $@"
                        SELECT 
                            Id, Phone, TuQuy, Taxi2, Taxi3, Taxi4, Taxi5, 
                            TaxiDu2, TaxiDu3, DuoiTien, SanhGiua, TamHoa, 
                            SoiGuong, AXA_AYA, AXA_BXB, AXA_BYB, ABABAC, ABACAC,
                            XXXYYY
                        FROM Records 
                        {whereClause}
                        ORDER BY Id DESC";

                    using (var command = new SQLiteCommand(selectQuery, connection))
                    {
                        if (!string.IsNullOrWhiteSpace(filterOptions.PhoneFilter))
                        {
                            string searchPattern = "%" + filterOptions.PhoneFilter.Replace('?', '_') + "%";
                            command.Parameters.AddWithValue("@PhoneFilter", searchPattern);
                        }

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var record = new Record
                                {
                                    Id = reader.GetInt32(0),
                                    Phone = reader.GetString(1),
                                    TuQuy = reader.GetInt32(2) == 1,
                                    Taxi2 = reader.GetInt32(3) == 1,
                                    Taxi3 = reader.GetInt32(4) == 1,
                                    Taxi4 = reader.GetInt32(5) == 1,
                                    Taxi5 = reader.GetInt32(6) == 1,
                                    TaxiDu2 = reader.GetInt32(7) == 1,
                                    TaxiDu3 = reader.GetInt32(8) == 1,
                                    DuoiTien = reader.GetInt32(9) == 1,
                                    SanhGiua = reader.GetInt32(10) == 1,
                                    TamHoa = reader.GetInt32(11) == 1,
                                    SoiGuong = reader.GetInt32(12) == 1,
                                    AXA_AYA = reader.GetInt32(13) == 1,
                                    AXA_BXB = reader.GetInt32(14) == 1,
                                    AXA_BYB = reader.GetInt32(15) == 1,
                                    ABABAC = reader.GetInt32(16) == 1,
                                    ABACAC = reader.GetInt32(17) == 1,
                                    XXXYYY = reader.GetInt32(18) == 1
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

        public int GetRecordCount(RecordFilterOptions filterOptions)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    var typeConditions = new List<string>();

                    AddStoredTypeConditions(filterOptions, typeConditions);
                    AddAdvancedTypeConditions(filterOptions, typeConditions);

                    var allConditions = new List<string>();

                    if (!string.IsNullOrWhiteSpace(filterOptions.PhoneFilter))
                    {
                        allConditions.Add("Phone LIKE @PhoneFilter");
                    }
                    AddExclusionCondition(filterOptions, allConditions);

                    if (typeConditions.Count > 0)
                    {
                        allConditions.Add("(" + string.Join(" OR ", typeConditions) + ")");
                    }

                    string whereClause = allConditions.Count > 0 ? "WHERE " + string.Join(" AND ", allConditions) : "";

                    string countQuery = $"SELECT COUNT(*) FROM Records {whereClause}";

                    using (var command = new SQLiteCommand(countQuery, connection))
                    {
                        if (!string.IsNullOrWhiteSpace(filterOptions.PhoneFilter))
                        {
                            string searchPattern = "%" + filterOptions.PhoneFilter.Replace('?', '_') + "%";
                            command.Parameters.AddWithValue("@PhoneFilter", searchPattern);
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

        private static void AddStoredTypeConditions(RecordFilterOptions filterOptions, List<string> conditions)
        {
            if (filterOptions.FilterTuQuy) conditions.Add("TuQuy = 1");
            if (filterOptions.FilterTaxi2) conditions.Add("Taxi2 = 1");
            if (filterOptions.FilterTaxi3) conditions.Add("Taxi3 = 1");
            if (filterOptions.FilterTaxi4) conditions.Add("Taxi4 = 1");
            if (filterOptions.FilterTaxi5) conditions.Add("Taxi5 = 1");
            if (filterOptions.FilterTaxiDu2) conditions.Add("TaxiDu2 = 1");
            if (filterOptions.FilterTaxiDu3) conditions.Add("TaxiDu3 = 1");
            if (filterOptions.FilterDuoiTien) conditions.Add("DuoiTien = 1");
            if (filterOptions.FilterSanhGiua) conditions.Add("SanhGiua = 1");
            if (filterOptions.FilterTamHoa) conditions.Add("TamHoa = 1");
            if (filterOptions.FilterSoiGuong) conditions.Add("SoiGuong = 1");
            if (filterOptions.FilterAXA_AYA) conditions.Add("AXA_AYA = 1");
            if (filterOptions.FilterAXA_BXB) conditions.Add("AXA_BXB = 1");
            if (filterOptions.FilterAXA_BYB) conditions.Add("AXA_BYB = 1");
            if (filterOptions.FilterABABAC) conditions.Add("ABABAC = 1");
            if (filterOptions.FilterABACAC) conditions.Add("ABACAC = 1");
            if (filterOptions.FilterXXXYYY) conditions.Add("XXXYYY = 1");
        }

        private static void AddAdvancedTypeConditions(RecordFilterOptions filterOptions, List<string> conditions)
        {
            // Các mẫu đều được xét trên 6 số cuối. substr(..., -6, n) dùng chỉ số
            // tương đối từ cuối chuỗi nên hoạt động với cả dữ liệu 9 và 10 chữ số.
            if (filterOptions.FilterXXAYYA)
            {
                conditions.Add("(length(Phone) >= 6 AND substr(Phone,-6,1)=substr(Phone,-5,1) AND substr(Phone,-3,1)=substr(Phone,-2,1) AND substr(Phone,-4,1)=substr(Phone,-1,1))");
            }

            if (filterOptions.FilterXAXBXC)
            {
                conditions.Add("(length(Phone) >= 6 AND substr(Phone,-6,1)=substr(Phone,-4,1) AND substr(Phone,-4,1)=substr(Phone,-2,1))");
            }

            if (filterOptions.FilterAXBXCX)
            {
                conditions.Add("(length(Phone) >= 6 AND substr(Phone,-5,1)=substr(Phone,-3,1) AND substr(Phone,-3,1)=substr(Phone,-1,1))");
            }

            if (filterOptions.FilterDuoiDacBiet)
            {
                conditions.Add("substr(Phone,-2) IN ('39','43','79','68','52','99','86','89')");
            }

            if (filterOptions.FilterTaxiDu3Tang1)
            {
                conditions.Add(@"(length(Phone) >= 6 AND (
                    (substr(Phone,-5,1)=substr(Phone,-2,1) AND substr(Phone,-4,1)=substr(Phone,-1,1) AND CAST(substr(Phone,-3,1) AS INTEGER)=CAST(substr(Phone,-6,1) AS INTEGER)+1)
                    OR (substr(Phone,-6,1)=substr(Phone,-3,1) AND substr(Phone,-4,1)=substr(Phone,-1,1) AND CAST(substr(Phone,-2,1) AS INTEGER)=CAST(substr(Phone,-5,1) AS INTEGER)+1)
                    OR (substr(Phone,-6,1)=substr(Phone,-3,1) AND substr(Phone,-5,1)=substr(Phone,-2,1) AND CAST(substr(Phone,-1,1) AS INTEGER)=CAST(substr(Phone,-4,1) AS INTEGER)+1)
                ))");
            }
        }

        private static void AddExclusionCondition(RecordFilterOptions filterOptions, List<string> conditions)
        {
            if (filterOptions.ExcludeBadSequences)
            {
                // Theo yêu cầu, loại khi 4, 49, 53 hoặc 74 xuất hiện ở bất kỳ vị trí nào.
                conditions.Add("(instr(Phone,'4')=0 AND instr(Phone,'49')=0 AND instr(Phone,'53')=0 AND instr(Phone,'74')=0)");
            }
        }

        public void TruncateRecordsTable()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string sql = @"DELETE FROM Records;
                               DELETE FROM sqlite_sequence WHERE name = 'Records';";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (var vacuumCmd = new SQLiteCommand("VACUUM;", connection))
                {
                    vacuumCmd.ExecuteNonQuery();
                }
            }
        }
    }
}
