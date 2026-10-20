using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAppSession, AppSession>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ICommonService, CommonService>();
builder.Services.AddScoped<IUsReportTemplateService, UsReportTemplateService>();
builder.Services.AddScoped<IReportDictionaryService, ReportDictionaryService>();
builder.Services.AddScoped<IUsBillingMirrorService, UsBillingMirrorService>();
        builder.Services.AddScoped<IXrReportTemplateService, XrReportTemplateService>();
        builder.Services.AddScoped<IXrReportDictionaryService, XrReportDictionaryService>();
        builder.Services.AddScoped<IXrBillingMirrorService, XrBillingMirrorService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ILookupSeedService, LookupSeedService>();

// Indoor Patient (IPD Bill) form - VFP BS0110 replacement
builder.Services.AddScoped<HmisWeb.Services.Billing.IpdBillReportService>();
builder.Services.AddScoped<HmisWeb.Services.Billing.BillingReportService>();
builder.Services.AddScoped<HmisWeb.Services.Pharmacy.PhService>();
builder.Services.AddScoped<HmisWeb.Services.Billing.IpdBillQueryService>();
builder.Services.AddScoped<HmisWeb.Services.Billing.IpdBillLineService>();
builder.Services.AddScoped<HmisWeb.Services.Billing.IpdBillWorkflowService>();
// Outdoor Patient (OPD Bill) form - VFP BS0101 replacement (/opd/Billing)
builder.Services.AddScoped<HmisWeb.Services.Billing.OpdBillQueryService>();
builder.Services.AddScoped<HmisWeb.Services.Billing.OpdBillLineService>();
builder.Services.AddScoped<HmisWeb.Services.Billing.OpdBillWorkflowService>();
builder.Services.AddScoped<HmisWeb.Services.Billing.OpdBillGenerateService>();
// Recovery module - phase 4 sprint 1: company Invoice/Letter (/Recovery/Invoice)
builder.Services.AddScoped<HmisWeb.Services.Billing.RecoveryInvoiceQueryService>();
builder.Services.AddScoped<HmisWeb.Services.Billing.RecoveryInvoiceService>();
builder.Services.AddScoped<HmisWeb.Services.Billing.RecoveryInvoiceReportService>();

builder.Services.AddAuthentication("HmisAuth")
    .AddCookie("HmisAuth", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(90);
    });

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(90);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<ILookupSeedService>();
    await seeder.EnsureSeededAsync();
}

app.Run();
