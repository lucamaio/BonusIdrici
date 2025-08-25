using Microsoft.EntityFrameworkCore;
using BonusIdrici2.Data;
using MySql.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ✅ Configurazione Sessione
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();

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
    // Gestione eccezioni globali
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// HTTPS + file statici
app.UseHttpsRedirection();
app.UseStaticFiles();

// Sessione PRIMA del routing
app.UseSession();

app.UseRouting();

// Autenticazione e autorizzazione
app.UseAuthentication();
app.UseAuthorization();

// 🔹 Gestione codici di errore (404, 403, ecc.)
app.UseStatusCodePagesWithReExecute("/Home/HandleError", "?code={0}");

// 🔹 Route di default
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
