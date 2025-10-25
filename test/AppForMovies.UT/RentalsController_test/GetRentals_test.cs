using AppForMovies.API.Controllers;
using AppForMovies.API.DTOs.RentalDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppForMovies.UT.RentalsController_test
{
    // Clase de pruebas unitarias para el endpoint GetRental del RentalsController.
    // Hereda de AppForMovies4SqliteUT que prepara un contexto de EF Core en memoria/SQLite
    public class GetRentals_test : AppForMovies4SqliteUT
    {
        // El constructor popula la BD de prueba con datos necesarios para los tests:
        // - géneros
        // - películas
        // - un usuario de aplicación
        // - un alquiler que contiene un RentalItem
        public GetRentals_test()
        {

            var genres = new List<Genre>() {
                new Genre("Sci - Fi"),
                new Genre("Drama"),
            };

            var movies = new List<Movie>(){
                new Movie("The lord of the rings", genres[0],new DateTime(2011, 10, 20),10.0m, 5,1.0,1),
                new Movie("The man in the high castle", genres[1],new DateTime(2015, 01, 01),10.0m,0, 4.0,15),
            };

            ApplicationUser user = new ApplicationUser("1", "Elena", "Navarro Martínez", "elena@uclm.es");

            // Creamos un Rental y le añadimos un RentalItem asociado a movies[0]
            var rental = new Rental("elena.navarro@uclm.es", "Elena Navarro",
                   user, "Avda. España s/n, Albacete 02071",
                    DateTime.Now, AppForMovies.API.Models.PaymentMethodTypes.CreditCard,
                    DateTime.Today.AddDays(2), DateTime.Today.AddDays(5),
                    new List<RentalItem>());
            rental.RentalItems.Add(new RentalItem(movies[0], rental, "My favourite movie"));

            // Persistimos las entidades en el contexto de prueba
            _context.ApplicationUsers.Add(user);
            _context.AddRange(genres);
            _context.AddRange(movies);
            _context.Add(rental);
            _context.SaveChanges();
        }

        // Test: cuando se solicita un alquiler que no existe, el controlador debe devolver NotFound (404).
        [Fact]
        [Trait("Database", "WithoutFixture")]
        [Trait("LevelTesting", "Unit Testing")]
        public async Task GetRental_NotFound_test()
        {
            // Arrange -> creamos un mock de logger y el controlador con el contexto de prueba
            var mock = new Mock<ILogger<RentalsController>>();
            ILogger<RentalsController> logger = mock.Object;

            var controller = new RentalsController(_context, logger);

            // Act -> solicitamos un rental con id 0 (no existente en la BD de prueba)
            var result = await controller.GetRental(0);

            //Assert -> comprobamos que la respuesta sea NotFoundResult
            //we check that the response type is OK and obtain the list of movies
            Assert.IsType<NotFoundResult>(result);

        }

        // Test: cuando se solicita un alquiler existente, debe devolverse OK (200) con el RentalDetailDTO esperado.
        [Fact]
        [Trait("LevelTesting", "Unit Testing")]
        [Trait("Database", "WithoutFixture")]
        public async Task GetRental_Found_test()
        {
            // Arrange -> mock del logger y construcción del controlador
            var mock = new Mock<ILogger<RentalsController>>();
            ILogger<RentalsController> logger = mock.Object;
            var controller = new RentalsController(_context, logger);

            // Construimos el DTO esperado con los datos que introducimos en el constructor.
            // Observación: la comparación de fechas puede ser sensible; el test usa Equal
            var expectedRental = new RentalDetailDTO(1, DateTime.Now, "elena.navarro@uclm.es", "Elena Navarro",
                        "Avda. España s/n, Albacete 02071", PaymentMethodTypes.CreditCard,
                        DateTime.Today.AddDays(2), DateTime.Today.AddDays(5),
                        new List<RentalItemDTO>());
            expectedRental.RentalItems.Add(new RentalItemDTO(1, "The lord of the rings", "Sci - Fi", 1.0, "My favourite movie"));

            // Act -> solicitamos el rental con id 1
            var result = await controller.GetRental(1);

            //Assert
            //we check that the response type is OK and obtain the rental
            // - Comprobamos que la respuesta sea OkObjectResult
            var okResult = Assert.IsType<OkObjectResult>(result);
            // - Extraemos el DTO retornado y comprobamos su tipo
            var rentalDTOActual = Assert.IsType<RentalDetailDTO>(okResult.Value);
            ////var eq = expectedRental.Equals(rentalDTOActual);
            //we check that the expected and actual are the same
            // - Comparamos el objeto obtenido con el esperado usando Equals/Equality definido en el DTO
            Assert.Equal(expectedRental, rentalDTOActual);

        }
    }
}
