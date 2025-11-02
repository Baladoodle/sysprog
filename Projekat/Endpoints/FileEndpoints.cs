using Projekat.Services;
using Projekat.Caching;

namespace Projekat.Endpoints;

public static class Rute
{
    public static void MapujRute(this WebApplication app)
    {
    app.MapGet("/{fileName}", Analiziraj)
       .WithName("Analiziraj")
          .Produces<OdgovorAnalize>(200) // uspešan odgovor
   .Produces<OdgovorGreske>(400) // loš zahtev
            .Produces<OdgovorGreske>(404); // fajl nije pronađen
    }

    private static async Task<IResult> Analiziraj(
  string fileName,
  IServisAnaliza servis,
 IServisKes kes,
     ILogger<Program> log,
    HttpContext ctx)
    {
var vreme = DateTime.UtcNow;
   var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "UNKNOWN";

  log.LogInformation(
   "[{Vreme:yyyy-MM-dd HH:mm:ss}] Zahtev primljen - IP: {Ip}, Fajl: {Ime}, Keš stavki: {Br}",
         vreme, ip, fileName, kes.BrojStavki());

    var rootPath = AppContext.BaseDirectory;
var rez = await servis.AnalizirajAsync(fileName, rootPath);

     if (!rez.Uspesno)
    {
      log.LogError(
      "[{Vreme:yyyy-MM-dd HH:mm:ss}] GREŠKA - IP: {Ip}, Fajl: {Ime}, Razlog: {Greska}",
  vreme, ip, fileName, rez.Greska);
      return Results.BadRequest(new OdgovorGreske { Greska = rez.Greska! });
      }

 if (rez.Prazan)
        {
      log.LogWarning(
   "[{Vreme:yyyy-MM-dd HH:mm:ss}] PRAZAN FAJL - IP: {Ip}, Fajl: {Ime}",
 vreme, ip, fileName);

        var prazan = new OdgovorAnalize
       {
 Ime = rez.Ime,
     Poruka = rez.Poruka
    };
    return Results.Ok(prazan);
  }

      var odg = new OdgovorAnalize
        {
    Ime = rez.Ime,
     ProsecnaDuzina = Math.Round(rez.ProsecnaDuzina, 2),
     BrojReci = rez.BrojReci,
       UkupnoKaraktera = rez.UkupnoKaraktera
      };

 log.LogInformation(
   "[{Vreme:yyyy-MM-dd HH:mm:ss}] USPEŠNO - IP: {Ip}, Fajl: {Ime}, Prosek: {Pros}, Reči: {Br}",
    vreme, ip, fileName, odg.ProsecnaDuzina, rez.BrojReci);

  return Results.Ok(odg);
    }
}

public class OdgovorAnalize
{
    public string Ime { get; set; } = string.Empty;
    public double? ProsecnaDuzina { get; set; }
    public int BrojReci { get; set; }
    public int UkupnoKaraktera { get; set; }
  public string? Poruka { get; set; }
}

public class OdgovorGreske
{
    public string Greska { get; set; } = string.Empty;
}
