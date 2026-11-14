using System;
using System.Collections.Generic;

namespace HmisWeb.Services.Lab
{
    /// <summary>
    /// DTOs shared by the single-screen LAB SERVICE INDENT (INVOICE) page
    /// (/lab/indent), its query service and its save service.
    ///
    /// Purpose
    ///   * Keep the JSON contract between the Razor page and the server small and
    ///     explicit.  Money is always <see cref="decimal"/> on the wire; the legacy
    ///     tables (OBIL_MST/DET, IBIL_MST/DET) are decimal as well
    ///     (decimal(18,2)).
    ///   * The client never sends a rate it wants to be trusted: only a service id,
    ///     quantity, discount and urgent flag.  The server re-reads SERVICES and the
    ///     company rate card and recomputes every amount.
    ///
    /// Validations reflected in the DTOs
    ///   * PatientId / EncounterId are ids of the ASP.NET PATIENT and
    ///     PATIENTENCOUNTER rows (not the legacy REGISTER row).
    ///   * Line.Qty defaults to 1; the UI adds one unit per test.
    ///   * Line.Discount is 0..(Qty * master rate) and is re-checked server-side.
    ///   * ConcessionApprover is mandatory when any line discount is > 0.
    ///   * IdempotencyToken is the page's GUID and is stored on BILL.SessionId.
    ///
    /// Developer : Tajwer Jalal
    /// Created   : 26-Oct-2026 13:05
    /// </summary>
    public class PatientSuggestionDto
    {
        public long Id { get; set; }
        public long? PatientId { get; set; }
        public string Phn { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string NameWithInitials { get; set; }
        public string Age { get; set; }
        public string Sex { get; set; }
        public string Mobile { get; set; }
        public string Nic { get; set; }
        public bool IsAdmitted { get; set; }
        public bool IsExact { get; set; }
    }

    public class PatientContextDto
    {
        public long Id { get; set; }
        public long? PatientId { get; set; }
        public string Phn { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string NameWithInitials { get; set; }
        public string Age { get; set; }
        public string Sex { get; set; }
        public string Mobile { get; set; }
        public string Phone { get; set; }
        public string Nic { get; set; }

        /// <summary>True when the patient has at least one active (not discharged) IPD stay.</summary>
        public bool IsAdmitted { get; set; }

        /// <summary>Active inpatient stays. The first one is the default IPD encounter.</summary>
        public List<PatientEncounterDto> ActiveIpdEncounters { get; set; } = new();

        /// <summary>Latest OPD encounter, when the patient has one. Used for the visit number.</summary>
        public PatientEncounterDto OpdEncounter { get; set; }

        public long? CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string CompanyCode { get; set; }

        /// <summary>Sum of unpaid BILL rows (NetTotal - PaidAmount) for this patient.</summary>
        public decimal UnpaidBalance { get; set; }

        /// <summary>Preview only. The real number is allocated inside the save transaction.</summary>
        public string NextIndentNo { get; set; }
        public string NextIndentDisplay { get; set; }
    }

    public class PatientEncounterDto
    {
        public long Id { get; set; }
        public string BhtNo { get; set; }
        public string BedNo { get; set; }
        public string WardName { get; set; }
        public DateTime? AdmissionDate { get; set; }
        public string ConsultantName { get; set; }
        public string ConsultantCode { get; set; }
        public bool IsIpd { get; set; }
    }

    public class TestSuggestionDto
    {
        public long Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Sname { get; set; }
        public decimal Rate { get; set; }
    }

    public class TestSearchResultDto
    {
        public List<TestSuggestionDto> Items { get; set; } = new();
        public int Total { get; set; }
        public bool HasMore => Total > Items.Count;
    }

    public class IndentNumberDto
    {
        /// <summary>9-digit legacy value stored in OBIL_MST.SLIPNO (e.g. 226000001).</summary>
        public string IndentNo { get; set; }

        /// <summary>Display form with the legacy hyphen (e.g. 226-000001).</summary>
        public string Display { get; set; }
    }

    public class NewPatientDto
    {
        public string Name { get; set; }
        public string NameWithInitials { get; set; }
        public string Nic { get; set; }
        public string Mobile { get; set; }
        public string Phone { get; set; }
        public string Sex { get; set; }
    }

    public class NewPatientResult
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
        public PatientContextDto Patient { get; set; }
    }

    public class SaveIndentLineDto
    {
        public long ServiceId { get; set; }
        public int Qty { get; set; } = 1;
        public decimal Discount { get; set; }
        public bool Urgent { get; set; }
    }

    public class SaveIndentDto
    {
        public long PatientId { get; set; }
        public long? EncounterId { get; set; }
        public bool Ipd { get; set; }
        public bool CashNow { get; set; }
        public string ConcessionApprover { get; set; }
        public string IdempotencyToken { get; set; }
        public List<SaveIndentLineDto> Lines { get; set; } = new();
    }

    public class SaveErrorDto
    {
        public string Field { get; set; }

        /// <summary>Zero-based line index for line-level errors (discount/urgent), otherwise null.</summary>
        public int? LineIndex { get; set; }

        public string Message { get; set; }
    }

    public class SaveIndentResult
    {
        public bool Ok { get; set; }
        public bool AlreadySaved { get; set; }
        public string Message { get; set; }
        public long BillId { get; set; }
        public string IndentNo { get; set; }
        public string InvoiceNumber { get; set; }
        public decimal GrossTotal { get; set; }
        public decimal Concession { get; set; }
        public decimal NetTotal { get; set; }
        public bool IsIpd { get; set; }
        public bool Paid { get; set; }
        public string ReceiptUrl { get; set; }
        public List<SaveErrorDto> Errors { get; set; } = new();
    }
}
