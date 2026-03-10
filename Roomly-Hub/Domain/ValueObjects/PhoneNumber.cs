using System.Text.RegularExpressions;

namespace Domain.ValueObjects
{
    public sealed record PhoneNumber
    {
        private static readonly Regex PhoneRegex = new(
            @"^\+?[1-9]\d{1,14}$",
            RegexOptions.Compiled);

        public string Value { get; }

        private PhoneNumber(string value)
        {
            Value = value;
        }

        public static PhoneNumber Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Phone number cannot be empty.", nameof(value));

            var normalizedPhone = value.Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

            if (normalizedPhone.Length < 10 || normalizedPhone.Length > 20)
                throw new ArgumentException("Phone number must be between 10 and 20 characters.", nameof(value));

            if (!PhoneRegex.IsMatch(normalizedPhone))
                throw new ArgumentException("Invalid phone number format.", nameof(value));

            return new PhoneNumber(normalizedPhone);
        }

        public override string ToString() => Value;

        public static implicit operator string(PhoneNumber phone) => phone.Value;
    }
}
