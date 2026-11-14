using System;
using System.Collections.Generic;

namespace HmisWeb.Models.Lab
{
    /*
     * ============================================================================
     * File........: Models/Lab/LabViewModels.cs
     * Purpose.....: View models shared by the LB0110 Test Result Entry page, its
     *               partial views, the page handlers and the lab services.
     * Inputs......: Populated by LabResultService / CultureService from REPORT,
     *               TESTTR, READTR, CULTURE, CUL_TR and the master tables.
     * Outputs.....: Strongly typed objects consumed by Razor partials and JSON
     *               handlers (no EF entities are exposed to the views).
     * Business
     *   rules.....: 1. LABNO/TID/TESTNO identify a test line; TESTNO is part of
     *                  the key in the legacy tables even though it is often "001".
     *               2. READTR rows are keyed by LABNO+TID+TESTNO+ID (reading id).
     *               3. Culture is one CULTURE row per test plus N CUL_TR rows.
     *               4. All strings are trimmed because the legacy VFP data was
     *                  fixed-width and arrives padded.
     * Developer...: Tajwer Jalal
     * Date/Time...: 2026-11-02 11:12
     * ============================================================================
     */

    /// <summary>One row of the "List" tab (REPORT master).</summary>
    public class LabIndentListItem
    {
        public string LabNo { get; set; } = "";
        public DateTime? Date { get; set; }
        public string Time { get; set; } = "";
        public string Cid { get; set; } = "";
        public string Hrno { get; set; } = "";
        public string Type { get; set; } = "";
        public string Name { get; set; } = "";
        public string Sex { get; set; } = "";
        public string Age { get; set; } = "";
        public string AType { get; set; } = "";
        public string BedNo { get; set; } = "";
        public string Posted { get; set; } = "";
        public double Net { get; set; }
        public int TestCount { get; set; }
        public int PendingCount { get; set; }

        /// <summary>Legacy 999-999999 display mask for the 9 digit LABNO.</summary>
        public string LabNoDisplay => FormatLabNo(LabNo);

        public static string FormatLabNo(string labNo)
        {
            labNo = (labNo ?? "").Trim();
            return labNo.Length == 9 ? labNo.Substring(0, 3) + "-" + labNo.Substring(3) : labNo;
        }
    }

    /// <summary>Paged result set for the List tab.</summary>
    public class LabIndentPage
    {
        public List<LabIndentListItem> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int Total { get; set; }
        public int TotalPages => Total <= 0 ? 1 : (int)Math.Ceiling(Total / (double)PageSize);
        public string LabNoFilter { get; set; } = "";
        public string HrnoFilter { get; set; } = "";
        public string NameFilter { get; set; } = "";
        public bool IncludeReported { get; set; }
    }

    /// <summary>REPORT header shown read-only on the Entry tab.</summary>
    public class LabIndentHeader
    {
        public string LabNo { get; set; } = "";
        public DateTime? Date { get; set; }
        public string Time { get; set; } = "";
        public string Name { get; set; } = "";
        public string PCode { get; set; } = "";
        public string Hrno { get; set; } = "";
        public string Sex { get; set; } = "";
        public string Age { get; set; } = "";
        public string AType { get; set; } = "";
        public string Cid { get; set; } = "";
        public string Concode { get; set; } = "";
        public string BedNo { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Type { get; set; } = "";
        public string Posted { get; set; } = "";
        public double Net { get; set; }
        public string Remarks { get; set; } = "";

        public string LabNoDisplay => LabIndentListItem.FormatLabNo(LabNo);

        /// <summary>Legacy XXX-XX-XXXXX display mask for the 9 digit patient code.</summary>
        public string PCodeDisplay
        {
            get
            {
                var p = (PCode ?? "").Trim();
                return p.Length == 9 ? p.Substring(0, 3) + "-" + p.Substring(3, 2) + "-" + p.Substring(5) : p;
            }
        }

        /// <summary>YEAR/MONTH/DAY/HOUR label for the stored Y/M/D/H value.</summary>
        public string ATypeDisplay => ATypeLabel(AType);

        public static string ATypeLabel(string atype)
        {
            atype = (atype ?? "").Trim().ToUpperInvariant();
            return atype switch
            {
                "Y" or "YEAR" => "YEAR",
                "M" or "MONTH" => "MONTH",
                "D" or "DAY" => "DAY",
                "H" or "HOUR" => "HOUR",
                _ => atype
            };
        }
    }

