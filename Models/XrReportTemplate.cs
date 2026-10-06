using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    /// <summary>
    /// Lifecycle of an X-ray report.
    /// Draft -> Saved -> Finalized. Finalized reports are locked unless reopened.
    /// </summary>
    public enum XrReportStatus
    {
        Draft = 0,
        Saved = 1,
        Finalized = 2
    }

    /// <summary>
    /// Predefined report format for an X-ray investigation, modelled on the ultrasound
    /// "Test Report Setup" screen. One template per XrService, plus an optional shared
    /// default. The stored HTML is reused untouched every time the template is loaded;
    /// placeholders are resolved on the fly so the template itself is never overwritten.
    /// </summary>
    public class XrReportTemplate
    {
        public long Id { get; set; }
        public long? XrServiceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Rich-text (Quill) delta-safe HTML body containing {{Placeholder}} tokens.
        public string TemplateHtml { get; set; } = string.Empty;

        // Section headers inserted by Report Setup, kept separate so the editor can offer
        // them in the "Insert standard section" menu without re-parsing the HTML.
        public string SectionsJson { get; set; } = "[]";

        public bool IsDefault { get; set; }
        public bool Retired { get; set; }
        public DateTime? RetiredAt { get; set; }
        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }

        public virtual XrService? XrService { get; set; }
        public virtual WebUser? Creater { get; set; }
        public virtual WebUser? Editer { get; set; }
    }

    /// <summary>
    /// Local radiology terminology used by the Medical Dictionary lookup. Kept in the
    /// database so the hospital system keeps working with no Internet access and no
    /// patient data ever leaves the server.
    /// </summary>
    public class XrMedicalTerm
    {
        public long Id { get; set; }
        public string Term { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Synonyms { get; set; } = string.Empty;
        public bool Retired { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}