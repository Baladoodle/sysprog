using System.Text.RegularExpressions;
using Projekat2.Caching;

namespace Projekat2.Services;

public interface IServisAnaliza
{
    Task<RezultatAnalize> AnalizirajAsync(string imeFajla, string rootPath);
}

public class ServisAnaliza : IServisAnaliza
{
    private const int MaxDubina = 10;
    private readonly ILogger<ServisAnaliza> _log;
    private readonly IServisKes _kes;

    public ServisAnaliza(ILogger<ServisAnaliza> log, IServisKes kes)
    {
        _log = log;
        _kes = kes;
    }

    public async Task<RezultatAnalize> AnalizirajAsync(string imeFajla, string rootPath)
    {
        // provera keša koristi async metodu
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
            // pretraga fajla koristi async rekurziju
            var putanja = await PronadjiAsync(imeFajla, rootPath, 0);

            if (putanja == null)
                return new RezultatAnalize { Greska = $"Fajl '{imeFajla}' nije prona?en" };

            var rez = await IzracunajProsekAsync(putanja);

            // keširanje koristi async metodu
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
        if (dubina > MaxDubina)
            return null;

        try
        {
            // paralelno pretraži fajlove i podfoldere taskovima
            var trazenjeTask = Task.Run(() => Directory.GetFiles(putanja, imeFajla));
            var folderiTask = Task.Run(() => Directory.GetDirectories(putanja));

            // čeka da se svi završe
            await Task.WhenAll(trazenjeTask, folderiTask);

            var fajlovi = await trazenjeTask;
            if (fajlovi.Length > 0)
                return fajlovi[0];

            var folderi = await folderiTask;

            // rekurzivna pretraga kroz subfoldere
            var pretrageZadaci = folderi
                .Select(folder => PronadjiAsync(imeFajla, folder, dubina + 1))
                .ToList();

            // čeka sve pretrage da se završe
            var rezultati = await Task.WhenAll(pretrageZadaci);

            // vratimo prvi pronađeni fajl
            return rezultati.FirstOrDefault(r => r != null);
        }
        catch (UnauthorizedAccessException)
        {
            _log.LogWarning("Nema pristupa folderu: {Put}", putanja);
        }

        return null;
    }

    private async Task<RezultatAnalize> IzracunajProsekAsync(string putanja)
    {
        // čitanje fajla pa se koristi async
        var sadrzaj = await File.ReadAllTextAsync(putanja);

        if (string.IsNullOrWhiteSpace(sadrzaj))
            return new RezultatAnalize
            {
                Ime = Path.GetFileName(putanja),
                Poruka = "Fajl je prazan",
                Prazan = true
            };

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
                Poruka = "Fajl ne sadrži re?i",
                Prazan = true
            };

        // računanje proseka paralelno
        var prosek = await Task.Run(() => reci.Average(r => r.Length));

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
