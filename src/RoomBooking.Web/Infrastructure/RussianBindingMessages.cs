using Microsoft.AspNetCore.Mvc;

namespace RoomBooking.Web.Infrastructure;

/// <summary>Русские тексты стандартных ошибок привязки данных формы (вместо английских по умолчанию).</summary>
public static class RussianBindingMessages
{
    public static void Apply(MvcOptions o)
    {
        var m = o.ModelBindingMessageProvider;
        m.SetValueMustNotBeNullAccessor(_ => "Поле обязательно для заполнения");
        m.SetMissingBindRequiredValueAccessor(f => $"Не указано значение поля «{f}»");
        m.SetAttemptedValueIsInvalidAccessor((v, _) => $"Значение «{v}» указано в неверном формате");
        m.SetUnknownValueIsInvalidAccessor(_ => "Значение указано в неверном формате");
        m.SetValueIsInvalidAccessor(v => $"Значение «{v}» указано в неверном формате");
        m.SetValueMustBeANumberAccessor(_ => "Значение должно быть числом");
        m.SetNonPropertyAttemptedValueIsInvalidAccessor(v => $"Значение «{v}» указано в неверном формате");
        m.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Значение указано в неверном формате");
        m.SetNonPropertyValueMustBeANumberAccessor(() => "Значение должно быть числом");
        m.SetMissingKeyOrValueAccessor(() => "Значение не указано");
        m.SetMissingRequestBodyRequiredValueAccessor(() => "Тело запроса не заполнено");
    }
}
