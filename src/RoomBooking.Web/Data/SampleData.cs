using RoomBooking.Core.Reservations;
using RoomBooking.Core.Rooms;

namespace RoomBooking.Web.Data;

/// <summary>Учебные данные для пустой базы: аудитории и бронирования относительно текущей даты.</summary>
public static class SampleData
{
    // Расписание звонков колледжа (пары)
    private static readonly (TimeOnly From, TimeOnly To)[] Pairs =
    [
        (new(8, 30), new(10, 0)), (new(10, 10), new(11, 40)), (new(12, 20), new(13, 50)),
        (new(14, 0), new(15, 30)), (new(15, 40), new(17, 10))
    ];

    public static void EnsureSeeded(BookingContext db, DateTime now)
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

        // Повторяющиеся занятия: аудитория, пара, группа, преподаватель, дисциплина, число студентов
        var lessons = new (int Room, int Pair, string Group, string Teacher, string Purpose, int People)[]
        {
            (2, 0, "ИСИП-23/1", "Крючков В. В.", "МДК.01.01 Разработка программных модулей", 22),
            (2, 1, "ИСИП-23/2", "Крючков В. В.", "МДК.01.01 Разработка программных модулей", 21),
            (3, 2, "ИСИП-24/1", "Лаптева О. Н.", "Основы алгоритмизации и программирования", 18),
            (1, 0, "ИСИП-24/2", "Гарипова Р. Ф.", "Иностранный язык в профессиональной деятельности", 14),
            (4, 1, "БД-23/1", "Мельникова Т. А.", "Бухгалтерский учёт", 26),
            (5, 3, "ИСИП-23/1", "Сафонов Д. И.", "Компьютерные сети", 15),
            (0, 2, "ИСИП-22/1", "Ахмедова Л. Р.", "Экономика организации, поток", 74),
            (6, 3, "ПД-24/1", "Кузнецова Е. В.", "Правовое обеспечение профессиональной деятельности", 27),
            (2, 3, "ИСИП-22/2", "Крючков В. В.", "Консультация по курсовому проектированию", 12)
        };

        var today = DateOnly.FromDateTime(now);
        var reservations = new List<Reservation>();
        for (var shift = -7; shift <= 14; shift++)
        {
            var day = today.AddDays(shift);
            if (day.DayOfWeek == DayOfWeek.Sunday) continue;
            for (var i = 0; i < lessons.Length; i++)
            {
                // Не каждое занятие проводится ежедневно: часть — через день
                if ((i + shift) % 3 == 0 && i > 4) continue;
                var l = lessons[i];
                var slot = TimeSlot.On(day, Pairs[l.Pair].From, Pairs[l.Pair].To);
                reservations.Add(Reservation.Seed(rooms[l.Room], slot, l.Teacher, l.Group, l.Purpose, l.People,
                    day.AddDays(-10).ToDateTime(new TimeOnly(9, 0))));
            }
        }

        // Разовые мероприятия, в том числе отменённое
        var events = today.AddDays(3).DayOfWeek == DayOfWeek.Sunday ? today.AddDays(4) : today.AddDays(3);
        reservations.Add(Reservation.Seed(rooms[7], TimeSlot.On(events, new(14, 0), new(16, 0)),
            "Бойко А. С.", null, "Собрание студенческого совета", 120, today.AddDays(-2).ToDateTime(new TimeOnly(11, 0))));
        reservations.Add(Reservation.Seed(rooms[7], TimeSlot.On(events, new(10, 0), new(12, 0)),
            "Тихонова М. П.", null, "Родительское собрание первых курсов", 150, today.AddDays(-5).ToDateTime(new TimeOnly(15, 0)),
            cancelReason: "Перенос собрания на следующий месяц"));
        reservations.Add(Reservation.Seed(rooms[3], TimeSlot.On(events, new(15, 40), new(17, 10)),
            "Крючков В. В.", "ИСИП-23/1", "Защита курсовых проектов", 20, today.AddDays(-1).ToDateTime(new TimeOnly(10, 0))));

        db.Reservations.AddRange(reservations);
        db.SaveChanges();
    }
}
