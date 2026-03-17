namespace Domain.ValueObjects
{
    public sealed record Address
    {
        public string Street { get; init; }
        public string City { get; init; }
        public string State { get; init; }
        public string Country { get; init; }
        public string ZipCode { get; init; }
        public string HouseNumber { get; init; }
        public string RoomNumber { get; init; }

        public Address() { }

        private Address(string street, string city, string state, string country, string zipCode, string houseNumber, string RoomNumber)
        {
            Street = street;
            City = city;
            State = state;
            Country = country;
            ZipCode = zipCode;
            HouseNumber = houseNumber;
            this.RoomNumber = RoomNumber;
        }
        public static Address Create(string street, string city, string state, string country, string zipCode, string houseNumber, string RoomNumber)
        {
            if (string.IsNullOrWhiteSpace(street))
                throw new ArgumentException("Street cannot be null or empty.", nameof(street));
            if (string.IsNullOrWhiteSpace(city))
                throw new ArgumentException("City cannot be null or empty", nameof(city));
            if (string.IsNullOrWhiteSpace(state))
                throw new ArgumentException("State cannot be null or empty", nameof(state));
            if (string.IsNullOrWhiteSpace(country))
                throw new ArgumentException("Country cannot be null or empty", nameof(country));
            if (string.IsNullOrWhiteSpace(zipCode))
                throw new ArgumentException("zipCode cannot be null or empty", nameof(zipCode));
            if (string.IsNullOrWhiteSpace(houseNumber)) throw new ArgumentException("HouseNumber cannot be null or empty", nameof(houseNumber));
            if (zipCode.Length > 10)
                throw new ArgumentException("ZipCode cannot be longer than 10 characters.", nameof(zipCode));
            if (houseNumber.Length > 10) throw new ArgumentException("HouseNumber cannot be longer than 10 characters.", nameof(houseNumber));
            if (string.IsNullOrWhiteSpace(RoomNumber)) throw new ArgumentException("RoomNumber cannot be null or empty", nameof(RoomNumber));
            if (RoomNumber.Length > 10) throw new ArgumentException("RoomNumber cannot be longer than 10 characters.", nameof(RoomNumber));

            return new Address(street, city, state, country, zipCode, houseNumber, RoomNumber);
        }
    }
}