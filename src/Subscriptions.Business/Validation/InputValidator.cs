using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Subscriptions.Business.Errors;
namespace Subscriptions.Business.Validation;


public static class InputValidator
{
    public static string Required(string? value, string field, int max)
    {
        var text = value?.Trim() ?? "";
        if (text.Length < 1 || text.Length > max)
            throw Invalid(field, $"Ожидается от 1 до {max} символов.");
        return text;
    }
    public static string Email(string? value)
    {
        var text = Required(value, "email", 254);
        if (!MailAddress.TryCreate(text, out var address) || address.Address != text)
            throw Invalid("email", "Некорректный адрес.");
        return text;
    }
    public static string Password(string? value)
    {
        if (value is null || value.Length is < 12 or > 128)
            throw Invalid("password", "Ожидается от 12 до 128 символов.");
        return value;
    }
    public static decimal Money(string? text, string field)
    {
        if (text is null || !Regex.IsMatch(text, @"\A[0-9]{1,9}(\.[0-9]{1,2})?\z"))
            throw Invalid(field, "Сумма до 999999999.99, не более двух дробных знаков.");
        var amount = decimal.Parse(text, CultureInfo.InvariantCulture);
        if (amount <= 0m)
            throw Invalid(field, "Сумма должна быть положительной.");
        return amount;
    }
    public static string Choice(string? value, string field, params string[] allowed)
    {
        if (value is null || !allowed.Contains(value, StringComparer.Ordinal))
            throw Invalid(field, "Недопустимое значение.");
        return value;
    }
    public static DateOnly Date(string? value, string field)
    {
        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date))
            throw Invalid(field, "Ожидается дата yyyy-MM-dd.");
        return date;
    }
    public static (DateOnly From, DateOnly To) Period(string? from, string? to)
    {
        var start = Date(from, "from");
        var end = Date(to, "to");
        if (start >= end)
            throw Invalid("to", "Дата to должна быть позже from.");
        return (start, end);
    }
    public static (int Page, int Size) Paging(string? page, string? size)
    {
        if (!int.TryParse(page ?? "1", out var number) || number < 1)
            throw Invalid("page", "Номер страницы должен быть положительным целым.");
        if (!int.TryParse(size ?? "20", out var count) || count is < 1 or > 100)
            throw Invalid("pageSize", "Ожидается целое от 1 до 100.");
        return (number, count);
    }
    public static string Amount(decimal amount) => amount.ToString("F2", CultureInfo.InvariantCulture);
    public static string DateText(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    public static BusinessException Invalid(string field, string message) =>
        new(400, "validation_error", "Некорректные данные.",
            new Dictionary<string, string[]> { [field] = new[] { message } });
}
