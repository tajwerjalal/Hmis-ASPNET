using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    /// <summary>
    /// Cashier module entities (migrated from the legacy VFP Register/Cashier app).
    /// Mirror of CASH.DBC tables: CRCVD, CRCVDDET, CPYMT, CPYMtdet, CHIST, CHISTDET,
    /// CCONTROL, CASHCNT, CCOUNTER, ACCHEAD. Business rules preserved from the VFP forms.
    /// Additive only - nothing existing was changed.
    /// </summary>

    [Table("ACCCHEAD")]
    public class AccountHead
    {
        [Key] [Column("ACCNO")] public string AccNo { get; set; } = "";
        [Column("ACCDESC")] public string AccDesc { get; set; } = "";
    }

    [Table("CCOUNTER")]
    public class CashCounter
    {
        [Key] [Column("CCODE")] public string CCode { get; set; } = "";
        [Column("DESC")] public string Desc { get; set; } = "";
        [Column("NEXT_NO")] public int NextNo { get; set; }
        [Column("STATUS")] public string Status { get; set; } = "A";   // A active / I inactive
        [Column("USER_ID")] public string UserId { get; set; } = "";
        [Column("UDATE")] public DateTime? UDate { get; set; }
        [Column("UTIME")] public string UTime { get; set; } = "";
    }

    [Table("CASHCNT")]
    public class CashCounterSession
    {
        [Column("CCODE")] public string CCode { get; set; } = "";
        [Column("LCODE")] public int LCode { get; set; }
        [Column("DESC")] public string Desc { get; set; } = "";
        [Column("ACTIVE_DT")] public DateTime? ActiveDt { get; set; }
        [Column("ACTIVE_TM")] public string ActiveTm { get; set; } = "";
        [Column("ACTIVE_BY")] public string ActiveBy { get; set; } = "";
        [Column("ACTIVE_KEY")] public string ActiveKey { get; set; } = "";
        [Column("CLOSE_DT")] public DateTime? CloseDt { get; set; }
        [Column("CLOSE_TM")] public string CloseTm { get; set; } = "";
        [Column("CLOSE_BY")] public string CloseBy { get; set; } = "";
        [Column("VNO")] public string Vno { get; set; } = "";
        [Column("NET_CASH")] public decimal NetCash { get; set; }
        [Column("STATUS")] public string Status { get; set; } = "A";   // A active / C closed
    }

    [Table("CCONTROL")]
    public class CashControl
    {
        // Single-row control table (RECNO/PAYNO/VOUNO counters).
        // Id is an IDENTITY column in the legacy schema; leave it store-generated.
        [Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)] public int Id { get; set; }
        [Column("RECNO")] public int RecNo { get; set; }
        [Column("PAYNO")] public int PayNo { get; set; }
        [Column("VOUNO")] public int VouNo { get; set; }
    }

    [Table("CRCVD")]
    public class CashReceipt
    {
        [Column("CCODE")] public string CCode { get; set; } = "";
        [Column("LCODE")] public int LCode { get; set; }
        [Key] [Column("TRNO")] public string TrNo { get; set; } = "";
        [Column("TRDATE")] public DateTime TrDate { get; set; }
        [Column("TRTIME")] public string TrTime { get; set; } = "";
        [Column("TYPE")] public string Type { get; set; } = "";
        [Column("REGNO")] public int? RegNo { get; set; }
        [Column("SETTLE")] public bool Settle { get; set; }
        [Column("REFERENCE")] public string Reference { get; set; } = "";
        [Column("MODE")] public int Mode { get; set; }            // 1 cash, 2 card
        [Column("CARDTYPE")] public int CardType { get; set; }
        [Column("CARDNAME")] public string CardName { get; set; } = "";
        [Column("CARDNO")] public string CardNo { get; set; } = "";
        [Column("CARDVALID")] public DateTime? CardValid { get; set; }
        [Column("CARDAPPRNO")] public string CardApprNo { get; set; } = "";
        [Column("CDISCOUNT")] public decimal CDisc { get; set; }
        [Column("DISCBY")] public string DiscBy { get; set; } = "";
        [Column("TOTALAMT")] public decimal TotalAmt { get; set; }
        [Column("USERID")] public string UserId { get; set; } = "";
        [Column("UDATE")] public DateTime? UDate { get; set; }
        [Column("UTIME")] public string UTime { get; set; } = "";

        public List<CashReceiptDetail> Details { get; set; } = new();
    }

    [Table("CRCVDDET")]
    public class CashReceiptDetail
    {
        [Column("TRNO")] public string TrNo { get; set; } = "";
        [Column("ACCNO")] public string AccNo { get; set; } = "";
        [Column("DEPT")] public string Dept { get; set; } = "";
        [Column("INDENTNO")] public string IndentNo { get; set; } = "";
        [Column("REMARKS")] public string Remarks { get; set; } = "";
        [Column("AMOUNT")] public decimal Amount { get; set; }
        [Column("USERID")] public string UserId { get; set; } = "";
        [Column("UDATE")] public DateTime? UDate { get; set; }
        [Column("UTIME")] public string UTime { get; set; } = "";
    }

    [Table("CPYMT")]
    public class CashPayment
    {
        [Column("CCODE")] public string CCode { get; set; } = "";
        [Column("LCODE")] public int LCode { get; set; }
        [Key] [Column("TRNO")] public string TrNo { get; set; } = "";
        [Column("TRDATE")] public DateTime TrDate { get; set; }
        [Column("TRTIME")] public string TrTime { get; set; } = "";
        [Column("TYPE")] public string Type { get; set; } = "";
        [Column("REGNO")] public int? RegNo { get; set; }
        [Column("ECODE")] public string ECode { get; set; } = "";
        [Column("SETTLE")] public bool Settle { get; set; }
        [Column("REFERENCE")] public string Reference { get; set; } = "";
        [Column("CDISCOUNT")] public decimal CDisc { get; set; }
        [Column("DISCBY")] public string DiscBy { get; set; } = "";
        [Column("TOTALAMT")] public decimal TotalAmt { get; set; }
        [Column("USERID")] public string UserId { get; set; } = "";
        [Column("UDATE")] public DateTime? UDate { get; set; }
        [Column("UTIME")] public string UTime { get; set; } = "";

        public List<CashPaymentDetail> Details { get; set; } = new();
    }

    [Table("CPYMtdet")]
    public class CashPaymentDetail
    {
        [Column("TRNO")] public string TrNo { get; set; } = "";
        [Column("ACCNO")] public string AccNo { get; set; } = "";
        [Column("DEPT")] public string Dept { get; set; } = "";
        [Column("INDENTNO")] public string IndentNo { get; set; } = "";
        [Column("REMARKS")] public string Remarks { get; set; } = "";
        [Column("AMOUNT")] public decimal Amount { get; set; }
        [Column("USERID")] public string UserId { get; set; } = "";
        [Column("UDATE")] public DateTime? UDate { get; set; }
        [Column("UTIME")] public string UTime { get; set; } = "";
    }

    [Table("CHIST")]
    public class CashHistory
    {
        [Key] [Column("VOUNO")] public string VouNo { get; set; } = "";
        [Column("VOUDATE")] public DateTime VouDate { get; set; }
        [Column("VOUTIME")] public string VouTime { get; set; } = "";
        [Column("TRTYPE")] public string TrType { get; set; } = "";   // R receipt / P payment
        [Column("TRNO")] public string TrNo { get; set; } = "";
        [Column("TRDATE")] public DateTime TrDate { get; set; }
        [Column("TRTIME")] public string TrTime { get; set; } = "";
        [Column("TYPE")] public string Type { get; set; } = "";
        [Column("REGNO")] public int? RegNo { get; set; }
        [Column("ECODE")] public string ECode { get; set; } = "";
        [Column("SETTLE")] public bool Settle { get; set; }
        [Column("REFERENCE")] public string Reference { get; set; } = "";
        [Column("MODE")] public int Mode { get; set; }
        [Column("CARDTYPE")] public int CardType { get; set; }
        [Column("CARDNAME")] public string CardName { get; set; } = "";
        [Column("CARDNO")] public string CardNo { get; set; } = "";
        [Column("CARDVALID")] public DateTime? CardValid { get; set; }
        [Column("CARDAPPRNO")] public string CardApprNo { get; set; } = "";
        [Column("CDISCOUNT")] public decimal CDisc { get; set; }
        [Column("DISCBY")] public string DiscBy { get; set; } = "";
        [Column("TOTALAMT")] public decimal TotalAmt { get; set; }
        [Column("USERID")] public string UserId { get; set; } = "";
        [Column("UDATE")] public DateTime? UDate { get; set; }
        [Column("UTIME")] public string UTime { get; set; } = "";

        public List<CashHistoryDetail> Details { get; set; } = new();
    }

    [Table("CHISTDET")]
    public class CashHistoryDetail
    {
        [Column("TRTYPE")] public string TrType { get; set; } = "";
        [Column("TRNO")] public string TrNo { get; set; } = "";
        [Column("ACCNO")] public string AccNo { get; set; } = "";
        [Column("DEPT")] public string Dept { get; set; } = "";
        [Column("INDENTNO")] public string IndentNo { get; set; } = "";
        [Column("REMARKS")] public string Remarks { get; set; } = "";
        [Column("AMOUNT")] public decimal Amount { get; set; }
        [Column("USERID")] public string UserId { get; set; } = "";
        [Column("UDATE")] public DateTime? UDate { get; set; }
        [Column("UTIME")] public string UTime { get; set; } = "";
    }
}
