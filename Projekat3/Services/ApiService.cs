using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Newtonsoft.Json;
using Projekat3.Models;
using Projekat3.Utils;
using Projekat3.Caching;

namespace Projekat3.Services;

public class ServisApi
{
    private readonly HttpClient _http;
    private readonly ServisKes _kes;
    private const string BaseUrl = "https://gutendex.com/books";

    public ServisApi()
    {
        _http = new HttpClient();
        _http.Timeout = TimeSpan.FromSeconds(30); // posle 30 sekundi baca izuzetak
        _kes = new ServisKes();
    }

    // animacija tokom API poziva
    private async Task<T> TriTacke<T>(Task<T> zadatak, string poruka)
    {
        var cts = new CancellationTokenSource(); // cancelation token za animaciju, služi za prekid animacije
        var animacijaTask = Task.Run(async () =>
        {
            var tackice = new[] { "", ".", "..", "..." };
            int i = 0;
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    Console.Write($"\r{poruka}{tackice[i % tackice.Length]}   ");
                    i++;
                    await Task.Delay(300, cts.Token).ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException)
            {
                // ocekujemo cancellation pa se ignorise
            }
        }, cts.Token);

        try
        {
            var rezultat = await zadatak;

            // zaustavi animaciju
            cts.Cancel();

            // sačekaj da se animacija završi
            try
            {
                await animacijaTask;
            }
            catch (TaskCanceledException)
            {
                // ignorisi
            }

            // obriši liniju
            Console.Write("\r" + new string(' ', poruka.Length + 10) + "\r");

            return rezultat;
        }
        catch
        {
            // zaustavi animaciju
            cts.Cancel();

            // sačekaj da se animacija završi
            try
            {
                await animacijaTask;
            }
            catch (TaskCanceledException)
            {
                // ignorisi
            }

            // obriši liniju
            Console.Write("\r" + new string(' ', poruka.Length + 10) + "\r");

            throw;
        }
        finally
        {
            cts.Dispose();
        }
    }

    // reactive stream - vraća Observable za knjige
    public IObservable<Knjiga> SveKnjigeAutora(string imeAutora)
    {
        var kljuc = $"autor_{imeAutora.ToLowerInvariant()}";

        // pokušaj da uzmeš iz keša prvo
        return _kes.UzmiAsync<List<Knjiga>>(kljuc)
        .SelectMany(kesiraneKnjige =>
        {
            if (kesiraneKnjige != null && kesiraneKnjige.Any())
            {
                Log.Info($"[KEŠ] Vraćam {kesiraneKnjige.Count} knjiga iz keša");
                return kesiraneKnjige.ToObservable();
            }

            // ako nema u kešu, pozovi API
            return PreuzmiSaApi(imeAutora, kljuc);
        });
    }

    private IObservable<Knjiga> PreuzmiSaApi(string imeAutora, string kljuc)
    {
        return Observable.Create<Knjiga>(async (obs, ct) =>
        {
            var sw = Stopwatch.StartNew();
            var threadId = Environment.CurrentManagedThreadId;

            try
            {
                Log.Api($"Pretraga za: {imeAutora} (Thread: {threadId})");

                var url = $"{BaseUrl}?search={Uri.EscapeDataString(imeAutora)}";

                // API poziv
                var apiSw = Stopwatch.StartNew();
                var json = await TriTacke(_http.GetStringAsync(url, ct), "[API] Prikupljaju se podaci");
                apiSw.Stop();
                Log.Perf($"HTTP GET zahtev: {apiSw.ElapsedMilliseconds}ms (Thread: {threadId})");

                var rez = JsonConvert.DeserializeObject<RezultatKnjiga>(json);

                if (rez?.Results == null)
                {
                    Log.Api("Nema rezultata");
                    obs.OnCompleted();
                    return;
                }

                Log.Api($"Pronađeno {rez.Results.Count} knjiga");

                // keširaj rezultate u pozadini
                _ = _kes.PostaviAsync(kljuc, rez.Results).Subscribe();

                foreach (var knjiga in rez.Results)
                {
                    if (!ct.IsCancellationRequested)
                        obs.OnNext(knjiga);
                }

                obs.OnCompleted();

                sw.Stop();
                Log.Perf($"API pretraga završena za {sw.ElapsedMilliseconds}ms (Thread ID: {threadId})");
            }
            catch (Exception ex)
            {
                sw.Stop();
                Log.Err(ex.Message);
                Log.Perf($"API greška nakon {sw.ElapsedMilliseconds}ms (Thread ID: {threadId})");
                obs.OnError(ex);
            }
        })
       .SubscribeOn(TaskPoolScheduler.Default);
    }

    public IObservable<string> UzmiOpise(string imeAutora)
    {
        var sw = Stopwatch.StartNew();

        return SveKnjigeAutora(imeAutora)
        // knjige se obrađuju paralelno
        .Select(knjiga => Observable.Start(() =>
        {
            var threadId = Environment.CurrentManagedThreadId;
            Log.Perf($"Obrada knjige '{knjiga.Naslov?[..Math.Min(50, knjiga.Naslov?.Length ?? 0)]}' na Thread: {threadId}");

            // prvo pokušaj description/summary
            if (!string.IsNullOrWhiteSpace(knjiga.Opis))
            {
                Log.Info($"[API] Pronađen opis za '{knjiga.Naslov}'");
                return new[] { knjiga.Opis }.AsEnumerable();
            }

            // fallback na subjects + title ako nema description
            var delovi = new List<string>();

            if (!string.IsNullOrWhiteSpace(knjiga.Naslov))
                delovi.Add(knjiga.Naslov);

            if (knjiga.Teme != null && knjiga.Teme.Any())
                delovi.AddRange(knjiga.Teme);

            if (delovi.Any())
            {
                Log.Info($"[API] Koristi subjects za '{knjiga.Naslov}' (nema description)");
                return delovi;
            }

            return Enumerable.Empty<string>();
        }, TaskPoolScheduler.Default))
       .Merge(Environment.ProcessorCount) // ograniči broj paralelnih zadataka na broj procesorskih jezgara
       .SelectMany(opisi => opisi.ToObservable()) // Observable<IEnumerable<string>> u Observable<string>
       .Where(opis => !string.IsNullOrWhiteSpace(opis) && opis.Length > 20) // samo smisleni opisi
       .ObserveOn(TaskPoolScheduler.Default) // obrada na ThreadPool
       .Do(opis =>
    {
        sw.Stop();
        Log.Rx($"Primljen: {opis[..Math.Min(100, opis.Length)]}... (Thread: {Environment.CurrentManagedThreadId})");
        sw.Start();
    });
    }
}
