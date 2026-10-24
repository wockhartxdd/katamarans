using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Получаем настройки из переменных окружения (передаются через docker-compose)
var dbHost = Environment.GetEnvironmentVariable("POSTGRES_SERVER") ?? "localhost";
var dbName = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "app";
var dbUser = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
var dbPassFile = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD_FILE");

// Читаем пароль из секрета (файла), если он передан, иначе дефолтный
string dbPassword = "1234"; 
if (!string.IsNullOrEmpty(dbPassFile) && File.Exists(dbPassFile))
{
    dbPassword = File.ReadAllText(dbPassFile).Trim();
}

var connectionString = $"Host={dbHost};Database={dbName};Username={dbUser};Password={dbPassword}";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

// Автоматическое применение миграций при старте (создание таблиц)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Ждем немного, чтобы база данных успела подняться
    for (int i = 0; i < 10; i++)
    {
        try
        {
            db.Database.EnsureCreated();
            break;
        }
        catch (Exception)
        {
            Thread.Sleep(3000);
        }
    }
}

// Эндпоинты (CRUD)
app.MapGet("/", () => Results.Ok(new { status = "ok" }));

app.MapGet("/catamarans", async (AppDbContext db) =>
    await db.Catamarans.ToListAsync());

app.MapPost("/catamarans", async (Catamaran catamaran, AppDbContext db) =>
{
    db.Catamarans.Add(catamaran);
    await db.SaveChangesAsync();
    return Results.Created($"/catamarans/{catamaran.Id}", catamaran);
});

app.MapPut("/catamarans/{id}", async (int id, Catamaran inputCatamaran, AppDbContext db) =>
{
    var catamaran = await db.Catamarans.FindAsync(id);
    if (catamaran is null) return Results.NotFound();

    catamaran.Name = inputCatamaran.Name;
    catamaran.Capacity = inputCatamaran.Capacity;
    catamaran.PricePerHour = inputCatamaran.PricePerHour;
    catamaran.Status = inputCatamaran.Status;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/catamarans/{id}", async (int id, AppDbContext db) =>
{
    var catamaran = await db.Catamarans.FindAsync(id);
    if (catamaran is null) return Results.NotFound();

    db.Catamarans.Remove(catamaran);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Deleted" });
});

app.Run();

// Модели данных
public class Catamaran
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal PricePerHour { get; set; }
    public string Status { get; set; } = "available";
}

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Catamaran> Catamarans => Set<Catamaran>();
}