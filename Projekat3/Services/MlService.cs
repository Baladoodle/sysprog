using System.Diagnostics;
using Microsoft.ML;
using Microsoft.ML.Data;
using Projekat3.Utils;

namespace Projekat3.Services;

public class ServisMl
{
    private readonly MLContext _ml;

    public ServisMl()
    {
        _ml = new MLContext(seed: 0); // seed je 0 za replikabilne rezultate
    }

    public void AnalizirajTeme(List<string> tekstovi, int brTema = 3)
    {
        var swTotal = Stopwatch.StartNew();
        var threadId = Environment.CurrentManagedThreadId;

        if (tekstovi.Count == 0)
        {
            Log.Ml("Nema tekstova");
            return;
        }

        Log.Ml($"Analiza {tekstovi.Count} opisa... (Thread: {threadId})");

        // faza pripreme podataka
        var sw = Stopwatch.StartNew();
        var podaci = tekstovi.Select(t => new Tekst { Sadrzaj = t }).ToList();
        var view = _ml.Data.LoadFromEnumerable(podaci);
        sw.Stop();
        Log.Perf($"Priprema podataka: {sw.ElapsedMilliseconds}ms (Thread: {threadId})");

        // faza kreiranja pipeline-a
        sw.Restart();
        var pipeline = _ml.Transforms.Text.NormalizeText("Sadrzaj") // normalizacija teksta
            .Append(_ml.Transforms.Text.TokenizeIntoWords("Tokens", "Sadrzaj")) // tokenizacija
            .Append(_ml.Transforms.Text.RemoveDefaultStopWords("Tokens")) // uklanjanje stop reči
            .Append(_ml.Transforms.Conversion.MapValueToKey("Tokens")) // mapiranje tokena u ključeve
            .Append(_ml.Transforms.Text.ProduceNgrams("Tokens")) // ngram ekstrakcija
            .Append(_ml.Transforms.Text.LatentDirichletAllocation("Features", "Tokens", numberOfTopics: brTema)); // modeliranje tema
        sw.Stop();

        Log.Perf($"Kreiranje pipeline-a: {sw.ElapsedMilliseconds}ms (Thread ID: {threadId})"); // pipeline kreiran

        // treniranja modela
        sw.Restart();
        Log.Ml("Treniranje započeto");

        // sam alocira sva dostupna jezgra
        var model = pipeline.Fit(view);

        sw.Stop();
        Log.Perf($"Treniranje modela): {sw.ElapsedMilliseconds}ms (Thread: {threadId})");
        Log.Perf($"- Treniranje koristi {Environment.ProcessorCount} jezgara");

        // transformacija i prikupljanje rezultata
        sw.Restart();
        var trans = model.Transform(view);
        var teme = _ml.Data.CreateEnumerable<RezTema>(trans, false).ToList();
        sw.Stop();
        Log.Perf($"Transformacija i ekstrakcija: {sw.ElapsedMilliseconds}ms (Thread: {threadId})");

        Prikazi(teme, brTema);
        PrikaziTopReci(tekstovi, teme, brTema);

        swTotal.Stop();
        Log.Perf($"ML analiza: {swTotal.ElapsedMilliseconds}ms (Thread: {threadId})");
    }

    private void Prikazi(List<RezTema> rez, int brTema)
    {
        Console.WriteLine($"|    TOPIC MODELING REZULTATI ({brTema} tema)    |");

        // header tabele
        Console.Write("+--------+");
        for (int t = 0; t < brTema; t++)
            Console.Write("----------+");
        Console.WriteLine();

        Console.Write("| Tekst  |");
        for (int t = 0; t < brTema; t++)
            Console.Write($" Tema {t + 1}   |");
        Console.WriteLine();

        Console.Write("+--------+");
        for (int t = 0; t < brTema; t++)
            Console.Write("----------+");
        Console.WriteLine();

        // sadržaj tabele
        for (int i = 0; i < Math.Min(10, rez.Count); i++)
        {
            var r = rez[i];
            Console.Write($"| {i + 1,-6} |");

            for (int t = 0; t < brTema; t++)
            {
                var verovatnoca = r.Features![t];
                if (verovatnoca > 0.1)
                    Console.Write($" {verovatnoca,6:P1}   |");
                else
                    Console.Write("     -    |");
            }
            Console.WriteLine();
        }

        // footer tabele
        Console.Write("+--------+");
        for (int t = 0; t < brTema; t++)
            Console.Write("----------+");
        Console.WriteLine("\n");
    }

    private void PrikaziTopReci(List<string> tekstovi, List<RezTema> teme, int brTema)
    {
        Console.WriteLine("TOP REČI PO TEMAMA");

        // ekstrakcija svih reči iz tekstova
        var sveReci = new Dictionary<string, int>();
        foreach (var tekst in tekstovi)
        {
            var reci = tekst.ToLower()
                .Split(new[] { ' ', ',', '.', ';', ':', '!', '?', '-', '(', ')', '[', ']', '"', '\'' },
            StringSplitOptions.RemoveEmptyEntries);

            foreach (var rec in reci)
            {
                if (rec.Length > 3) // samo reči duže od 3 slova
                {
                    sveReci[rec] = sveReci.GetValueOrDefault(rec, 0) + 1;
                }
            }
        }

        // grupisanje dokumenata po dominantnoj temi
        var dokumentiPoTemama = new Dictionary<int, List<int>>();
        for (int i = 0; i < teme.Count; i++)
        {
            var dominantnaTema = 0;
            var maxVerovatnoca = 0f;

            for (int t = 0; t < brTema; t++)
            {
                if (teme[i].Features![t] > maxVerovatnoca)
                {
                    maxVerovatnoca = teme[i].Features[t];
                    dominantnaTema = t;
                }
            }

            if (!dokumentiPoTemama.ContainsKey(dominantnaTema))
                dokumentiPoTemama[dominantnaTema] = new List<int>();

            dokumentiPoTemama[dominantnaTema].Add(i);
        }

        // prikazi top reči za svaku temu
        for (int t = 0; t < brTema; t++)
        {
            Log.Ml($"TEMA {t + 1}:");

            if (!dokumentiPoTemama.ContainsKey(t) || !dokumentiPoTemama[t].Any())
            {
                Console.WriteLine("  (nema dominantnih dokumenata)\n");
                continue;
            }

            // ekstrahovati reči iz dokumenata ove teme
            var reciTeme = new Dictionary<string, int>();
            foreach (var docIdx in dokumentiPoTemama[t])
            {
                var reci = tekstovi[docIdx].ToLower()
                      .Split(new[] { ' ', ',', '.', ';', ':', '!', '?', '-', '(', ')', '[', ']', '"', '\'' },
                       StringSplitOptions.RemoveEmptyEntries);

                foreach (var rec in reci)
                {
                    if (rec.Length > 3)
                    {
                        reciTeme[rec] = reciTeme.GetValueOrDefault(rec, 0) + 1;
                    }
                }
            }

            // sortiraj i prikazi top 10 reči
            var topReci = reciTeme
                .OrderByDescending(x => x.Value)
                .Take(10)
                .ToList();

            foreach (var rec in topReci)
            {
                Console.WriteLine($"  • {rec.Key,-20} (učestalost: {rec.Value})");
            }

            Console.WriteLine($"  → Broj dokumenata u temi: {dokumentiPoTemama[t].Count}\n");
        }

        Console.WriteLine("═══════════════════════════════════════════════════════════\n");
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
