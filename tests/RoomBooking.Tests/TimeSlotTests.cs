using RoomBooking.Core;
using RoomBooking.Core.Reservations;
using static RoomBooking.Tests.Moment;

namespace RoomBooking.Tests;

[TestFixture, Description("Объект-значение «Интервал времени»")]
public class TimeSlotTests
{
    [Test]
    public void Корректный_интервал_создаётся()
    {
        var slot = Tomorrow_("10:10", "11:40");
        Assert.Multiple(() =>
        {
            Assert.That(slot.Minutes, Is.EqualTo(90));
            Assert.That(slot.Date, Is.EqualTo(Tomorrow));
            Assert.That(slot.ToString(), Is.EqualTo("18.11.2026 10:10–11:40"));
        });
    }

    [TestCase("11:00", "10:00", "позже времени начала")]
    [TestCase("10:00", "10:00", "позже времени начала")]
    [TestCase("07:30", "09:00", "с 08:00 до 21:00")]
    [TestCase("20:00", "21:30", "с 08:00 до 21:00")]
    [TestCase("10:07", "11:00", "шагом 5 минут")]
    [TestCase("10:00", "10:20", "не менее 30 минут")]
    [TestCase("08:00", "14:30", "не более 6 часов")]
    public void Некорректный_интервал_отклоняется(string from, string to, string expected)
    {
        var ex = Assert.Throws<DomainException>(() => Tomorrow_(from, to));
        Assert.That(ex!.Message, Does.Contain(expected));
    }

    [Test]
    public void Интервал_через_полночь_отклоняется()
    {
        var start = Tomorrow.ToDateTime(new TimeOnly(20, 0));
        var ex = Assert.Throws<DomainException>(() => new TimeSlot(start, start.AddHours(5)));
        Assert.That(ex!.Message, Does.Contain("в один день"));
    }

    [Test]
    public void Границы_рабочего_дня_допустимы()
    {
        Assert.DoesNotThrow(() => Tomorrow_("08:00", "09:00"));
        Assert.DoesNotThrow(() => Tomorrow_("20:00", "21:00"));
        Assert.DoesNotThrow(() => Tomorrow_("08:00", "14:00"));
    }

    // Существующее бронирование 10:00–11:30; проверяемый интервал задан в параметрах
    [TestCase("09:00", "10:00", false, TestName = "Заканчивается в момент начала — не пересекается")]
    [TestCase("11:30", "12:30", false, TestName = "Начинается в момент окончания — не пересекается")]
    [TestCase("08:30", "09:30", false, TestName = "Полностью раньше — не пересекается")]
    [TestCase("09:30", "10:30", true, TestName = "Захватывает начало — пересекается")]
    [TestCase("11:00", "12:00", true, TestName = "Захватывает окончание — пересекается")]
    [TestCase("10:15", "11:00", true, TestName = "Лежит внутри — пересекается")]
    [TestCase("09:00", "12:00", true, TestName = "Охватывает целиком — пересекается")]
    [TestCase("10:00", "11:30", true, TestName = "Совпадает — пересекается")]
    public void Пересечение_полуинтервалов(string from, string to, bool overlaps)
    {
        var existing = Tomorrow_("10:00", "11:30");
        Assert.That(Tomorrow_(from, to).Overlaps(existing.Start, existing.End), Is.EqualTo(overlaps));
    }
}
