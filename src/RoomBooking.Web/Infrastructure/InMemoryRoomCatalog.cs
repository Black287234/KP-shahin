using RoomBooking.Core.Rooms;

namespace RoomBooking.Web.Infrastructure;

/// <summary>
/// Каталог аудиторий в оперативной памяти (этап 1). Данные теряются при перезапуске;
/// на этапе 2 каталог заменяется базой данных SQLite.
/// </summary>
public class InMemoryRoomCatalog
{
    private readonly List<Room> _rooms = new();

    public InMemoryRoomCatalog()
    {
        Add(new Room("А-101", "Корпус А", RoomKind.LectureHall, 120, true));
        Add(new Room("А-105", "Корпус А", RoomKind.Classroom, 30, true));
        Add(new Room("А-112", "Корпус А", RoomKind.ComputerLab, 25, true));
        Add(new Room("А-114", "Корпус А", RoomKind.ComputerLab, 20, false));
        Add(new Room("Б-203", "Корпус Б", RoomKind.Classroom, 28, false));
        Add(new Room("Б-207", "Корпус Б", RoomKind.Laboratory, 16, false));
    }

    public IReadOnlyList<Room> All() => _rooms.OrderBy(r => r.Code).ToList();

    public Room? Find(int id) => _rooms.FirstOrDefault(r => r.Id == id);

    private void Add(Room room)
    {
        room.AssignId(_rooms.Count + 1);
        _rooms.Add(room);
    }
}
