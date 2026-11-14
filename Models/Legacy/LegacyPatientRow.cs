using System;

namespace HmisWeb.Models.Legacy
{
    /// <summary>
    /// One row of the legacy patient search: a REGISTER (visit) row joined to
    /// its PATIENT master row. Mirrors the VFP query
    ///
    ///   SELECT r.TYPE, r.REGNO, r.RDATE, r.RTIME, r.PCODE,
    ///          p.CODE, p.NAME, p.SEX, p.AGE
    ///   FROM REGISTER r LEFT JOIN PATIENT p ON p.CODE = r.PCODE
    ///   WHERE p.NAME LIKE '%term%' OR r.REGNO LIKE '%term%' OR ...
    ///   ORDER BY r.RDATE DESC
    ///
    /// PATIENT.CODE is the permanent patient code. REGISTER.REGNO is issued
    /// again on every visit, so the screens show both values instead of treating
    /// the patient code as the registration number.
    /// </summary>
    public class LegacyPatientRow
    {
        /// <summary>PATIENT.id, resolved through REGISTER.PCODE = PATIENT.CODE.</summary>
        public long PatientAppId { get; set; }

        /// <summary>
        /// Registration number for rows that came from REGISTER (REGISTER.REGNO).
        /// For rows that only exist in PATIENT this is the patient search key
        /// (PHN, PatientId or CODE), kept for compatibility with the indent screen.
        /// </summary>
        public string RegNo { get; set; }

        /// <summary>
        /// Explicit visit registration number (REGISTER.REGNO) when the row came
        /// from REGISTER, otherwise the latest REGISTER.REGNO for the patient.
        /// Kept separate from <see cref="RegNo"/> because some callers still use
        /// RegNo as the patient's search key / fallback identifier.
        /// </summary>
        public string VisitRegNo { get; set; }

        /// <summary>
        /// PATIENT.CODE - the permanent patient code (one per patient, unlike the
        /// visit registration number). Falls back to PATIENT.PatientId for
        /// patients registered by this application with no legacy code.
        /// </summary>
        public string PatientCode { get; set; }

        /// <summary>PATIENT.PHN - the legacy hospital / patient number.</summary>
        public string Phn { get; set; }

        /// <summary>REGISTER.CID - legacy company/panel code.</summary>
        public string Cid { get; set; }

        /// <summary>REGISTER.CONCODE - legacy consultant code.</summary>
        public string Concode { get; set; }

        /// <summary>REGISTER.RDATE (visit date).</summary>
        public DateTime? RDate { get; set; }

        /// <summary>REGISTER.RTIME</summary>
        public string RTime { get; set; }

        /// <summary>REGISTER.TYPE - 'I' indoor / 'O' outdoor.</summary>
        public string Type { get; set; }

        /// <summary>REGISTER.PCODE / PATIENT.CODE</summary>
        public string PCode { get; set; }

        /// <summary>PATIENT.NAME</summary>
        public string Name { get; set; }

        /// <summary>PATIENT.SEX</summary>
        public string Sex { get; set; }

        /// <summary>PATIENT.AGE</summary>
        public string Age { get; set; }

        /// <summary>First available of PHONE1 / PHONE2 / PHONE3.</summary>
        public string Phone { get; set; }

        /// <summary>PATIENT.ADDRESS</summary>
        public string Address { get; set; }

        /// <summary>PATIENT.NIC</summary>
        public string Nic { get; set; }

        // ---- REGISTER billing counters for this visit ----

        /// <summary>REGISTER.TBILLAMT - total billed amount for the visit.</summary>
        public decimal? BillAmount { get; set; }

        /// <summary>REGISTER.TRCVD - receipts received from the patient.</summary>
        public decimal? Received { get; set; }

        /// <summary>REGISTER.TPYMT - payments made on the patient's behalf.</summary>
        public decimal? Paid { get; set; }

        /// <summary>REGISTER.CASHDISC - settlement discount.</summary>
        public decimal? Discount { get; set; }

        /// <summary>REGISTER.SETTLE - the visit bill is settled.</summary>
        public bool? Settled { get; set; }

        /// <summary>REGISTER.BILLGEN - a bill has been generated for the visit.</summary>
        public bool? BillGenerated { get; set; }

        /// <summary>REGISTER.BILLSTATUS - 0 open, 1 discharged, 2 updating, 3 generated, 4 settled.</summary>
        public byte? BillStatus { get; set; }

        /// <summary>Legacy amount due: billed - received + paid - discount.</summary>
        public decimal Balance => (BillAmount ?? 0) - (Received ?? 0) + (Paid ?? 0) - (Discount ?? 0);

        public string BillStatusText => BillStatus switch
        {
            0 => "Open",
            1 => "Discharged",
            2 => "Bill Updating",
            3 => "Bill Generated",
            4 => "Settled",
            _ => "-"
        };

        public string SettledText => Settled.HasValue ? (Settled.Value ? "Yes" : "No") : "-";

        public string BillGeneratedText => BillGenerated.HasValue ? (BillGenerated.Value ? "Yes" : "No") : "-";

        public string TypeText => Type == "I" ? "Indoor" : Type == "O" ? "Outdoor" : Type;
    }
}
