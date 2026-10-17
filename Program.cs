var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Каталог катамаранов (тестовые данные в памяти)
var catamarans = new List<Catamaran>
{
    new(1, "Бриз",  "Прогулочный", 2, 500m),
    new(2, "Волна", "Прогулочный", 4, 800m),
    new(3, "Шторм", "Спортивный",  4, 950m),
    new(4, "Чайка", "Семейный",    6, 1200m),
    new(5, "Парус", "Спортивный",  2, 700m)
};

// Брони (в памяти)
var bookings = new List<Booking>();
var nextBookingId = 1;

// Проверка работоспособности сервиса
app.MapGet("/health", () => Results.Ok(new { status = "OK", service = "Catamaran Rental API" }));

// GET /catamarans — список катамаранов
app.MapGet("/catamarans", () => catamarans);

// GET /catamarans/{id} — информация о катамаране
app.MapGet("/catamarans/{id:int}", (int id) =>
    catamarans.FirstOrDefault(c => c.Id == id) is { } c
        ? Results.Ok(c)
        : Results.NotFound(new { error = "Катамаран не найден" }));

// POST /bookings — создать бронь
app.MapPost("/bookings", (BookingRequest req) =>
{
    var cat = catamarans.FirstOrDefault(c => c.Id == req.CatamaranId);
    if (cat is null)
        return Results.NotFound(new { error = "Катамаран не найден" });
    if (req.Hours < 1)
        return Results.BadRequest(new { error = "Минимальная длительность аренды — 1 час" });

    var start = req.Time;
    var end = start.AddHours(req.Hours);

    // Проверка пересечения по времени с уже существующими бронями
    var busy = bookings.Any(b => b.CatamaranId == req.CatamaranId
                                 && b.Date == req.Date
                                 && b.Status == "Подтверждена"
                                 && start < b.Time.AddHours(b.Hours)
                                 && b.Time < end);
    if (busy)
        return Results.Conflict(new { error = "Катамаран уже занят на это время" });

    var booking = new Booking(nextBookingId++, req.CatamaranId, req.Date, req.Time,
                              req.Hours, cat.PricePerHour * req.Hours, "Подтверждена");
    bookings.Add(booking);
    return Results.Created($"/bookings/{booking.Id}", booking);
});

// GET /bookings/{id} — статус брони
app.MapGet("/bookings/{id:int}", (int id) =>
    bookings.FirstOrDefault(b => b.Id == id) is { } b
        ? Results.Ok(b)
        : Results.NotFound(new { error = "Бронь не найдена" }));

// DELETE /bookings/{id} — отменить бронь
app.MapDelete("/bookings/{id:int}", (int id) =>
{
    var idx = bookings.FindIndex(b => b.Id == id);
    if (idx < 0)
        return Results.NotFound(new { error = "Бронь не найдена" });
    bookings[idx] = bookings[idx] with { Status = "Отменена" };
    return Results.Ok(bookings[idx]);
});

app.Run();

record Catamaran(int Id, string Name, string Type, int Seats, decimal PricePerHour);
record BookingRequest(int CatamaranId, DateOnly Date, TimeOnly Time, int Hours);
record Booking(int Id, int CatamaranId, DateOnly Date, TimeOnly Time, int Hours, decimal TotalPrice, string Status);
