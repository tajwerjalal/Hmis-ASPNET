using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;

namespace HmisWeb.Services
{
    public interface ILookupSeedService
    {
        Task EnsureSeededAsync();
    }

    public class LookupSeedService : ILookupSeedService
    {
        private readonly ApplicationDbContext _db;

        public LookupSeedService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task EnsureSeededAsync()
        {
            await SeedItemsAsync("BG-", new[]
            {
                "O+", "O-", "A+", "A-", "B+", "B-", "AB+", "AB-"
            });

            await SeedItemsAsync("CS-", new[]
            {
                "Single", "Married", "Widowed", "Divorced", "Separated", "Unknown"
            });

            await SeedItemsAsync("RA-", new[]
            {
                "Sinhalese", "Sri Lankan Tamil", "Indian Tamil", "Sri Lankan Moor",
                "Burgher", "Malay", "Other"
            });

            await SeedItemsAsync("RL-", new[]
            {
                "Buddhist", "Hindu", "Islam", "Christian", "Other"
            });

            await SeedItemsAsync("OC-", new[]
            {
                "None", "Student", "Farmer", "Fisherman", "Government Employee",
                "Private Sector Employee", "Business Owner", "Labourer", "Retired", "Other"
            });

            await SeedRelationsAsync();

            await SeedAdmissionTypesAsync();

            await SeedBillableCatalogAsync();
        }

        private async Task SeedItemsAsync(string codePrefix, string[] names)
        {
            var existing = await _db.Items
                .Where(i => !i.Retired && i.Code != null && i.Code.StartsWith(codePrefix))
                .Select(i => i.Code)
                .ToListAsync();

            foreach (var name in names)
            {
                var code = codePrefix + name;
                if (existing.Contains(code)) continue;

                _db.Items.Add(new Item
                {
                    Name = name,
                    PrintName = name,
                    ShortName = name,
                    Code = code,
                    Billable = false,
                    CreatedAt = DateTime.Now
                });
            }

            await _db.SaveChangesAsync();
        }

        private async Task SeedRelationsAsync()
        {
            var names = new[]
            {
                "Father", "Mother", "Husband", "Wife", "Son", "Daughter",
                "Brother", "Sister", "Grandfather", "Grandmother", "Uncle", "Aunt",
                "Cousin", "Son-in-Law", "Daughter-in-Law", "Guardian", "Other"
            };

            _db.ChangeTracker.DetectChanges();
            var existingNames = await _db.Categories
                .OfType<Relation>()
                .Where(c => !c.Retired)
                .Select(c => c.Name)
                .ToListAsync();

            foreach (var name in names)
            {
                if (existingNames.Contains(name)) continue;

                _db.Categories.Add(new Relation
                {
                    Name = name,
                    CreatedAt = DateTime.Now
                });
            }

            await _db.SaveChangesAsync();
        }

        private async Task SeedAdmissionTypesAsync()
        {
            var existing = await _db.Items
                .Where(i => !i.Retired && i.Code != null && i.Code.StartsWith("AT-"))
                .Select(i => i.Code)
                .ToListAsync();

            var types = new[]
            {
                ("AT-Admission", "Admission", "Inpatient stay"),
                ("AT-DayCase", "Day Case", "Day care admission"),
                ("AT-Emergency", "Emergency", "Emergency admission"),
                ("AT-Elective", "Elective", "Planned / elective admission")
            };

            foreach (var (code, name, desc) in types)
            {
                if (existing.Contains(code)) continue;

                _db.Items.Add(new Item
                {
                    Name = name,
                    PrintName = name,
                    ShortName = name,
                    Code = code,
                    Descreption = desc,
                    Billable = false,
                    CreatedAt = DateTime.Now
                });
            }

            await _db.SaveChangesAsync();
        }

        private async Task SeedBillableCatalogAsync()
        {
            var existing = await _db.Items
                .Where(i => !i.Retired && i.Code != null
                    && (i.Code.StartsWith("SE-") || i.Code.StartsWith("INV-")))
                .Select(i => i.Code)
                .ToListAsync();

            var catalog = new[]
            {
                ("SE-CONSULT", "Consultation", "Outpatient doctor consultation", ItemType.Service, 1500.0),
                ("SE-FOLLOWUP", "Follow-up Consultation", "OPD follow-up consultation", ItemType.Service, 1000.0),
                ("SE-EMERGENCY", "Emergency Visit", "Emergency department visit", ItemType.Service, 2500.0),
                ("SE-DRESSING", "Dressing", "Wound dressing (small)", ItemType.Service, 500.0),
                ("SE-INJECTION", "Injection", "Intramuscular / subcutaneous injection", ItemType.Service, 350.0),
                ("SE-MED-REG", "Medical Registration", "One-time medical registration fee", ItemType.Service, 500.0),
                ("SE-IPD-ADMISSION", "IPD Admission Package", "Initial inpatient admission and assessment", ItemType.Service, 2500.0),
                ("SE-IPD-ROOM", "IPD Room & Nursing Charge", "Daily room and nursing care", ItemType.Service, 4500.0),
                ("SE-IPD-DOCTOR", "IPD Doctor Visit", "Daily consultant visit", ItemType.Service, 2000.0),
                ("INV-FBC", "Full Blood Count (FBC)", "Full blood count", ItemType.Investigation, 800.0),
                ("INV-CRP", "C-Reactive Protein (CRP)", "CRP", ItemType.Investigation, 1200.0),
                ("INV-RBS", "Random Blood Sugar (RBS)", "Random blood sugar", ItemType.Investigation, 300.0),
                ("INV-UR", "Urine Full Report (UFR)", "Urine full report", ItemType.Investigation, 500.0),
                ("INV-LFT", "Liver Function Test (LFT)", "Liver function", ItemType.Investigation, 2500.0),
                ("INV-RFT", "Renal Function Test (RFT)", "Renal function", ItemType.Investigation, 2500.0),
                ("INV-TSH", "Thyroid Stimulating Hormone (TSH)", "TSH", ItemType.Investigation, 1400.0),
                ("INV-LIPID", "Lipid Profile", "Lipid profile", ItemType.Investigation, 2200.0),
                ("INV-ECG", "ECG", "Electrocardiogram", ItemType.Investigation, 1000.0),
                ("INV-CXR", "Chest X-Ray", "Chest X-ray (single view)", ItemType.Investigation, 1500.0),
                ("INV-USG", "Ultrasound Scan", "Ultrasound scan", ItemType.Investigation, 3500.0)
            };

            foreach (var (code, name, desc, type, fee) in catalog)
            {
                if (existing.Contains(code)) continue;

                _db.Items.Add(new Item
                {
                    Name = name,
                    PrintName = name,
                    ShortName = name,
                    Code = code,
                    Descreption = desc,
                    ItemType = type,
                    Billable = true,
                    DiscountAllowed = true,
                    Total = fee,
                    TotalForForeigner = fee * 1.5,
                    ChargesVisibleForInward = true,
                    CreatedAt = DateTime.Now
                });
            }

            await _db.SaveChangesAsync();
        }
    }
}