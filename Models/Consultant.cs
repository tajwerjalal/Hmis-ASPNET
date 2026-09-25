namespace HmisWeb.Models
{
    public class Consultant : Doctor
    {
        public string Degrees { get; set; }
        public double FirstVisitFee { get; set; }
        public double SubsequentVisitFee { get; set; }

        public string WorkDays { get; set; }
        public string SessionStartTime { get; set; }
        public string SessionEndTime { get; set; }
        public string DefaultRoomNo { get; set; }
        public double DefaultHospitalFee { get; set; }
    }
}
