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
builder.Services.AddScoped<HmisWeb.Services.Lab.LabSchemaService>();
builder.Services.AddScoped<HmisWeb.Services.Lab.NormalRangeService>();
builder.Services.AddScoped<HmisWeb.Services.Lab.CultureService>();
builder.Services.AddScoped<HmisWeb.Services.Lab.LabResultService>();
builder.Services.AddScoped<HmisWeb.Services.Lab.LabReportFunctionService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ICommonService, CommonService>();
builder.Services.AddScoped<IUsReportTemplateService, UsReportTemplateService>();
builder.Services.AddScoped<IReportDictionaryService, ReportDictionaryService>();
builder.Services.AddScoped<IUsBillingMirrorService, UsBillingMirrorService>();
        builder.Services.AddScoped<IXrReportTemplateService, XrReportTemplateService>();
        builder.Services.AddScoped<IXrReportDictionaryService, XrReportDictionaryService>();
        builder.Services.AddScoped<IXrBillingMirrorService, XrBillingMirrorService>();
        builder.Services.AddScoped<IDeptBillingMirrorService, DeptBillingMirrorService>();
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
// Recovery module - phase 4 sprint 2: Receipts/Cheques (/Recovery/Receipt)
builder.Services.AddScoped<HmisWeb.Services.Billing.RecoveryReceiptQueryService>();
builder.Services.AddScoped<HmisWeb.Services.Billing.RecoveryReceiptService>();

builder.Services.AddAuthentication("HmisAuth")
    .AddCookie("HmisAuth", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
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
app.UseSession();

// The app keeps the signed-in user in the in-memory session. A restart (or
// an app-pool recycle) clears that session even though the browser still has
// the "Remember me" cookie. Rehydrate the session from the auth cookie so the
// user stays signed in until the cookie expires.
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true &&
        string.IsNullOrEmpty(context.Session.GetString("LoggedUserId")))
    {
        var id = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (long.TryParse(id, out _))
        {
            context.Session.SetString("LoggedUserId", id);

            var dept = context.User.FindFirst("DepartmentId")?.Value;
            if (long.TryParse(dept, out _))
                context.Session.SetString("LoggedDepartmentId", dept);
        }
    }

    await next();
});

// Require a signed-in user for every page except the authentication flow.
// The login page is a standalone screen (no sidebar) and is always the first
// page an anonymous visitor sees.
app.Use(async (context, next) =>
{
    var path = context.Request.Path;

    var isPublicPath =
        path.StartsWithSegments("/Account/Login", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/Account/Setup", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/Account/Logout", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/Account/AccessDenied", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/Error", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/favicon.ico", StringComparison.OrdinalIgnoreCase);

    if (!isPublicPath)
    {
        var isAuthenticated = context.User.Identity?.IsAuthenticated == true
            || !string.IsNullOrEmpty(context.Session.GetString("LoggedUserId"));

        if (!isAuthenticated)
        {
            var loginUrl = "/Account/Login";
            if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
            {
                var returnUrl = context.Request.Path + context.Request.QueryString;
                loginUrl += "?returnUrl=" + Uri.EscapeDataString(returnUrl);
            }

            context.Response.Redirect(loginUrl);
            return;
        }
    }

    await next();
});

app.UseAuthorization();
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<ILookupSeedService>();
    await seeder.EnsureSeededAsync();

    // Billing mirrors: every department service on the shared SERVICES
    // master (DEPT='01') needs an INV-LB-{code} ITEM row so bills can
    // reference it. Idempotent - only missing mirrors are created.
    var deptMirrors = scope.ServiceProvider.GetRequiredService<IDeptBillingMirrorService>();
    await deptMirrors.EnsureAllMissingAsync();
}

app.Run();
