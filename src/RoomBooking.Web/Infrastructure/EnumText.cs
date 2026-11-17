using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace RoomBooking.Web.Infrastructure;

/// <summary>Русское название значения перечисления из атрибута [Display].</summary>
public static class EnumText
{
    public static string Text(this Enum value) =>
        value.GetType().GetMember(value.ToString()).FirstOrDefault()
             ?.GetCustomAttribute<DisplayAttribute>()?.Name ?? value.ToString();
}
