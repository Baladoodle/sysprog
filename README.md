# Sistemsko Programiranje - Projekti

## Pregled

Ovaj workspace sadrži **3 projekta** za kurs Sistemskog Programiranja:

- **Projekat 1 & 2**: Web server za analizu prose?ne dužine re?i u fajlovima
- **Projekat 3**: Reactive programming sa Gutendex API i ML.NET topic modeling

---

## ?? Brzo Pokretanje

### Projekat 1 & 2 - Web Server

```bash
# Pokreni server
dotnet run --project Projekat

# Server sluša na http://localhost:5050
```

**Testiranje:**
```bash
# U browser-u ili curl
curl http://localhost:5050/primer.txt
curl http://localhost:5050/health
```

**Klju?ne Funkcionalnosti:**
- ? Analiza prose?ne dužine re?i u fajlovima
- ? Rekurzivna pretraga kroz podfoldere
- ? In-memory keširanje sa `ReaderWriterLockSlim`
- ? ThreadPool koriš?enje sa `Task.Run()`
- ? Detaljno logovanje svih zahteva
- ? Thread-safe operacije

### Projekat 3 - Reactive Programming + ML

```bash
# Pokreni aplikaciju
dotnet run --project Projekat3

# Unesi ime autora kada se traži (npr. "Shakespeare")
```

**Klju?ne Funkcionalnosti:**
- ? Reactive streams sa System.Reactive (Rx.NET)
- ? Gutendex API integracija
- ? Topic modeling sa ML.NET (LDA)
- ? Observable pipelines
- ? Async/await sa reactive extensions

---

## ?? Struktura

```
Projekat/
??? Projekat/   # Web server (1 & 2)
?   ??? Services/
?   ?   ??? FileAnalysisService.cs (ServisAnaliza)
?   ??? Endpoints/
?   ?   ??? FileEndpoints.cs (Rute)
?   ??? Caching/
?   ?   ??? MemoryCacheService.cs (ServisKes)
?   ??? Program.cs
?
??? Projekat.Tests/ # Testovi za projekat 1 & 2
?   ??? FileAnalysisServiceTests.cs
?   ??? CacheServiceTests.cs (TestoviKesa)
?
??? Projekat3/     # Reactive + ML
?   ??? Services/
?   ?   ??? ApiService.cs (ServisApi)
?   ?   ??? MlService.cs (ServisMl)
?   ??? Models/
?   ?   ??? Book.cs (Knjiga, Autor)
?   ??? Utils/
?   ?   ??? Log.cs
?   ??? Program.cs
?
??? Projekat3.Tests/ # Testovi za projekat 3
    ??? Tests.cs (TestoviApi, TestoviMl)
```

---

## ?? Testiranje

```bash
# Svi testovi
dotnet test

# Sa detaljima
dotnet test --verbosity detailed
```

---

## ?? Tehnologije

| Projekat | Stack |
|----------|-------|
| Projekat 1 & 2 | ASP.NET Core 8, System.Threading, ConcurrentDictionary |
| Projekat 3 | System.Reactive, ML.NET, Newtonsoft.Json |
| Testovi | xUnit, Moq |

---

## ?? Zahtevi

- **.NET 8 SDK**
- **Windows / Mac / Linux**
- **Internet konekcija** (za Projekat 3)

---

## ?? Primeri Koriš?enja

### Projekat 1 & 2

```bash
# Analiza fajla
GET http://localhost:5050/primer.txt

# Odgovor
{
  "ime": "primer.txt",
  "prosecnaDuzina": 4.56,
  "brojReci": 28,
  "ukupnoKaraktera": 128
}
```

**Keš:**
- Prvi zahtev: ~100-500ms (analiza)
- Drugi zahtev: ~1-5ms (iz keša) **100x brže!**

### Projekat 3

```bash
dotnet run --project Projekat3

# Unos
Unesi ime autora (npr. 'Shakespeare'): Dickens

# Output
[API] Pretraga za: Dickens
[API] Prona?eno 45 knjiga
[RX] Primljen: Fiction -- England -- London -- 19th century...
[RX] Primljen: Historical fiction...
[RX] Stream završen. Ukupno: 120 opisa

=== REZULTATI (3 tema) ===

Tekst 1:
  Tema 1: 45.2%
  Tema 2: 32.1%

Tekst 2:
  Tema 1: 12.3%
  Tema 3: 67.8%
...
```

---

## ?? Klju?ne Karakteristike

### Projekat 1 & 2

- **Thread-Safety**: `ReaderWriterLockSlim` za optimalno ?itanje/pisanje
- **Keširanje**: 5 min TTL, max 1000 stavki, auto-cleanup
- **Logovanje**: Timestamp, IP, status, detalji
- **Security**: Path traversal zaštita, depth limit
- **Performance**: Async I/O, ThreadPool, keširanje
- **Srpska imena**: Kratki, jasni nazivi klasa i metoda

### Projekat 3

- **Reactive**: Observable streams sa Rx.NET
- **API**: Gutendex integration za knjige
- **ML**: LDA topic modeling sa ML.NET
- **Async**: Task-based reactive pipeline
- **Clean**: Simple logging, kratki nazivi
- **Srpska imena**: ServisApi, ServisMl, jednostavne metode

---

## ?? Troubleshooting

**Port 5050 zauzet?**
```csharp
// Program.cs (Projekat)
app.Urls.Add("http://localhost:6000");
```

**API timeout?**
```json
// appsettings.json (Projekat3)
{
  "Api": {
    "TimeoutSeconds": 60
  }
}
```

---

## ?? Dodatno

Svi projekti prate:
- ? SOLID principe
- ? Best practices za C# i .NET 8
- ? Asinkrionu obradu
- ? Dependency injection
- ? Unit testove
- ? ?itljiv kod sa srpskim komentarima
- ? **Kratka srpska imena klasa i metoda**

---

**Verzija:** 1.0  
**.NET:** 8.0  
**C#:** 12.0  
**Status:** Production Ready ?
