using System.Collections.Concurrent;
using System.Reactive.Linq;
using Projekat3.Utils;

namespace Projekat3.Caching;

public interface IServisKes
{
    IObservable<T> UzmiAsync<T>(string kljuc);
    IObservable<bool> PostaviAsync<T>(string kljuc, T vrednost);
    IObservable<int> OcistiAsync();
    IObservable<int> BrojStavkiAsync();
}

public class ServisKes : IServisKes
{
    private readonly ConcurrentDictionary<string, Stavka> _kes;
    private readonly SemaphoreSlim _semafor; // async sinhronizacija
    private readonly int _maxVelicina;
    private readonly int _expirationMinutes;

    public ServisKes(int maxVelicina = 1000, int expirationMinutes = 5)
    {
        _kes = new ConcurrentDictionary<string, Stavka>();
        _semafor = new SemaphoreSlim(1, 1); // samo jedan task odjednom za pisanje
        _maxVelicina = maxVelicina;
        _expirationMinutes = expirationMinutes;
    }

    public IObservable<T> UzmiAsync<T>(string kljuc)
    {
        return Observable.FromAsync(async () =>
        {
            if (_kes.TryGetValue(kljuc, out var stavka))
            {
                // prvo proveravamo da li je isteklo
                if (DateTime.UtcNow < stavka.Istice)
                {
                    Log.Info($"[KEŠ] Prona?en: {kljuc}");
                    return (T?)stavka.Vrednost;
                }

                // ako je istekao obrisati ga (sa semafor lock)
                await _semafor.WaitAsync();
                try
                {
                    _kes.TryRemove(kljuc, out _);
                    Log.Info($"[KEŠ] Istekao i obrisan: {kljuc}");
                }
                finally
                {
                    _semafor.Release(); // oslobodi semafor
                }
            }

            return default;
        })!;
    }

    public IObservable<bool> PostaviAsync<T>(string kljuc, T vrednost)
    {
        return Observable.FromAsync(async () =>
        {
            // ?eka dok semafor ne bude slobodan - async sinhronizacija
            await _semafor.WaitAsync();
            try
            {
                // ako je keš prepun, obriši najstarije stavke
                if (_kes.Count >= _maxVelicina)
                {
                    // Task.Run koristi ThreadPool za paralelno brisanje
                    var zadatak = Task.Run(() =>
                    {
                        var najstariji = _kes
                        .OrderBy(x => x.Value.Kreirano)
                        .Take(_kes.Count / 10) // obriši 10% najstarijih
                        .Select(x => x.Key)
                        .ToList();

                    foreach (var k in najstariji)
                    {
                        _kes.TryRemove(k, out _);
                    }
                });

                    await zadatak;
                    Log.Info("[KEŠ] Prepun, obrisane najstarije stavke");
                }

                var stavka = new Stavka
                {
                    Vrednost = vrednost,
                    Kreirano = DateTime.UtcNow,
                    Istice = DateTime.UtcNow.AddMinutes(_expirationMinutes) // kešira konfigurisano vreme
               };

               _kes.AddOrUpdate(kljuc, stavka, (k, v) => stavka);
               Log.Info($"[KEŠ] Keširano: {kljuc} (isti?e za {_expirationMinutes}min)");

               return true;
           }
           finally
           {
               _semafor.Release(); // osloba?a semafor
           }
       });
    }

    public IObservable<int> OcistiAsync()
    {
        return Observable.FromAsync(async () =>
    {
        await _semafor.WaitAsync();
        try
        {
            var br = _kes.Count;
            _kes.Clear();
            Log.Info($"[KEŠ] O?iš?en ({br} stavki)");
            return br;
        }
        finally
        {
            _semafor.Release();
        }
    });
    }

    public IObservable<int> BrojStavkiAsync()
    {
        return Observable.Return(_kes.Count);
    }
}

public class Stavka
{
    public object? Vrednost { get; set; }
    public DateTime Kreirano { get; set; }
    public DateTime Istice { get; set; }
}
