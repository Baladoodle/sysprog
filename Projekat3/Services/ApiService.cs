using System.Reactive.Linq;
using Newtonsoft.Json;
using Projekat3.Models;
using Projekat3.Utils;

namespace Projekat3.Services;

public class ServisApi
{
    private readonly HttpClient _http;
    private const string BaseUrl = "https://gutendex.com/books";

    public ServisApi()
    {
        _http = new HttpClient();
    }

    // reactive stream - vraća Observable za knjige
    public IObservable<Knjiga> SveKnjigeAutora(string imeAutora)
    {
        return Observable.Create<Knjiga>(async (obs, ct) =>
        {
            try
            {
                Log.Api($"Pretraga za: {imeAutora}");

                var url = $"{BaseUrl}?search={Uri.EscapeDataString(imeAutora)}";
                var json = await _http.GetStringAsync(url, ct);
                var rez = JsonConvert.DeserializeObject<RezultatKnjiga>(json);

                if (rez?.Results == null)
                {
                    Log.Api("Nema rezultata");
                    obs.OnCompleted();
                    return;
                }

                Log.Api($"Pronađeno {rez.Results.Count} knjiga");

                foreach (var knjiga in rez.Results)
                {
                    if (!ct.IsCancellationRequested)
                        obs.OnNext(knjiga);
                }

                obs.OnCompleted();
            }
            catch (Exception ex)
            {
                Log.Err(ex.Message);
                obs.OnError(ex);
            }
        });
    }

    public IObservable<string> UzmiOpise(string imeAutora)
    {
        return SveKnjigeAutora(imeAutora)
              .Where(k => k.Teme != null && k.Teme.Any())
              .SelectMany(k => k.Teme!)
              .Distinct();
    }
}
