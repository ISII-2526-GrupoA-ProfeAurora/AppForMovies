-- 🔹 1. Géneros
INSERT INTO Genres (Name) VALUES 
('Action'), ('Comedy'), ('Drama'), ('Sci-Fi'), ('Horror');

-- 🔹 2. Usuarios
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, Name, Surname)
VALUES
('U1', 'alice', 'ALICE', 'alice@example.com', 'ALICE@EXAMPLE.COM', 1, 'hash1', 'stamp1', 'concurrency1', 0, 0, 0, 0, 'Alice', 'Smith'),
('U2', 'bob', 'BOB', 'bob@example.com', 'BOB@EXAMPLE.COM', 1, 'hash2', 'stamp2', 'concurrency2', 0, 0, 0, 0, 'Bob', 'Jones'),
('U3', 'carol', 'CAROL', 'carol@example.com', 'CAROL@EXAMPLE.COM', 1, 'hash3', 'stamp3', 'concurrency3', 0, 0, 0, 0, 'Carol', 'Taylor'),
('U4', 'dave', 'DAVE', 'dave@example.com', 'DAVE@EXAMPLE.COM', 1, 'hash4', 'stamp4', 'concurrency4', 0, 0, 0, 0, 'Dave', 'Brown'),
('U5', 'eve', 'EVE', 'eve@example.com', 'EVE@EXAMPLE.COM', 1, 'hash5', 'stamp5', 'concurrency5', 0, 0, 0, 0, 'Eve', 'Davis');

-- 🔹 3. Películas
INSERT INTO Movies (Title, GenreId, ReleaseDate, PriceForPurchase, PriceForRenting, QuantityForPurchase, QuantityForRenting)
VALUES
('Movie A', 1, '2022-01-01', 10.99, 2.99, 100, 50),
('Movie B', 2, '2021-05-15', 12.50, 3.50, 80, 40),
('Movie C', 3, '2020-10-10', 8.75, 1.99, 120, 60),
('Movie D', 4, '2023-03-20', 15.00, 4.00, 90, 45),
('Movie E', 5, '2022-07-07', 9.99, 2.50, 110, 55);

-- 🔹 4. Compras
INSERT INTO Purchases (ApplicationUserId, CustomerUserName, CustomerNameSurname, DeliveryAddress, PaymentMethod, PurchaseDate, TotalPrice)
VALUES
('U1', 'alice', 'Alice Smith', '123 Main St', 0, '2023-01-01', 21.98),
('U2', 'bob', 'Bob Jones', '456 Elm St', 1, '2023-01-02', 25.00),
('U3', 'carol', 'Carol Taylor', '789 Oak St', 2, '2023-01-03', 17.50),
('U4', 'dave', 'Dave Brown', '321 Pine St', 0, '2023-01-04', 30.00),
('U5', 'eve', 'Eve Davis', '654 Cedar St', 1, '2023-01-05', 19.99);

-- 🔹 5. Ítems de compra
INSERT INTO PurchaseItem (MovieId, PurchaseId, Price, Quantity)
VALUES
(1, 1, 10.99, 1),
(2, 1, 10.99, 1),
(3, 2, 12.50, 2),
(4, 3, 8.75, 2),
(5, 4, 15.00, 2);

-- 🔹 6. Alquileres
INSERT INTO Rentals (ApplicationUserId, CustomerUserName, CustomerNameSurname, DeliveryAddress, PaymentMethod, RentalDate, RentalDateFrom, RentalDateTo, TotalPrice)
VALUES
('U1', 'alice', 'Alice Smith', '123 Main St', 0, '2023-02-01', '2023-02-01', '2023-02-05', 5.98),
('U2', 'bob', 'Bob Jones', '456 Elm St', 1, '2023-02-02', '2023-02-02', '2023-02-06', 7.00),
('U3', 'carol', 'Carol Taylor', '789 Oak St', 2, '2023-02-03', '2023-02-03', '2023-02-07', 3.98),
('U4', 'dave', 'Dave Brown', '321 Pine St', 0, '2023-02-04', '2023-02-04', '2023-02-08', 8.00),
('U5', 'eve', 'Eve Davis', '654 Cedar St', 1, '2023-02-05', '2023-02-05', '2023-02-09', 4.99);

-- 🔹 7. Ítems de alquiler
INSERT INTO RentalItem (MovieId, RentalId, Description, PriceForRenting)
VALUES
(1, 1, 'First rental', 2.99),
(2, 2, 'Second rental', 3.50),
(3, 3, 'Third rental', 1.99),
(4, 4, 'Fourth rental', 4.00),
(5, 5, 'Fifth rental', 2.50);
