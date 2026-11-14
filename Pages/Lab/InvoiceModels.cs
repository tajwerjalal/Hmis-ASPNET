using System;
using System.Collections.Generic;

namespace HmisWeb.Pages.Lab
{
    /// <summary>Invoice model for printable lab service invoice.</summary>
    public class LabInvoiceModel
    {
        public long BillId { get; set; }
        public string InvoiceNumber { get; set; }
        public string IndentNo { get; set; }
        public string HospitalName { get; set; }
        public string HospitalAddress { get; set; }
        public string HospitalPhone { get; set; }
        public string HospitalNo { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string PatientName { get; set; }
        public long? PatientPhn { get; set; }
        public string PatientRegNo { get; set; }
        public string PatientAge { get; set; }
        public string PatientSex { get; set; }
        public string PatientPhone { get; set; }
        public string PatientAddress { get; set; }
        public string Guardian { get; set; }
        public string Diagnosis { get; set; }
        public string Bed { get; set; }
        public string VisitNumber { get; set; }
        public string DepartmentName { get; set; }
        public List<InvoiceLine> Lines { get; set; } = new();
        public double SubTotal { get; set; }
        public double Discount { get; set; }
        public double NetTotal { get; set; }
        public string Cashier { get; set; }
        public bool IsIPD { get; set; }
        public string Comments { get; set; }
        public int ReprintCount { get; set; }

        /// <summary>Payment status of the indent bill: 'PAID' / 'UNPAID'.</summary>
        public string PaymentStatus { get; set; } = "UNPAID";

        /// <summary>Bill mode: 'OPD' (pay at cashier) or 'IPD' (account).</summary>
        public string Mode { get; set; } = "OPD";

        /// <summary>Cashier receipt number (null when not yet paid).</summary>
        public string ReceiptNo { get; set; }

        /// <summary>Server timestamp of the payment (null while UNPAID).</summary>
        public DateTime? PaidAt { get; set; }

        /// <summary>Last original-slip print timestamp.</summary>
        public DateTime? PrintedAt { get; set; }

        /// <summary>Last duplicate-slip print timestamp.</summary>
        public DateTime? DuplicatePrintedAt { get; set; }

        /// <summary>User id who printed the original slip.</summary>
        public long PrintedUserId { get; set; }

        /// <summary>User id who printed the duplicate slip.</summary>
        public long DuplicatePrintedUserId { get; set; }

        /// <summary>Total number of prints issued for this indent.</summary>
        public int PrintCount { get; set; }

        /// <summary>1 = duplicate copy, 0 = original slip.</summary>
        public bool Duplicate { get; set; }
    }

    public class InvoiceLine
    {
        public string Code { get; set; }
        public string ItemName { get; set; }
        public int Qty { get; set; }
        public double Rate { get; set; }
        public double Discount { get; set; }
        public double NetValue { get; set; }
    }
}
