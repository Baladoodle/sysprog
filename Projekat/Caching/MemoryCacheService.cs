using System.Collections.Concurrent;
using System.Threading;
using Projekat.Services;

namespace Projekat.Caching;

/// <summary>
/// Thread-safe keš za memoriju sa ReaderWriterLockSlim za optimalno ?itanje
/// </summary>
public interface IServisKes
{
    Task<T?> UzmiAsync<T>(string kljuc);
    Task PostaviAsync<T>(string kljuc, T vrednost);
    void Ocisti();
    int BrojStavki();
}

public class ServisKes : IServisKes
{
    private readonly ConcurrentDictionary<string, Stavka> _kes;
 private readonly ReaderWriterLockSlim _lock; // sinhronizacija - ?itanja bez lock, pisanja sa lock
    private readonly ILogger<ServisKes> _log;
    private const int MaxVelicina = 1000; // limite keširanje da ne raste preusmeren memoriju

  public ServisKes(ILogger<ServisKes> log)
    {
      _kes = new ConcurrentDictionary<string, Stavka>();
  _lock = new ReaderWriterLockSlim();
  _log = log;
    }

    public async Task<T?> UzmiAsync<T>(string kljuc)
    {
    // ?itanje sa ReaderLock - više threadova može ?itati istovremeno
        _lock.EnterReadLock();
        try
  {
   if (_kes.TryGetValue(kljuc, out var stavka))
   {
      // prvo proveravamo da li je isteklo
  if (DateTime.UtcNow < stavka.Istice)
  {
 _log.LogInformation("Keš prona?en: {Kljuc}", kljuc);
           return (T?)stavka.Vrednost;
   }

    // isteklo - trebam ga obrisati (ali to radi sa write lock)
 _lock.ExitReadLock();
    _lock.EnterWriteLock();
       try
        {
 _kes.TryRemove(kljuc, out _);
      _log.LogInformation("Keš istekao i obrisan: {Kljuc}", kljuc);
    }
        finally
         {
 _lock.ExitWriteLock();
      }

 return default;
}

   return await Task.FromResult<T?>(default);
        }
 finally
        {
         // osloba?anje lock-a samo ako još uvek imamo read lock
     if (_lock.IsReadLockHeld)
       _lock.ExitReadLock();
    }
    }

    public async Task PostaviAsync<T>(string kljuc, T vrednost)
    {
        // pisanje sa WriteLock - samo jedan thread može pisati
   _lock.EnterWriteLock();
      try
        {
     // ako je keš prepun, obriši najstarije stavke
       if (_kes.Count >= MaxVelicina)
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

      _log.LogWarning("Keš je prepun, obrisane najstarije stavke");
     }

         var stavka = new Stavka
            {
   Vrednost = vrednost,
      Kreirano = DateTime.UtcNow,
     Istice = DateTime.UtcNow.AddMinutes(5) // kešira 5 minuta
     };

        _kes.AddOrUpdate(kljuc, stavka, (k, v) => stavka);
 _log.LogInformation("Stavka keširana: {Kljuc}", kljuc);

         await Task.CompletedTask;
        }
     finally
        {
     _lock.ExitWriteLock();
        }
}

    public void Ocisti()
    {
        _lock.EnterWriteLock();
    try
{
      _kes.Clear();
            _log.LogInformation("Keš o?iš?en");
    }
   finally
        {
    _lock.ExitWriteLock();
        }
    }

    public int BrojStavki()
    {
        _lock.EnterReadLock();
        try
      {
   return _kes.Count;
      }
        finally
  {
         _lock.ExitReadLock();
        }
    }
}

public class Stavka
{
    public object? Vrednost { get; set; }
    public DateTime Kreirano { get; set; }
    public DateTime Istice { get; set; }
}
