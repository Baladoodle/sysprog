using System.Text.RegularExpressions;
using Projekat.Caching;

namespace Projekat.Services;

public interface IServisAnaliza
{
    Task<RezultatAnalize> AnalizirajAsync(string imeFajla, string rootPath);
}

public class ServisAnaliza : IServisAnaliza
{
  private const int MaxDubina = 10; // sprečavanje rekursivne pretrage 
    private readonly ILogger<ServisAnaliza> _log;
    private readonly IServisKes _kes; // keš za memoizaciju rezultata

    public ServisAnaliza(ILogger<ServisAnaliza> log, IServisKes kes)
    {
        _log = log;
        _kes = kes;
    }

    public async Task<RezultatAnalize> AnalizirajAsync(string imeFajla, string rootPath)
    {
        // prvo proveravamo keš - ako postoji, vraćamo odmah
   var kljuc = $"analiza_{imeFajla}_{rootPath}".ToLower();
        var kesan = await _kes.UzmiAsync<RezultatAnalize>(kljuc);
        if (kesan != null)
  {
    _log.LogInformation("Vraćen rezultat iz keša za: {Ime}", imeFajla);
   return kesan;
  }

  if (string.IsNullOrWhiteSpace(imeFajla))
            return new RezultatAnalize { Greska = "Naziv fajla ne sme biti prazan" };

    // validacija imena fajla
        if (imeFajla.Contains("..") || imeFajla.Contains("/") || imeFajla.Contains("\\"))
       return new RezultatAnalize { Greska = "Nevažeće ime fajla" };

        try
        {
        var putanja = await PronadjiAsync(imeFajla, rootPath, 0);

            if (putanja == null)
          return new RezultatAnalize { Greska = $"Fajl '{imeFajla}' nije pronađen" };

    var rez = await IzracunajProsekAsync(putanja);
   
     // ako je uspešna analiza, keširaj rezultat
            if (rez.Uspesno)
          {
  await _kes.PostaviAsync(kljuc, rez);
            }
     
  return rez;
        }
 catch (Exception ex)
{
       _log.LogError(ex, "Greška pri analizi fajla: {Ime}", imeFajla);
         return new RezultatAnalize { Greska = "Došlo je do greške pri analizi fajla" };
    }
    }

    private async Task<string?> PronadjiAsync(string imeFajla, string putanja, int dubina)
 {
        // sprečavanje prevelike dubine pretrage
        if (dubina > MaxDubina)
        return null;

        try
     {
        // Task.Run koristi ThreadPool za asinkrionu obradu
  var fajlovi = await Task.Run(() => Directory.GetFiles(putanja, imeFajla));
   if (fajlovi.Length > 0)
  return fajlovi[0];

 var folderi = await Task.Run(() => Directory.GetDirectories(putanja));
    foreach (var folder in folderi)
            {
        var rez = await PronadjiAsync(imeFajla, folder, dubina + 1);
  if (rez != null)
      return rez;
            }
        }
        catch (UnauthorizedAccessException)
    {
        // nema pristupa folderu
      _log.LogWarning("Nema pristupa folderu: {Put}", putanja);
        }

    return null;
    }

    private async Task<RezultatAnalize> IzracunajProsekAsync(string putanja)
    {
     var sadrzaj = await File.ReadAllTextAsync(putanja);

   // da li je fajl prazan
        if (string.IsNullOrWhiteSpace(sadrzaj))
   return new RezultatAnalize
     {
      Ime = Path.GetFileName(putanja),
  Poruka = "Fajl je prazan",
          Prazan = true
   };

        // sve što nije slovo ili cifra se smatra separatorom
        // Task.Run koristi ThreadPool za CPU-intenzivnu regex obradu
  var reci = await Task.Run(() =>
         Regex
     .Matches(sadrzaj, @"\b[\p{L}\d]+\b")
  .Cast<Match>()
            .Select(m => m.Value)
     .ToList()
 );

        if (reci.Count == 0)
    return new RezultatAnalize
  {
          Ime = Path.GetFileName(putanja),
      Poruka = "Fajl ne sadrži reči",
 Prazan = true
            };

        var prosek = reci.Average(r => r.Length);

     return new RezultatAnalize
  {
         Ime = Path.GetFileName(putanja),
       ProsecnaDuzina = prosek,
            BrojReci = reci.Count,
        UkupnoKaraktera = sadrzaj.Length
   };
    }
}

public class RezultatAnalize
{
    public string Ime { get; set; } = string.Empty;
    public double ProsecnaDuzina { get; set; }
    public int BrojReci { get; set; }
    public int UkupnoKaraktera { get; set; }
    public bool Prazan { get; set; }
public string? Poruka { get; set; }
    public string? Greska { get; set; }

    public bool Uspesno => Greska == null;
}
