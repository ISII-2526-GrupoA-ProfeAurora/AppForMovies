using AppForMovies.API.Models;
using AppForMovies.Shared.MovieDTOs;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.CodeDom.Compiler;

namespace AppForMovies.API.Controllers
{
    /// <summary>
    /// Controlador para operaciones relacionadas con películas.
    /// Expone endpoints destinados principalmente a obtener listados de películas
    /// para el escenario de alquiler (rental). Utiliza EF Core a través de <see cref="ApplicationDbContext"/>.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MoviesController> _logger;

        /// <summary>
        /// Constructor que recibe el contexto de base de datos y un logger.
        /// </summary>
        public MoviesController(ApplicationDbContext context, ILogger<MoviesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Devuelve la lista de películas disponibles para alquiler aplicando filtros:
        /// - movieTitle: búsqueda por título parcial (Contains)
        /// - movieGenre: filtro por nombre del género (Equals)
        /// - fromDate / toDate: ventana temporal del alquiler. Si no se proporcionan,
        ///   se asignan valores por defecto (hoy+1 y hoy+2).
        /// 
        /// Se valida que fromDate <= toDate; si no, se devuelve BadRequest con ValidationProblemDetails.
        /// La consulta utiliza Include para cargar Genre y RentalItems -> Rent y
        /// filtra las películas cuya cantidad disponible para alquilar
        /// (QuantityForRenting menos reservas en el intervalo) sea mayor que 0.
        /// </summary>
        [HttpGet]
        [Route("[action]")]
        [ProducesResponseType(typeof(IList<MovieForRentalDTO>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult> GetMoviesForRental(string? movieTitle, string? movieGenre, DateTime? fromDate, DateTime? toDate)
        {
            //    var movies = await _context.Movies
            //        .Include(m=>m.Genre)
            //        .Include(m=>m.RentalItems)
            //            .ThenInclude(ri=>ri.Rent)
            //        .Where(m=>((m.Title.Contains(title)) || (title==null))
            //            && ((m.Genre.Name.Equals(genre)) || (genre==null)) 
            //            && (m.RentalItems.Count(ri=>ri.Rent.RentalDateFrom<=to
            //                && ri.Rent.RentalDateTo>=from) < m.QuantityForRenting))
            //        .OrderBy(m=>m.Title)
            //            .ThenBy(m=>m.PriceForRenting)
            //        .Select(m=>new MovieForRentalDTO(m.Id, m.Title, m.Genre.Name,
            //                    m.ReleaseDate, m.PriceForPurchase,
            //                    m.RentalItems.Max(ri=>ri.Rent.RentalDate)))
            //        .ToListAsync();
            //    return Ok(movies);
            //}
            // Nota: fragmento comentado muestra una alternativa de consulta más explícita.
            if (fromDate != null && toDate != null && fromDate > toDate)
            {
                //return BadRequest( Problem("fromDate must be earlier than toDate", 
                //    $"fromDate ({fromDate}) toDate({toDate})", 400,"Bad Request", 
                //    "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1"));
                ModelState.AddModelError("fromDate&toDate", "fromDate must be earlier than toDate");
                _logger.LogError($"{DateTime.Now} Error: fromDate must be earlier than toDate");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }

            //if not renting dates are provided a value by default is assigned
            // Si no se proporcionan fechas de alquiler, se asignan valores por defecto
            // (comportamiento del API: hoy+1 -> hoy+2).
            fromDate = fromDate == null ? DateTime.Today.AddDays(1) : fromDate;
            toDate = toDate == null ? DateTime.Today.AddDays(2) : toDate;

            // Construcción de la consulta:
            // - Incluimos el Genre para poder filtrar por nombre del género.
            // - Incluimos RentalItems y luego Rent para contar reservas que se solapan
            //   con el intervalo solicitado.
            // - Filtramos por título y género si se proporcionan.
            // - Comprobamos que el número de rental items que se solapan en el rango
            //   es menor que QuantityForRenting (por tanto hay unidades disponibles).
            // - Ordenamos por título y proyectamos al DTO de respuesta.
            IList<MovieForRentalDTO> selectMovies = await _context.Movies

                //join table Movie and table Genre
                .Include(m => m.Genre)

                //join tables Movie and PurchaseItem and then Purchase
                //more info about lazy loading and eager loading https://learn.microsoft.com/en-us/ef/core/querying/related-data/eager
                .Include(m => m.RentalItems).ThenInclude(ri => ri.Rent)

                .Where(m => // where clause
                   (movieTitle == null || m.Title.Contains(movieTitle)) //in case user has provided a title
                    && (movieGenre == null || m.Genre.Name.Equals(movieGenre))
                    //we check that it has not 
                    // calculamos cuántas reservas activas hay en el rango [fromDate,toDate]
                    // y comprobamos que ese número sea menor que la cantidad total disponib
                    && (m.RentalItems.Where(ri => ri.Rent.RentalDateFrom <= toDate
                                            && ri.Rent.RentalDateTo >= fromDate).Count() < m.QuantityForRenting)

                    )

                .OrderBy(m => m.Title)

                // Proyección a DTO que se devolverá al cliente
                .Select(m => new MovieForRentalDTO(m.Id, m.Title, m.Genre.Name, m.ReleaseDate, m.PriceForRenting))
                .ToListAsync();

            return Ok(selectMovies);
        }


        [HttpGet]
        [Route("[action]")]
        [ProducesResponseType(typeof(IList<MovieForRentalDTO>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult> GetMoviesForRentalBasic(string? movieTitle, string? movieGenre)
        {
            IList<MovieForRentalDTO> selectMovies = await _context.Movies

                //join table Movie and table Genre
                .Include(m => m.Genre)
                .Where(m => // where clause
                   (movieTitle == null || m.Title.Contains(movieTitle)) //in case user has provided a title
                    && (movieGenre == null || m.Genre.Name.Equals(movieGenre) ) )
                .OrderBy(m => m.Title)
                //ignorar date y price
                .Select(m => new MovieForRentalDTO(m.Id, m.Title, m.Genre.Name, System.DateTime.Now, 0.0))
                .ToListAsync();

            return Ok(selectMovies);
        }

        [HttpGet]
        [Route("[action]")]
        [ProducesResponseType(typeof(IList<MovieForRentalDTO>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult> GetMoviesForRentalBasicDivided(string? movieTitle, string? movieGenre)
        {
            //Paso 1: Consulta base con Include - baseQuery
            var query = _context.Movies
                //join table Movie and table Genre
                .Include(m => m.Genre).AsQueryable();

            //Paso 2: Aplicar filtros condicionales  - filtered
            if (!string.IsNullOrEmpty(movieTitle))
                query = query.Where(m => m.Title.Contains(movieTitle));

            if (!string.IsNullOrEmpty(movieGenre))
                query = query.Where(m => m.Genre.Name.Equals(movieGenre));

            //Paso 3: Ordenar
            query = query.OrderBy(m => m.Title);

            //Paso 4: Inspeccionar SQL generado(requiere EF Core 5 +)
            var sql = query.ToQueryString();
            Console.WriteLine(sql); // O inspección en el depurador

            //Paso 5: Ejecutar y proyectar al DTO
        var selectedMovies = await query
        .Select(m => new MovieForRentalDTO(
            m.Id,
            m.Title,
            m.Genre.Name,
            DateTime.Now, // ignorar fecha real
            0.0))         // ignorar precio real
        .ToListAsync();

            return Ok(selectedMovies);
        }

    }
}
