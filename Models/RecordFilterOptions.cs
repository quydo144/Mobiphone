namespace Mobiphone.Models
{
    public class RecordFilterOptions
    {
        public string PhoneFilter { get; set; } = string.Empty;

        // 16 loại lọc mới theo ảnh quy tắc
        public bool FilterTuQuy { get; set; } = false;
        public bool FilterTaxi2 { get; set; } = false;
        public bool FilterTaxi3 { get; set; } = false;
        public bool FilterTaxi4 { get; set; } = false;
        public bool FilterTaxi5 { get; set; } = false;
        public bool FilterTaxiDu2 { get; set; } = false;
        public bool FilterTaxiDu3 { get; set; } = false;
        public bool FilterDuoiTien { get; set; } = false;
        public bool FilterSanhGiua { get; set; } = false;
        public bool FilterTamHoa { get; set; } = false;
        public bool FilterSoiGuong { get; set; } = false;
        public bool FilterAXA_AYA { get; set; } = false;
        public bool FilterAXA_BXB { get; set; } = false;
        public bool FilterAXA_BYB { get; set; } = false;
        public bool FilterABABAC { get; set; } = false;
        public bool FilterABACAC { get; set; } = false;
        public bool FilterXXXYYY { get; set; } = false;

        public RecordFilterOptions()
        {

        }

        public RecordFilterOptions(
            string phoneFilter = "",
            bool filterTuQuy = false,
            bool filterTaxi2 = false,
            bool filterTaxi3 = false,
            bool filterTaxi4 = false,
            bool filterTaxi5 = false,
            bool filterTaxiDu2 = false,
            bool filterTaxiDu3 = false,
            bool filterDuoiTien = false,
            bool filterSanhGiua = false,
            bool filterTamHoa = false,
            bool filterSoiGuong = false,
            bool filterAXA_AYA = false,
            bool filterAXA_BXB = false,
            bool filterAXA_BYB = false,
            bool filterABABAC = false,
            bool filterABACAC = false,
            bool filterXXXYYY = false)
        {
            PhoneFilter = phoneFilter;
            FilterTuQuy = filterTuQuy;
            FilterTaxi2 = filterTaxi2;
            FilterTaxi3 = filterTaxi3;
            FilterTaxi4 = filterTaxi4;
            FilterTaxi5 = filterTaxi5;
            FilterTaxiDu2 = filterTaxiDu2;
            FilterTaxiDu3 = filterTaxiDu3;
            FilterDuoiTien = filterDuoiTien;
            FilterSanhGiua = filterSanhGiua;
            FilterTamHoa = filterTamHoa;
            FilterSoiGuong = filterSoiGuong;
            FilterAXA_AYA = filterAXA_AYA;
            FilterAXA_BXB = filterAXA_BXB;
            FilterAXA_BYB = filterAXA_BYB;
            FilterABABAC = filterABABAC;
            FilterABACAC = filterABACAC;
            FilterXXXYYY = filterXXXYYY;
        }
    }
}