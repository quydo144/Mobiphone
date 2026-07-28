namespace Mobiphone.Models
{
    public class Record
    {
        public int Id { get; set; }
        public string Phone { get; set; } = string.Empty;
        public bool TuQuy { get; set; } = false;
        public bool Taxi2 { get; set; } = false;
        public bool Taxi3 { get; set; } = false;
        public bool Taxi4 { get; set; } = false;
        public bool Taxi5 { get; set; } = false;
        public bool TaxiDu2 { get; set; } = false;
        public bool TaxiDu3 { get; set; } = false;
        public bool DuoiTien { get; set; } = false;
        public bool SanhGiua { get; set; } = false;
        public bool TamHoa { get; set; } = false;
        public bool SoiGuong { get; set; } = false;
        public bool AXA_AYA { get; set; } = false;
        public bool AXA_BXB { get; set; } = false;
        public bool AXA_BYB { get; set; } = false;
        public bool ABABAC { get; set; } = false;
        public bool ABACAC { get; set; } = false;
    }
}
