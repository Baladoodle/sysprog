using Projekat2.Caching;
using Projekat2.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Projekat2.Tests;

public class CacheServiceTests
{
    private ILogger<ServisKes> KreirajLogger()
    {
 var factory = LoggerFactory.Create(builder => builder.AddConsole());
   return factory.CreateLogger<ServisKes>();
    }

    [Fact]
    public async Task PostaviAsync_UzmiAsync_VracaIspravnuVrednost()
    {
        // Arrange
        var log = KreirajLogger();
        var kes = new ServisKes(log);
    var kljuc = "test_kljuc";
    var vrednost = "test_vrednost";

        // Act
        await kes.PostaviAsync(kljuc, vrednost);
        var rezultat = await kes.UzmiAsync<string>(kljuc);

        // Assert
        Assert.Equal(vrednost, rezultat);
    }

    [Fact]
    public async Task UzmiAsync_NepostojeciKljuc_VracaNull()
    {
        // Arrange
        var log = KreirajLogger();
        var kes = new ServisKes(log);

        // Act
        var rezultat = await kes.UzmiAsync<string>("nepostojeci_kljuc");

        // Assert
    Assert.Null(rezultat);
    }

    [Fact]
    public async Task BrojStavkiAsync_PraznogKesa_VracaNula()
    {
   // Arrange
     var log = KreirajLogger();
        var kes = new ServisKes(log);

        // Act
        var broj = await kes.BrojStavkiAsync();

        // Assert
        Assert.Equal(0, broj);
    }

    [Fact]
    public async Task BrojStavkiAsync_NakonDodavanja_VracaTacanBroj()
    {
        // Arrange
        var log = KreirajLogger();
      var kes = new ServisKes(log);

        // Act
        await kes.PostaviAsync("kljuc1", "vrednost1");
        await kes.PostaviAsync("kljuc2", "vrednost2");
        await kes.PostaviAsync("kljuc3", "vrednost3");
        var broj = await kes.BrojStavkiAsync();

        // Assert
        Assert.Equal(3, broj);
    }

    [Fact]
    public async Task OcistiAsync_BriseSveStavke()
    {
        // Arrange
        var log = KreirajLogger();
        var kes = new ServisKes(log);
        await kes.PostaviAsync("kljuc1", "vrednost1");
        await kes.PostaviAsync("kljuc2", "vrednost2");

        // Act
        await kes.OcistiAsync();
        var broj = await kes.BrojStavkiAsync();

        // Assert
        Assert.Equal(0, broj);
    }

    [Fact]
    public async Task PostaviAsync_IstKljuc_AzuriraVrednost()
 {
        // Arrange
        var log = KreirajLogger();
        var kes = new ServisKes(log);
        var kljuc = "test_kljuc";

        // Act
        await kes.PostaviAsync(kljuc, "stara_vrednost");
        await kes.PostaviAsync(kljuc, "nova_vrednost");
        var rezultat = await kes.UzmiAsync<string>(kljuc);

        // Assert
 Assert.Equal("nova_vrednost", rezultat);
    }

[Fact]
  public async Task ParalelnoPostavljanje_NeProuzrokujeProblem()
    {
        // Arrange
        var log = KreirajLogger();
        var kes = new ServisKes(log);
   var brojZadataka = 100;

   // Act - paralelno dodavanje u keš
    var zadaci = Enumerable.Range(0, brojZadataka)
            .Select(i => kes.PostaviAsync($"kljuc_{i}", $"vrednost_{i}"))
     .ToArray();

        await Task.WhenAll(zadaci);
var broj = await kes.BrojStavkiAsync();

        // Assert
        Assert.Equal(brojZadataka, broj);
    }

