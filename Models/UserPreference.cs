using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("USERPREFERENCE")]
    public class UserPreference
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long? WebUserId { get; set; }
        public long? DepartmentId { get; set; }
        public long? InstitutionId { get; set; }

        public string AbbreviationForHistory { get; set; }
        public string AbbreviationForExamination { get; set; }
        public string AbbreviationForInvestigations { get; set; }
        public string AbbreviationForTreatments { get; set; }
        public string AbbreviationForManagement { get; set; }

        public string PharmacyRetailBillTemplate { get; set; }
        public string PharmacyWholesaleBillTemplate { get; set; }

        public string InwardDepositBillTemplate { get; set; }
        public string InwardDepositCancelBillTemplate { get; set; }

        public string ChannellingBillTemplate { get; set; }
        public string ChannellingCancellationBillTemplate { get; set; }
        public string ChannelingDoctorPaymentBillTemplate { get; set; }

        public string OpdBillForCashierTemplate { get; set; }
        public string SmsTemplateForOpdBillSetting { get; set; }

        public bool InstitutionSpecificItems { get; set; }

        public string OpdBillTemplate { get; set; }
        public bool InstitutionRestrictedBilling { get; set; }
        public bool OpdSettleWithoutReferralDetails { get; set; }
        public bool PartialPaymentOfOpdBillsAllowed { get; set; }
        public bool PartialPaymentOfOpdPreBillsAllowed { get; set; }
        public bool PaymentMethodAllowedInInwardMatrix { get; set; }
        public bool PharmacyBillPrabodha { get; set; }
        public bool CheckPaymentSchemeValidation { get; set; }
        public bool GrnBillDetailed { get; set; }
        public bool BhtNumberWithYear { get; set; }
        public bool BhtNumberWithOutAdmissionType { get; set; }
        public bool DepNumGenFromToDepartment { get; set; }
        public bool TranferNetTotalbyRetailRate { get; set; }
        public bool AllowtoChangePaymentMethodDuringPayment { get; set; }
        public bool OpdSettleWithoutCashTendered { get; set; }
        public bool ChannelWithOutReferenceNumber { get; set; }
        public bool PharmayPurchaseWithLastRate { get; set; }
        public bool InwardAddServiceBillTimeCheck { get; set; }
        public bool InwardMoChargeCalculateInitialTime { get; set; }
        public bool InwardChangeAdmissionFee { get; set; }
        public bool PharmacyBillWithOutItem { get; set; }
        public bool FiveFivePaperWithHeadings { get; set; }
        public bool ShowOnlyMarkedDoctors { get; set; }
        public bool ChannelSettleWithoutPatientPhoneNumber { get; set; }
        public bool OpdSettleWithoutPatientPhoneNumber { get; set; }
        public bool ChannelBillDouble { get; set; }
        public bool HasAwebsiteAsFrontEnd { get; set; }
        public string ThemeName { get; set; }
        public string LogoUrl { get; set; }
        public string LoggingHeader { get; set; }
        public string LoggingText { get; set; }
        public bool ChannelDoctorArivalMsgSend { get; set; }
        public string MicroBiologyFont { get; set; }
        public string LogoName { get; set; }

        public PaperType? OpdBillPaperType { get; set; }
        public PaperType? InwardServiceBillPaperType { get; set; }
        public PaperType? PharmacyBillPaperType { get; set; }
        public PaperType? ChannelBillPaperType { get; set; }
        public PaperType? InwardDepositPaymentBillPaper { get; set; }

        public bool PartialPaymentOfPharmacyBillsAllowed { get; set; }

        public ApplicationInstitution? ApplicationInstitution { get; set; }
        public PaymentMethod? ChannellingPaymentMethod { get; set; }

        public bool? CanSettleOpdBillWithInvestigationsWithoutReferringDoctor { get; set; }
        public bool? PrintBarcodeInOpdBill { get; set; }
        public bool? SentEmailWithInvestigationReportApproval { get; set; }
        public bool? SentSmsWithInvestigationRequestApproval { get; set; }
        public bool? SentDailySmsSummeryForReferringDoctors { get; set; }

        public string SmsUrl { get; set; }
        public string SmsUsername { get; set; }
        public string SmsPassword { get; set; }
        public string SmsUserAlias { get; set; }
        public string SmsUsernameParameterName { get; set; }
        public string SmsPasswordParameterName { get; set; }
        public string SmsUserAliasParameterName { get; set; }
        public string SmsPhoneNumberParameterName { get; set; }
        public string SmsMessageParameterName { get; set; }

        public RestAuthenticationType? SmsAuthenticationType { get; set; }

        public bool FamilyMembership { get; set; }
        public bool MembershipExpires { get; set; }

        public bool NeedAreaForPatientRegistration { get; set; }
        public bool NeedNicForPatientRegistration { get; set; }
        public bool NeedPhoneNumberForPatientRegistration { get; set; }

        public bool SendBulkSms { get; set; }
        public string PharmacyBillFooter { get; set; }
        public string PharmacyBillHeader { get; set; }
        public string LongDateFormat { get; set; }
        public string ShortDateFormat { get; set; }
        public string LongDateTimeFormat { get; set; }
        public string ShortDateTimeFormat { get; set; }
        public string LongTimeFormat { get; set; }
        public string ShortTimeFormat { get; set; }
        public string EncrptionKey { get; set; }

        public string NameRegex { get; set; }
        public string MobileRegex { get; set; }
        public string EmailRegex { get; set; }
        public string NicRegex { get; set; }

        public string LengthOfOTPIndexes { get; set; }

        public ItemListingStrategy? OpdItemListingStrategy { get; set; }
        public ItemListingStrategy? CcItemListingStrategy { get; set; }
        public OpdBillingStrategy? OpdBillingStrategy { get; set; }
        public ItemListingStrategy? InwardItemListingStrategy { get; set; }
        public OpdTokenNumberGenerationStrategy? OpdTokenNumberGenerationStrategy { get; set; }
        public bool PrintOpdTokenNumber { get; set; }

        public bool AutodisplayMenu { get; set; }
        public bool ShowBillWiseDetails { get; set; }
        public string InpatientFinalBillPrintHeader { get; set; }
        public string ChangeTextCasesPatientName { get; set; }

        public bool SendSmsOnChannelBooking { get; set; }
        public bool SendSmsOnChannelBookingCancellation { get; set; }
        public bool SendSmsOnChannelDoctorArrival { get; set; }
        public bool SendSmsOnChannelBookingCompletion { get; set; }
        public bool SendSmsOnChannelBookingNoShow { get; set; }
        public bool SendSmsOnChannelBookingDocterPayment { get; set; }

        public string SmsTemplateForChannelBooking { get; set; }
        public string SmsTemplateForChannelBookingCancellation { get; set; }
        public string SmsTemplateForChannelDoctorArrival { get; set; }
        public string SmsTemplateForChannelBookingCompletion { get; set; }
        public string SmsTemplateForChannelBookingNoShow { get; set; }
        public string SmsTemplateForChannelBookingDoctorPayment { get; set; }

        public bool OpdSettleWithoutPatientArea { get; set; }
        public bool OpdBillingAftershiftStart { get; set; }

        public int? NumberOfOPDBillCopies { get; set; }
        public int? NumberOfCCBillCopies { get; set; }
        public int? NumberOfChannellingBillCopies { get; set; }

        public string InwardAddmissionStatemenetEnglish { get; set; }
        public string InwardAddmissionStatemenetSinhala { get; set; }

        public string ChannelingBillHeaderTemplate { get; set; }

        public bool ShowBarCodeInChannelBookingBill { get; set; }
        public bool GenarateOpdTokenNumbersToCounterWise { get; set; }

        [ForeignKey("WebUserId")]
        public virtual WebUser WebUser { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }

        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }
    }
}
