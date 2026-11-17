using Microsoft.EntityFrameworkCore;
using RoomBooking.Web.Application;
using RoomBooking.Web.Data;
using RoomBooking.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages().AddMvcOptions(RussianBindingMessages.Apply);
builder.Services.AddDbContext<BookingContext>(o => o.UseSqlite(builder.Configuration.GetConnectionString("Booking")));
builder.Services.AddScoped<RoomRegistry>();

var app = builder.Build();

// Создание или обновление базы данных по миграциям и загрузка учебных данных
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookingContext>();
    db.Database.Migrate();
    SampleData.EnsureSeeded(db);
}

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error");

// Ответы 404 без содержимого выводятся страницей «Запись не найдена»
app.UseStatusCodePagesWithReExecute("/NotFound");
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

// JSON API каталога аудиторий
var api = app.MapGroup("/api");
api.MapGet("/rooms", (RoomRegistry rooms, string? text, int? minSeats) =>
    rooms.Find(new RoomQuery(text, MinSeats: minSeats)).Select(RoomJson));
api.MapGet("/rooms/{id:int}", (int id, RoomRegistry rooms) =>
    rooms.Get(id) is { } r ? Results.Ok(RoomJson(r)) : Results.NotFound(new { error = $"Аудитория с идентификатором {id} не найдена" }));

app.Run();

static object RoomJson(RoomBooking.Core.Rooms.Room r) => new
{
    r.Id, r.Code, r.Building, Kind = r.Kind.ToString(), r.Seats, r.HasProjector, r.IsOutOfService, r.OutOfServiceReason
};

public partial class Program;