    /// <summary>One TESTTR line of the selected indent.</summary>
    public class LabTestRow
    {
        public string Tid { get; set; } = "";
        public string TestNo { get; set; } = "";
        public string Name { get; set; } = "";
        public string GroupCode { get; set; } = "";
        public string GroupName { get; set; } = "";
        public double Charges { get; set; }
        public DateTime? TDate { get; set; }
        public DateTime? RepoDt { get; set; }
        public string Gid { get; set; } = "";
        public bool Culture { get; set; }
        public string Remarks { get; set; } = "";
        public int SrNo { get; set; }
        public string Urgent { get; set; } = "";

        public string Key => (Tid ?? "") + "|" + (TestNo ?? "");
        public bool Reported => RepoDt.HasValue;
    }

    /// <summary>One READTR row (reading + entered value) for the result grid.</summary>
    public class LabReadingRow
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Unit { get; set; } = "";
        public string Range { get; set; } = "";
        public string Category { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string Value { get; set; } = "";
        public List<string> Suggestions { get; set; } = new();

        /// <summary>True when the reading id is not present in the master (legacy "Untracable Unit").</summary>
        public bool IsUntraceable { get; set; }
        public string UnitDisplay => IsUntraceable ? "Untracable Unit" : (Unit ?? "");
    }

    /// <summary>Everything required to render the Entry tab.</summary>
    public class LabEntryView
    {
        public LabIndentHeader Header { get; set; }
        public List<LabTestRow> Tests { get; set; } = new();
        public LabTestRow SelectedTest { get; set; }
        public string GroupCode { get; set; } = "";
        public string GroupName { get; set; } = "";
        public List<LabReadingRow> Readings { get; set; } = new();
        public LabCultureView Culture { get; set; } = new();
        public List<string> RemarkOptions { get; set; } = new();
        public string NextTid { get; set; } = "";
        public string NextTestNo { get; set; } = "";
        public string Error { get; set; } = "";
    }

    /// <summary>Culture header + medicine sensitivity grid.</summary>
    public class LabCultureView
    {
        public string Growth1 { get; set; } = "";
        public string Growth2 { get; set; } = "";
        public string Growth3 { get; set; } = "";
        public string Species1 { get; set; } = "";
        public string Species2 { get; set; } = "";
        public string Species3 { get; set; } = "";
        public List<LabCultureMedicine> Medicines { get; set; } = new();
    }

    /// <summary>One CUL_TR medicine line (A/B/C sensitivity).</summary>
    public class LabCultureMedicine
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string S1 { get; set; } = "";
        public string S2 { get; set; } = "";
        public string S3 { get; set; } = "";
    }

    /// <summary>JSON payload posted by the Save button.</summary>
    public class LabSaveRequest
    {
        public string LabNo { get; set; } = "";
        public string Tid { get; set; } = "";
        public string TestNo { get; set; } = "";
        public string AType { get; set; } = "";
        public string Remarks { get; set; } = "";
        public string ReportDate { get; set; } = "";
        public int SrNo { get; set; }

        // Original TESTTR values for optimistic concurrency (no rowversion exists).
        public string OriginalRemarks { get; set; } = "";
        public string OriginalReportDate { get; set; } = "";
        public int OriginalSrNo { get; set; }

        public List<LabSaveReading> Readings { get; set; } = new();
        public LabSaveCulture Culture { get; set; } = new();
    }

    /// <summary>One result line posted by the Save button.</summary>
    public class LabSaveReading
    {
        public string Code { get; set; } = "";
        public string Value { get; set; } = "";
    }

    /// <summary>Culture block posted by the Save button.</summary>
    public class LabSaveCulture
    {
        public string Growth1 { get; set; } = "";
        public string Growth2 { get; set; } = "";
        public string Growth3 { get; set; } = "";
        public string Species1 { get; set; } = "";
        public string Species2 { get; set; } = "";
        public string Species3 { get; set; } = "";
        public List<LabCultureMedicine> Medicines { get; set; } = new();
    }

    /// <summary>JSON response returned by the Save handler.</summary>
    public class LabSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string NextUrl { get; set; } = "";
    }
}
