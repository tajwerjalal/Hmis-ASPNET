using System.Text.Json;

namespace HmisWeb.Pages.Opd
{
    public class OpdCartItem
    {
        public long ItemId { get; set; }
        public string Code { get; set; }
        public string ItemName { get; set; }
        public double Qty { get; set; }
        public double Rate { get; set; }
        public double Discount { get; set; }

        public double GrossValue => Math.Round(Qty * Rate, 2);
        public double NetValue => Math.Round(Qty * Rate - Discount, 2);
    }

    public static class OpdCartHelper
    {
        public const string PreBillCartKey = "OpdPreBillCart";
        public const string PreBillPatientKey = "OpdPreBillPatientId";
        public const string DirectBillCartKey = "OpdDirectBillCart";
        public const string DirectBillPatientKey = "OpdDirectBillPatientId";

        public static List<OpdCartItem> GetCart(ISession session, string key)
        {
            var bytes = session?.Get(key);
            if (bytes == null) return new List<OpdCartItem>();
            var list = JsonSerializer.Deserialize<List<OpdCartItem>>(System.Text.Encoding.UTF8.GetString(bytes));
            return list ?? new List<OpdCartItem>();
        }

        public static void SaveCart(ISession session, string key, List<OpdCartItem> cart)
        {
            session?.Set(key, System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cart ?? new List<OpdCartItem>())));
        }

        public static long? GetPatientId(ISession session, string key)
        {
            var bytes = session?.Get(key);
            if (bytes == null) return null;
            if (long.TryParse(System.Text.Encoding.UTF8.GetString(bytes), out var id)) return id;
            return null;
        }

        public static void SavePatientId(ISession session, string key, long? id)
        {
            if (id == null) session?.Remove(key);
            else session?.Set(key, System.Text.Encoding.UTF8.GetBytes(id.Value.ToString()));
        }

        public static void Clear(ISession session, string cartKey, string patientKey)
        {
            SaveCart(session, cartKey, new List<OpdCartItem>());
            SavePatientId(session, patientKey, null);
        }
    }
}