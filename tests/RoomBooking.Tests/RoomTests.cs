using RoomBooking.Core;
using RoomBooking.Core.Rooms;

namespace RoomBooking.Tests;

[TestFixture, Description("Сущность «Аудитория»")]
public class RoomTests
{
    [TestCase(" а-105 ", "А-105")]
    [TestCase("A-101", "А-101", TestName = "Латинская A заменяется русской")]
    [TestCase("b-203", "В-203", TestName = "Латинская b заменяется русской В")]
    [TestCase("ACT-1", "АСТ-1", TestName = "Все латинские буквы имеют русских двойников")]
    [TestCase("LAB-2", "LAB-2", TestName = "Латинское обозначение без двойников сохраняется")]
    [TestCase("314", "314")]
    public void Номер_приводится_к_единому_виду(string input, string expected)
    {
        Assert.That(new Room(input, "Корпус А", RoomKind.Classroom, 20, false).Code, Is.EqualTo(expected));
    }

    [TestCase("", "Корпус А", 20, "Не указан номер")]
    [TestCase("А-1", "", 20, "Не указан корпус")]
    [TestCase("А-1", "Корпус А", 0, "от 1 до 500")]
    [TestCase("А-1", "Корпус А", 501, "от 1 до 500")]
    [TestCase("А-123456789012345678901", "Корпус А", 20, "20 символов")]
    public void Некорректные_сведения_отклоняются(string code, string building, int seats, string expected)
    {
        var ex = Assert.Throws<DomainException>(() => new Room(code, building, RoomKind.Classroom, seats, false));
        Assert.That(ex!.Message, Does.Contain(expected));
    }

    [Test]
    public void Неизвестное_назначение_отклоняется()
    {
        Assert.Throws<DomainException>(() => new Room("А-1", "Корпус А", (RoomKind)42, 20, false));
    }

    [Test]
    public void Вывод_из_эксплуатации_и_возврат()
    {
        var room = new Room("Б-305", "Корпус Б", RoomKind.Classroom, 24, false);

        Assert.That(Assert.Throws<DomainException>(() => room.TakeOutOfService("рем"))!.Message, Does.Contain("причину"));
        room.TakeOutOfService("Ремонт потолка");
        Assert.Multiple(() =>
        {
            Assert.That(room.IsOutOfService, Is.True);
            Assert.That(room.OutOfServiceReason, Is.EqualTo("Ремонт потолка"));
            Assert.Throws<DomainException>(() => room.TakeOutOfService("Повторный вывод"));
        });

        room.ReturnToService();
        Assert.That(room.IsOutOfService, Is.False);
        Assert.That(room.OutOfServiceReason, Is.Null);
        Assert.Throws<DomainException>(() => room.ReturnToService());
    }
}
