using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HmisWeb.Data;
using HmisWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Services
{
    /// <summary>
    /// Everything the report editor needs to populate {{Placeholder}} tokens for one order.
    /// Kept as a single DTO so the template resolver never has to touch the database.
    /// </summary>
    public class XrReportPlaceholderValues
    {
        public string PatientName { get; set; } = "";
        public string PatientAge { get; set; } = "";
        public string PatientAgeYears { get; set; } = "";
        public string PatientAgeMonths { get; set; } = "";
        public string Gender { get; set; } = "";
        public string PatientId { get; set; } = "";
        public string Phn { get; set; } = "";
        public string Nic { get; set; } = "";
        public string Mobile { get; set; } = "";
        public string MrNo { get; set; } = "";
        public string BillNo { get; set; } = "";
        public string XrIndentNo { get; set; } = "";
        public string VisitNo { get; set; } = "";
        public string Barcode { get; set; } = "";
        public string TestName { get; set; } = "";
        public string TestCode { get; set; } = "";
        public string SampleType { get; set; } = "";
        public string ReferringDoctor { get; set; } = "";
        public string InvestigatedBy { get; set; } = "";
        public string Institution { get; set; } = "";
        public string Department { get; set; } = "";
        public string InvestigationDate { get; set; } = "";
        public string ReportDate { get; set; } = "";
        public string ReportTime { get; set; } = "";
        public string CurrentDate { get; set; } = "";
        public string CurrentDateTime { get; set; } = "";
        public string WeekDays { get; set; } = "";
    }

    /// <summary>
    /// Loads per-service report templates and resolves {{Placeholder}} tokens against a real
    /// order. The stored template HTML is never modified; resolution happens on a copy so the
    /// same template can be reused for every patient.
    /// </summary>
    public interface IXrReportTemplateService
    {
        Task<XrReportTemplate> GetTemplateForServiceAsync(long? xrServiceId);
        Task<List<XrReportTemplate>> GetAllTemplatesAsync();
        Task<XrReportPlaceholderValues> BuildPlaceholderValuesAsync(XrOrder order);
        string ResolvePlaceholders(string templateHtml, XrReportPlaceholderValues values);
        string ToPlainText(string html);
        string StripDangerousHtml(string html);
        string GetDefaultTemplateHtml();
        List<string> GetStandardSections();
        List<string> GetStandardObservations();
    }

    public class XrReportTemplateService : IXrReportTemplateService
    {
        private static readonly Regex PlaceholderPattern =
            new(@"\{\{\s*(?<key>[A-Za-z0-9_]+)\s*\}\}", RegexOptions.Compiled);

        private readonly ApplicationDbContext _db;

        public XrReportTemplateService(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Prefers a template registered against the specific investigation, then any
        /// template flagged as the shared default. Falls back to the built-in generic
        /// X-ray layout so the editor is never empty.
        /// </summary>
        public async Task<XrReportTemplate> GetTemplateForServiceAsync(long? xrServiceId)
        {
            if (xrServiceId.HasValue)
            {
                var specific = await _db.XrReportTemplates
                    .FirstOrDefaultAsync(t => t.XrServiceId == xrServiceId.Value && !t.Retired);
                if (specific != null) return specific;
            }

            var fallback = await _db.XrReportTemplates
                .Where(t => t.XrServiceId == null && !t.Retired)
                .OrderByDescending(t => t.IsDefault)
                .ThenByDescending(t => t.Id)
                .FirstOrDefaultAsync();

            return fallback;
        }

        public async Task<List<XrReportTemplate>> GetAllTemplatesAsync()
        {
            return await _db.XrReportTemplates
                .Include(t => t.XrService)
                .Where(t => !t.Retired)
                .OrderBy(t => t.XrService == null ? 0 : 1)
                .ThenBy(t => t.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Reads the real patient / order / bill values needed by the template placeholders.
        /// </summary>
        public async Task<XrReportPlaceholderValues> BuildPlaceholderValuesAsync(XrOrder order)
        {
            var now = DateTime.Now;

            var person = order.Patient?.Person;
            if (order.Patient != null && person == null)
            {
                var patient = await _db.Patients
                    .Include(p => p.Person)
                    .FirstOrDefaultAsync(p => p.Id == order.PatientId.Value);
                order.Patient = patient;
                person = patient?.Person;
            }

            string age = string.Empty;
            int ageYears = 0;
            int ageMonths = 0;
            if (person?.Dob != null)
            {
                var span = now - person.Dob.Value;
                ageYears = span.Days / 365;
                ageMonths = (span.Days % 365) / 30;
                var ageDays = (span.Days % 365) % 30;
                age = ageYears > 0
                    ? $"{ageYears} years {ageMonths} months"
                    : (ageMonths > 0 ? $"{ageMonths} months {ageDays} days" : $"{ageDays} days");
            }

            string referringDoctor = null;
            if (order.PatientEncounter != null)
            {
                referringDoctor = order.PatientEncounter.ReferringConsultant?.Person?.Name
                    ?? order.PatientEncounter.ReferringStaff?.Person?.Name
                    ?? order.PatientEncounter.OpdDoctor?.Person?.Name;
            }
            if (string.IsNullOrWhiteSpace(referringDoctor) && order.DoctorId.HasValue)
            {
                referringDoctor = await ResolveStaffNameAsync(order.DoctorId.Value);
            }

            string investigatedBy = null;
            if (order.DoctorId.HasValue)
                investigatedBy = await ResolveStaffNameAsync(order.DoctorId.Value);
            if (string.IsNullOrWhiteSpace(investigatedBy))
                investigatedBy = order.Collector?.UserName ?? order.SampleCollectedBy?.UserName;

            var serviceCode = order.XrService?.Code
                ?? await _db.XrServices()
                    .Where(s => s.Id == order.XrServiceId.Value)
                    .Select(s => s.Code)
                    .FirstOrDefaultAsync();

            var serviceName = order.OrderName;
            if (string.IsNullOrWhiteSpace(serviceName) && order.XrServiceId.HasValue)
            {
                serviceName = await _db.XrServices()
                    .Where(s => s.Id == order.XrServiceId.Value)
                    .Select(s => s.Name)
                    .FirstOrDefaultAsync();
            }

            return new XrReportPlaceholderValues
            {
                PatientName = string.IsNullOrWhiteSpace(person?.Name) ? "" : person.Name,
                PatientAge = age,
                PatientAgeYears = ageYears > 0 ? ageYears.ToString() : "",
                PatientAgeMonths = ageMonths.ToString(),
                Gender = person?.Sex?.ToString() ?? "",
                PatientId = order.Patient?.PatientId?.ToString() ?? "",
                Phn = order.Patient?.Phn ?? "",
                Nic = person?.Nic ?? "",
                Mobile = person?.Mobile ?? "",
                MrNo = order.Patient?.PatientId?.ToString() ?? "",
                BillNo = order.BillId?.ToString() ?? "",
                XrIndentNo = order.BillId?.ToString() ?? "",
                VisitNo = order.VisitId?.ToString() ?? "",
                Barcode = order.Barcode ?? "",
                TestName = serviceName ?? "",
                TestCode = serviceCode ?? "",
                SampleType = order.SampleType ?? "",
                ReferringDoctor = referringDoctor ?? "",
                InvestigatedBy = investigatedBy ?? "",
                Institution = order.Institution?.Name ?? "",
                Department = order.Department?.Name ?? "",
                InvestigationDate = (order.SampleCollectedAt ?? order.CollectionDateTime ?? order.CreatedAt ?? now)
                    .ToString("dd MMM yyyy"),
                ReportDate = now.ToString("dd MMM yyyy"),
                ReportTime = now.ToString("hh:mm tt"),
                CurrentDate = now.ToString("dd MMM yyyy"),
                CurrentDateTime = now.ToString("dd MMM yyyy hh:mm tt"),
                WeekDays = string.Join(", ", DateTime.Now.ToString("dddd").Split(','))
            };
        }

        /// <summary>
        /// Doctors are Staff records, so the display name always comes from the linked Person.
        /// </summary>
        private async Task<string> ResolveStaffNameAsync(long staffId)
        {
            return await _db.Staffs
                .Where(s => s.Id == staffId)
                .Select(s => s.Person != null ? s.Person.Name : s.Code)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Replaces every known {{Token}} with the matching patient value. Unknown tokens are
        /// left untouched so the radiologist can see a template still needs configuring.
        /// </summary>
        public string ResolvePlaceholders(string templateHtml, XrReportPlaceholderValues values)
        {
            if (string.IsNullOrEmpty(templateHtml)) return templateHtml;

            return PlaceholderPattern.Replace(templateHtml, m =>
            {
                var key = m.Groups["key"].Value;
                var value = key switch
                {
                    nameof(XrReportPlaceholderValues.PatientName) => values.PatientName,
                    nameof(XrReportPlaceholderValues.PatientAge) => values.PatientAge,
                    nameof(XrReportPlaceholderValues.PatientAgeYears) => values.PatientAgeYears,
                    nameof(XrReportPlaceholderValues.PatientAgeMonths) => values.PatientAgeMonths,
                    nameof(XrReportPlaceholderValues.Gender) => values.Gender,
                    nameof(XrReportPlaceholderValues.PatientId) => values.PatientId,
                    nameof(XrReportPlaceholderValues.Phn) => values.Phn,
                    nameof(XrReportPlaceholderValues.Nic) => values.Nic,
                    nameof(XrReportPlaceholderValues.Mobile) => values.Mobile,
                    nameof(XrReportPlaceholderValues.MrNo) => values.MrNo,
                    nameof(XrReportPlaceholderValues.BillNo) => values.BillNo,
                    nameof(XrReportPlaceholderValues.XrIndentNo) => values.XrIndentNo,
                    nameof(XrReportPlaceholderValues.VisitNo) => values.VisitNo,
                    nameof(XrReportPlaceholderValues.Barcode) => values.Barcode,
                    nameof(XrReportPlaceholderValues.TestName) => values.TestName,
                    nameof(XrReportPlaceholderValues.TestCode) => values.TestCode,
                    nameof(XrReportPlaceholderValues.SampleType) => values.SampleType,
                    nameof(XrReportPlaceholderValues.ReferringDoctor) => values.ReferringDoctor,
                    nameof(XrReportPlaceholderValues.InvestigatedBy) => values.InvestigatedBy,
                    nameof(XrReportPlaceholderValues.Institution) => values.Institution,
                    nameof(XrReportPlaceholderValues.Department) => values.Department,
                    nameof(XrReportPlaceholderValues.InvestigationDate) => values.InvestigationDate,
                    nameof(XrReportPlaceholderValues.ReportDate) => values.ReportDate,
                    nameof(XrReportPlaceholderValues.ReportTime) => values.ReportTime,
                    nameof(XrReportPlaceholderValues.CurrentDate) => values.CurrentDate,
                    nameof(XrReportPlaceholderValues.CurrentDateTime) => values.CurrentDateTime,
                    nameof(XrReportPlaceholderValues.WeekDays) => values.WeekDays,
                    _ => null
                };

                return string.IsNullOrWhiteSpace(value) ? "" : value;
            });
        }

        /// <summary>Converts report HTML into the plain text kept in XRORDER.Result.</summary>
        public string ToPlainText(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return "";

            var text = Regex.Replace(html, @"<\s*br\s*/?\s*>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<\s*/\s*(p|div|h[1-6]|li|tr)\s*>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<\s*li[^>]*>", "- ", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<[^>]+>", string.Empty);
            text = System.Net.WebUtility.HtmlDecode(text);

            // Normalise blank lines that Quill leaves behind for empty paragraphs.
            text = Regex.Replace(text, @"[ \t]+\n", "\n");
            text = Regex.Replace(text, @"\n{3,}", "\n\n");

            return text.Trim();
        }

        /// <summary>
        /// Removes script/style/event handlers so the editor body can be rendered safely
        /// back into the page when a saved report is reloaded.
        /// </summary>
        public string StripDangerousHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return "";

            var text = Regex.Replace(html, @"<\s*(script|style|iframe|object|embed|link|meta)\b.*?<\s*/\s*\1\s*>",
                string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            text = Regex.Replace(text, @"<\s*(script|style|iframe|object|embed|link|meta)\b[^>]*/?>",
                string.Empty, RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\son[a-z]+\s*=\s*""[^""]*""|\son[a-z]+\s*=\s*'[^']*'|\son[a-z]+\s*=\s*[^\s>]+",
                string.Empty, RegexOptions.IgnoreCase);

            // Script URLs must go in both quoting styles, with or without inner whitespace,
            // and must also cover unquoted attributes such as href=javascript:alert(1).
            text = Regex.Replace(text,
                @"(href|src|xlink:href|formaction|action)\s*=\s*(?:""\s*javascript:[^""]*""|'\s*javascript:[^']*'|[^\s>]*javascript:[^\s>]*)",
                "href=\"#\"", RegexOptions.IgnoreCase);

            // Browsers decode entities inside attribute values, so jav&#09;ascript: is a
            // working script URL. Strip numeric entities from URL attributes before the
            // javascript: check so the evasion cannot survive.
            text = Regex.Replace(text,
                @"((?:href|src|xlink:href|formaction|action)\s*=\s*[""']?)([^""'>\s]*)",
                m => m.Groups[1].Value + Regex.Replace(m.Groups[2].Value, @"&#[xX]?[0-9a-fA-F]+;?", ""),
                RegexOptions.IgnoreCase);

            // Belt and braces: anything still naming a javascript: scheme is neutralised.
            text = Regex.Replace(text, @"javascript\s*:", "blocked:", RegexOptions.IgnoreCase);

            return text;
        }

        /// <summary>Generic X-ray layout used when no template is registered yet.</summary>
        public string GetDefaultTemplateHtml()
        {
            return @"<h2 style=""text-align:center;"">X-RAY REPORT</h2>
<p style=""text-align:center;""><strong>Investigation: {{TestName}}</strong></p>
<hr>
<p><strong>Patient Name:</strong> {{PatientName}} &nbsp;|&nbsp; <strong>Age:</strong> {{PatientAge}} &nbsp;|&nbsp; <strong>Gender:</strong> {{Gender}}</p>
<p><strong>MR No:</strong> {{MrNo}} &nbsp;|&nbsp; <strong>XR Indent No:</strong> {{XrIndentNo}} &nbsp;|&nbsp; <strong>Date:</strong> {{InvestigationDate}}</p>
<p><strong>Referring Doctor:</strong> {{ReferringDoctor}} &nbsp;|&nbsp; <strong>Investigated By:</strong> {{InvestigatedBy}}</p>
<h3>TECHNIQUE</h3>
<p><br></p>
<h3>OBSERVATION / FINDINGS</h3>
<p><br></p>
<h3>IMPRESSION / CONCLUSION</h3>
<p><br></p>
<h3>RECOMMENDATION</h3>
<p><br></p>
<p style=""text-align:right;""><em>Radiologist signature</em></p>";
        }

        /// <summary>Sections offered by the editor's insert menu and by Report Setup.</summary>
        public List<string> GetStandardSections()
        {
            return new List<string>
            {
                "PATIENT INFORMATION",
                "CLINICAL INFORMATION",
                "TECHNIQUE",
                "OBSERVATION / FINDINGS",
                "MEASUREMENTS",
                "IMPRESSION / CONCLUSION",
                "RECOMMENDATION"
            };
        }

        /// <summary>
        /// Short standard sentences a sonographer can drop into a report instead of typing
        /// the same normal findings over and over.
        /// </summary>
        public List<string> GetStandardObservations()
        {
            return new List<string>
            {
                "Study performed with adequate acoustic windows.",
                "Liver is normal in size, shape and echotexture.",
                "No focal lesion is seen in the liver.",
                "Gall bladder is well distended with no calculus.",
                "CBD is not dilated.",
                "Spleen is normal in size with no focal lesion.",
                "Pancreas is normal in size and echotexture.",
                "Both kidneys are normal in size, shape and echogenicity.",
                "Renal corticomedullary differentiation is preserved.",
                "No hydronephrosis is seen.",
                "No perinephric collection is seen.",
                "Urinary bladder is well distended and empty of calculus.",
                "Bladder wall is smooth and of normal thickness.",
                "Uterus is normal in size, shape and echogenicity.",
                "Endometrial cavity is normal.",
                "Gestational sac is seen within the endometrial cavity.",
                "Fetal cardiac activity is present.",
                "Fetal biometry is consistent with the gestational age.",
                "Liquor volume is adequate.",
                "Placenta is anterior and of normal appearance.",
                "No focal lesion is seen in the abdomen or pelvis.",
                "Colour and spectral Doppler study performed.",
                "No abnormal vascular flow is demonstrated."
            };
        }
    }
}
