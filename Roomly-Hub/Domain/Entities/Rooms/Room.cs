using Domain.enums.Booking;
using Domain.enums.Room;
using Domain.ValueObjects;

namespace Domain.Entities.Rooms
{
    public class Room : BaseEntity
    {
        private readonly List<RoomPhoto> _photos = new();
        private readonly List<AmenityType> _amenities = new();
        private readonly List<RoomAvailability> _availabilities = new();
        public Guid HostId { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public decimal PricePerNight { get; private set; }
        public int MaxGuests { get; private set; }
        public RoomType RoomType { get; private set; }
        public Address Address { get; private set; }
        public TimeSpan CheckInTime { get; private set; }
        public TimeSpan CheckOutTime { get; private set; }
        public bool FreeCancellation { get; private set; }
        public RoomListingStatus Status { get; private set; }
        public RoomAvailabilityStatus StatusAvailability { get; private set; }
        public decimal? AverageRating { get; private set; }
        public string? RejectionReason { get; private set; }
        public SourceStatus Source { get; private set; }
        public BookingMode BookingMode { get; private set; }
        public CancellationPolicy CancellationPolicy { get; private set; }
        public IReadOnlyCollection<RoomPhoto> Photos => _photos.AsReadOnly();
        public IReadOnlyCollection<AmenityType> Amenities => _amenities.AsReadOnly();
        public IReadOnlyCollection<RoomAvailability> Availabilities => _availabilities.AsReadOnly();

        private Room()
        { }

        public static Room Create(
            Guid hostId,
            string title,
            string description,
            RoomType roomType,
            Address address,
            decimal pricePerNight,
            int maxGuests,
            TimeSpan checkInTime,
            TimeSpan checkOutTime,
            bool freeCancellation,
            List<AmenityType>? amenities

            )
        {
            if (hostId == Guid.Empty)
                throw new ArgumentException("Host ID is required.", nameof(hostId));

            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title is required.", nameof(title));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description is required.", nameof(description));

            if (pricePerNight <= 0)
                throw new ArgumentException("Price must be greater than zero.", nameof(pricePerNight));

            if (maxGuests <= 0)
                throw new ArgumentException("Max guests must be greater than zero.", nameof(maxGuests));

            if (checkOutTime <= checkInTime)
                throw new ArgumentException("Check-out time must be after check-in time.");

            var room = new Room
            {
                HostId = hostId,
                Title = title.Trim(),
                Description = description.Trim(),
                RoomType = roomType,
                Address = address,
                PricePerNight = pricePerNight,
                MaxGuests = maxGuests,
                CheckInTime = checkInTime,
                CheckOutTime = checkOutTime,
                FreeCancellation = freeCancellation,
                Status = RoomListingStatus.Draft,
                AverageRating = null,
                RejectionReason = null
            };

            if (amenities != null)
            {
                foreach (var amenity in amenities)
                {
                    room.AddAmenity(amenity);
                }
            }

            return room;
        }

        public static Room Create(
            Guid hostId,
            string title,
            string description,
            RoomType roomType,
            string? unitNumber,
            Address address,
            decimal pricePerNight,
            int maxGuests,
            TimeSpan checkInTime,
            TimeSpan checkOutTime,
            bool freeCancellation,
            List<AmenityType>? amenities)
        {
            return Create(
                hostId,
                title,
                description,
                roomType,
                address,
                pricePerNight,
                maxGuests,
                checkInTime,
                checkOutTime,
                freeCancellation,
                amenities);
        }

        public static Room Create(
            Guid hostId,
            string title,
            string description,
            RoomType roomType,
            string? unitNumber,
            Address address,
            decimal pricePerNight,
            int maxGuests,
            TimeSpan checkInTime,
            TimeSpan checkOutTime,
            bool freeCancellation)
        {
            return Create(
                hostId,
                title,
                description,
                roomType,
                address,
                pricePerNight,
                maxGuests,
                checkInTime,
                checkOutTime,
                freeCancellation,
                null);
        }

        public void SubmitForReview()
        {
            if (Status != RoomListingStatus.Draft)
                throw new InvalidOperationException("Only Draft rooms can be submitted for review.");

            if (!_photos.Any())
                throw new InvalidOperationException("Room must have at least one photo.");

            if (!_amenities.Any())
                throw new InvalidOperationException("Room must have at least one amenity.");

            Status = RoomListingStatus.PendingReview;
            RejectionReason = null;
            MarkUpdated();
        }

        public void ChangeSourceMode(SourceStatus newMode)
        {
            if (Source == newMode)
                return;
            Source = newMode;
            MarkUpdated();
        }

        public void ChangeBookingMode(BookingMode newMode)
        {
            if (BookingMode == newMode)
                return;
            BookingMode = newMode;
            MarkUpdated();
        }

        public void ChangeCancellationPolicy(CancellationPolicy newPolicy)
        {
            if (CancellationPolicy == newPolicy)
                return;
            CancellationPolicy = newPolicy;
            MarkUpdated();
        }

        public void Approve()
        {
            if (Status != RoomListingStatus.PendingReview)
                throw new InvalidOperationException("Only PendingReview rooms can be approved.");

            Status = RoomListingStatus.Published;
            RejectionReason = null;
            MarkUpdated();
        }

