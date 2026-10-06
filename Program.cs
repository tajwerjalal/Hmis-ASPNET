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
