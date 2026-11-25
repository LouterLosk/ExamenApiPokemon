using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using ExamenApiPokemon.Models;

namespace ExamenApiPokemon.Controllers
{
    public class PokemonController : Controller
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://pokeapi.co/api/v2/")
        };

        // GET: /Pokemon?q=pikachu&tipo=electric&pagina=1
        public async Task<ActionResult> Index(string q = "", string tipo = "", int pagina = 1)
        {
            if (pagina < 1) pagina = 1;

            var vm = new PokemonListaViewModel
            {
                Q = q?.Trim(),
                Tipo = tipo?.Trim().ToLowerInvariant(),
                Pagina = pagina,
                TamanoPagina = 20
            };

            try
            {
                // CASO 1: Filtro por Tipo
                if (!string.IsNullOrEmpty(vm.Tipo))
                {
                    var respTipo = await _httpClient.GetAsync($"type/{Uri.EscapeDataString(vm.Tipo)}");
                    if (!respTipo.IsSuccessStatusCode)
                    {
                        vm.Error = $"No se encontró el tipo '{vm.Tipo}'.";
                        return View(vm);
                    }

                    var jsonTipo = await respTipo.Content.ReadAsStringAsync();
                    var dataTipo = JsonConvert.DeserializeObject<TipoDetalleResponse>(jsonTipo);

                    var listaFiltrada = dataTipo?.Pokemon?
                        .Select(p => p.Pokemon)
                        .Where(p => p != null && p.Id > 0)
                        .ToList() ?? new List<PokemonResumen>();

                    if (!string.IsNullOrEmpty(vm.Q))
                    {
                        listaFiltrada = listaFiltrada
                            .Where(p => p.Name.IndexOf(vm.Q, StringComparison.OrdinalIgnoreCase) >= 0
                                     || p.Id.ToString() == vm.Q.TrimStart('#'))
                            .ToList();
                    }

                    vm.Total = listaFiltrada.Count;
                    vm.TotalPaginas = (int)Math.Ceiling((double)vm.Total / vm.TamanoPagina);
                    if (vm.TotalPaginas > 0 && vm.Pagina > vm.TotalPaginas) vm.Pagina = vm.TotalPaginas;

                    vm.Pokemones = listaFiltrada
                        .Skip((vm.Pagina - 1) * vm.TamanoPagina)
                        .Take(vm.TamanoPagina)
                        .ToList();

                    return View(vm);
                }

                // CASO 2: Búsqueda directa por nombre o número de Pokédex
                if (!string.IsNullOrEmpty(vm.Q))
                {
                    var termino = vm.Q.TrimStart('#').ToLowerInvariant();
                    var respBusqueda = await _httpClient.GetAsync($"pokemon/{Uri.EscapeDataString(termino)}");

                    if (respBusqueda.StatusCode == HttpStatusCode.NotFound)
                    {
                        vm.Error = $"No se encontró ningún Pokémon con el nombre o número '{vm.Q}'.";
                        return View(vm);
                    }

                    respBusqueda.EnsureSuccessStatusCode();
                    var jsonPoke = await respBusqueda.Content.ReadAsStringAsync();
                    var poke = JsonConvert.DeserializeObject<Pokemon>(jsonPoke);

                    if (poke != null)
                    {
                        vm.Pokemones.Add(new PokemonResumen
                        {
                            Name = poke.Name,
                            Url = $"https://pokeapi.co/api/v2/pokemon/{poke.Id}/"
                        });
                        vm.Total = 1;
                        vm.TotalPaginas = 1;
                        vm.Pagina = 1;
                    }

                    return View(vm);
                }

                // CASO 3: Listado general paginado
                int offset = (vm.Pagina - 1) * vm.TamanoPagina;
                var response = await _httpClient.GetAsync($"pokemon?offset={offset}&limit={vm.TamanoPagina}");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<PokemonListResponse>(json);

                if (data != null)
                {
                    vm.Pokemones = data.Results ?? new List<PokemonResumen>();
                    vm.Total = data.Count;
                    vm.TotalPaginas = (int)Math.Ceiling((double)data.Count / vm.TamanoPagina);
                }
            }
            catch (HttpRequestException ex)
            {
                vm.Error = "Error de conexión con PokéAPI: " + ex.Message;
            }
            catch (Exception ex)
            {
                vm.Error = "Ocurrió un error inesperado al cargar los datos: " + ex.Message;
            }

            return View(vm);
        }

        // GET: /Pokemon/Detalle/25
        public async Task<ActionResult> Detalle(string id)
        {
            var vm = new PokemonDetalleViewModel();

            if (string.IsNullOrWhiteSpace(id))
            {
                vm.Error = "Debe especificar el ID o nombre del Pokémon.";
                return View(vm);
            }

            try
            {
                var limpio = id.Trim().ToLowerInvariant();
                var response = await _httpClient.GetAsync($"pokemon/{Uri.EscapeDataString(limpio)}");

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    vm.Error = $"El Pokémon '{id}' no existe en la Pokédex.";
                    return View(vm);
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                vm.Pokemon = JsonConvert.DeserializeObject<Pokemon>(json);
            }
            catch (HttpRequestException ex)
            {
                vm.Error = "No se pudo conectar con PokéAPI: " + ex.Message;
            }
            catch (Exception ex)
            {
                vm.Error = "Error al obtener el detalle del Pokémon: " + ex.Message;
            }

            return View(vm);
        }
    }
}