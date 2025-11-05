using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Projekat3.Services;
using Projekat3.Utils;

namespace Projekat3;

class Program
{
    static async Task Main(string[] args)
    {
        var mainThreadId = Environment.CurrentManagedThreadId;

        // inicijalizacija servisa - api i ml
        var api = new ServisApi();
        var ml = new ServisMl();

        while (true)
        {
            Console.Write("\nUnesi ime autora ('Shakespeare'): ");
            var autor = Console.ReadLine()?.Trim();

            // timer krece posle unosa
            var swTotal = Stopwatch.StartNew();

            var opisi = new List<string>();
            var lockObj = new object(); // thread safe lock

            Log.Rx("Stream pokrenut...\n");

            // reaktivni stream za uzimanje opisa knjiga
            var swStream = Stopwatch.StartNew();
            var task = api.UzmiOpise(autor).Do(o =>
            {
                var threadId = Environment.CurrentManagedThreadId;
                lock (lockObj) // thread safe dodavanje
                {
                    opisi.Add(o);
                }
            })
            .ToList() // sakupi sve u listu
            .ToTask(); // konvertuj u Task za await

            try
            {
                await task; // čeka da se stream završi
                swStream.Stop(); // zaustavi merenje vremena API poziva

                Log.Rx($"\nZavršeno. Ukupno: {opisi.Count} opisa");
                Log.Perf($"Reactive stream: {swStream.ElapsedMilliseconds}ms");
                Log.Perf($"Prosečno vreme po opisu: {(opisi.Count > 0 ? swStream.ElapsedMilliseconds / (double)opisi.Count : 0):F2}ms\n");

                // topic modeling na zasebnom thread
                if (opisi.Count > 0)
                {
                    await Task.Run(() => ml.AnalizirajTeme(opisi));
                }
                else
                    Log.Info("Nema podataka");
            }
            catch (Exception ex)
            {
                swStream.Stop();
                Log.Err(ex.Message);
                Log.Perf($"Stream prekinut nakon {swStream.ElapsedMilliseconds}ms");
            }

            swTotal.Stop();

            // konacni izveštaj
            Log.Perf($"Ukupno vreme izvršavanja: {swTotal.ElapsedMilliseconds}ms");
            Log.Perf($"Završeno na thread: {Environment.CurrentManagedThreadId}");
            Console.WriteLine("\n" + new string('=', 60));
        }

        Log.Info("Program završen.");
    }
}
