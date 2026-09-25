namespace HmisWeb.Data.Enums
{
    public class Denomination
    {
        public double Amount { get; set; }
        public int Count { get; set; }
        public double Total => Amount * Count;
    }
}
