using Microsoft.EntityFrameworkCore;
using HmisWeb.Models;
using HMISRoute = HmisWeb.Models.Route;

namespace HmisWeb.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Person> Persons { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Institution> Institutions { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Consultant> Consultants { get; set; }
        public DbSet<WebUser> WebUsers { get; set; }
        public DbSet<WebUserRole> WebUserRoles { get; set; }
        public DbSet<WebUserRoleUser> WebUserRoleUsers { get; set; }
        public DbSet<WebUserPrivilege> WebUserPrivileges { get; set; }
        public DbSet<WebUserDepartment> WebUserDepartments { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<BillItem> BillItems { get; set; }
        public DbSet<BillFee> BillFees { get; set; }
        public DbSet<BillSession> BillSessions { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PriceMatrix> PriceMatrices { get; set; }
        public DbSet<Fee> Fees { get; set; }
        public DbSet<Area> Areas { get; set; }
        public DbSet<HMISRoute> Routes { get; set; }
        public DbSet<Speciality> Specialities { get; set; }
        public DbSet<MeasurementUnit> MeasurementUnits { get; set; }
        public DbSet<MembershipScheme> MembershipSchemes { get; set; }
        public DbSet<PaymentScheme> PaymentSchemes { get; set; }
        public DbSet<PatientEncounter> PatientEncounters { get; set; }
        public DbSet<PatientRoom> PatientRooms { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<RoomCategory> RoomCategories { get; set; }
        public DbSet<Admission> Admissions { get; set; }
        public DbSet<CashBook> CashBooks { get; set; }
        public DbSet<CashBookEntry> CashBookEntries { get; set; }
        public DbSet<CashTransaction> CashTransactions { get; set; }
        public DbSet<Drawer> Drawers { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }
        public DbSet<Logins> Logins { get; set; }
        public DbSet<ConfigOption> ConfigOptions { get; set; }
        public DbSet<Vtm> Vtms { get; set; }
        public DbSet<Vmp> Vmps { get; set; }
        public DbSet<Vmpp> Vmpps { get; set; }
        public DbSet<Amp> Atps { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<ItemBatch> ItemBatches { get; set; }
        public DbSet<PharmaceuticalBillItem> PharmaceuticalBillItems { get; set; }
        public DbSet<Machine> Machines { get; set; }
        public DbSet<Roster> Rosters { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<StaffSalary> StaffSalaries { get; set; }
        public DbSet<StaffCategory> StaffCategories { get; set; }
        public DbSet<Designation> Designations { get; set; }
        public DbSet<Grade> Grades { get; set; }

        // ===== Entities added during full entity migration =====
        public DbSet<AgentHistory> AgentHistories { get; set; }
        public DbSet<AppEmail> AppEmails { get; set; }
        public DbSet<AuditEvent> AuditEvents { get; set; }
        public DbSet<BillComponent> BillComponents { get; set; }
        public DbSet<BillEntry> BillEntries { get; set; }
        public DbSet<BillFeePayment> BillFeePayments { get; set; }
        public DbSet<BillNumber> BillNumbers { get; set; }
        public DbSet<CategoryItem> CategoryItems { get; set; }
        public DbSet<EncounterCreditCompany> EncounterCreditCompanies { get; set; }
        public DbSet<Family> Families { get; set; }
        public DbSet<FamilyMember> FamilyMembers { get; set; }
        public DbSet<FeeChange> FeeChanges { get; set; }
        public DbSet<FeeValue> FeeValues { get; set; }
        public DbSet<Form> Forms { get; set; }
        public DbSet<IssueRateMargins> IssueRateMargins { get; set; }
        public DbSet<ItemMapping> ItemMappings { get; set; }
        public DbSet<ItemPackage> ItemPackages { get; set; }
        public DbSet<ItemsCategories> ItemsCategories { get; set; }
        public DbSet<Mapping> Mappings { get; set; }
        public DbSet<MedicalPackageItem> MedicalPackageItems { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<PackageItem> PackageItems { get; set; }
        public DbSet<PatientDeposit> PatientDeposits { get; set; }
        public DbSet<PatientDepositHistory> PatientDepositHistories { get; set; }
        public DbSet<PatientFlag> PatientFlags { get; set; }
        public DbSet<PatientItem> PatientItems { get; set; }
        public DbSet<PaymentGatewayTransaction> PaymentGatewayTransactions { get; set; }
        public DbSet<PersonInstitution> PersonInstitutions { get; set; }
        public DbSet<ReportTemplate> ReportTemplates { get; set; }
        public DbSet<ServiceSessionInstance> ServiceSessionInstances { get; set; }
        public DbSet<Sms> Sms { get; set; }
        public DbSet<Token> Tokens { get; set; }
        public DbSet<TriggerSubscription> TriggerSubscriptions { get; set; }
        public DbSet<Upload> Uploads { get; set; }
        public DbSet<UserIcon> UserIcons { get; set; }
        public DbSet<UserNotification> UserNotifications { get; set; }
        public DbSet<UserPreference> UserPreferences { get; set; }
        public DbSet<WebContent> WebContents { get; set; }
        public DbSet<WebLanguage> WebLanguages { get; set; }
        public DbSet<WebTheme> WebThemes { get; set; }
public DbSet<WebUserDashboard> WebUserDashboards { get; set; }
        public DbSet<WebUserPaymentScheme> WebUserPaymentSchemes { get; set; }
        public DbSet<WebUserRolePrivilege> WebUserRolePrivileges { get; set; }
        public DbSet<OpdSession> OpdSessions { get; set; }
public DbSet<ConsultantSettlement> ConsultantSettlements { get; set; }
        public DbSet<CompanyServiceRate> CompanyServiceRates { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<ResourceServiceRate> ResourceServiceRates { get; set; }
        public DbSet<ConsultantServiceRate> ConsultantServiceRates { get; set; }
        public DbSet<CompanyEmployee> CompanyEmployees { get; set; }
        public DbSet<RefundRequest> RefundRequests { get; set; }
        public DbSet<MiscFee> MiscFees { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<InvestigationOrder> InvestigationOrders { get; set; }
        public DbSet<UsService> UsServices { get; set; }
        public DbSet<UsServiceCategory> UsServiceCategories { get; set; }
        public DbSet<UsOrder> UsOrders { get; set; }
        public DbSet<UsReportTemplate> UsReportTemplates { get; set; }
        public DbSet<MedicalTerm> MedicalTerms { get; set; }
        public DbSet<XrService> XrServices { get; set; }
        public DbSet<XrServiceCategory> XrServiceCategories { get; set; }
        public DbSet<XrOrder> XrOrders { get; set; }
        public DbSet<XrReportTemplate> XrReportTemplates { get; set; }
        public DbSet<XrMedicalTerm> XrMedicalTerms { get; set; }
        public DbSet<PanelServiceCharge> PanelServiceCharges { get; set; }
        public DbSet<PanelDepartment> PanelDepartments { get; set; }
        public DbSet<TestData> TestData { get; set; }
        public DbSet<TestRemark> TestRemarks { get; set; }
        public DbSet<GrowthDescription> GrowthDescriptions { get; set; }
        public DbSet<Organism> Organisms { get; set; }
        public DbSet<FollowUp> FollowUps { get; set; }
        public DbSet<WebUserRoute> WebUserRoutes { get; set; }
        public DbSet<PatientOwner> PatientOwners { get; set; }
        public DbSet<ConsultantCommission> ConsultantCommissions { get; set; }
        public DbSet<UnsettleRequest> UnsettleRequests { get; set; }

        // ===== Legacy billing tables (migrated VFP DBFs - additive mapping only) =====
        public DbSet<HmisWeb.Models.Legacy.RegisterLegacy> LegacyRegisters { get; set; }
        public DbSet<HmisWeb.Models.Legacy.IbilMst> IbilMsts { get; set; }
        public DbSet<HmisWeb.Models.Legacy.IbilDet> IbilDets { get; set; }
        public DbSet<HmisWeb.Models.Legacy.IpdBillHeader> IpdBillHeaders { get; set; }
        public DbSet<HmisWeb.Models.Legacy.Bcontrol> Bcontrols { get; set; }
        public DbSet<HmisWeb.Models.Legacy.IpdBillAudit> IpdBillAudits { get; set; }
        public DbSet<HmisWeb.Models.Legacy.LegacyDepartment> LegacyDepartments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== TPH inheritance =====
            modelBuilder.Entity<Department>()
                .HasDiscriminator<string>("DTYPE")
                .HasValue<Department>("Department");

            modelBuilder.Entity<Item>()
                .HasDiscriminator<string>("DTYPE")
                .HasValue<Item>("Item")
                .HasValue<Service>("Service")
                .HasValue<BillExpense>("BillExpense")
                .HasValue<CashierItem>("CashierItem")
                .HasValue<MedicalPackage>("MedicalPackage")
                .HasValue<Packege>("Packege")
                .HasValue<ServiceSession>("ServiceSession")
                .HasValue<ServiceSessionLeave>("ServiceSessionLeave");

            modelBuilder.Entity<Category>()
                .HasDiscriminator<string>("DTYPE")
                .HasValue<Category>("Category")
                .HasValue<FormFormat>("FormFormat")
                .HasValue<InventoryCategory>("InventoryCategory")
                .HasValue<MetadataCategory>("MetadataCategory")
                .HasValue<MetadataSuperCategory>("MetadataSuperCategory")
                .HasValue<Nationality>("Nationality")
                .HasValue<Relation>("Relation")
                .HasValue<Religion>("Religion")
                .HasValue<ServiceCategory>("ServiceCategory")
                .HasValue<ServiceSubCategory>("ServiceSubCategory")
                .HasValue<SessionNumberGenerator>("SessionNumberGenerator")
                .HasValue<Vocabulary>("Vocabulary");

            modelBuilder.Entity<Bill>()
                .HasDiscriminator<string>("DTYPE")
                .HasValue<Bill>("Bill");

            modelBuilder.Entity<Staff>()
                .HasDiscriminator<string>("DTYPE")
                .HasValue<Staff>("Staff")
                .HasValue<Doctor>("Doctor")
                .HasValue<Consultant>("Consultant");

            modelBuilder.Entity<Fee>()
                .HasDiscriminator<string>("DTYPE")
                .HasValue<Fee>("Fee")
                .HasValue<ItemFee>("ItemFee")
                .HasValue<MedicalPackageFee>("MedicalPackageFee")
                .HasValue<PackageFee>("PackageFee");

            modelBuilder.Entity<PatientEncounter>()
                .HasDiscriminator<string>("DTYPE")
                .HasValue<PatientEncounter>("PatientEncounter")
                .HasValue<Appointment>("Appointment")
                .HasValue<Channeling>("Channeling")
                .HasValue<OpdVisit>("OpdVisit");

            modelBuilder.Entity<Speciality>()
                .HasDiscriminator<string>("DTYPE")
                .HasValue<Speciality>("Speciality")
                .HasValue<DoctorSpeciality>("DoctorSpeciality");

            // ===== Person =====
            modelBuilder.Entity<Person>()
                .HasOne(p => p.Creater).WithMany().HasForeignKey(p => p.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Person>()
                .HasOne(p => p.Editer).WithMany().HasForeignKey(p => p.EditerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Person>()
                .HasOne(p => p.Retirer).WithMany().HasForeignKey(p => p.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Person>()
                .HasOne(p => p.Area).WithMany().HasForeignKey(p => p.AreaId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Person>()
                .HasOne(p => p.Institution).WithMany().HasForeignKey(p => p.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Person>()
                .HasOne(p => p.Department).WithMany().HasForeignKey(p => p.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Person>()
                .HasOne(p => p.CivilStatus).WithMany().HasForeignKey(p => p.CivilStatusId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Person>()
                .HasOne(p => p.Race).WithMany().HasForeignKey(p => p.RaceId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Person>()
                .HasOne(p => p.BloodGroup).WithMany().HasForeignKey(p => p.BloodGroupId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Person>()
                .HasOne(p => p.Occupation).WithMany().HasForeignKey(p => p.OccupationId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Person>()
                .HasOne(p => p.Religion).WithMany().HasForeignKey(p => p.ReligionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Person>()
                .HasOne(p => p.MembershipScheme).WithMany().HasForeignKey(p => p.MembershipSchemeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Person>()
                .HasOne(p => p.WebUser)
                .WithOne(w => w.WebUserPerson)
                .HasForeignKey<WebUser>(w => w.WebUserPersonId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== WebUser =====
            modelBuilder.Entity<WebUser>()
                .HasOne(w => w.Institution).WithMany().HasForeignKey(w => w.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUser>()
                .HasOne(w => w.Department).WithMany().HasForeignKey(w => w.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUser>()
                .HasOne(w => w.Staff).WithMany().HasForeignKey(w => w.StaffId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUser>()
                .HasOne(w => w.WebUserRole).WithMany().HasForeignKey(w => w.WebUserRoleUserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUser>()
                .HasOne(w => w.Creater).WithMany().HasForeignKey(w => w.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUser>()
                .HasOne(w => w.Retirer).WithMany().HasForeignKey(w => w.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUser>()
                .HasOne(w => w.Activator).WithMany().HasForeignKey(w => w.ActivatorId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUser>()
                .HasOne(w => w.SelfRole).WithMany().HasForeignKey(w => w.SelfRoleUserId).OnDelete(DeleteBehavior.Restrict);

            // ===== WebUserRole =====
            modelBuilder.Entity<WebUserRole>()
                .HasOne(r => r.Creater).WithMany().HasForeignKey(r => r.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUserRole>()
                .HasOne(r => r.Retirer).WithMany().HasForeignKey(r => r.RetirerId).OnDelete(DeleteBehavior.Restrict);

            // ===== WebUserRoleUser =====
            modelBuilder.Entity<WebUserRoleUser>()
                .HasOne(x => x.WebUserRole).WithMany().HasForeignKey(x => x.WebUserRoleId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUserRoleUser>()
                .HasOne(x => x.WebUser).WithMany().HasForeignKey(x => x.WebUserId).OnDelete(DeleteBehavior.Restrict);

            // ===== WebUserPrivilege =====
            modelBuilder.Entity<WebUserPrivilege>()
                .HasOne(x => x.WebUserRole).WithMany().HasForeignKey(x => x.WebUserRoleId).OnDelete(DeleteBehavior.Restrict);

            // ===== WebUserDepartment =====
            modelBuilder.Entity<WebUserDepartment>()
                .HasOne(x => x.WebUser).WithMany().HasForeignKey(x => x.WebUserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUserDepartment>()
                .HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);

            // ===== Institution =====
            modelBuilder.Entity<Institution>()
                .HasOne(i => i.Parent).WithMany().HasForeignKey(i => i.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Institution>()
                .HasOne(i => i.BankBranch).WithMany().HasForeignKey(i => i.BankBranchId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Institution>()
                .HasOne(i => i.ParentInstitution).WithMany().HasForeignKey(i => i.ParentInstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Institution>()
                .HasOne(i => i.LabInstitution).WithMany().HasForeignKey(i => i.LabInstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Institution>()
                .HasOne(i => i.ContactPerson).WithMany().HasForeignKey(i => i.ContactPersonId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Institution>()
                .HasOne(i => i.LabDepartment).WithMany().HasForeignKey(i => i.LabDepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Institution>()
                .HasOne(i => i.FeeListType).WithMany().HasForeignKey(i => i.FeeListTypeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Institution>()
                .HasOne(i => i.Route).WithMany().HasForeignKey(i => i.RouteId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Institution>()
                .HasOne(i => i.Creater).WithMany().HasForeignKey(i => i.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Institution>()
                .HasOne(i => i.Retirer).WithMany().HasForeignKey(i => i.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Institution>()
                .HasOne(i => i.Editer).WithMany().HasForeignKey(i => i.EditerId).OnDelete(DeleteBehavior.Restrict);

            // ===== Department =====
            modelBuilder.Entity<Department>()
                .HasOne(d => d.Institution).WithMany().HasForeignKey(d => d.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Department>()
                .HasOne(d => d.Site).WithMany().HasForeignKey(d => d.SiteId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Department>()
                .HasOne(d => d.LabInstitution).WithMany().HasForeignKey(d => d.LabInstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Department>()
                .HasOne(d => d.SuperDepartment).WithMany().HasForeignKey(d => d.SuperDepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Department>()
                .HasOne(d => d.SampleDepartment).WithMany().HasForeignKey(d => d.SampleDepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Department>()
                .HasOne(d => d.LabDepartment).WithMany().HasForeignKey(d => d.LabDepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Department>()
                .HasOne(d => d.Creater).WithMany().HasForeignKey(d => d.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Department>()
                .HasOne(d => d.Retirer).WithMany().HasForeignKey(d => d.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Department>()
                .HasOne(d => d.Editer).WithMany().HasForeignKey(d => d.EditerId).OnDelete(DeleteBehavior.Restrict);

            // ===== Item (TPH with Service) =====
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Category).WithMany().HasForeignKey(i => i.CategoryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .HasOne(i => i.FinancialCategory).WithMany().HasForeignKey(i => i.FinancialCategoryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Institution).WithMany().HasForeignKey(i => i.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Department).WithMany().HasForeignKey(i => i.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Speciality).WithMany().HasForeignKey(i => i.SpecialityId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Staff).WithMany().HasForeignKey(i => i.StaffId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .HasOne(i => i.ForInstitution).WithMany().HasForeignKey(i => i.ForInstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .HasOne(i => i.ForDepartment).WithMany().HasForeignKey(i => i.ForDepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .HasOne(i => i.BilledAs).WithMany().HasForeignKey(i => i.BilledAsId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .HasOne(i => i.ReportedAs).WithMany().HasForeignKey(i => i.ReportedAsId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .HasOne(i => i.MasterItemReference).WithMany().HasForeignKey(i => i.MasterItemReferenceId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .HasOne(i => i.ParentItem).WithMany().HasForeignKey(i => i.ParentItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .HasOne(i => i.MeasurementUnit).WithMany().HasForeignKey(i => i.MeasurementUnitId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Worksheet).WithMany().HasForeignKey(i => i.WorksheetId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .HasOne(i => i.ReportFormat).WithMany().HasForeignKey(i => i.ReportFormatId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Manufacturer).WithMany().HasForeignKey(i => i.ManufacturerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Importer).WithMany().HasForeignKey(i => i.ImporterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Vmp).WithMany().HasForeignKey(i => i.VmpId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Machine).WithMany().HasForeignKey(i => i.MachineId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .HasOne(i => i.StrengthUnit).WithMany().HasForeignKey(i => i.StrengthUnitId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .HasOne(i => i.BaseUnit).WithMany().HasForeignKey(i => i.BaseUnitId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .HasOne(i => i.IssueUnit).WithMany().HasForeignKey(i => i.IssueUnitId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Creater).WithMany().HasForeignKey(i => i.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Retirer).WithMany().HasForeignKey(i => i.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Editer).WithMany().HasForeignKey(i => i.EditerId).OnDelete(DeleteBehavior.Restrict);

            // ===== Category =====
            modelBuilder.Entity<Category>()
                .HasOne(c => c.ParentCategory).WithMany().HasForeignKey(c => c.ParentCategoryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Category>()
                .HasOne(c => c.PaymentScheme).WithMany().HasForeignKey(c => c.PaymentSchemeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Institution).WithMany().HasForeignKey(c => c.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Creater).WithMany().HasForeignKey(c => c.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Retirer).WithMany().HasForeignKey(c => c.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Editer).WithMany().HasForeignKey(c => c.EditerId).OnDelete(DeleteBehavior.Restrict);

            // ===== Bill =====
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.Item).WithMany().HasForeignKey(b => b.ItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.Patient).WithMany().HasForeignKey(b => b.PatientId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.Person).WithMany().HasForeignKey(b => b.PersonId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.Staff).WithMany().HasForeignKey(b => b.StaffId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.Institution).WithMany().HasForeignKey(b => b.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.Department).WithMany().HasForeignKey(b => b.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.Creater).WithMany().HasForeignKey(b => b.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.Editor).WithMany().HasForeignKey(b => b.EditorId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.CheckedBy).WithMany().HasForeignKey(b => b.CheckedById).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.Retirer).WithMany().HasForeignKey(b => b.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.BilledBill).WithMany().HasForeignKey(b => b.BilledBillId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.CancelledBill).WithMany().HasForeignKey(b => b.CancelledBillId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.RefundedBill).WithMany().HasForeignKey(b => b.RefundedBillId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.ReferenceBill).WithMany().HasForeignKey(b => b.ReferenceBillId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.ForwardReferenceBill).WithMany().HasForeignKey(b => b.ForwardReferenceBillId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.BackwardReferenceBill).WithMany().HasForeignKey(b => b.BackwardReferenceBillId).OnDelete(DeleteBehavior.Restrict);
modelBuilder.Entity<Bill>()
                .HasOne(b => b.PaidBill).WithMany().HasForeignKey(b => b.PaidBillId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.ConsultantSettlement).WithMany().HasForeignKey(b => b.ConsultantSettlementId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.FromInstitution).WithMany().HasForeignKey(b => b.FromInstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.ToInstitution).WithMany().HasForeignKey(b => b.ToInstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.CreditCompany).WithMany().HasForeignKey(b => b.CreditCompanyId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.FromDepartment).WithMany().HasForeignKey(b => b.FromDepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.ToDepartment).WithMany().HasForeignKey(b => b.ToDepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.PatientEncounter).WithMany().HasForeignKey(b => b.PatientEncounterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.PaymentScheme).WithMany().HasForeignKey(b => b.PaymentSchemeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.CollectingCentre).WithMany().HasForeignKey(b => b.CollectingCentreId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.PaymentSchemeInstitution).WithMany().HasForeignKey(b => b.PaymentSchemeInstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.ReferredByInstitution).WithMany().HasForeignKey(b => b.ReferredByInstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.ReferenceInstitution).WithMany().HasForeignKey(b => b.ReferenceInstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.CashTransaction).WithMany().HasForeignKey(b => b.CashTransactionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.ReferredBy).WithMany().HasForeignKey(b => b.ReferredById).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.MembershipScheme).WithMany().HasForeignKey(b => b.MembershipSchemeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.Category).WithMany().HasForeignKey(b => b.CategoryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.Bank).WithMany().HasForeignKey(b => b.BankId).OnDelete(DeleteBehavior.Restrict);

            // ===== BillItem =====
            modelBuilder.Entity<BillItem>()
                .HasOne(bi => bi.Bill).WithMany(b => b.BillItems).HasForeignKey(bi => bi.BillId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillItem>()
                .HasOne(bi => bi.ExpenseBill).WithMany().HasForeignKey(bi => bi.ExpenseBillId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillItem>()
                .HasOne(bi => bi.Item).WithMany().HasForeignKey(bi => bi.ItemId1).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillItem>()
                .HasOne(bi => bi.ParentBillItem).WithMany(bi => bi.ChiledBillItems).HasForeignKey(bi => bi.ParentBillItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillItem>()
                .HasOne(bi => bi.ReferanceBillItem).WithMany().HasForeignKey(bi => bi.ReferanceBillItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillItem>()
                .HasOne(bi => bi.PaidForBillFee).WithMany().HasForeignKey(bi => bi.PaidForBillFeeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillItem>()
                .HasOne(bi => bi.PatientEncounter).WithMany().HasForeignKey(bi => bi.PatientEncounterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillItem>()
                .HasOne(bi => bi.ReferenceBill).WithMany().HasForeignKey(bi => bi.ReferenceBillId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillItem>()
                .HasOne(bi => bi.PriceMatrix).WithMany().HasForeignKey(bi => bi.PriceMatrixId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillItem>()
                .HasOne(bi => bi.Creater).WithMany().HasForeignKey(bi => bi.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillItem>()
                .HasOne(bi => bi.Editor).WithMany().HasForeignKey(bi => bi.EditorId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillItem>()
                .HasOne(bi => bi.Retirer).WithMany().HasForeignKey(bi => bi.RetirerId).OnDelete(DeleteBehavior.Restrict);

            // BillItem -> PharmaceuticalBillItem (1:1)
            modelBuilder.Entity<BillItem>()
                .HasOne(bi => bi.PharmaceuticalBillItem)
                .WithOne(p => p.BillItem)
                .HasForeignKey<PharmaceuticalBillItem>(p => p.BillItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // BillItem -> BillSession (1:1)
            modelBuilder.Entity<BillItem>()
                .HasOne(bi => bi.BillSession)
                .WithOne(bs => bs.BillItem)
                .HasForeignKey<BillSession>(bs => bs.BillItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== BillFee =====
            modelBuilder.Entity<BillFee>()
                .HasOne(bf => bf.Fee).WithMany().HasForeignKey(bf => bf.FeeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillFee>()
                .HasOne(bf => bf.Patient).WithMany().HasForeignKey(bf => bf.PatientId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillFee>()
                .HasOne(bf => bf.PatienEncounter).WithMany().HasForeignKey(bf => bf.PatienEncounterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillFee>()
                .HasOne(bf => bf.ChildEncounter).WithMany().HasForeignKey(bf => bf.ChildEncounterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillFee>()
                .HasOne(bf => bf.Staff).WithMany().HasForeignKey(bf => bf.StaffId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillFee>()
                .HasOne(bf => bf.Institution).WithMany().HasForeignKey(bf => bf.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillFee>()
                .HasOne(bf => bf.Department).WithMany().HasForeignKey(bf => bf.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillFee>()
                .HasOne(bf => bf.Speciality).WithMany().HasForeignKey(bf => bf.SpecialityId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillFee>()
                .HasOne(bf => bf.ReferenceBillFee).WithMany().HasForeignKey(bf => bf.ReferenceBillFeeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillFee>()
                .HasOne(bf => bf.BillItem).WithMany(bi => bi.BillFees).HasForeignKey(bf => bf.BillItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillFee>()
                .HasOne(bf => bf.ReferenceBillItem).WithMany(bi => bi.ProFees).HasForeignKey(bf => bf.ReferenceBillItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillFee>()
                .HasOne(bf => bf.Bill).WithMany(b => b.BillFees).HasForeignKey(bf => bf.BillId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillFee>()
                .HasOne(bf => bf.PriceMatrix).WithMany().HasForeignKey(bf => bf.PriceMatrixId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillFee>()
                .HasOne(bf => bf.ReferencePatientRoom).WithMany().HasForeignKey(bf => bf.ReferencePatientRoomId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillFee>()
                .HasOne(bf => bf.Creater).WithMany().HasForeignKey(bf => bf.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillFee>()
                .HasOne(bf => bf.Editor).WithMany().HasForeignKey(bf => bf.EditorId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillFee>()
                .HasOne(bf => bf.Retirer).WithMany().HasForeignKey(bf => bf.RetirerId).OnDelete(DeleteBehavior.Restrict);

            // ===== BillSession =====
            modelBuilder.Entity<BillSession>()
                .HasOne(bs => bs.Bill).WithMany().HasForeignKey(bs => bs.BillId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillSession>()
                .HasOne(bs => bs.Creater).WithMany().HasForeignKey(bs => bs.CreaterId).OnDelete(DeleteBehavior.Restrict);

            // ===== Payment =====
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Bill).WithMany(b => b.Payments).HasForeignKey(p => p.BillId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Bank).WithMany().HasForeignKey(p => p.BankId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Institution).WithMany().HasForeignKey(p => p.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Department).WithMany().HasForeignKey(p => p.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Realizer).WithMany().HasForeignKey(p => p.RealizerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Creater).WithMany().HasForeignKey(p => p.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Retirer).WithMany().HasForeignKey(p => p.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.HandoverCreatedBill).WithMany().HasForeignKey(p => p.HandoverCreatedBillId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.HandoverAcceptBill).WithMany().HasForeignKey(p => p.HandoverAcceptBillId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Cashbook).WithMany().HasForeignKey(p => p.CashbookId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.CashbookEntry).WithMany().HasForeignKey(p => p.CashbookEntryId).OnDelete(DeleteBehavior.Restrict);

            // ===== PriceMatrix =====
            modelBuilder.Entity<PriceMatrix>()
                .HasOne(pm => pm.Item).WithMany().HasForeignKey(pm => pm.ItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PriceMatrix>()
                .HasOne(pm => pm.Institution).WithMany().HasForeignKey(pm => pm.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PriceMatrix>()
                .HasOne(pm => pm.Department).WithMany().HasForeignKey(pm => pm.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PriceMatrix>()
                .HasOne(pm => pm.Creater).WithMany().HasForeignKey(pm => pm.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PriceMatrix>()
                .HasOne(pm => pm.Retirer).WithMany().HasForeignKey(pm => pm.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PriceMatrix>()
                .HasOne(pm => pm.Editer).WithMany().HasForeignKey(pm => pm.EditerId).OnDelete(DeleteBehavior.Restrict);

            // ===== Fee =====
            modelBuilder.Entity<Fee>()
                .HasOne(f => f.Item).WithMany().HasForeignKey(f => f.ItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Fee>()
                .HasOne(f => f.Institution).WithMany().HasForeignKey(f => f.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Fee>()
                .HasOne(f => f.Department).WithMany().HasForeignKey(f => f.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Fee>()
                .HasOne(f => f.Speciality).WithMany().HasForeignKey(f => f.SpecialityId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Fee>()
                .HasOne(f => f.Staff).WithMany().HasForeignKey(f => f.StaffId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Fee>()
                .HasOne(f => f.Creater).WithMany().HasForeignKey(f => f.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Fee>()
                .HasOne(f => f.Editer).WithMany().HasForeignKey(f => f.EditerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Fee>()
                .HasOne(f => f.Retirer).WithMany().HasForeignKey(f => f.RetirerId).OnDelete(DeleteBehavior.Restrict);

            // ===== Area =====
            modelBuilder.Entity<Area>()
                .HasOne(a => a.Institution).WithMany().HasForeignKey(a => a.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Area>()
                .HasOne(a => a.Creater).WithMany().HasForeignKey(a => a.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Area>()
                .HasOne(a => a.Retirer).WithMany().HasForeignKey(a => a.RetirerId).OnDelete(DeleteBehavior.Restrict);

            // ===== Route =====
            modelBuilder.Entity<HMISRoute>()
                .HasOne(r => r.Institution).WithMany().HasForeignKey(r => r.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<HMISRoute>()
                .HasOne(r => r.Creater).WithMany().HasForeignKey(r => r.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<HMISRoute>()
                .HasOne(r => r.Retirer).WithMany().HasForeignKey(r => r.RetirerId).OnDelete(DeleteBehavior.Restrict);

            // ===== Speciality =====
            modelBuilder.Entity<Speciality>()
                .HasOne(s => s.Institution).WithMany().HasForeignKey(s => s.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Speciality>()
                .HasOne(s => s.Creater).WithMany().HasForeignKey(s => s.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Speciality>()
                .HasOne(s => s.Retirer).WithMany().HasForeignKey(s => s.RetirerId).OnDelete(DeleteBehavior.Restrict);

            // ===== MeasurementUnit =====
            modelBuilder.Entity<MeasurementUnit>()
                .HasOne(m => m.Creater).WithMany().HasForeignKey(m => m.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MeasurementUnit>()
                .HasOne(m => m.Editer).WithMany().HasForeignKey(m => m.EditerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MeasurementUnit>()
                .HasOne(m => m.Retirer).WithMany().HasForeignKey(m => m.RetirerId).OnDelete(DeleteBehavior.Restrict);

            // ===== MembershipScheme =====
            modelBuilder.Entity<MembershipScheme>()
                .HasOne(m => m.Institution).WithMany().HasForeignKey(m => m.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MembershipScheme>()
                .HasOne(m => m.Creater).WithMany().HasForeignKey(m => m.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MembershipScheme>()
                .HasOne(m => m.Retirer).WithMany().HasForeignKey(m => m.RetirerId).OnDelete(DeleteBehavior.Restrict);

            // ===== PaymentScheme =====
            modelBuilder.Entity<PaymentScheme>()
                .HasOne(ps => ps.Institution).WithMany().HasForeignKey(ps => ps.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PaymentScheme>()
                .HasOne(ps => ps.Department).WithMany().HasForeignKey(ps => ps.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PaymentScheme>()
                .HasOne(ps => ps.Creater).WithMany().HasForeignKey(ps => ps.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PaymentScheme>()
                .HasOne(ps => ps.Retirer).WithMany().HasForeignKey(ps => ps.RetirerId).OnDelete(DeleteBehavior.Restrict);

            // ===== Patient =====
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.Person).WithMany().HasForeignKey(p => p.PersonId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.CreatedInstitution).WithMany().HasForeignKey(p => p.CreatedInstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.Creater).WithMany().HasForeignKey(p => p.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.Editer).WithMany().HasForeignKey(p => p.EditerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.Retirer).WithMany().HasForeignKey(p => p.RetirerId).OnDelete(DeleteBehavior.Restrict);

            // ===== PatientEncounter =====
            modelBuilder.Entity<PatientEncounter>()
                .HasOne(e => e.Patient).WithMany().HasForeignKey(e => e.PatientId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientEncounter>()
                .HasOne(e => e.Institution).WithMany().HasForeignKey(e => e.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientEncounter>()
                .HasOne(e => e.Department).WithMany().HasForeignKey(e => e.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientEncounter>()
                .HasOne(e => e.Creater).WithMany().HasForeignKey(e => e.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientEncounter>()
                .HasOne(e => e.Editer).WithMany().HasForeignKey(e => e.EditerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientEncounter>()
                .HasOne(e => e.Retirer).WithMany().HasForeignKey(e => e.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientEncounter>()
                .HasOne(e => e.ReferringDepartment).WithMany().HasForeignKey(e => e.ReferringDepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientEncounter>()
                .HasOne(e => e.ReferringInstitution).WithMany().HasForeignKey(e => e.ReferringInstitutionId).OnDelete(DeleteBehavior.Restrict);
modelBuilder.Entity<PatientEncounter>()
                .HasOne(e => e.ReferringStaff).WithMany().HasForeignKey(e => e.ReferringStaffId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientEncounter>()
                .HasOne(e => e.ReferringConsultant).WithMany().HasForeignKey(e => e.ReferringConsultantId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientEncounter>()
                .HasOne(e => e.OpdDoctor).WithMany().HasForeignKey(e => e.OpdDoctorId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientEncounter>()
                .HasOne(e => e.Workplace).WithMany().HasForeignKey(e => e.WorkplaceId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientEncounter>()
                .HasOne(e => e.Guardian).WithMany().HasForeignKey(e => e.GuardianId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientEncounter>()
                .HasOne(e => e.GuardianRelationshipToPatient).WithMany().HasForeignKey(e => e.GuardianRelationshipToPatientId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientEncounter>()
                .HasOne(e => e.CreditCompany).WithMany().HasForeignKey(e => e.CreditCompanyId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientEncounter>()
                .HasOne(e => e.FinalBill).WithMany().HasForeignKey(e => e.FinalBillId).OnDelete(DeleteBehavior.Restrict);

            // ===== PatientRoom =====
            modelBuilder.Entity<PatientRoom>()
                .HasOne(pr => pr.Room).WithMany().HasForeignKey(pr => pr.RoomId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientRoom>()
                .HasOne(pr => pr.PatientEncounter).WithMany().HasForeignKey(pr => pr.PatientEncounterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientRoom>()
                .HasOne(pr => pr.Admission).WithMany().HasForeignKey(pr => pr.AdmissionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientRoom>()
                .HasOne(pr => pr.Creater).WithMany().HasForeignKey(pr => pr.CreaterId).OnDelete(DeleteBehavior.Restrict);

            // ===== Room =====
            modelBuilder.Entity<Room>()
                .HasOne(r => r.RoomCategory).WithMany().HasForeignKey(r => r.RoomCategoryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Room>()
                .HasOne(r => r.Institution).WithMany().HasForeignKey(r => r.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Room>()
                .HasOne(r => r.Department).WithMany().HasForeignKey(r => r.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Room>()
                .HasOne(r => r.Creater).WithMany().HasForeignKey(r => r.CreaterId).OnDelete(DeleteBehavior.Restrict);

            // ===== RoomCategory =====
            modelBuilder.Entity<RoomCategory>()
                .HasOne(rc => rc.Institution).WithMany().HasForeignKey(rc => rc.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RoomCategory>()
                .HasOne(rc => rc.Creater).WithMany().HasForeignKey(rc => rc.CreaterId).OnDelete(DeleteBehavior.Restrict);

            // ===== Admission =====
            modelBuilder.Entity<Admission>()
                .HasOne(a => a.PatientEncounter).WithMany().HasForeignKey(a => a.PatientEncounterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Admission>()
                .HasOne(a => a.Creater).WithMany().HasForeignKey(a => a.CreaterId).OnDelete(DeleteBehavior.Restrict);

            // ===== CashBook =====
            modelBuilder.Entity<CashBook>()
                .HasOne(cb => cb.Institution).WithMany().HasForeignKey(cb => cb.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CashBook>()
                .HasOne(cb => cb.Department).WithMany().HasForeignKey(cb => cb.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CashBook>()
                .HasOne(cb => cb.Creater).WithMany().HasForeignKey(cb => cb.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CashBook>()
                .HasOne(cb => cb.Retirer).WithMany().HasForeignKey(cb => cb.RetirerId).OnDelete(DeleteBehavior.Restrict);

            // ===== CashBookEntry =====
            modelBuilder.Entity<CashBookEntry>()
                .HasOne(e => e.CashBook).WithMany().HasForeignKey(e => e.CashBookId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CashBookEntry>()
                .HasOne(e => e.Institution).WithMany().HasForeignKey(e => e.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CashBookEntry>()
                .HasOne(e => e.Department).WithMany().HasForeignKey(e => e.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CashBookEntry>()
                .HasOne(e => e.Creater).WithMany().HasForeignKey(e => e.CreaterId).OnDelete(DeleteBehavior.Restrict);

            // ===== CashTransaction =====
            modelBuilder.Entity<CashTransaction>()
                .HasOne(ct => ct.Drawer).WithMany().HasForeignKey(ct => ct.DrawerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CashTransaction>()
                .HasOne(ct => ct.Institution).WithMany().HasForeignKey(ct => ct.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CashTransaction>()
                .HasOne(ct => ct.Department).WithMany().HasForeignKey(ct => ct.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CashTransaction>()
                .HasOne(ct => ct.Creater).WithMany().HasForeignKey(ct => ct.CreaterId).OnDelete(DeleteBehavior.Restrict);

            // ===== Drawer =====
            modelBuilder.Entity<Drawer>()
                .HasOne(d => d.Institution).WithMany().HasForeignKey(d => d.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Drawer>()
                .HasOne(d => d.Department).WithMany().HasForeignKey(d => d.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Drawer>()
                .HasOne(d => d.Creater).WithMany().HasForeignKey(d => d.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Drawer>()
                .HasOne(d => d.Cashier).WithMany().HasForeignKey(d => d.CashierId).OnDelete(DeleteBehavior.Restrict);

            // ===== ApiKey =====
            modelBuilder.Entity<ApiKey>()
                .HasOne(k => k.Institution).WithMany().HasForeignKey(k => k.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ApiKey>()
                .HasOne(k => k.Creater).WithMany().HasForeignKey(k => k.CreaterId).OnDelete(DeleteBehavior.Restrict);

            // ===== Logins =====
            modelBuilder.Entity<Logins>()
                .HasOne(l => l.Institution).WithMany().HasForeignKey(l => l.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Logins>()
                .HasOne(l => l.Department).WithMany().HasForeignKey(l => l.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Logins>()
                .HasOne(l => l.WebUser).WithMany().HasForeignKey(l => l.WebUserId).OnDelete(DeleteBehavior.Restrict);

            // ===== ConfigOption =====
            modelBuilder.Entity<ConfigOption>()
                .HasOne(co => co.Institution).WithMany().HasForeignKey(co => co.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ConfigOption>()
                .HasOne(co => co.Department).WithMany().HasForeignKey(co => co.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ConfigOption>()
                .HasOne(co => co.Creater).WithMany().HasForeignKey(co => co.CreaterId).OnDelete(DeleteBehavior.Restrict);

            // ===== Vtm =====
            modelBuilder.Entity<Vtm>()
                .HasOne(v => v.Creater).WithMany().HasForeignKey(v => v.CreaterId).OnDelete(DeleteBehavior.Restrict);

            // ===== Vmp =====
            modelBuilder.Entity<Vmp>()
                .HasOne(v => v.Vtm).WithMany().HasForeignKey(v => v.VtmId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Vmp>()
                .HasOne(v => v.Creater).WithMany().HasForeignKey(v => v.CreaterId).OnDelete(DeleteBehavior.Restrict);

            // ===== Vmpp =====
            modelBuilder.Entity<Vmpp>()
                .HasOne(v => v.Vmp).WithMany().HasForeignKey(v => v.VmpId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Vmpp>()
                .HasOne(v => v.Creater).WithMany().HasForeignKey(v => v.CreaterId).OnDelete(DeleteBehavior.Restrict);

            // ===== Amp =====
            modelBuilder.Entity<Amp>()
                .HasOne(a => a.Creater).WithMany().HasForeignKey(a => a.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Amp>()
                .HasOne(a => a.Editer).WithMany().HasForeignKey(a => a.EditerId).OnDelete(DeleteBehavior.Restrict);

            // ===== Stock =====
            modelBuilder.Entity<Stock>()
                .HasOne(s => s.ItemBatch).WithMany().HasForeignKey(s => s.ItemBatchId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Stock>()
                .HasOne(s => s.Item).WithMany().HasForeignKey(s => s.ItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Stock>()
                .HasOne(s => s.Department).WithMany().HasForeignKey(s => s.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Stock>()
                .HasOne(s => s.Creater).WithMany().HasForeignKey(s => s.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Stock>()
                .HasOne(s => s.Editer).WithMany().HasForeignKey(s => s.EditerId).OnDelete(DeleteBehavior.Restrict);

            // ===== ItemBatch =====
            modelBuilder.Entity<ItemBatch>()
                .HasOne(ib => ib.Item).WithMany().HasForeignKey(ib => ib.ItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ItemBatch>()
                .HasOne(ib => ib.Department).WithMany().HasForeignKey(ib => ib.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ItemBatch>()
                .HasOne(ib => ib.Creater).WithMany().HasForeignKey(ib => ib.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ItemBatch>()
                .HasOne(ib => ib.Editer).WithMany().HasForeignKey(ib => ib.EditerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ItemBatch>()
                .HasOne(ib => ib.Retirer).WithMany().HasForeignKey(ib => ib.RetirerId).OnDelete(DeleteBehavior.Restrict);

            // ===== PharmaceuticalBillItem =====
            modelBuilder.Entity<PharmaceuticalBillItem>()
                .HasOne(p => p.ItemBatch).WithMany().HasForeignKey(p => p.ItemBatchId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PharmaceuticalBillItem>()
                .HasOne(p => p.Creater).WithMany().HasForeignKey(p => p.CreaterId).OnDelete(DeleteBehavior.Restrict);

            // ===== Machine =====
            modelBuilder.Entity<Machine>()
                .HasOne(m => m.Institution).WithMany().HasForeignKey(m => m.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Machine>()
                .HasOne(m => m.Department).WithMany().HasForeignKey(m => m.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Machine>()
                .HasOne(m => m.Creater).WithMany().HasForeignKey(m => m.CreaterId).OnDelete(DeleteBehavior.Restrict);

            // ===== Roster =====
            modelBuilder.Entity<Roster>()
                .HasOne(r => r.Institution).WithMany().HasForeignKey(r => r.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Roster>()
                .HasOne(r => r.Creater).WithMany().HasForeignKey(r => r.CreaterId).OnDelete(DeleteBehavior.Restrict);

            // ===== Shift =====
            modelBuilder.Entity<Shift>()
                .HasOne(s => s.Institution).WithMany().HasForeignKey(s => s.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Shift>()
                .HasOne(s => s.Creater).WithMany().HasForeignKey(s => s.CreaterId).OnDelete(DeleteBehavior.Restrict);

            // ===== Staff =====
            modelBuilder.Entity<Staff>()
                .HasOne(s => s.Person).WithMany().HasForeignKey(s => s.PersonId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Staff>()
                .HasOne(s => s.Designation).WithMany().HasForeignKey(s => s.DesignationId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Staff>()
                .HasOne(s => s.Grade).WithMany().HasForeignKey(s => s.GradeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Staff>()
                .HasOne(s => s.StaffCategory).WithMany().HasForeignKey(s => s.StaffCategoryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Staff>()
                .HasOne(s => s.Roster).WithMany().HasForeignKey(s => s.RosterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Staff>()
                .HasOne(s => s.Speciality).WithMany().HasForeignKey(s => s.SpecialityId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Staff>()
                .HasOne(s => s.Institution).WithMany().HasForeignKey(s => s.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Staff>()
                .HasOne(s => s.Department).WithMany().HasForeignKey(s => s.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Staff>()
                .HasOne(s => s.WorkingDepartment).WithMany().HasForeignKey(s => s.WorkingDepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Staff>()
                .HasOne(s => s.Creater).WithMany().HasForeignKey(s => s.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Staff>()
                .HasOne(s => s.Retirer).WithMany().HasForeignKey(s => s.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Staff>()
                .HasOne(s => s.Editer).WithMany().HasForeignKey(s => s.EditerId).OnDelete(DeleteBehavior.Restrict);

            // ===== Doctor (TPH subclass of Staff) =====
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.ChannelingDestinationInstitution).WithMany().HasForeignKey(d => d.ChannelingDestinationInstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.ChannelingDestinationDepartment).WithMany().HasForeignKey(d => d.ChannelingDestinationDepartmentId).OnDelete(DeleteBehavior.Restrict);

            // ===== UsOrder rich-text report lifecycle =====
            modelBuilder.Entity<UsOrder>()
                .HasOne(o => o.ReportTemplate).WithMany().HasForeignKey(o => o.ReportTemplateId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UsOrder>()
                .HasOne(o => o.FinalizedBy).WithMany().HasForeignKey(o => o.FinalizedById).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UsReportTemplate>()
                .HasOne(t => t.UsService).WithMany().HasForeignKey(t => t.UsServiceId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UsReportTemplate>()
                .HasOne(t => t.Creater).WithMany().HasForeignKey(t => t.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UsReportTemplate>()
                .HasOne(t => t.Editer).WithMany().HasForeignKey(t => t.EditerId).OnDelete(DeleteBehavior.Restrict);

            // ===== XrOrder rich-text report lifecycle (X-ray mirror of the US module) =====
            modelBuilder.Entity<XrOrder>()
                .HasOne(o => o.ReportTemplate).WithMany().HasForeignKey(o => o.ReportTemplateId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<XrOrder>()
                .HasOne(o => o.FinalizedBy).WithMany().HasForeignKey(o => o.FinalizedById).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<XrReportTemplate>()
                .HasOne(t => t.XrService).WithMany().HasForeignKey(t => t.XrServiceId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<XrReportTemplate>()
                .HasOne(t => t.Creater).WithMany().HasForeignKey(t => t.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<XrReportTemplate>()
                .HasOne(t => t.Editer).WithMany().HasForeignKey(t => t.EditerId).OnDelete(DeleteBehavior.Restrict);

            // ===== StaffSalary =====
            modelBuilder.Entity<StaffSalary>()
                .HasOne(s => s.Staff).WithMany().HasForeignKey(s => s.StaffId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<StaffSalary>()
                .HasOne(s => s.Institution).WithMany().HasForeignKey(s => s.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<StaffSalary>()
                .HasOne(s => s.Department).WithMany().HasForeignKey(s => s.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<StaffSalary>()
                .HasOne(s => s.Creater).WithMany().HasForeignKey(s => s.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<StaffSalary>()
                .HasOne(s => s.Editer).WithMany().HasForeignKey(s => s.EditerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<StaffSalary>()
                .HasOne(s => s.Retirer).WithMany().HasForeignKey(s => s.RetirerId).OnDelete(DeleteBehavior.Restrict);

            // ===== StaffCategory =====
            modelBuilder.Entity<StaffCategory>()
                .HasOne(s => s.Creater).WithMany().HasForeignKey(s => s.CreaterId).OnDelete(DeleteBehavior.Restrict);

            // ===== Designation =====
            modelBuilder.Entity<Designation>()
                .HasOne(d => d.Creater).WithMany().HasForeignKey(d => d.CreaterId).OnDelete(DeleteBehavior.Restrict);

            // ===== Grade =====
            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Creater).WithMany().HasForeignKey(g => g.CreaterId).OnDelete(DeleteBehavior.Restrict);

            // ===== Table mappings =====
            modelBuilder.Entity<Person>().ToTable("PERSON");
            modelBuilder.Entity<Patient>().ToTable("PATIENT");
            modelBuilder.Entity<Institution>().ToTable("INSTITUTION");
            modelBuilder.Entity<Department>().ToTable("DEPARTMENT");
            modelBuilder.Entity<Staff>().ToTable("STAFF");
            modelBuilder.Entity<Doctor>().ToTable("STAFF");
            modelBuilder.Entity<WebUser>().ToTable("WEBUSER");
            modelBuilder.Entity<WebUserRole>().ToTable("WEBUSERROLE");
            modelBuilder.Entity<Item>().ToTable("ITEM");
            modelBuilder.Entity<Category>().ToTable("CATEGORY");
            modelBuilder.Entity<Bill>().ToTable("BILL");
            modelBuilder.Entity<BillItem>().ToTable("BILLITEM");
            modelBuilder.Entity<BillFee>().ToTable("BILLFEE");
            modelBuilder.Entity<Payment>().ToTable("PAYMENT");
            modelBuilder.Entity<Fee>().ToTable("FEE");
            modelBuilder.Entity<PriceMatrix>().ToTable("PRICEMATRIX");
            modelBuilder.Entity<Area>().ToTable("AREA");
            modelBuilder.Entity<HMISRoute>().ToTable("ROUTES");
            modelBuilder.Entity<Speciality>().ToTable("SPECIALITY");
            modelBuilder.Entity<MeasurementUnit>().ToTable("MEASUREMENTUNIT");
            modelBuilder.Entity<MembershipScheme>().ToTable("MEMBERSHIPSCHEME");
            modelBuilder.Entity<PaymentScheme>().ToTable("PAYMENTSCHEME");
            modelBuilder.Entity<PatientEncounter>().ToTable("PATIENTENCOUNTER");
            modelBuilder.Entity<PatientRoom>().ToTable("PATIENTROOM");
            modelBuilder.Entity<Room>().ToTable("ROOM");
            modelBuilder.Entity<RoomCategory>().ToTable("ROOMCATEGORY");
            modelBuilder.Entity<Admission>().ToTable("ADMISSION");
            modelBuilder.Entity<CashBook>().ToTable("CASHBOOK");
            modelBuilder.Entity<CashBookEntry>().ToTable("CASHBOOKENTRY");
            modelBuilder.Entity<CashTransaction>().ToTable("CASHTRANSACTION");
            modelBuilder.Entity<Drawer>().ToTable("DRAWER");
            modelBuilder.Entity<ApiKey>().ToTable("APIKEY");
            modelBuilder.Entity<Logins>().ToTable("LOGINS");
            modelBuilder.Entity<ConfigOption>().ToTable("CONFIGOPTION");
            modelBuilder.Entity<Vtm>().ToTable("VTM");
            modelBuilder.Entity<Vmp>().ToTable("VMP");
            modelBuilder.Entity<Vmpp>().ToTable("VMPP");
            modelBuilder.Entity<Amp>().ToTable("AMP");
            modelBuilder.Entity<Stock>().ToTable("STOCK");
            modelBuilder.Entity<ItemBatch>().ToTable("ITEMBATCH");
            modelBuilder.Entity<PharmaceuticalBillItem>().ToTable("PHARMACEUTICALBILLITEM");
            modelBuilder.Entity<Machine>().ToTable("MACHINE");
            modelBuilder.Entity<Roster>().ToTable("ROSTER");
            modelBuilder.Entity<Shift>().ToTable("SHIFT");
            modelBuilder.Entity<StaffSalary>().ToTable("STAFFSALARY");
            modelBuilder.Entity<StaffCategory>().ToTable("STAFFCATEGORY");
            modelBuilder.Entity<Designation>().ToTable("DESIGNATION");
            modelBuilder.Entity<Grade>().ToTable("GRADE");
            modelBuilder.Entity<BillSession>().ToTable("BILLSESSION");
            modelBuilder.Entity<UsService>().ToTable("USSERVICE");
            modelBuilder.Entity<UsServiceCategory>().ToTable("USSERVICECATEGORY");
            modelBuilder.Entity<UsOrder>().ToTable("USORDER");
            modelBuilder.Entity<UsReportTemplate>().ToTable("USREPORTTEMPLATE");
            modelBuilder.Entity<MedicalTerm>().ToTable("MEDICALTERM");
            modelBuilder.Entity<XrService>().ToTable("XRSERVICE");
            modelBuilder.Entity<XrServiceCategory>().ToTable("XRSERVICECATEGORY");
            modelBuilder.Entity<XrOrder>().ToTable("XRORDER");
            modelBuilder.Entity<XrReportTemplate>().ToTable("XREPORTTEMPLATE");
            modelBuilder.Entity<XrMedicalTerm>().ToTable("XRMEDICALTERM");
            modelBuilder.Entity<PanelServiceCharge>().ToTable("PANELSERVICECHARGE");
            modelBuilder.Entity<PanelServiceCharge>()
                .HasIndex(c => new { c.CompanyId, c.DepartmentCode, c.ServiceCode }).IsUnique();
            modelBuilder.Entity<PanelServiceCharge>()
                .HasOne(c => c.Company).WithMany().HasForeignKey(c => c.CompanyId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PanelServiceCharge>()
                .HasOne(c => c.Item).WithMany().HasForeignKey(c => c.ItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PanelDepartment>().ToTable("PANELDEPARTMENT");
            modelBuilder.Entity<PanelDepartment>().HasIndex(d => d.Code).IsUnique();
            modelBuilder.Entity<TestData>().ToTable("TESTDATA");
            modelBuilder.Entity<TestData>()
                .HasOne(d => d.Category).WithMany().HasForeignKey(d => d.CategoryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TestRemark>().ToTable("TESTREMARK");
            modelBuilder.Entity<TestRemark>()
                .HasOne(r => r.Item).WithMany().HasForeignKey(r => r.ItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<GrowthDescription>().ToTable("GROWTHDESCRIPTION");
            modelBuilder.Entity<GrowthDescription>().HasIndex(g => g.Code).IsUnique();
            modelBuilder.Entity<Organism>().ToTable("ORGANISM");
            modelBuilder.Entity<Organism>().HasIndex(o => o.Code).IsUnique();
            modelBuilder.Entity<WebUserRoleUser>().ToTable("WEBUSERROLEUSER");
            modelBuilder.Entity<WebUserPrivilege>().ToTable("WEBUSERPRIVILEGE");
            modelBuilder.Entity<WebUserDepartment>().ToTable("WEBUSERDEPARTMENT");

            // ===== Restrict config for entities added during full entity migration =====
            modelBuilder.Entity<AgentHistory>()
                .HasOne(x => x.BillItem).WithMany().HasForeignKey(x => x.BillItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AgentHistory>()
                .HasOne(x => x.Bill).WithMany().HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AgentHistory>()
                .HasOne(x => x.BillSession).WithMany().HasForeignKey(x => x.BillSessionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AgentHistory>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AgentHistory>()
                .HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AgentHistory>()
                .HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AgentHistory>()
                .HasOne(x => x.Agency).WithMany().HasForeignKey(x => x.AgencyId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AgentHistory>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AppEmail>()
                .HasOne(x => x.Bill).WithMany().HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AppEmail>()
                .HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AppEmail>()
                .HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AppEmail>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AppEmail>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Appointment>()
                .HasOne(x => x.Bill).WithMany().HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Appointment>()
                .HasOne(x => x.PatientEncounter).WithMany().HasForeignKey(x => x.PatientEncounterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillComponent>()
                .HasOne(x => x.Packege).WithMany().HasForeignKey(x => x.PackegeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillComponent>()
                .HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillComponent>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillComponent>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillComponent>()
                .HasOne(x => x.Bill).WithMany().HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillComponent>()
                .HasOne(x => x.BillItem).WithMany().HasForeignKey(x => x.BillItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillComponent>()
                .HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillComponent>()
                .HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillComponent>()
                .HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillComponent>()
                .HasOne(x => x.Speciality).WithMany().HasForeignKey(x => x.SpecialityId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillFeePayment>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillFeePayment>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillFeePayment>()
                .HasOne(x => x.BillFee).WithMany().HasForeignKey(x => x.BillFeeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillFeePayment>()
                .HasOne(x => x.Payment).WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillFeePayment>()
                .HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillFeePayment>()
                .HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillNumber>()
                .HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillNumber>()
                .HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillNumber>()
                .HasOne(x => x.ToDepartment).WithMany().HasForeignKey(x => x.ToDepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillNumber>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CategoryItem>()
                .HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CategoryItem>()
                .HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CategoryItem>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CategoryItem>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EncounterCreditCompany>()
                .HasOne(x => x.PatientEncounter).WithMany().HasForeignKey(x => x.PatientEncounterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EncounterCreditCompany>()
                .HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EncounterCreditCompany>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EncounterCreditCompany>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Family>()
                .HasOne(x => x.MembershipScheme).WithMany().HasForeignKey(x => x.MembershipSchemeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Family>()
                .HasOne(x => x.CreatedInstitution).WithMany().HasForeignKey(x => x.CreatedInstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Family>()
                .HasOne(x => x.CreatedDepartment).WithMany().HasForeignKey(x => x.CreatedDepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Family>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Family>()
                .HasOne(x => x.Editer).WithMany().HasForeignKey(x => x.EditerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Family>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FamilyMember>()
                .HasOne(x => x.Family).WithMany().HasForeignKey(x => x.FamilyId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FamilyMember>()
                .HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FamilyMember>()
                .HasOne(x => x.RelationToChh).WithMany().HasForeignKey(x => x.RelationToChhId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FamilyMember>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FamilyMember>()
                .HasOne(x => x.Editer).WithMany().HasForeignKey(x => x.EditerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FamilyMember>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FeeChange>()
                .HasOne(x => x.Fee).WithMany().HasForeignKey(x => x.FeeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FeeChange>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FeeChange>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FeeValue>()
                .HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FeeValue>()
                .HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FeeValue>()
                .HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FeeValue>()
                .HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FeeValue>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FeeValue>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Form>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Form>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Form>()
                .HasOne(x => x.Editor).WithMany().HasForeignKey(x => x.EditorId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<IssueRateMargins>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<IssueRateMargins>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<IssueRateMargins>()
                .HasOne(x => x.FromDepartment).WithMany().HasForeignKey(x => x.FromDepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<IssueRateMargins>()
                .HasOne(x => x.ToDepartment).WithMany().HasForeignKey(x => x.ToDepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<IssueRateMargins>()
                .HasOne(x => x.FromInstitution).WithMany().HasForeignKey(x => x.FromInstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<IssueRateMargins>()
                .HasOne(x => x.ToInstitution).WithMany().HasForeignKey(x => x.ToInstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ItemMapping>()
                .HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ItemMapping>()
                .HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ItemMapping>()
                .HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ItemMapping>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ItemMapping>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ItemsCategories>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ItemsCategories>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ItemsCategories>()
                .HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ItemsCategories>()
                .HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Mapping>()
                .HasOne(x => x.Item1).WithMany().HasForeignKey(x => x.Item1Id).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Mapping>()
                .HasOne(x => x.Item2).WithMany().HasForeignKey(x => x.Item2Id).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MedicalPackageItem>()
                .HasOne(x => x.Packege).WithMany().HasForeignKey(x => x.PackegeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MedicalPackageItem>()
                .HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MedicalPackageItem>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MedicalPackageItem>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Notification>()
                .HasOne(x => x.Bill).WithMany().HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Notification>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Notification>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PackageItem>()
                .HasOne(x => x.Packege).WithMany().HasForeignKey(x => x.PackegeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PackageItem>()
                .HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PackageItem>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PackageItem>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientDeposit>()
                .HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientDeposit>()
                .HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientDeposit>()
                .HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientDeposit>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientDeposit>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientDepositHistory>()
                .HasOne(x => x.BillItem).WithMany().HasForeignKey(x => x.BillItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientDepositHistory>()
                .HasOne(x => x.Bill).WithMany().HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientDepositHistory>()
                .HasOne(x => x.BillSession).WithMany().HasForeignKey(x => x.BillSessionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientDepositHistory>()
                .HasOne(x => x.PatientDeposit).WithMany().HasForeignKey(x => x.PatientDepositId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientDepositHistory>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientDepositHistory>()
                .HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientDepositHistory>()
                .HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientDepositHistory>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientFlag>()
                .HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientFlag>()
                .HasOne(x => x.PatientEncounter).WithMany().HasForeignKey(x => x.PatientEncounterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientFlag>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientFlag>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientItem>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientItem>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientItem>()
                .HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientItem>()
                .HasOne(x => x.PatientEncounter).WithMany().HasForeignKey(x => x.PatientEncounterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientItem>()
                .HasOne(x => x.Bill).WithMany().HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientItem>()
                .HasOne(x => x.BillItem).WithMany().HasForeignKey(x => x.BillItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientItem>()
                .HasOne(x => x.Packege).WithMany().HasForeignKey(x => x.PackegeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientItem>()
                .HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PaymentGatewayTransaction>()
                .HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PaymentGatewayTransaction>()
                .HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PaymentGatewayTransaction>()
                .HasOne(x => x.Bill).WithMany().HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PaymentGatewayTransaction>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PaymentGatewayTransaction>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PersonInstitution>()
                .HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PersonInstitution>()
                .HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PersonInstitution>()
                .HasOne(x => x.Person).WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PersonInstitution>()
                .HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PersonInstitution>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PersonInstitution>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ReportTemplate>()
                .HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ReportTemplate>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ReportTemplate>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ServiceSession>()
                .HasOne(x => x.AfterSession).WithMany().HasForeignKey(x => x.AfterSessionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ServiceSession>()
                .HasOne(x => x.SessionNumberGenerator).WithMany().HasForeignKey(x => x.SessionNumberGeneratorId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ServiceSession>()
                .HasOne(x => x.BeforeSession).WithMany().HasForeignKey(x => x.BeforeSessionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ServiceSession>()
                .HasOne(x => x.OriginatingSession).WithMany().HasForeignKey(x => x.OriginatingSessionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ServiceSessionInstance>()
                .HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ServiceSessionInstance>()
                .HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ServiceSessionInstance>()
                .HasOne(x => x.Speciality).WithMany().HasForeignKey(x => x.SpecialityId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ServiceSessionInstance>()
                .HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ServiceSessionInstance>()
                .HasOne(x => x.ForInstitution).WithMany().HasForeignKey(x => x.ForInstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ServiceSessionInstance>()
                .HasOne(x => x.ForDepartment).WithMany().HasForeignKey(x => x.ForDepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ServiceSessionInstance>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ServiceSessionInstance>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ServiceSessionInstance>()
                .HasOne(x => x.Editer).WithMany().HasForeignKey(x => x.EditerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ServiceSessionInstance>()
                .HasOne(x => x.SessionNumberGenerator).WithMany().HasForeignKey(x => x.SessionNumberGeneratorId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ServiceSessionInstance>()
                .HasOne(x => x.OriginatingSession).WithMany().HasForeignKey(x => x.OriginatingSessionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<SessionNumberGenerator>()
                .HasOne(x => x.Speciality).WithMany().HasForeignKey(x => x.SpecialityId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<SessionNumberGenerator>()
                .HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Sms>()
                .HasOne(x => x.Bill).WithMany().HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Sms>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Sms>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Sms>()
                .HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Sms>()
                .HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Token>()
                .HasOne(x => x.Caterory).WithMany().HasForeignKey(x => x.CateroryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Token>()
                .HasOne(x => x.Bill).WithMany().HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Token>()
                .HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Token>()
                .HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Token>()
                .HasOne(x => x.FromDepartment).WithMany().HasForeignKey(x => x.FromDepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Token>()
                .HasOne(x => x.ToDepartment).WithMany().HasForeignKey(x => x.ToDepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Token>()
                .HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Token>()
                .HasOne(x => x.FromInstitution).WithMany().HasForeignKey(x => x.FromInstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Token>()
                .HasOne(x => x.ToInstitution).WithMany().HasForeignKey(x => x.ToInstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Token>()
                .HasOne(x => x.ServiceCounter).WithMany().HasForeignKey(x => x.ServiceCounterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Token>()
                .HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Token>()
                .HasOne(x => x.RetiredBy).WithMany().HasForeignKey(x => x.RetiredById).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Token>()
                .HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Token>()
                .HasOne(x => x.FromStaff).WithMany().HasForeignKey(x => x.FromStaffId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Token>()
                .HasOne(x => x.ToStaff).WithMany().HasForeignKey(x => x.ToStaffId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Token>()
                .HasOne(x => x.PatientEncounter).WithMany().HasForeignKey(x => x.PatientEncounterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Token>()
                .HasOne(x => x.Counter).WithMany().HasForeignKey(x => x.CounterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Token>()
                .HasOne(x => x.Doctor).WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
modelBuilder.Entity<Token>()
                .HasOne(x => x.ReferaToken).WithMany().HasForeignKey(x => x.ReferaTokenId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Token>()
                .HasOne(x => x.Session).WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);

            // ===== OPD Consultation module =====
            modelBuilder.Entity<OpdSession>()
                .HasOne(x => x.Consultant).WithMany().HasForeignKey(x => x.ConsultantId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<OpdSession>()
                .HasOne(x => x.Speciality).WithMany().HasForeignKey(x => x.SpecialityId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<OpdSession>()
                .HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<OpdSession>()
                .HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<OpdSession>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ConsultantSettlement>()
                .HasOne(x => x.Consultant).WithMany().HasForeignKey(x => x.ConsultantId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ConsultantSettlement>()
                .HasOne(x => x.ReceivedBy).WithMany().HasForeignKey(x => x.ReceivedById).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ConsultantSettlement>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TriggerSubscription>()
                .HasOne(x => x.WebUser).WithMany().HasForeignKey(x => x.WebUserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TriggerSubscription>()
                .HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TriggerSubscription>()
                .HasOne(x => x.WebUserRole).WithMany().HasForeignKey(x => x.WebUserRoleId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TriggerSubscription>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TriggerSubscription>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TriggerSubscription>()
                .HasOne(x => x.Editer).WithMany().HasForeignKey(x => x.EditerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Upload>()
                .HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Upload>()
                .HasOne(x => x.WebContent).WithMany().HasForeignKey(x => x.WebContentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Upload>()
                .HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Upload>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Upload>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserIcon>()
                .HasOne(x => x.WebUser).WithMany().HasForeignKey(x => x.WebUserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserIcon>()
                .HasOne(x => x.WebUserRole).WithMany().HasForeignKey(x => x.WebUserRoleId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserIcon>()
                .HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserNotification>()
                .HasOne(x => x.Notification).WithMany().HasForeignKey(x => x.NotificationId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserNotification>()
                .HasOne(x => x.WebUser).WithMany().HasForeignKey(x => x.WebUserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserNotification>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserNotification>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserNotification>()
                .HasOne(x => x.ToDepartment).WithMany().HasForeignKey(x => x.ToDepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserPreference>()
                .HasOne(x => x.WebUser).WithMany().HasForeignKey(x => x.WebUserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserPreference>()
                .HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserPreference>()
                .HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebContent>()
                .HasOne(x => x.WebLanguage).WithMany().HasForeignKey(x => x.WebLanguageId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebContent>()
                .HasOne(x => x.Parent).WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebTheme>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebTheme>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUserDashboard>()
                .HasOne(x => x.WebUser).WithMany().HasForeignKey(x => x.WebUserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUserDashboard>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUserDashboard>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUserPaymentScheme>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUserPaymentScheme>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUserPaymentScheme>()
                .HasOne(x => x.WebUser).WithMany().HasForeignKey(x => x.WebUserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUserPaymentScheme>()
                .HasOne(x => x.PaymentScheme).WithMany().HasForeignKey(x => x.PaymentSchemeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUserRolePrivilege>()
                .HasOne(x => x.WebUserRole).WithMany().HasForeignKey(x => x.WebUserRoleId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUserRolePrivilege>()
                .HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUserRolePrivilege>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUserRolePrivilege>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUserRoute>()
                .HasOne(x => x.Creater).WithMany().HasForeignKey(x => x.CreaterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUserRoute>()
                .HasOne(x => x.Retirer).WithMany().HasForeignKey(x => x.RetirerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUserRoute>()
                .HasOne(x => x.WebUser).WithMany().HasForeignKey(x => x.WebUserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WebUserRoute>()
                .HasOne(x => x.Route).WithMany().HasForeignKey(x => x.RouteId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientOwner>()
                .HasIndex(x => new { x.Type, x.Regno });
            modelBuilder.Entity<PatientOwner>()
                .HasIndex(x => x.EncounterId);
            modelBuilder.Entity<ConsultantCommission>()
                .HasIndex(x => new { x.CompanyCode, x.ConCode, x.SCode });
            modelBuilder.Entity<UnsettleRequest>()
                .HasIndex(x => x.ReqNo);
            modelBuilder.Entity<UnsettleRequest>()
                .HasIndex(x => x.EncounterId);

            // ===== Legacy IPD bill form (VFP BS0110) =====
            // REGISTER / IBIL_MST / IBIL_DET are shared legacy tables - no keys are
            // dropped and no data is migrated here (see db/migrations/01).
            modelBuilder.Entity<HmisWeb.Models.Legacy.RegisterLegacy>()
                .HasKey(r => new { r.Type, r.RegNo });
            modelBuilder.Entity<HmisWeb.Models.Legacy.RegisterLegacy>()
                .HasIndex(r => r.BillStatus);
            modelBuilder.Entity<HmisWeb.Models.Legacy.IbilMst>()
                .HasIndex(m => new { m.HosCode, m.Dept });
            modelBuilder.Entity<HmisWeb.Models.Legacy.IbilDet>()
                .HasIndex(d => new { d.Dept, d.SlipNo });
            modelBuilder.Entity<HmisWeb.Models.Legacy.IpdBillHeader>()
                .HasKey(h => h.BillNo);
            modelBuilder.Entity<HmisWeb.Models.Legacy.IpdBillHeader>()
                .HasIndex(h => h.HosCode);
            modelBuilder.Entity<HmisWeb.Models.Legacy.IpdBillAudit>()
                .HasIndex(a => new { a.RegNo, a.CreatedAt });
            modelBuilder.Entity<HmisWeb.Models.Legacy.IpdBillAudit>()
                .HasIndex(a => new { a.Entity, a.EntityKey, a.CreatedAt });
            modelBuilder.Entity<HmisWeb.Models.Legacy.LegacyDepartment>()
                .HasIndex(d => d.Code).IsUnique();
        }
    }
}