using RoomBooking.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSingleton<InMemoryRoomCatalog>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error");

// Ответы 404 без содержимого выводятся страницей «Запись не найдена»
app.UseStatusCodePagesWithReExecute("/NotFound");
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

// JSON API каталога аудиторий
var api = app.MapGroup("/api");
api.MapGet("/rooms", (InMemoryRoomCatalog catalog) => catalog.All().Select(r => new
{
    r.Id, r.Code, r.Building, Kind = r.Kind.ToString(), r.Seats, r.HasProjector
}));
api.MapGet("/rooms/{id:int}", (int id, InMemoryRoomCatalog catalog) =>
    catalog.Find(id) is { } r
        ? Results.Ok(new { r.Id, r.Code, r.Building, Kind = r.Kind.ToString(), r.Seats, r.HasProjector })
        : Results.NotFound(new { error = $"Аудитория с идентификатором {id} не найдена" }));

app.Run();
