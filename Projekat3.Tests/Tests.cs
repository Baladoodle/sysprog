using Xunit;
using Projekat3.Services;
using System.Reactive.Linq;

namespace Projekat3.Tests;

public class TestoviApi
{
    [Fact]
    public async Task Api_VracaKnjige()
    {
        // test da observable vraća knjige
        var api = new ServisApi();
        var knjige = await api.SveKnjigeAutora("Shakespeare")
           .Take(5) // samo prvih 5
           .ToList()
           .ToTask();

        Assert.NotEmpty(knjige);
    }

    [Fact]
    public async Task Api_VracaOpise()
    {
        // test da vraća opise
        var api = new ServisApi();
        var opisi = await api.UzmiOpise("Dickens")
           .Take(10)
           .ToList()
           .ToTask();

        Assert.NotEmpty(opisi);
    }
}

public class TestoviMl
{
    [Fact]
    public void Ml_AnaliziraTeme()
    {
        // test da ML radi bez exception
        var ml = new ServisMl();
        var tekstovi = new List<string>
    {
        "Fiction Literature Drama",
        "Science Technology Engineering",
        "History Politics War"
    };

        // ne sme exception
        ml.AnalizirajTeme(tekstovi, 2);
    }

    [Fact]
    public void Ml_RukovaPraznomListom()
    {
        // test prazne liste
        var ml = new ServisMl();
        var tekstovi = new List<string>();

        // ne sme exception
        ml.AnalizirajTeme(tekstovi);
    }
}
