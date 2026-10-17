namespace HmisWeb.Models.Legacy
{
    /// <summary>
    /// INPATIENT BILL LIFECYCLE STATUS - stored in REGISTER.BILLSTATUS (tinyint).
    /// Mirrors the VFP flow: admitted -> discharged -> bill updating -> generated -> settled.
    /// Edits are permitted only while the status is Discharged or BillUpdating.
    ///
    /// Developer: Tajwer Jalal | Date: 12-Oct-2026 | Time: 12:06
    /// </summary>
    public enum IpdBillStatus
    {
        /// <summary>Still admitted (REGISTER.DDATE IS NULL).</summary>
        Admitted = 0,

        /// <summary>Discharged, file received by billing, nothing edited yet.</summary>
        Discharged = 1,

        /// <summary>Billing executive is updating the file.</summary>
        BillUpdating = 2,

        /// <summary>Bill generated - locked (no line/total/discount edits).</summary>
        BillGenerated = 3,

        /// <summary>Settled - fully locked.</summary>
        Settled = 4
    }
}
