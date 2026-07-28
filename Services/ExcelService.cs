using System.IO;
using OfficeOpenXml;
using Mobiphone.Models;
using System.Text.RegularExpressions;
using OfficeOpenXml.Style;

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
            int len = phone.Length;

            // 1. TỨ QUÝ: Chứa dãy 4 số giống nhau liên tục (AAAA)
            record.TuQuy = Regex.IsMatch(phone, @"(\d)\1{3}");

            // 2. TAXI 2: Chứa 3 cặp số giống nhau ở đuôi (AB.AB.AB)
            if (len >= 6)
            {
                string sub6 = phone.Substring(len - 6);
                record.Taxi2 = sub6[0] == sub6[2] && sub6[2] == sub6[4] &&
                               sub6[1] == sub6[3] && sub6[3] == sub6[5];
            }

            // 3. TAXI 3: Chứa 2 bộ 3 số giống nhau ở đuôi (ABC.ABC)
            if (len >= 6)
            {
                record.Taxi3 = phone.Substring(len - 6, 3) == phone.Substring(len - 3, 3);
            }

            // 4. TAXI 4: Chứa 2 bộ 4 số giống nhau ở đuôi (ABCD.ABCD)
            if (len >= 8)
            {
                record.Taxi4 = phone.Substring(len - 8, 4) == phone.Substring(len - 4, 4);
            }

            // 5. TAXI 5: Chứa 2 bộ 5 số giống nhau ở đuôi (ABCDE.ABCDE)
            if (len >= 10)
            {
                record.Taxi5 = phone.Substring(len - 10, 5) == phone.Substring(len - 5, 5);
            }

            // 6. TAXI DÙ 2: Chứa 3 cặp số ở đuôi dạng tiến/tăng dần (AB.A(B+1).A(B+2) hoặc AB.(A+1)B.(A+2)B)
            if (len >= 6)
            {
                int d1 = phone[len - 6] - '0', d2 = phone[len - 5] - '0';
                int d3 = phone[len - 4] - '0', d4 = phone[len - 3] - '0';
                int d5 = phone[len - 2] - '0', d6 = phone[len - 1] - '0';
                bool type1 = (d1 == d3 && d3 == d5) && (d4 == d2 + 1) && (d6 == d4 + 1);
                bool type2 = (d2 == d4 && d4 == d6) && (d3 == d1 + 1) && (d5 == d3 + 1);
                record.TaxiDu2 = type1 || type2;
            }

            // 7. TAXI DÙ 3: Chứa 2 bộ 3 số ở đuôi chỉ khác nhau 1 vị trí (ABC.ABD / ABC.DBC / ABD.ADC)
            if (len >= 6)
            {
                string p1 = phone.Substring(len - 6, 3);
                string p2 = phone.Substring(len - 3, 3);

                int diffCount = 0;
                for (int i = 0; i < 3; i++)
                {
                    if (p1[i] != p2[i]) diffCount++;
                }
                record.TaxiDu3 = (diffCount == 1);
            }

            // 8. ĐUÔI TIẾN: Chứa 3 số đuôi tăng dần liên tiếp A(A+1)(A+2)
            if (len >= 3)
            {
                int a = phone[len - 3] - '0';
                int b = phone[len - 2] - '0';
                int c = phone[len - 1] - '0';
                record.DuoiTien = (b == a + 1) && (c == b + 1);
            }

            // 9. SẢNH GIỮA: Có dãy 4 số tiến ở giữa, để lại 2 số đuôi A(A+1)(A+2)(A+3).??
            record.SanhGiua = false;
            if (len >= 6)
            {
                for (int i = 0; i <= len - 6; i++)
                {
                    int n1 = phone[i] - '0';
                    int n2 = phone[i + 1] - '0';
                    int n3 = phone[i + 2] - '0';
                    int n4 = phone[i + 3] - '0';

                    if (n2 == n1 + 1 && n3 == n2 + 1 && n4 == n3 + 1)
                    {
                        record.SanhGiua = true;
                        break;
                    }
                }
            }

            // 10. TAM HOA: Có chứa 3 số giống nhau nằm ở CUỐI (AAA)
            if (len >= 3)
            {
                record.TamHoa = (phone[len - 1] == phone[len - 2]) && (phone[len - 2] == phone[len - 3]);
            }

            // 11. SOI GƯƠNG: Có chứa 2 bộ 3 số (6 số đuôi), bộ 1 và bộ 2 cùng các số nhưng ngược thứ tự (Ví dụ: ABC.CBA)
            if (len >= 6)
            {
                string p1 = phone.Substring(len - 6, 3);
                string p2 = phone.Substring(len - 3, 3);
                char[] arr = p2.ToCharArray();
                Array.Reverse(arr);
                string p2Reversed = new string(arr);

                record.SoiGuong = (p1 == p2Reversed);
            }

            // 12. AXA.AYA: Cấu trúc 6 số đuôi AXA.AYA (chỉ số đuôi cuối cùng khác)
            if (len >= 6)
            {
                string sub6 = phone.Substring(len - 6);
                record.AXA_AYA = (sub6[0] == sub6[2] && sub6[2] == sub6[3] && sub6[3] == sub6[5]) &&
                                 (sub6[1] != sub6[4]);
            }

            // 13. AXA.BXB: Cấu trúc 6 số đuôi AXA.BXB (chữ số ở giữa giống nhau)
            if (len >= 6)
            {
                string sub6 = phone.Substring(len - 6);
                record.AXA_BXB = (sub6[1] == sub6[4]) && (sub6[0] == sub6[2]) && (sub6[3] == sub6[5]) &&
                                 (sub6[0] != sub6[3]);
            }

            // 14. AXA.BYB: Cấu trúc 6 số đuôi AXA.BYB
            if (len >= 6)
            {
                string sub6 = phone.Substring(len - 6);
                record.AXA_BYB = (sub6[0] == sub6[2]) && (sub6[3] == sub6[5]) &&
                                 (sub6[0] != sub6[3]) && (sub6[1] != sub6[4]);
            }

            // 15. ABABAC: Dạng 6 số đuôi AB.AB.AC
            if (len >= 6)
            {
                string sub6 = phone.Substring(len - 6);
                record.ABABAC = (sub6[0] == sub6[2] && sub6[2] == sub6[4]) && // Chữ số A
                                (sub6[1] == sub6[3]) &&                      // Chữ số B
                                (sub6[1] != sub6[5]);                        // B khác C
            }

            // 16. ABACAC: Dạng 6 số đuôi AB.AC.AC
            if (len >= 6)
            {
                string sub6 = phone.Substring(len - 6);
                record.ABACAC = (sub6[0] == sub6[2] && sub6[2] == sub6[4]) && // Chữ số A
                                (sub6[3] == sub6[5]) &&                      // Chữ số C
                                (sub6[1] != sub6[3]);                        // B khác C
            }
        }

        public void ExportToExcel(List<Record> records, string filePath)
        {
            if (records == null || records.Count == 0)
            {
                throw new ArgumentException("Danh sách dữ liệu xuất Excel không được để rỗng.", nameof(records));
            }

            var fileInfo = new FileInfo(filePath);

            using (var package = new ExcelPackage(fileInfo))
            {
                var worksheet = package.Workbook.Worksheets.Add("Danh Sách Sim");
                worksheet.Cells.Style.Font.Name = "Segoe UI";
                worksheet.Cells.Style.Font.Size = 11;

                string[] headers = new string[]
                {
                "STT", "Số Điện Thoại", "Tứ Quý", "Taxi 2", "Taxi 3", "Taxi 4", "Taxi 5",
                "Taxi Dư 2", "Taxi Dư 3", "Đuôi Tiến", "Sảnh Giữa", "Tam Hoa",
                "Soi Gương", "AXA_AYA", "AXA_BXB", "AXA_BYB", "ABABAC", "ABACAC"
                };

                for (int col = 0; col < headers.Length; col++)
                {
                    var cell = worksheet.Cells[1, col + 1];
                    cell.Value = headers[col];

                    // Style Header: Nền xanh dương, chữ trắng đậm, căn giữa
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.Color.SetColor(Color.White);
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(33, 150, 243)); // #2196F3
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                }

                // Đặt chiều cao cho dòng Header
                worksheet.Row(1).Height = 28;

                // 2. Điền dữ liệu (Data Rows)
                int row = 2;
                foreach (var record in records)
                {
                    worksheet.Cells[row, 1].Value = row - 1; // STT

                    // Lưu ý: Đặt dạng Text để số điện thoại không bị mất số 0 ở đầu
                    var phoneCell = worksheet.Cells[row, 2];
                    phoneCell.Style.Numberformat.Format = "@";
                    phoneCell.Value = record.Phone;

                    // Các cột Boolean (Hiển thị "x" nếu True, để trống nếu False)
                    worksheet.Cells[row, 3].Value = record.TuQuy ? "x" : "";
                    worksheet.Cells[row, 4].Value = record.Taxi2 ? "x" : "";
                    worksheet.Cells[row, 5].Value = record.Taxi3 ? "x" : "";
                    worksheet.Cells[row, 6].Value = record.Taxi4 ? "x" : "";
                    worksheet.Cells[row, 7].Value = record.Taxi5 ? "x" : "";
                    worksheet.Cells[row, 8].Value = record.TaxiDu2 ? "x" : "";
                    worksheet.Cells[row, 9].Value = record.TaxiDu3 ? "x" : "";
                    worksheet.Cells[row, 10].Value = record.DuoiTien ? "x" : "";
                    worksheet.Cells[row, 11].Value = record.SanhGiua ? "x" : "";
                    worksheet.Cells[row, 12].Value = record.TamHoa ? "x" : "";
                    worksheet.Cells[row, 13].Value = record.SoiGuong ? "x" : "";
                    worksheet.Cells[row, 14].Value = record.AXA_AYA ? "x" : "";
                    worksheet.Cells[row, 15].Value = record.AXA_BXB ? "x" : "";
                    worksheet.Cells[row, 16].Value = record.AXA_BYB ? "x" : "";
                    worksheet.Cells[row, 17].Value = record.ABABAC ? "x" : "";
                    worksheet.Cells[row, 18].Value = record.ABACAC ? "x" : "";

                    // Định dạng căn giữa cho tất cả các cột dữ liệu trừ cột SĐT (căn trái)
                    for (int col = 1; col <= headers.Length; col++)
                    {
                        if (col != 2)
                        {
                            worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        }
                        worksheet.Cells[row, col].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    worksheet.Row(row).Height = 20;
                    row++;
                }

                // 4. Tự động căn chỉnh độ rộng cột theo nội dung (AutoFit)
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Đảm bảo các cột boolean có độ rộng tối thiểu để đẹp mắt
                for (int col = 3; col <= headers.Length; col++)
                {
                    if (worksheet.Column(col).Width < 12)
                    {
                        worksheet.Column(col).Width = 12;
                    }
                }

                // 5. Lưu file
                package.Save();
            }
        }
    }
}
