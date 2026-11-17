using Microsoft.EntityFrameworkCore;
using RoomBooking.Web.Application;
using RoomBooking.Web.Data;
using RoomBooking.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages().AddMvcOptions(RussianBindingMessages.Apply);
builder.Services.AddDbContext<BookingContext>(o => o.UseSqlite(builder.Configuration.GetConnectionString("Booking")));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<RoomRegistry>();
builder.Services.AddScoped<ReservationDesk>();
builder.Services.AddScoped<ScheduleBuilder>();
ApiErrors.Configure(builder.Services);

var app = builder.Build();

// Создание или обновление базы данных по миграциям и загрузка учебных данных
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookingContext>();
    db.Database.Migrate();
    SampleData.EnsureSeeded(db, DateTime.Now);
}

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error");

// Ответы 404 без содержимого выводятся страницей «Запись не найдена»
app.UseStatusCodePagesWithReExecute("/NotFound");
ApiErrors.Use(app);
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

// JSON API: каталог, бронирования, расписание, поиск свободных аудиторий
BookingApi.Map(app.MapGroup("/api"));

app.Run();

public partial class Program;
