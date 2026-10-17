using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;

namespace HmisWeb.Pages.Lab
{
    /// <summary>
    /// Shared query/formatting helpers for the laboratory collection reports
    /// (periodic / company / patient / group / test wise), migrated from the VFP Labsys
    /// reports <c>lb02401</c> - <c>lb02405</c>.
    /// </summary>
    public static class LabCollectionQuery
    {
        public class Row
        {
            public string Group { get; set; } = "";
            public int Count { get; set; }
            public double Gross { get; set; }
            public double Discount { get; set; }
            public double Net { get; set; }
        }

        public static IQueryable<BillItem> LabBillItems(ApplicationDbContext db, DateTime from, DateTime to)
        {
            var toExclusive = to.Date.AddDays(1);
            return db.BillItems
                .Include(b => b.Item)
                .Include(b => b.Bill).ThenInclude(x => x.Patient).ThenInclude(p => p.Person)
                .Include(b => b.Bill).ThenInclude(x => x.Institution)
                .Where(b => !b.Retired
                    && b.ItemId1 != null
                    && b.Item != null
                    && (b.Item.ItemType == ItemType.Investigation || b.Item.ItemType == ItemType.Service)
                    && b.Bill != null
                    && b.Bill.CreatedAt != null
                    && b.Bill.CreatedAt >= from.Date
                    && b.Bill.CreatedAt < toExclusive);
        }

        public static string Csv(IEnumerable<Row> rows, string groupHeader)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{groupHeader},Count,Gross,Discount,Net");
            foreach (var r in rows)
            {
                sb.AppendLine(string.Join(",",
                    Q(r.Group),
                    r.Count.ToString(CultureInfo.InvariantCulture),
                    r.Gross.ToString("0.00", CultureInfo.InvariantCulture),
                    r.Discount.ToString("0.00", CultureInfo.InvariantCulture),
                    r.Net.ToString("0.00", CultureInfo.InvariantCulture)));
            }
            return sb.ToString();
        }

        public static string Q(string v)
        {
            if (string.IsNullOrEmpty(v)) return "";
            return v.Contains(',') || v.Contains('"') ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
        }
    }
}