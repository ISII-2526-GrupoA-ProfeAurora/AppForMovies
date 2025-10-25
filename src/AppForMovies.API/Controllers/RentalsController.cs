using AppForMovies.API.DTOs.RentalDTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppForMovies.API.Controllers
{
    /// <summary>
    /// Controlador responsable de las operaciones relacionadas con alquileres (Rentals).
    /// - Proporciona endpoints para obtener el detalle de un alquiler y para crear un nuevo alquiler.
    /// - Usa <see cref="ApplicationDbContext"/> para acceder a la base de datos y <see cref="ILogger{T}"/> para registrar eventos/errores.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class RentalsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RentalsController> _logger;

        /// <summary>
        /// Constructor que inyecta el contexto de datos y el logger.
        /// </summary>
        public RentalsController(ApplicationDbContext context, ILogger<RentalsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene el detalle de un alquiler por su identificador.
        /// Responde:
        /// - 200 OK con <see cref="RentalDetailDTO"/> si existe.
        /// - 404 NotFound si la tabla Rentals no existe o el alquiler no se encuentra.
        /// </summary>
        /// <param name="id">Id del alquiler a recuperar.</param>
        [HttpGet]
        [Route("[action]")]
        [ProducesResponseType(typeof(RentalDetailDTO), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult> GetRental(int id)
        {
            // Comprobación defensiva: si el DbSet Rentals no está disponible devolvemos NotFound
            if (_context.Rentals == null)
            {
                _logger.LogError("Error: Rentals table does not exist");
                return NotFound();
            }

            // Consulta que:
            // - Filtra por id del rental
            // - Incluye relaciones necesarias: RentalItems -> Movie -> Genre
            // - Proyecta a RentalDetailDTO con su lista de RentalItemDTO
            var rental = await _context.Rentals
             .Where(r => r.Id == id)
                 .Include(r => r.RentalItems) //join table RentalItems  -- // incluye RentalItems
                    .ThenInclude(ri => ri.Movie) //then join table Movies -- // incluye la película de cada Rental
                        .ThenInclude(movie => movie.Genre) //then join table Genre  -- // incluye el género de cada película
             .Select(r => new RentalDetailDTO(r.Id, r.RentalDate, r.CustomerUserName,
                    r.CustomerNameSurname, r.DeliveryAddress,
                    (PaymentMethodTypes)r.PaymentMethod,
                    r.RentalDateFrom, r.RentalDateTo,
                    r.RentalItems
                        .Select(ri => new RentalItemDTO(ri.Movie.Id,
                                ri.Movie.Title, ri.Movie.Genre.Name,
                                ri.Movie.PriceForRenting, ri.Description)).ToList<RentalItemDTO>()))
             .FirstOrDefaultAsync();

            // Si no existe el alquiler con ese id, registramos y devolvemos NotFound
            if (rental == null)
            {
                _logger.LogError($"Error: Rental with id {id} does not exist");
                return NotFound();
            }

            // Devolvemos 200 OK con el DTO de detalle
            return Ok(rental);
        }

        /// <summary>
        /// Crea un nuevo alquiler (Rental) a partir de un <see cref="RentalForCreateDTO"/>.
        /// Responde:
        /// - 201 Created con <see cref="RentalDetailDTO"/> si se crea correctamente.
        /// - 400 BadRequest con <see cref="ValidationProblemDetails"/> si hay errores de validación.
        /// - 409 Conflict (o 500) si ocurre un error al persistir los datos.
        /// 
        /// Validaciones que realiza:
        /// - Las fechas de inicio/fin son coherentes (inicio > hoy, fin > inicio).
        /// - Se incluye al menos una película en RentalItems.
        /// - El usuario (CustomerUserName) existe.
        /// - Cada película solicitada existe y tiene unidades disponibles para el intervalo solicitado.
        /// </summary>
        /// <param name="rentalForCreate">DTO con los datos necesarios para crear el alquiler.</param>
        [HttpPost]
        [Route("[action]")]
        [ProducesResponseType(typeof(RentalDetailDTO), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Conflict)]
        public async Task<ActionResult> CreateRental(RentalForCreateDTO rentalForCreate) {
            //any validation defined in PurchaseForCreate is checked before running the method so they don't have to be checked again
            // Validaciones básicas del DTO que no están cubiertas por el Model Binding / DataAnnotations
            if (rentalForCreate.RentalDateFrom <= DateTime.Today)
                ModelState.AddModelError("RentalDateFrom", "Error! Your rental date must start later than today");

            if (rentalForCreate.RentalDateFrom >= rentalForCreate.RentalDateTo)
                ModelState.AddModelError("RentalDateFrom&RentalDateTo", "Error! Your rental must end later than it starts");

            if (rentalForCreate.RentalItems.Count == 0)
                ModelState.AddModelError("RentalItems", "Error! You must include at least one movie to be rented");

            // if (!_context.ApplicationUsers.Any(au=>au.UserName==rentalForCreate.CustomerUserName))
            // Validar existencia del usuario en la DB
            var user = _context.ApplicationUsers.FirstOrDefault(au => au.UserName == rentalForCreate.CustomerUserName);
            if (user == null)
                ModelState.AddModelError("RentalApplicationUser", "Error! UserName is not registered");

            // Si hay errores de validación, devolvemos BadRequest con detalles
            if (ModelState.ErrorCount > 0)
                return BadRequest(new ValidationProblemDetails(ModelState));

            // Obtenemos los títulos solicitados y cargamos las películas correspondientes con su información
            // necesaria para comprobar disponibilidad (se incluyen RentalItems para
            var movieTitles = rentalForCreate.RentalItems.Select(ri => ri.Title).ToList<string>();

            var movies = _context.Movies.Include(m => m.RentalItems)
                .ThenInclude(ri => ri.Rent)
                .Where(m => movieTitles.Contains(m.Title))
                //we use an anonymous type https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/anonymous-types
                // Proyección a un tipo anónimo con los campos necesarios para la lógica posterior
                .Select(m => new {
                    m.Id,
                    m.Title,
                    m.QuantityForRenting,
                    m.PriceForRenting,
                    //we count the number of rentalItems that are within the rental period
                    NumberOfRentedItems = m.RentalItems.Count(ri => ri.Rent.RentalDateFrom <= rentalForCreate.RentalDateTo
                            && ri.Rent.RentalDateTo >= rentalForCreate.RentalDateFrom)
                })
                .ToList();

            // Construimos la entidad Rental en memoria (aún no persistida)
            Rental rental = new Rental(rentalForCreate.CustomerUserName, rentalForCreate.CustomerNameSurname,
                user, rentalForCreate.DeliveryAddress, DateTime.Now,
                (AppForMovies.API.Models.PaymentMethodTypes)rentalForCreate.PaymentMethod,
rentalForCreate.RentalDateFrom, rentalForCreate.RentalDateTo, new List<RentalItem>());


            rental.TotalPrice = 0;
            var numDays = (rental.RentalDateTo - rental.RentalDateFrom).TotalDays;

            // Recorremos los items solicitados y comprobamos disponibilidad por título
            foreach (var item in rentalForCreate.RentalItems) {
                var movie = movies.FirstOrDefault(m => m.Title == item.Title);
                //we must check that there is enough quantity to be rented in the database
                // Si la película no existe o no tiene unidades disponibles en el periodo, añadimos error
                if ((movie == null) || (movie.NumberOfRentedItems >= movie.QuantityForRenting)) {
                    ModelState.AddModelError("RentalItems", $"Error! Movie titled '{item.Title}' is not available for being rented from {rentalForCreate.RentalDateFrom.ToShortDateString()} to {rentalForCreate.RentalDateTo.ToShortDateString()}");
                }
                else {
                    // rental does not exist in the database yet and does not have a valid Id, so we must relate rentalitem to the object rental
                    // Asociamos un RentalItem a la entidad Rental en memoria.
                    // Se usa movie.Id para vincular la relación y guardamos el precio act
                    rental.RentalItems.Add(new RentalItem(movie.Id, rental, movie.PriceForRenting, item.Description));
                    item.PriceForRenting = movie.PriceForRenting;
                }
            }
            // Calculamos el precio total en base a los días y el precio por día de cada rental item
            rental.TotalPrice = rental.RentalItems.Sum(ri => ri.PriceForRenting * numDays);


            //if there is any problem because of the available quantity of movies or because the movie does not exist
            // Si durante la comprobación de disponibilidad se añadieron errores, devolvemos BadRequest con detalles
            if (ModelState.ErrorCount > 0) {
                return BadRequest(new ValidationProblemDetails(ModelState));
            }

            // Añadimos la entidad Rental al contexto (incluye RentalItems)
            _context.Add(rental);

            try {
                //we store in the database both rental and its rentalitems
                // Persistimos en la base de datos
                await _context.SaveChangesAsync();
            }
            catch (Exception ex) {
                // En caso de error de persistencia registramos y devolvemos Conflict
                _logger.LogError(ex.Message);
                ModelState.AddModelError("Rental", $"Error! There was an error while saving your rental, plese, try again later");
                return Conflict("Error" + ex.Message);

            }

            //it returns rentalDetail
            // Construimos el DTO de respuesta con los datos del rental creado
            var rentalDetail = new RentalDetailDTO(rental.Id, rental.RentalDate,
                rental.CustomerUserName, rental.CustomerNameSurname,
                rental.DeliveryAddress, rentalForCreate.PaymentMethod,
                rental.RentalDateFrom, rental.RentalDateTo,
                rentalForCreate.RentalItems);

            // Devolvemos 201 Created apuntando a GetRental para recuperar el recurso recién creado
            return CreatedAtAction("GetRental", new { id = rental.Id }, rentalDetail);
        }
    }

}