    [Fact]
    public async Task ParalelnoCitanjeIPisanje_NeBlokira()
    {
        // Arrange
        var log = KreirajLogger();
        var kes = new ServisKes(log);
    await kes.PostaviAsync("test", "vrednost");

   // Act - miks ?itanja i pisanja
        var zadaci = new List<Task>();
        for (int i = 0; i < 50; i++)
        {
        zadaci.Add(kes.UzmiAsync<string>("test"));
            zadaci.Add(kes.PostaviAsync($"key_{i}", $"value_{i}"));
  }

        // Assert - ne bi trebalo da baci exception
        await Task.WhenAll(zadaci);
        var broj = await kes.BrojStavkiAsync();
        Assert.True(broj > 0);
    }
}

public class FileAnalysisServiceTests
{
    private ILogger<ServisAnaliza> KreirajAnalizaLogger()
    {
        var factory = LoggerFactory.Create(builder => builder.AddConsole());
   return factory.CreateLogger<ServisAnaliza>();
    }

    private ILogger<ServisKes> KreirajKesLogger()
    {
  var factory = LoggerFactory.Create(builder => builder.AddConsole());
        return factory.CreateLogger<ServisKes>();
    }

    [Fact]
    public async Task AnalizirajAsync_PrazanNaziv_VracaGresku()
    {
        // Arrange
        var logAnaliza = KreirajAnalizaLogger();
        var logKes = KreirajKesLogger();
        var kes = new ServisKes(logKes);
        var servis = new ServisAnaliza(logAnaliza, kes);

 // Act
        var rezultat = await servis.AnalizirajAsync("", AppContext.BaseDirectory);

        // Assert
        Assert.False(rezultat.Uspesno);
        Assert.NotNull(rezultat.Greska);
    }

    [Fact]
    public async Task AnalizirajAsync_NevalidnoIme_VracaGresku()
    {
     // Arrange
        var logAnaliza = KreirajAnalizaLogger();
        var logKes = KreirajKesLogger();
        var kes = new ServisKes(logKes);
        var servis = new ServisAnaliza(logAnaliza, kes);

  // Act
  var rezultat = await servis.AnalizirajAsync("../test.txt", AppContext.BaseDirectory);

        // Assert
        Assert.False(rezultat.Uspesno);
      Assert.Contains("Nevaže?e", rezultat.Greska);
    }

    [Fact]
    public async Task AnalizirajAsync_NepostojeciFajl_VracaGresku()
    {
        // Arrange
        var logAnaliza = KreirajAnalizaLogger();
        var logKes = KreirajKesLogger();
        var kes = new ServisKes(logKes);
        var servis = new ServisAnaliza(logAnaliza, kes);

 // Act
      var rezultat = await servis.AnalizirajAsync("nepostojeci_fajl_12345.txt", AppContext.BaseDirectory);

        // Assert
        Assert.False(rezultat.Uspesno);
        Assert.Contains("nije prona?en", rezultat.Greska);
    }

    [Fact]
    public async Task AnalizirajAsync_DvaputaIstiZahtev_KoristiKes()
    {
        // Arrange
     var logAnaliza = KreirajAnalizaLogger();
     var logKes = KreirajKesLogger();
        var kes = new ServisKes(logKes);
        var servis = new ServisAnaliza(logAnaliza, kes);

   // Kreiraj test fajl
      var testFajl = Path.Combine(AppContext.BaseDirectory, "test_cache.txt");
    await File.WriteAllTextAsync(testFajl, "test re? dva tri");

        try
        {
            // Act
            var rezultat1 = await servis.AnalizirajAsync("test_cache.txt", AppContext.BaseDirectory);
         var rezultat2 = await servis.AnalizirajAsync("test_cache.txt", AppContext.BaseDirectory);

   // Assert - oba poziva bi trebala vratiti isti rezultat
            Assert.True(rezultat1.Uspesno);
        Assert.True(rezultat2.Uspesno);
       Assert.Equal(rezultat1.ProsecnaDuzina, rezultat2.ProsecnaDuzina);
   Assert.Equal(rezultat1.BrojReci, rezultat2.BrojReci);
    }
        finally
        {
    // Cleanup
      if (File.Exists(testFajl))
           File.Delete(testFajl);
        }
}
}
