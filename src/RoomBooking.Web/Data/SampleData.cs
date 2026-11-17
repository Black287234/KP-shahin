using RoomBooking.Core.Rooms;

namespace RoomBooking.Web.Data;

/// <summary>Учебные данные для пустой базы.</summary>
public static class SampleData
{
    public static void EnsureSeeded(BookingContext db)
    {
        if (db.Rooms.Any()) return;

        var rooms = new[]
        {
            new Room("А-101", "Корпус А", RoomKind.LectureHall, 120, true),
            new Room("А-105", "Корпус А", RoomKind.Classroom, 30, true),
            new Room("А-112", "Корпус А", RoomKind.ComputerLab, 25, true),
            new Room("А-114", "Корпус А", RoomKind.ComputerLab, 20, false),
            new Room("Б-203", "Корпус Б", RoomKind.Classroom, 28, false),
            new Room("Б-207", "Корпус Б", RoomKind.Laboratory, 16, false),
            new Room("Б-210", "Корпус Б", RoomKind.Classroom, 32, true),
            new Room("Б-301", "Корпус Б", RoomKind.AssemblyHall, 200, true),
            new Room("Б-305", "Корпус Б", RoomKind.Classroom, 24, false)
        };
        rooms[^1].TakeOutOfService("Ремонт до окончания семестра");
        db.Rooms.AddRange(rooms);
        db.SaveChanges();
    }
}
