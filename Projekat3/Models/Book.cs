using Newtonsoft.Json;

namespace Projekat3.Models;

public class RezultatKnjiga
{
    public int Count { get; set; }
    public string? Next { get; set; }
    public List<Knjiga>? Results { get; set; }
}

public class Knjiga
{
    public int Id { get; set; }
    
    [JsonProperty("title")]
    public string? Naslov { get; set; }
    
    [JsonProperty("authors")]
    public List<Autor>? Autori { get; set; }
    
    [JsonProperty("subjects")]
    public List<string>? Teme { get; set; }
    
    [JsonProperty("bookshelves")]
    public List<string>? Police { get; set; }

    [JsonProperty("description")]
    public string? Opis { get; set; }
}

public class Autor
{
    [JsonProperty("name")]
    public string? Ime { get; set; }
    
    [JsonProperty("birth_year")]
    public int? GodinaRodjenja { get; set; }
    
    [JsonProperty("death_year")]
    public int? GodinaSmrti { get; set; }
}
