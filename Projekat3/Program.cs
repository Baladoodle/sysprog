using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Projekat3.Services;
using Projekat3.Utils;

namespace Projekat3;

class Program
{
    static async Task Main(string[] args)
    {
        Console.Write("Unesi ime autora (npr. 'Shakespeare'): ");
        var autor = Console.ReadLine() ?? "Shakespeare";
        var api = new ServisApi();
        var ml = new ServisMl();
        var opisi = new List<string>();

        Log.Rx("Stream pokrenut...\n");

        // reactive pipeline sa ToTask() za await
        var task = api.UzmiOpise(autor)
            .Do(o =>
            {
                Log.Rx($"Primljen: {o[..Math.Min(50, o.Length)]}...");
                opisi.Add(o);
            })
            .ToList() // sakupi sve u listu
            .ToTask(); // konvertuj u Task za await

        try
        {
            await task;
            Log.Rx($"\nStream završen. Ukupno: {opisi.Count} opisa\n");

            // topic modeling
            if (opisi.Count > 0)
                ml.AnalizirajTeme(opisi);
            else
                Log.Info("Nema podataka");
        }
        catch (Exception ex)
        {
            Log.Err(ex.Message);
        }

        Console.WriteLine("\nPritisni bilo koji taster...");
        Console.ReadKey();
    }
}
