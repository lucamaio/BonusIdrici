using Microsoft.EntityFrameworkCore;
using Data;
using MySql.EntityFrameworkCore;
using BonusIdrici2.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("BonusIdrici2");

// ✅ Configurazione Sessione
builder.Services.AddDistributedMemoryCache();
builder.Services.AddMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Timeout di 30 minuti
    options.Cookie.HttpOnly = true;                // Non accessibile da JS
    options.Cookie.IsEssential = true;             // Necessario per GDPR
    options.Cookie.Name = ".BonusIdrici.Session";  // Nome personalizzato cookie
});

// ✅ Abilita accesso al contesto HTTP
builder.Services.AddHttpContextAccessor();

// ✅ MVC
builder.Services.AddControllersWithViews();

builder.Services.Configure<LogCleanupOptions>(
    builder.Configuration.GetSection(LogCleanupOptions.SectionName));
builder.Services.AddHostedService<LogCleanupHostedService>();
builder.Services.AddScoped<SectionActivityService>();
builder.Services.AddScoped<IndirizziService>();
builder.Services.AddSingleton<AppCacheService>();

// ✅ Connessione DB
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("La stringa di connessione 'DefaultConnection' non è definita nel file appsettings.json.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySQL(connectionString));

var app = builder.Build();

// 🔹 Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 🔹 Sessione PRIMA di Authentication/Authorization
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// 🔹 Gestione codici di errore (404, 403, ecc.)
app.UseStatusCodePagesWithReExecute("/Home/HandleError", "?code={0}");

// 🔹 Route di default
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
