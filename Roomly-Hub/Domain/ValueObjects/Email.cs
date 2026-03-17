using System.Text.RegularExpressions;

namespace Domain.ValueObjects
{
    public sealed record Email
    {
        private static readonly Regex EmailRegex = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string Value { get; }

        private Email(string value)
        {
            Value = value;
        }

        public static Email Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Email cannot be empty.", nameof(value));

            var normalizedEmail = value.Trim().ToLowerInvariant();

            if (normalizedEmail.Length > 255)
                throw new ArgumentException("Email cannot exceed 255 characters.", nameof(value));

            if (!EmailRegex.IsMatch(normalizedEmail))
                throw new ArgumentException("Invalid email format.", nameof(value));

            return new Email(normalizedEmail);
        }

        public override string ToString() => Value;

        public static implicit operator string(Email email) => email.Value;
    }
}