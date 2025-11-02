using Microsoft.ML;
using Microsoft.ML.Data;
using Projekat3.Utils;

namespace Projekat3.Services;

public class ServisMl
{
    private readonly MLContext _ml;

    public ServisMl()
    {
        _ml = new MLContext(seed: 0);
    }

    public void AnalizirajTeme(List<string> tekstovi, int brTema = 3)
    {
        if (tekstovi.Count == 0)
        {
            Log.Ml("Nema tekstova");
            return;
        }

        Log.Ml($"Analiza {tekstovi.Count} opisa...");

        var podaci = tekstovi.Select(t => new Tekst { Sadrzaj = t }).ToList();
        var view = _ml.Data.LoadFromEnumerable(podaci);

        var pipeline = _ml.Transforms.Text.NormalizeText("Sadrzaj")
           .Append(_ml.Transforms.Text.TokenizeIntoWords("Tokens", "Sadrzaj"))
           .Append(_ml.Transforms.Text.RemoveDefaultStopWords("Tokens"))
           .Append(_ml.Transforms.Conversion.MapValueToKey("Tokens"))
           .Append(_ml.Transforms.Text.ProduceNgrams("Tokens"))
           .Append(_ml.Transforms.Text.LatentDirichletAllocation("Features", "Tokens", numberOfTopics: brTema));

        var model = pipeline.Fit(view);
        var trans = model.Transform(view);
        var teme = _ml.Data.CreateEnumerable<RezTema>(trans, false).ToList();

        Prikazi(teme, brTema);
    }

    private void Prikazi(List<RezTema> rez, int brTema)
    {
        Console.WriteLine($"\n=== REZULTATI ({brTema} tema) ===\n");

        for (int i = 0; i < Math.Min(10, rez.Count); i++)
        {
            var r = rez[i];
            Console.WriteLine($"Tekst {i + 1}:");

            for (int t = 0; t < brTema; t++)
            {
                var verovatnoca = r.Features![t];
                if (verovatnoca > 0.1)
                    Console.WriteLine($"  Tema {t + 1}: {verovatnoca:P1}");
            }
            Console.WriteLine();
        }
    }
}

public class Tekst
{
    public string Sadrzaj { get; set; } = "";
}

public class RezTema
{
    [VectorType]
    public float[]? Features { get; set; }
}
