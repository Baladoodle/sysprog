using Projekat.Services;
using Projekat.Endpoints;
using Projekat.Caching;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// registruj servisima - IServisKes mora biti singleton jer deli memoriju između zahteva
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

// info o servisu i keš status
app.MapGet("/", (IServisKes kes) =>
{
    return Results.Ok(new
    {
        poruka = "Web server za analizu prosečne dužine reči u fajlovima",
        upotreba = "GET /{fileName} - Analiza fajla",
        primer = "GET /test.txt",
        kesStatus = new
        {
            stavki = kes.BrojStavki(),
            info = "Rezultati se keširaju 5 minuta"
        }
    });
})
.WithName("Home");

app.Urls.Add("http://localhost:5050");

app.Run();
