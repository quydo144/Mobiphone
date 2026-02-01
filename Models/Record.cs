namespace ExcelToSQLite.Models
{
    public class Record
    {
        public int Id { get; set; }
        public string Phone { get; set; } = string.Empty;
        public bool MiniThanTai { get; set; } = false;
        public bool BigThanTai { get; set; } = false;
        public bool LocPhat { get; set; } = false;
        public bool TuQuy { get; set; } = false;
        public bool TuQuyGiua { get; set; } = false;
        public bool TamHoaKep { get; set; } = false;
    }
}
