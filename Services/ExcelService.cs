using System.IO;
using System.Linq;
using OfficeOpenXml;
using Mobiphone.Models;

namespace Mobiphone.Services
{
    public class ExcelService
    {
        public ExcelService()
        {
            // Set EPPlus license context for non-commercial use
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public List<Record> ReadExcelFile(string filePath)
        {
            var records = new List<Record>();

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                    if (worksheet == null)
                    {
                        throw new Exception("No worksheet found in the Excel file.");
                    }

                    // Get the number of rows and columns
                    int rowCount = worksheet.Dimension?.Rows ?? 0;
                    int colCount = worksheet.Dimension?.Columns ?? 0;

                    if (rowCount <= 1)
                    {
                        return records; // No data rows (only header or empty)
                    }

                    // Read data starting from row 2 (assuming row 1 is header)
                    // Expected column: Phone (column 1)
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var phone = worksheet.Cells[row, 1].Value?.ToString()?.Trim() ?? string.Empty;

                        // Remove all non-numeric characters
                        phone = new string(phone.Where(char.IsDigit).ToArray());

                        // Handle phone numbers with 10 digits (remove leading 0)
                        if (phone.Length == 10 && phone.StartsWith("0"))
                        {
                            phone = phone.Substring(1); // Remove leading 0
                        }

                        // Only add valid phone numbers (exactly 9 digits after processing)
                        if (phone.Length == 9)
                        {
                            var record = new Record { Phone = phone };
                            ClassifyPhoneType(record);
                            records.Add(record);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading Excel file: {ex.Message}", ex);
            }

            return records;
        }

        private void ClassifyPhoneType(Record record)
        {
            string phone = record.Phone;

            // Check MINI thần tài: số thứ 8 (index 7) = "3" AND số thứ 9 (index 8) = "9"
            record.MiniThanTai = phone[7] == '3' && phone[8] == '9';

            // Check BIG Thần tài: số thứ 8 = "7" AND số thứ 9 = "9"
            record.BigThanTai = phone[7] == '7' && phone[8] == '9';

            // Check Lộc phát: số thứ 8 = "6" AND số thứ 9 = "8"
            record.LocPhat = phone[7] == '6' && phone[8] == '8';

            // Check Tứ quý: 4 số cuối giống nhau (I=J=K=L)
            record.TuQuy = phone[5] == phone[6] && phone[6] == phone[7] && phone[7] == phone[8];

            // Check Tứ quý giữa: có 4 số liên tiếp giống nhau ở giữa
            record.TuQuyGiua = (phone[0] == phone[1] && phone[1] == phone[2] && phone[2] == phone[3]) ||
                                (phone[1] == phone[2] && phone[2] == phone[3] && phone[3] == phone[4]) ||
                                (phone[2] == phone[3] && phone[3] == phone[4] && phone[4] == phone[5]) ||
                                (phone[3] == phone[4] && phone[4] == phone[5] && phone[5] == phone[6]) ||
                                (phone[4] == phone[5] && phone[5] == phone[6] && phone[6] == phone[7]);

            // Check Tam hoa kép: G=H=I AND J=K=L
            record.TamHoaKep = (phone[3] == phone[4] && phone[4] == phone[5]) &&
                                (phone[6] == phone[7] && phone[7] == phone[8]);
        }
    }
}
