using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ExamenApiPokemon.Models
{
    // ---------- GET https://pokeapi.co/api/v2/pokemon?offset=0&limit=20 ----------
    public class PokemonListResponse
    {
        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("next")]
        public string Next { get; set; }

        [JsonProperty("previous")]
        public string Previous { get; set; }

        [JsonProperty("results")]
        public List<PokemonResumen> Results { get; set; }
    }

    public class PokemonResumen
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonIgnore]
        public int Id
        {
            get
            {
                if (string.IsNullOrEmpty(Url)) return 0;
                var limpia = Url.TrimEnd('/');
                int id;
                return int.TryParse(limpia.Substring(limpia.LastIndexOf('/') + 1), out id) ? id : 0;
            }
        }

        [JsonIgnore]
        public string NumeroFormateado { get { return "#" + Id.ToString("000"); } }

        [JsonIgnore]
        public string NombreCapitalizado { get { return PokemonHelper.Capitalizar(Name); } }

        [JsonIgnore]
        public string ImagenUrl
        {
            get
            {
                return Id > 0
                    ? $"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/other/official-artwork/{Id}.png"
                    : string.Empty;
            }
        }
    }

    // ---------- GET https://pokeapi.co/api/v2/pokemon/{id o nombre} ----------
    public class Pokemon
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("base_experience")] public int? BaseExperience { get; set; }
        [JsonProperty("height")] public int Height { get; set; }
        [JsonProperty("weight")] public int Weight { get; set; }
        [JsonProperty("types")] public List<PokemonTipoSlot> Types { get; set; }
        [JsonProperty("abilities")] public List<PokemonHabilidadSlot> Abilities { get; set; }
        [JsonProperty("stats")] public List<PokemonStatSlot> Stats { get; set; }
        [JsonProperty("sprites")] public PokemonSprites Sprites { get; set; }

        [JsonIgnore]
        public string NumeroFormateado { get { return "#" + Id.ToString("000"); } }

        [JsonIgnore]
        public string NombreCapitalizado { get { return PokemonHelper.Capitalizar(Name); } }

        [JsonIgnore]
        public double AlturaMetros { get { return Height / 10.0; } }

        [JsonIgnore]
        public double PesoKilogramos { get { return Weight / 10.0; } }

        [JsonIgnore]
        public string TipoPrincipalClase
        {
            get
            {
                var primerTipo = Types != null && Types.Any() ? Types.First().Type?.Name : null;
                return PokemonHelper.Slug(primerTipo);
            }
        }

        [JsonIgnore]
        public string ImagenPrincipal
        {
            get
            {
                return Sprites?.Other?.OfficialArtwork?.FrontDefault
                    ?? Sprites?.FrontDefault
                    ?? $"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/other/official-artwork/{Id}.png";
            }
        }
    }

    public class PokemonTipoSlot
    {
        [JsonProperty("slot")] public int Slot { get; set; }
        [JsonProperty("type")] public RecursoApi Type { get; set; }

        [JsonIgnore]
        public string NombreEs { get { return PokemonHelper.TipoEs(Type?.Name); } }

        [JsonIgnore]
        public string ClaseCss { get { return PokemonHelper.Slug(Type?.Name); } }
    }

    public class PokemonHabilidadSlot
    {
        [JsonProperty("is_hidden")] public bool IsHidden { get; set; }
        [JsonProperty("slot")] public int Slot { get; set; }
        [JsonProperty("ability")] public RecursoApi Ability { get; set; }
    }

    public class PokemonStatSlot
    {
        [JsonProperty("base_stat")] public int BaseStat { get; set; }
        [JsonProperty("effort")] public int Effort { get; set; }
        [JsonProperty("stat")] public RecursoApi Stat { get; set; }

        [JsonIgnore]
        public string NombreEs { get { return PokemonHelper.StatEs(Stat?.Name); } }
    }

    public class PokemonSprites
    {
        [JsonProperty("front_default")] public string FrontDefault { get; set; }
        [JsonProperty("front_shiny")] public string FrontShiny { get; set; }
        [JsonProperty("other")] public OtherSprites Other { get; set; }
    }

    public class OtherSprites
    {
        [JsonProperty("official-artwork")]
        public OfficialArtwork OfficialArtwork { get; set; }
    }

    public class OfficialArtwork
    {
        [JsonProperty("front_default")] public string FrontDefault { get; set; }
        [JsonProperty("front_shiny")] public string FrontShiny { get; set; }
    }

    public class RecursoApi
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("url")] public string Url { get; set; }
    }

    // ---------- GET https://pokeapi.co/api/v2/type/{tipo} ----------
    public class TipoDetalleResponse
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("pokemon")] public List<TipoPokemonEntry> Pokemon { get; set; }
    }

    public class TipoPokemonEntry
    {
        [JsonProperty("slot")] public int Slot { get; set; }
        [JsonProperty("pokemon")] public PokemonResumen Pokemon { get; set; }
    }

    // ---------- ViewModels ----------
    public class PokemonListaViewModel
    {
        public List<PokemonResumen> Pokemones { get; set; } = new List<PokemonResumen>();
        public string Q { get; set; }
        public string Tipo { get; set; }
        public int Pagina { get; set; } = 1;
        public int TamanoPagina { get; set; } = 20;
        public int TotalPaginas { get; set; }
        public int Total { get; set; }
        public string Error { get; set; }
    }

    public class PokemonDetalleViewModel
    {
        public Pokemon Pokemon { get; set; }
        public string Error { get; set; }
    }

    // ---------- Traducciones y utilidades ----------
    public static class PokemonHelper
    {
        private static readonly Dictionary<string, string> Tipos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"normal","Normal"},{"fire","Fuego"},{"water","Agua"},{"electric","Eléctrico"},
            {"grass","Planta"},{"ice","Hielo"},{"fighting","Lucha"},{"poison","Veneno"},
            {"ground","Tierra"},{"flying","Volador"},{"psychic","Psíquico"},{"bug","Bicho"},
            {"rock","Roca"},{"ghost","Fantasma"},{"dragon","Dragón"},{"dark","Siniestro"},
            {"steel","Acero"},{"fairy","Hada"}
        };

        private static readonly Dictionary<string, string> Estadisticas = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"hp","PS"},{"attack","Ataque"},{"defense","Defensa"},
            {"special-attack","At. Especial"},{"special-defense","Def. Especial"},{"speed","Velocidad"}
        };

        public static string TipoEs(string tipo)
        {
            string v;
            return tipo != null && Tipos.TryGetValue(tipo, out v) ? v : Capitalizar(tipo);
        }

        public static string StatEs(string stat)
        {
            string v;
            return stat != null && Estadisticas.TryGetValue(stat, out v) ? v : Capitalizar(stat);
        }

        public static string Slug(string texto)
        {
            return string.IsNullOrEmpty(texto) ? "unknown" : texto.Trim().ToLowerInvariant();
        }

        public static string Capitalizar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
            var palabras = texto.Replace('-', ' ').Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", palabras.Select(p => char.ToUpper(p[0]) + p.Substring(1).ToLowerInvariant()));
        }
    }
}