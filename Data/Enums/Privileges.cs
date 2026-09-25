namespace HmisWeb.Data.Enums
{
    [Flags]
    public enum Privileges
    {
        None = 0,
        // Admin
        Administration = 1,
        Application = 2,
        // User
        UserManagement = 4,
        PrivilegeManagement = 8,
        RoleManagement = 16,
        // Department
        DepartmentManagement = 32,
        DepartmentAdding = 64,
        // Institution
        InstitutionManagement = 128,
        InstitutionAdding = 256,
        // Staff
        StaffManagement = 512,
        StaffAdding = 1024,
        // Patient
        PatientRegistration = 2048,
        PatientManagement = 4096,
        // OPD
        OpdBilling = 8192,
        OpdPreBilledBill = 16384,
        OpdBilledBill = 32768,
        OpdCancelBilledBill = 65536,
        OpdSettleBilledBill = 131072,
        OpdGrantDiscount = 262144,
        OpdReturnBilledBill = 524288,
        OpdBilledBillReport = 1048576,
        // Channel
        ChannelBilling = 2097152,
        ChannelBookManagement = 4194304,
        ChannelScheduleManagement = 8388608,
        // Inward
        InwardAdmission = 16777216,
        InwardBilling = 33554432,
        InwardManagement = 67108864,
        InwardFinalBill = 134217728,
        InwardProfessionalBill = 268435456,
        // Pharmacy
        PharmacyBilling = 536870912,
        PharmacyManagement = 1073741824,
    }
}
