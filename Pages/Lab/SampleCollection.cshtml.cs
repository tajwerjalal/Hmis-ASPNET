using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Lab
{
    public class SampleCollectionModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ICommonService _common;
        private readonly IAppSession _appSession;

        public SampleCollectionModel(ApplicationDbContext db, ICommonService common, IAppSession appSession)
        {
            _db = db;
            _common = common;
            _appSession = appSession;
        }

        private const string PatientKey = "LabSamplePatientId";

        [BindProperty]
        public string PatientSearch { get; set; }

        [BindProperty]
        public string NewName { get; set; }

        [BindProperty]
        public string NewNameWithInitials { get; set; }

        [BindProperty]
        public string NewNic { get; set; }

        [BindProperty]
        public string NewMobile { get; set; }

        [BindProperty]
        public string NewPhone { get; set; }

        [BindProperty]
        public Sex? NewSex { get; set; }

        [BindProperty]
        public long? SelectedPatientId { get; set; }

        [BindProperty]
        public List<SampleCollectionInput> SampleCollections { get; set; } = new();

        [BindProperty]
        public long? SelectedEncounterId { get; set; }

        [BindProperty]
        public string CollectionNotes { get; set; }

        public Patient SelectedPatient { get; set; }
        public List<Patient> PatientResults { get; set; } = new();
        public List<InvestigationOrder> PendingCollections { get; set; } = new();
        public List<PatientEncounter> ActiveEncounters { get; set; } = new();
        public PatientEncounter SelectedEncounter { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Success { get; set; }

        public async Task OnGetAsync()
        {
            await LoadStateAsync();
            if (TempData["SampleCollectionSuccess"] != null)
                Success = TempData["SampleCollectionSuccess"].ToString();
            if (TempData["SampleCollectionMessage"] != null)
                Message = TempData["SampleCollectionMessage"].ToString();
        }

        public async Task<IActionResult> OnPostSearchPatientsAsync()
        {
            await LoadStateAsync();
            PatientResults = await _common.SearchPatientsAsync(PatientSearch);
            return Page();
        }

        public async Task<IActionResult> OnPostSelectPatientAsync()
        {
            if (SelectedPatientId == null)
                return RedirectToPage();

            var patient = await _db.Patients
                .Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == SelectedPatientId.Value && !p.Retired);
            if (patient == null)
                return RedirectToPage();

            HttpContext.Session.SetString(PatientKey, patient.Id.ToString());
            return RedirectToPage("/Lab/SampleCollection");
        }

        public IActionResult OnPostStartNewAsync()
        {
            HttpContext.Session.Remove(PatientKey);
            return RedirectToPage("/Lab/SampleCollection");
        }

        public async Task<IActionResult> OnPostCreatePatientAsync()
        {
            if (string.IsNullOrWhiteSpace(NewName))
            {
                ErrorMessage = "Patient name is required.";
                return Page();
            }

            var person = new Person
            {
                Name = NewName.Trim(),
                NameWithInitials = string.IsNullOrWhiteSpace(NewNameWithInitials) ? NewName.Trim() : NewNameWithInitials.Trim(),
                Nic = NewNic?.Trim() ?? "",
                Mobile = NewMobile?.Trim() ?? "",
                Phone = NewPhone?.Trim() ?? "",
                Sex = NewSex,
                CreatedAt = DateTime.Now,
                CreaterId = _appSession.WebUserId
            };
            _db.Persons.Add(person);
            await _db.SaveChangesAsync();

            long? maxPatientId = await _db.Patients.MaxAsync(p => (long?)p.PatientId);
            var patient = new Patient
            {
                PersonId = person.Id,
                PatientId = (maxPatientId ?? 0) + 1,
                CreatedAt = DateTime.Now,
                CreaterId = _appSession.WebUserId,
                CreatedInstitutionId = _appSession.LoggedSite?.Id
            };
            _db.Patients.Add(patient);
            await _db.SaveChangesAsync();

            HttpContext.Session.SetString(PatientKey, patient.Id.ToString());
            return RedirectToPage("/Lab/SampleCollection");
        }

        public async Task<IActionResult> OnPostSaveCollectionsAsync()
        {
            await LoadStateAsync();

            var patientId = GetSelectedPatientId();
            if (patientId == null)
            {
                ErrorMessage = "Please select a patient first.";
                return Page();
            }

            if (SampleCollections == null || !SampleCollections.Any(x => x.Collect))
            {
                ErrorMessage = "No samples selected for collection.";
                return Page();
            }

            var now = DateTime.Now;
            var userId = _appSession.WebUserId;

            foreach (var input in SampleCollections.Where(x => x.Collect && x.OrderId > 0))
            {
                var order = await _db.InvestigationOrders
                    .FirstOrDefaultAsync(o => o.Id == input.OrderId && !o.Retired);
                if (order == null) continue;

                order.SampleCollected = true;
                order.SampleCollectedAt = now;
                order.SampleCollectedById = userId;
                order.SampleType = input.SampleType;
                order.SampleContainer = input.SampleContainer;
                order.SampleVolume = input.SampleVolume;
                order.SampleCondition = input.SampleCondition;
                order.Barcode = input.Barcode ?? GenerateBarcode(order.Id);
                order.BarcodeLabel = input.BarcodeLabel;
                order.CollectorId = userId;
                order.CollectionDateTime = input.CollectionDateTime ?? now;
                order.CollectionNotes = input.CollectionNotes ?? CollectionNotes ?? "";
            }

            await _db.SaveChangesAsync();

            TempData["SampleCollectionSuccess"] = "Sample collection recorded successfully.";
            return RedirectToPage("/Lab/SampleCollection");
        }

        public async Task<IActionResult> OnPostGenerateBarcodesAsync()
        {
            await LoadStateAsync();

            var patientId = GetSelectedPatientId();
            if (patientId == null)
            {
                ErrorMessage = "Please select a patient first.";
                return Page();
            }

            var orders = await _db.InvestigationOrders
                .Where(o => o.PatientId == patientId && !o.Retired && !o.SampleCollected && o.Ordered)
                .ToListAsync();

            foreach (var order in orders)
            {
                if (string.IsNullOrEmpty(order.Barcode))
                {
                    order.Barcode = GenerateBarcode(order.Id);
                }
            }

            await _db.SaveChangesAsync();

            TempData["SampleCollectionSuccess"] = $"Generated barcodes for {orders.Count} samples.";
            return RedirectToPage("/Lab/SampleCollection");
        }

        public async Task<IActionResult> OnGetPrintLabelsAsync(long? patientId)
        {
            var pid = patientId ?? GetSelectedPatientId();
            if (pid == null) return NotFound();

            var orders = await _db.InvestigationOrders
                .Include(o => o.Item)
                .Include(o => o.Patient).ThenInclude(p => p.Person)
                .Where(o => o.PatientId == pid && !o.Retired && o.SampleCollected && !string.IsNullOrEmpty(o.Barcode))
                .ToListAsync();

            if (!orders.Any()) return NotFound();

            var pdfBytes = BuildBarcodeLabels(orders);
            return File(pdfBytes, "application/pdf", $"SampleLabels-{pid}-{DateTime.Now:yyyyMMdd}.pdf");
        }

        private async Task LoadStateAsync()
        {
            SelectedPatient = await GetSelectedPatientAsync();
            if (SelectedPatient != null)
            {
                ActiveEncounters = await GetActiveEncountersAsync(SelectedPatient.Id);
                if (SelectedEncounterId.HasValue && ActiveEncounters.Any(e => e.Id == SelectedEncounterId.Value))
                    SelectedEncounter = ActiveEncounters.First(e => e.Id == SelectedEncounterId.Value);
                else if (ActiveEncounters.Any())
                    SelectedEncounter = ActiveEncounters.First();
                else
                    SelectedEncounter = null;

                await LoadPendingCollectionsAsync();
            }
            else
            {
                ActiveEncounters = new();
                SelectedEncounter = null;
                PendingCollections = new();
            }
        }

        private async Task LoadPendingCollectionsAsync()
        {
            var patientId = GetSelectedPatientId();
            if (patientId == null)
            {
                PendingCollections = new();
                return;
            }

            var query = _db.InvestigationOrders
                .Include(o => o.Item)
                .Where(o => o.PatientId == patientId && !o.Retired && o.Ordered && !o.SampleCollected && !o.Completed);

            if (SelectedEncounterId.HasValue)
                query = query.Where(o => o.PatientEncounterId == SelectedEncounterId.Value);

            PendingCollections = await query.OrderBy(o => o.CreatedAt).ToListAsync();

            // Initialize SampleCollections with pending orders
            SampleCollections = PendingCollections.Select(o => new SampleCollectionInput
            {
                OrderId = o.Id,
                InvestigationName = o.InvestigationName,
                ItemName = o.Item?.Name ?? "",
                SampleType = GetDefaultSampleType(o.Item),
                SampleContainer = GetDefaultContainer(o.Item),
                CollectionDateTime = DateTime.Now,
                Collect = true
            }).ToList();
        }

        private string GetDefaultSampleType(Item item)
        {
            if (item == null) return "Blood";
            var name = (item.Name ?? "").ToLower();
            if (name.Contains("urine")) return "Urine";
            if (name.Contains("stool") || name.Contains("feces")) return "Stool";
            if (name.Contains("sputum")) return "Sputum";
            if (name.Contains("swab")) return "Swab";
            if (name.Contains("csf") || name.Contains("cerebrospinal")) return "CSF";
            if (name.Contains("fluid")) return "Body Fluid";
            if (name.Contains("tissue") || name.Contains("biopsy")) return "Tissue";
            return "Blood";
        }

        private string GetDefaultContainer(Item item)
        {
            if (item == null) return "Vacutainer";
            var name = (item.Name ?? "").ToLower();
            if (name.Contains("urine")) return "Urine Container";
            if (name.Contains("stool") || name.Contains("feces")) return "Stool Container";
            if (name.Contains("sputum")) return "Sputum Container";
            if (name.Contains("swab")) return "Swab Tube";
            if (name.Contains("csf") || name.Contains("cerebrospinal")) return "CSF Tube";
            if (name.Contains("fluid")) return "Fluid Container";
            if (name.Contains("tissue") || name.Contains("biopsy")) return "Formalin Jar";
            return "Vacutainer (EDTA/Plain)";
        }

        private string GenerateBarcode(long orderId)
        {
            return $"SMP{DateTime.Now:yyyyMMdd}{orderId:D6}";
        }

        private long? GetSelectedPatientId()
        {
            var val = HttpContext.Session.GetString(PatientKey);
            return long.TryParse(val, out var id) ? id : null;
        }

        private async Task<Patient> GetSelectedPatientAsync()
        {
            var id = GetSelectedPatientId();
            if (id == null) return null;
            return await _db.Patients
                .Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == id.Value && !p.Retired);
        }

        private async Task<List<PatientEncounter>> GetActiveEncountersAsync(long patientId)
        {
            return await _db.PatientEncounters
                .Where(e => e.PatientId == patientId && !e.Retired
                    && e.EncounterType == EncounterType.Inward && e.DateOfDischarge == null)
                .OrderByDescending(e => e.DateOfAdmission)
                .ToListAsync();
        }

        private byte[] BuildBarcodeLabels(List<InvestigationOrder> orders)
        {
            using var ms = new System.IO.MemoryStream();
            using (var doc = new PdfSharpCore.Pdf.PdfDocument())
            {
                var page = doc.AddPage();
                page.Width = PdfSharpCore.Drawing.XUnit.FromMillimeter(100);
                page.Height = PdfSharpCore.Drawing.XUnit.FromMillimeter(150);

                using (var g = PdfSharpCore.Drawing.XGraphics.FromPdfPage(page))
                {
                    var font = new PdfSharpCore.Drawing.XFont("Arial", 8);
                    var fontBold = new PdfSharpCore.Drawing.XFont("Arial", 8, PdfSharpCore.Drawing.XFontStyle.Bold);
                    var fontSmall = new PdfSharpCore.Drawing.XFont("Arial", 6);
                    var fontBarcode = new PdfSharpCore.Drawing.XFont("Consolas", 10, PdfSharpCore.Drawing.XFontStyle.Bold);

                    double y = 10;
                    double x = 5;
                    double w = 90;
                    double h = 35;

                    foreach (var order in orders)
                    {
                        if (y > 130)
                        {
                            doc.AddPage();
                            y = 10;
                        }

                        // Border
                        g.DrawRectangle(PdfSharpCore.Drawing.XPens.Black, x, y, w, h);

                        // Patient name
                        g.DrawString($"Patient: {order.Patient?.Person?.NameWithInitials ?? "N/A"}", fontBold, PdfSharpCore.Drawing.XBrushes.Black, x + 2, y + 2);
                        // PHN
                        g.DrawString($"PHN: {order.Patient?.PatientId ?? 0}", fontSmall, PdfSharpCore.Drawing.XBrushes.Black, x + 2, y + 10);
                        // Test name
                        g.DrawString($"Test: {order.InvestigationName}", font, PdfSharpCore.Drawing.XBrushes.Black, x + 2, y + 16);
                        // Barcode
                        g.DrawString(order.Barcode ?? "", fontBarcode, PdfSharpCore.Drawing.XBrushes.Black, x + 2, y + 24);

                        y += h + 2;
                    }
                }
                doc.Save(ms, false);
            }
            return ms.ToArray();
        }

        public class SampleCollectionInput
        {
            public long OrderId { get; set; }
            public string InvestigationName { get; set; }
            public string ItemName { get; set; }
            public bool Collect { get; set; }
            public string SampleType { get; set; }
            public string SampleContainer { get; set; }
            public string SampleVolume { get; set; }
            public string SampleCondition { get; set; }
            public string Barcode { get; set; }
            public string BarcodeLabel { get; set; }
            public DateTime? CollectionDateTime { get; set; }
            public string CollectionNotes { get; set; }
        }
    }
}