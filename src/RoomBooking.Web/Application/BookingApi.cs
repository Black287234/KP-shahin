using RoomBooking.Core;
using RoomBooking.Core.Reservations;
using RoomBooking.Core.Rooms;

namespace RoomBooking.Web.Application;

/// <summary>Заявка на бронирование в формате JSON.</summary>
public record BookingJson(int RoomId, DateOnly Date, TimeOnly From, TimeOnly To,
    string? Organizer, string? StudyGroup, string? Purpose, int Attendees);

public record CancelJson(string? Reason);

/// <summary>
/// Конечные точки JSON API. Нарушение правил предметной области возвращается кодом 409 (конфликт)
/// или 422 (неверные данные) с текстом ошибки на русском языке в поле error.
/// </summary>
public static class BookingApi
{
    public static void Map(RouteGroupBuilder api)
    {
        api.MapGet("/rooms", (RoomRegistry rooms, string? text, int? minSeats) =>
            rooms.Find(new RoomQuery(text, MinSeats: minSeats)).Select(RoomJson));

        api.MapGet("/rooms/{id:int}", (int id, RoomRegistry rooms) =>
            rooms.Get(id) is { } r ? Results.Ok(RoomJson(r)) : NotFound($"Аудитория с идентификатором {id} не найдена"));

        api.MapGet("/rooms/free", (ReservationDesk desk, DateOnly date, TimeOnly from, TimeOnly to, int? seats, bool? projector) =>
            Guard(() => Results.Ok(desk.FreeRooms(TimeSlot.On(date, from, to), seats ?? 1, projector ?? false).Select(RoomJson))));

        api.MapGet("/reservations", (ReservationDesk desk, DateOnly? from, DateOnly? to, int? roomId) =>
        {
            var now = desk.Now;
            return desk.Find(new ReservationQuery(from, to, roomId)).Select(r => ReservationJson(r, now));
        });

        api.MapGet("/reservations/{id:int}", (int id, ReservationDesk desk) =>
            desk.Get(id) is { } r ? Results.Ok(ReservationJson(r, desk.Now)) : NotFound($"Бронирование с идентификатором {id} не найдено"));

        api.MapPost("/reservations", (BookingJson b, ReservationDesk desk) => Guard(() =>
        {
            var r = desk.Book(b.RoomId, new BookingRequest(TimeSlot.On(b.Date, b.From, b.To), b.Organizer, b.StudyGroup, b.Purpose, b.Attendees));
            return Results.Created($"/api/reservations/{r.Id}", ReservationJson(desk.Get(r.Id)!, desk.Now));
        }));

        api.MapPost("/reservations/{id:int}/cancel", (int id, CancelJson body, ReservationDesk desk) => Guard(() =>
        {
            desk.Cancel(id, body.Reason);
            return Results.Ok(ReservationJson(desk.Get(id)!, desk.Now));
        }));

        api.MapGet("/schedule/{date}", (DateOnly date, ScheduleBuilder schedule) =>
            schedule.Day(date).Select(d => new
            {
                room = d.Room.Code,
                loadPercent = d.LoadPercent,
                busy = d.Blocks.Where(b => !b.IsFree).Select(b => new
                {
                    from = b.From.ToString("HH:mm"), to = b.To.ToString("HH:mm"),
                    b.Reservation!.Id, b.Reservation.Purpose, b.Reservation.Organizer, b.Reservation.StudyGroup
                })
            }));
    }

    private static IResult Guard(Func<IResult> action)
    {
        try { return action(); }
        catch (NotFoundException ex) { return NotFound(ex.Message); }
        catch (BookingConflictException ex)
        {
            return Results.Json(new { error = ex.Message, conflictId = ex.Conflict.Id }, statusCode: 409);
        }
        catch (DomainException ex) { return Results.Json(new { error = ex.Message }, statusCode: 422); }
    }

    private static IResult NotFound(string message) => Results.NotFound(new { error = message });

    public static object RoomJson(Room r) => new
    {
        r.Id, r.Code, r.Building, Kind = r.Kind.ToString(), r.Seats, r.HasProjector, r.IsOutOfService, r.OutOfServiceReason
    };

    public static object ReservationJson(Reservation r, DateTime now) => new
    {
        r.Id, r.RoomId, Room = r.Room?.Code,
        Date = DateOnly.FromDateTime(r.Start), From = r.Start.ToString("HH:mm"), To = r.End.ToString("HH:mm"),
        r.Organizer, r.StudyGroup, r.Purpose, r.Attendees,
        State = r.StateAt(now).ToString(), r.CancelReason
    };
}
