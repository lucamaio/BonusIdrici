using Microsoft.EntityFrameworkCore;
using BonusIdrici2.Data;
using MySql.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Durata della sessione inattiva
    options.Cookie.HttpOnly = true; // Impedisce l'accesso al cookie via JavaScript
    options.Cookie.IsEssential = true; // Il cookie di sessione è essenziale per l'applicazione
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddSession();

// Add services to the container.
builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("La stringa di connessione 'DefaultConnection' non è definita nel file appsettings.json.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySQL(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Mostra la pagina di errore generica per eccezioni
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();

    // 🔹 intercetta i codici di stato (404, 403, ecc.)
    app.UseStatusCodePagesWithReExecute("/Home/HandleError", "?code={0}");
}

app.UseHttpsRedirection();

// *** AGGIUNGI QUESTA RIGA PER SERVIRE I FILE STATICI ***
app.UseStaticFiles(); // Abilita il servizio dei file da wwwroot
// ******************************************************

app.UseRouting();

app.UseAuthorization();

// Abilita il middleware di autenticazione e autorizzazione (fondamentale per il login)
app.UseAuthentication();
app.UseAuthorization();

// Abilita il middleware di sessione
app.UseSession(); // Questo deve essere posizionato prima di UseRouting e UseEndpoints (o UseMVC)


// Se app.MapStaticAssets() e .WithStaticAssets() non sono strettamente necessarie
// per altre funzionalità o se causano problemi, potresti volerle rimuovere/commentare.
// Per un setup standard, queste non sono necessarie per servire CSS/JS da wwwroot.
// app.MapStaticAssets(); 
// app.MapControllerRoute(
//     name: "default",
//     pattern: "{controller=Home}/{action=Index}/{id?}")
//     .WithStaticAssets();

// Per un setup standard, tieni solo questa:
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();