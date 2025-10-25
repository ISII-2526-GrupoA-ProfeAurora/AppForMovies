using AppForMovies.API.Controllers;
using AppForMovies.Shared.MovieDTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppForMovies.UT.MoviesController_test
{
    // Clase de pruebas unitarias para el método GetMoviesForRental del MoviesController.
    // Hereda de AppForMovies4SqliteUT que proporciona un contexto en memoria / Sqlite para pruebas.
    public class GetMovies_test: AppForMovies4SqliteUT
    {
        public GetMovies_test()
        {
            // Seed de datos en la BD de prueba:
            // - Definimos algunos géneros
            var genres = new List<Genre>() {
                new Genre("Sci - Fi"),
                new Genre("Drama"),
                new Genre("Comedy"),
                new Genre("Soap opera")
            };

            // - Definimos varias películas asociadas a los géneros anteriores.
            //   Observación importante: la última película tiene quantityforpurchase=0 y quantityforrenting=0
            //   por lo que no debería aparecer en los listados de compra o alquiler.
            var movies = new List<Movie>(){
                new Movie("The lord of the rings", genres[0],new DateTime(2011, 10, 20),10.0m, 5,1.0,1),
                new Movie("The mechanic orange", genres[0],new DateTime(1988, 02, 23),15.0m, 10,2.0,2),
                new Movie("The flying castle", genres[1],new DateTime(2007, 04, 04),20.0m, 15,3.0,10),
                //this movie has quantityforpurchase=0 and quantityforrenting=0 so it shouldn't be returned when 
                //quering for movies for being purchased or rented
                // esta película tiene quantityforpurchase=0 y quantityforrenting=0 -> no debe retornarse para compra/alquiler
                new Movie("The man in the high castle", genres[2],new DateTime(2015, 01, 01),10.0m,0, 4.0,0),
            };

            // Usuario de ejemplo
            ApplicationUser user = new ApplicationUser("1", "Elena", "Navarro Martínez", "elena@uclm.es");

            // Creamos una compra y añadimos un PurchaseItem (relacionado con la película que tiene cantidades a 0)
            var purchase = new Purchase("elena.navarro@uclm.es", "Elena Navarro", user,
                "Avda. España s/n, Albacete", DateTime.Today, new List<PurchaseItem>(), PaymentMethodTypes.CreditCard);

            var purchaseItem = new PurchaseItem(movies[movies.Count - 1], 1, purchase);
            purchase.PurchaseItems.Add(purchaseItem);

            // Creamos un alquiler y añadimos un RentalItem (relacionado con movies[0])
            var rental = new Rental("elena.navarro@uclm.es", "Elena Navarro", user,
                "Avda. España s/n, Albacete 02071", DateTime.Now,
                PaymentMethodTypes.CreditCard, DateTime.Today.AddDays(2), DateTime.Today.AddDays(5),
                     new List<RentalItem>());


            rental.RentalItems.Add(new RentalItem(movies[0], rental));

            // Persistimos los datos en el contexto de prueba
            _context.Add(user);
            _context.AddRange(genres);
            _context.AddRange(movies);
            _context.Add(purchase); //
            _context.Add(rental); //
            _context.SaveChanges();
        }

        // Construye los casos de prueba para GetMoviesForRental_OK.
        // Cada caso contiene filtros (title, genre, fromDate, toDate) y la lista esperada de MovieForRent
        public static IEnumerable<object[]> TestCasesFor_GetMoviesForRental_OK()
        {
            // DTOs esperados (simulan la proyección que realiza el controlador para alquiler)
            var movieDTOs = new List<MovieForRentalDTO>() {
                new MovieForRentalDTO(1,"The lord of the rings","Sci - Fi",new DateTime(2011, 10, 20),1),
                new MovieForRentalDTO(2,"The mechanic orange","Sci - Fi" , new DateTime(1988, 02, 23),2),
                new MovieForRentalDTO(3, "The flying castle", "Drama", new DateTime(2007, 04, 04), 3),
            };

            // Caso de prueba 1: esperamos una lista concreta ordenada por título (comportamiento del método)
            var movieDTOsTC1 = new List<MovieForRentalDTO>() { movieDTOs[1], movieDTOs[2] }
                    //the GetMoviesForPurchase method returns the movies ordered by title
                    .OrderBy(m => m.Title).ToList();

            // Caso de prueba 2: filtro por título (contiene "mechanic")
            var movieDTOsTC2 = new List<MovieForRentalDTO>() { movieDTOs[1] };
            // Caso de prueba 3: filtro por género (Drama)
            var movieDTOsTC3 = new List<MovieForRentalDTO>() { movieDTOs[2] };

            // Caso que extiende el rango de fechas para incluir la primera película también
            var movieDTOsTC4 = new List<MovieForRentalDTO>() { movieDTOs[0], movieDTOs[1], movieDTOs[2] }
                //the GetMoviesForPurchase method returns the movies ordered by title
                // el método GetMoviesForPurchase/ForRental devuelve las películas ordenadas por título
                .OrderBy(m => m.Title).ToList();

            var allTests = new List<object[]>
            {   //filters to apply - expected movies
                //by default datefrom=today +1, dateto=today+2, thus movieDTOs[0] cannot be returned
                // Formato: { filterTitle, filterGenre, fromDate, toDate, expectedMovies }
                // Nota: por defecto datefrom=today +1, dateto=today+2 en las llamadas al controlador en producción,
                //       de ahí que movieDTOs[0] no pueda ser devuelta si usamos las fechas por defecto.
                new object[] { null, null, null, null, movieDTOsTC1,  },
                new object[] { "mechanic", null, null, null, movieDTOsTC2, },
                new object[] { null, "Drama", null, null, movieDTOsTC3, },
                new object[] { null, null, DateTime.Today.AddDays(6), DateTime.Today.AddDays(8), movieDTOsTC4, },
            };

            return allTests;
        }

        [Theory]
        [MemberData(nameof(TestCasesFor_GetMoviesForRental_OK))]
        [Trait("Database", "WithoutFixture")]
        [Trait("LevelTesting", "Unit Testing")]
        public async Task GetMoviesForRental_OK_test(string? filterTitle, string? filterGenre, DateTime? fromDate, DateTime? toDate,
            IList<MovieForRentalDTO> expectedMovies)
        {
            // Arrange -> creamos el controlador con el contexto de prueba (sin logger necesario para este cas
            var controller = new MoviesController(_context, null);

            // Act -> llamamos al método a probar con los filtros proporcionados
            var result = await controller.GetMoviesForRental(filterTitle, filterGenre, fromDate, toDate);

            //Assert
            //we check that the response type is OK 
            var okResult = Assert.IsType<OkObjectResult>(result);
            //and obtain the list of movies   ---   Se usa Equals/HashCode de MovieForRentalDTO para comparar colecciones
            var movieDTOsActual = Assert.IsType<List<MovieForRentalDTO>>(okResult.Value);
            Assert.Equal(expectedMovies, movieDTOsActual);

        }


        [Fact]
        [Trait("LevelTesting", "Unit Testing")]
        [Trait("Database", "WithoutFixture")]
        public async Task GetMoviesForRental_badrequest_test()
        {
            // Arrange
            // - Creamos un Mock del logger para pasar al controlador (aquí el logger no es el sujeto de la prueba)
            var mock = new Mock<ILogger<MoviesController>>();
            ILogger<MoviesController> logger = mock.Object;
            var controller = new MoviesController(_context, logger);

            // Act
            // Llamamos con fromDate posterior a toDate para forzar una validación fallida en el controlador
            var result = await controller.GetMoviesForRental(null, null, DateTime.Today.AddDays(5), DateTime.Today.AddDays(1));

            //Assert
            //we check that the response type is OK and obtain the list of movies
            // - Se espera un BadRequest con ValidationProblemDetails y un mensaje concreto de error
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequestResult.Value);
            var problem = problemDetails.Errors.First().Value[0];

            Assert.Equal("fromDate must be earlier than toDate", problem);
        }

    }
}