        public void Reject(string reason)
        {
            if (Status != RoomListingStatus.PendingReview)
                throw new InvalidOperationException("Only PendingReview rooms can be rejected.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Rejection reason is required.", nameof(reason));

            if (reason.Length > 500)
                throw new ArgumentException("Rejection reason cannot exceed 500 characters.", nameof(reason));

            Status = RoomListingStatus.Draft;
            RejectionReason = reason.Trim();
            MarkUpdated();
        }

        public void Deactivate()
        {
            if (Status != RoomListingStatus.Published)
                throw new InvalidOperationException("Only Active rooms can be deactivated.");

            Status = RoomListingStatus.Inactive;
            MarkUpdated();
        }

        public void Activate()
        {
            if (Status != RoomListingStatus.Inactive)
                throw new InvalidOperationException("Only Inactive rooms can be activated.");

            Status = RoomListingStatus.Published;
            MarkUpdated();
        }

        public void UpdateDetails(
            string title,
            string description,
            decimal pricePerNight,
            int maxGuests,
            TimeSpan checkInTime,
            TimeSpan checkOutTime,
            bool freeCancellation)
        {
            if (!CanBeEdited())
                throw new InvalidOperationException("Room cannot be edited in its current status.");

            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title is required.", nameof(title));

            if (pricePerNight <= 0)
                throw new ArgumentException("Price must be greater than zero.", nameof(pricePerNight));

            if (checkOutTime <= checkInTime)
                throw new ArgumentException("Check-out time must be after check-in time.");

            Title = title.Trim();
            Description = description.Trim();
            PricePerNight = pricePerNight;
            MaxGuests = maxGuests;
            CheckInTime = checkInTime;
            CheckOutTime = checkOutTime;
            FreeCancellation = freeCancellation;
            MarkUpdated();
        }

        public void UpdateAverageRating(decimal newRating)
        {
            if (newRating < 1 || newRating > 5)
                throw new ArgumentException("Rating must be between 1 and 5.", nameof(newRating));

            AverageRating = newRating;
            MarkUpdated();
        }

        public void AddPhoto(RoomPhoto photo)
        {
            if (photo is null)
                throw new ArgumentNullException(nameof(photo));

            _photos.Add(photo);
            MarkUpdated();
        }

        public void RemovePhoto(Guid photoId)
        {
            var photo = _photos.FirstOrDefault(p => p.Id == photoId);

            if (photo is null)
                throw new InvalidOperationException("Photo not found.");

            if (_photos.Count == 1)
                throw new InvalidOperationException("Room must have at least one photo.");

            _photos.Remove(photo);
            MarkUpdated();
        }

        public void AddAmenity(AmenityType amenity)
        {
            var exists = _amenities.Any(a => a == amenity);

            if (exists)
                throw new InvalidOperationException($"Amenity {amenity} already exists.");

            _amenities.Add(amenity);
            MarkUpdated();
        }

        public void RemoveAmenity(AmenityType amenity)
        {
            var existing = _amenities.FirstOrDefault(a => a == amenity);

            if (existing is null)
                throw new InvalidOperationException($"Amenity {amenity} not found.");

            _amenities.Remove(existing);
            MarkUpdated();
        }

        public void BlockDateRange(DateOnly from, DateOnly to, AvailabilityReason reason)
        {
            if (to < from)
                throw new ArgumentException("To date must be greater than or equal to from date.", nameof(to));

            for (var date = from; date <= to; date = date.AddDays(1))
            {
                BlockDate(date, reason);
            }
        }

        public void UnblockDateRange(DateOnly from, DateOnly to)
        {
            if (to < from)
                throw new ArgumentException("To date must be greater than or equal to from date.", nameof(to));

            for (var date = from; date <= to; date = date.AddDays(1))
            {
                UnblockDate(date);
            }
        }

        public void BlockDate(DateOnly date, AvailabilityReason reason)
        {
            if (_availabilities.Any(a => a.BlockedDate == date && !a.IsDeleted))
                throw new InvalidOperationException("Date is already blocked.");

            _availabilities.Add(RoomAvailability.Create(Id, date, reason));
            MarkUpdated();
        }

        public void UnblockDate(DateOnly date)
        {
            var blockedDate = _availabilities.FirstOrDefault(a => a.BlockedDate == date && !a.IsDeleted);
            if (blockedDate is null)
                throw new InvalidOperationException("Date is not blocked.");

            blockedDate.SoftDelete();
            MarkUpdated();
        }

        public bool CanBeEdited()
        {
            return Status == RoomListingStatus.Draft ||
                   Status == RoomListingStatus.Inactive;
        }

        public bool CanBeDeactivated()
        {
            return Status == RoomListingStatus.Published;
        }

        public bool CanGoToAuction()
        {
            return Status == RoomListingStatus.Published;
        }

        public bool CanBeDeleted()
        {
            return Status == RoomListingStatus.Draft ||
                   Status == RoomListingStatus.Inactive;
        }

        public bool CanBeBooked()
        {
            return Status == RoomListingStatus.Published &&
                RoomAvailabilityStatus.Available == StatusAvailability;
        }
    }
}