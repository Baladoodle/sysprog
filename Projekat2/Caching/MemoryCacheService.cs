using System.Collections.Concurrent;
using Projekat2.Services;

namespace Projekat2.Caching;


// koriste se taskovi i async/await umesto ReaderWriterLockSlim
public interface IServisKes
{
    Task<T?> UzmiAsync<T>(string kljuc);
    Task PostaviAsync<T>(string kljuc, T vrednost);
    Task OcistiAsync();
    Task<int> BrojStavkiAsync();
}

public class ServisKes : IServisKes
{
    private readonly ConcurrentDictionary<string, Stavka> _kes;
    private readonly SemaphoreSlim _semafor; // za async sinhronizaciju
    private readonly ILogger<ServisKes> _log;
    private const int MaxVelicina = 1000;

    public ServisKes(ILogger<ServisKes> log)
    {
        _kes = new ConcurrentDictionary<string, Stavka>();
        _semafor = new SemaphoreSlim(1, 1); // samo jedan zadatak odjednom za pisanje
        _log = log;
    }

    public async Task<T?> UzmiAsync<T>(string kljuc)
    {
        if (_kes.TryGetValue(kljuc, out var stavka))
        {
            // provera da li je istekao
            if (DateTime.UtcNow < stavka.Istice)
            {
                _log.LogInformation("Keš prona?en: {Kljuc}", kljuc);
                return await Task.FromResult((T?)stavka.Vrednost);
            }

            // istekao - ukloni ga asinhrono
            await _semafor.WaitAsync();
            try
            {
                _kes.TryRemove(kljuc, out _);
                _log.LogInformation("Keš istekao i obrisan: {Kljuc}", kljuc);
            }
            finally
            {
                _semafor.Release();
            }
        }

        return default;
    }

    public async Task PostaviAsync<T>(string kljuc, T vrednost)
    {
        await _semafor.WaitAsync(); // čeka dok semafor ne bude slobodan
        try
        {
            // ako je keš prepun, obriši najstarije asinhrono
            if (_kes.Count >= MaxVelicina)
            {
                var zadatak = Task.Run(() =>
                {
                    var najstariji = _kes
                       .OrderBy(x => x.Value.Kreirano)
                       .Take(_kes.Count / 10)
                       .Select(x => x.Key)
                       .ToList();

                    foreach (var k in najstariji)
                    {
                        _kes.TryRemove(k, out _);
                    }
                });

                await zadatak;
                _log.LogWarning("Keš je prepun, obrisane najstarije stavke");
            }

            var stavka = new Stavka
            {
                Vrednost = vrednost,
                Kreirano = DateTime.UtcNow,
                Istice = DateTime.UtcNow.AddMinutes(5)
            };

            _kes.AddOrUpdate(kljuc, stavka, (k, v) => stavka);
            _log.LogInformation("Stavka keširana: {Kljuc}", kljuc);
        }
        finally
        {
            _semafor.Release(); // oslobađa semafor
        }
    }

    public async Task OcistiAsync()
    {
        await _semafor.WaitAsync();
        try
        {
            _kes.Clear();
            _log.LogInformation("Keš očišćen");
        }
        finally
        {
            _semafor.Release();
        }
    }

    public async Task<int> BrojStavkiAsync()
    {
        // ?itanje iz ConcurrentDictionary je thread-safe
        return await Task.FromResult(_kes.Count);
    }
}

public class Stavka
{
    public object? Vrednost { get; set; }
    public DateTime Kreirano { get; set; }
    public DateTime Istice { get; set; }
}
