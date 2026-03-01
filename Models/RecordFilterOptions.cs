namespace ExcelToSQLite.Models
{
    public class RecordFilterOptions
    {
        public string PhoneFilter { get; set; } = string.Empty;
        public bool FilterMiniThanTai { get; set; } = false;
        public bool FilterBigThanTai { get; set; } = false;
        public bool FilterLocPhat { get; set; } = false;
        public bool FilterTuQuy { get; set; } = false;
        public bool FilterTuQuyGiua { get; set; } = false;
        public bool FilterTamHoaKep { get; set; } = false;

        public RecordFilterOptions()
        {
        }

        public RecordFilterOptions(string phoneFilter = "",
            bool filterMiniThanTai = false,
            bool filterBigThanTai = false,
            bool filterLocPhat = false,
            bool filterTuQuy = false,
            bool filterTuQuyGiua = false,
            bool filterTamHoaKep = false)
        {
            PhoneFilter = phoneFilter;
            FilterMiniThanTai = filterMiniThanTai;
            FilterBigThanTai = filterBigThanTai;
            FilterLocPhat = filterLocPhat;
            FilterTuQuy = filterTuQuy;
            FilterTuQuyGiua = filterTuQuyGiua;
            FilterTamHoaKep = filterTamHoaKep;
        }
    }
}
