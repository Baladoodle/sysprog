using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using Projekat.Services;
using Projekat.Caching;

namespace Projekat.Tests;

public class TestoviKesa : IDisposable
{
    private readonly ServisKes _kes;
    private readonly Mock<ILogger<ServisKes>> _logMock;
  private readonly ServisAnaliza _servis;
    private readonly Mock<ILogger<ServisAnaliza>> _svcLogMock;
    private readonly string _tmpDir;

    public TestoviKesa()
    {
  // Setup keša i servisa
   _tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
     Directory.CreateDirectory(_tmpDir);

        _logMock = new Mock<ILogger<ServisKes>>();
    _kes = new ServisKes(_logMock.Object);

      _svcLogMock = new Mock<ILogger<ServisAnaliza>>();
   _servis = new ServisAnaliza(_svcLogMock.Object, _kes);
    }

  [Fact]
    public async Task Kes_CuvaIVraca()
    {
        // provera da keš ?uva i vra?a vrednosti
    const string kljuc = "test_key";
        var vrednost = "test_value";

      // postavi vrednost
  await _kes.PostaviAsync(kljuc, vrednost);

        // preuzmi vrednost
     var preuzeto = await _kes.UzmiAsync<string>(kljuc);

        // trebalo bi da bude ista vrednost
        Assert.Equal(vrednost, preuzeto);
  }

 [Fact]
    public async Task Kes_VracaPoslednju()
    {
  // provera da keš vra?a poslednju vrednost
       const string kljuc = "test_key";

        await _kes.PostaviAsync(kljuc, "value1");
    await _kes.PostaviAsync(kljuc, "value2");
  var preuzeto = await _kes.UzmiAsync<string>(kljuc);

     Assert.Equal("value2", preuzeto);
    }

    [Fact]
    public async Task Servis_KesiraRezultate()
    {
 // provera da se rezultati analize keširaju
   const string ime = "cache_test.txt";
        var putanja = Path.Combine(_tmpDir, ime);
 await File.WriteAllTextAsync(putanja, "Ovo je test fajl");

   // prvi zahtev - ide direktno u analizu
        var rez1 = await _servis.AnalizirajAsync(ime, _tmpDir);

     // drugi zahtev - trebalo bi iz keša
   var rez2 = await _servis.AnalizirajAsync(ime, _tmpDir);

        // oba trebalo bi da budu ista
     Assert.Equal(rez1.ProsecnaDuzina, rez2.ProsecnaDuzina);
      Assert.Equal(rez1.BrojReci, rez2.BrojReci);
    }

 [Fact]
    public async Task Kes_OcistiUklanjaSve()
    {
     // provera da Ocisti() uklanja sve stavke
      await _kes.PostaviAsync("key1", "value1");
    await _kes.PostaviAsync("key2", "value2");

var brPre = _kes.BrojStavki();
 _kes.Ocisti();
       var brPosle = _kes.BrojStavki();

    Assert.Equal(2, brPre);
   Assert.Equal(0, brPosle);
    }

  [Fact]
    public async Task Kes_ThreadSafe()
    {
        // provera thread-safety - više threadova ?ita istovremeno
  const string kljuc = "multi_read_key";
     await _kes.PostaviAsync(kljuc, "test_value");

 var taskovi = Enumerable.Range(0, 10)
  .Select(async _ => await _kes.UzmiAsync<string>(kljuc))
         .ToArray();

        var rez = await Task.WhenAll(taskovi);

        // svi trebalo bi da dobiju istu vrednost
 Assert.All(rez, r => Assert.Equal("test_value", r));
}

   [Fact]
    public async Task Kes_RukovaNull()
    {
 // provera rukovanja sa null vrednostima
        const string kljuc = "null_key";

    // postavi null
     await _kes.PostaviAsync<string?>(kljuc, null);

       var preuzeto = await _kes.UzmiAsync<string?>(kljuc);

  Assert.Null(preuzeto);
    }

    [Fact]
    public async Task Kes_LimitVelicine()
    {
        // provera da se keš ne raste preusmeren
        // postavimo mnogo stavki da vidimo da li ?e biti obrisane najstarije

 for (int i = 0; i < 1050; i++)
    {
     await _kes.PostaviAsync($"key_{i}", $"value_{i}");
      }

     // trebalo bi da je smanjen jer je limit 1000
  var br = _kes.BrojStavki();
   Assert.True(br <= 1000, $"Keš je presko?io limit: {br}");
    }

  public void Dispose()
   {
        try
   {
     _kes.Ocisti();
       if (Directory.Exists(_tmpDir))
       Directory.Delete(_tmpDir, true);
        }
    catch { /* ignoriši greške pri brisanju */ }
    }
}
