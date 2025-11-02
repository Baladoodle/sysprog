using Projekat2.Services;
using Projekat2.Endpoints;
using Projekat2.Caching;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// registruj servise
builder.Services.AddSingleton<IServisKes, ServisKes>();
builder.Services.AddScoped<IServisAnaliza, ServisAnaliza>();

builder.Services.AddLogging(cfg =>
{
    cfg.ClearProviders();
    cfg.AddConsole();
    cfg.SetMinimumLevel(LogLevel.Information);
});

var app = builder.Build();

// ovo preusmerava HTTP zahteve na HTTPS
app.UseHttpsRedirection();

app.MapujRute();

// provera stanja aplikacije
app.MapGet("/health", () => Results.Ok(new { status = "OK" }))
   .WithName("Health");

// info o servisu i keš status - koristi async metodu za brojanje
app.MapGet("/", async (IServisKes kes) =>
{
    var brojStavki = await kes.BrojStavkiAsync();

    return Results.Ok(new
    {
        poruka = "Web server za analizu prosečne dužine reči u fajlovima (Projekat 2 - Tasks & Async)",
        upotreba = "GET /{fileName} - Analiza fajla",
        primer = "GET /test.txt",
        kesStatus = new
        {
            stavki = brojStavki,
            info = "Rezultati se keširaju 5 minuta"
        }
    });
})
.WithName("Home");

app.Urls.Add("http://localhost:5050");

app.Run();
