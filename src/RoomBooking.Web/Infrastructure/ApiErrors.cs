
namespace RoomBooking.Web.Infrastructure;

/// <summary>
/// Ошибки разбора запросов к JSON API. По умолчанию ASP.NET Core отвечает кодом 400 без пояснения
/// (в режиме разработки — текстом исключения на английском языке); здесь ответ приводится к виду {"error": "..."}.
/// </summary>
public static class ApiErrors
{
    public static void Configure(IServiceCollection services)
    {
        // Ошибки привязки параметров передаются в виде исключения BadHttpRequestException
        services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);
    }

    public static void Use(WebApplication app)
    {
        app.UseWhen(c => c.Request.Path.StartsWithSegments("/api"), api => api.Use(async (context, next) =>
        {
            try
            {
                await next(context);
            }
            catch (BadHttpRequestException ex)
            {
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = Describe(ex) });
            }
        }));
    }

    private static string Describe(BadHttpRequestException ex)
    {
        var text = ex.ToString();
        if (text.Contains("JsonException") || text.Contains("as JSON"))
            return "Тело запроса не является корректным JSON или содержит значение неверного формата (дата — гггг-мм-дд, время — чч:мм)";
        if (text.Contains("Required parameter") || text.Contains("was not provided"))
            return "Не указан обязательный параметр запроса";
        if (text.Contains("Failed to bind parameter"))
            return "Параметр запроса указан в неверном формате (дата — гггг-мм-дд, время — чч:мм)";
        if (text.Contains("Content-Type") || text.Contains("content type"))
            return "Тело запроса должно передаваться в формате application/json";
        return "Неверный запрос";
    }
}
