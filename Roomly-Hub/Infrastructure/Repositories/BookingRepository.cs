using Domain.Entities.Booking;
using Domain.enums.Booking;
using Domain.Interfaces.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly AppDbContext _context;

        public BookingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellation = default)
        {
            return await _context.Set<Booking>()
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.Id == bookingId && !b.IsDeleted, cancellation);
        }

        public async Task<Booking?> GetBookingByInvoiceIdAsync(string invoiceId, CancellationToken cancellation = default)
        {
            return await _context.Set<Booking>()
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.PaymentInvoiceId == invoiceId && !b.IsDeleted, cancellation);
        }

        public async Task<IEnumerable<Booking>> GetBookingsByGuestIdAsync(Guid guestId, CancellationToken cancellation = default)
        {
            return await _context.Set<Booking>()
                .Include(b => b.Room)
                .Where(b => b.GuestId == guestId && !b.IsDeleted)
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync(cancellation);
        }

        public async Task<IEnumerable<Booking>> GetRoomBookingsForHostAsync(Guid hostId, Guid roomId, DateTime from, DateTime to, CancellationToken cancellation = default)
        {
            return await _context.Set<Booking>()
                .Include(b => b.Room)
                .Where(b => b.RoomId == roomId
                            && b.Room.HostId == hostId
                            && !b.IsDeleted
                            && b.Status != BookingStatus.Cancelled
                            && from < b.CheckOutDate
                            && to > b.CheckInDate)
                .OrderBy(b => b.CheckInDate)
                .ToListAsync(cancellation);
        }

        public async Task<IEnumerable<Booking>> GetPendingRequestsByHostIdAsync(Guid hostId, CancellationToken cancellation = default)
        {
            return await _context.Set<Booking>()
                .Include(b => b.Room)
                .Where(b =>
                    b.Status == BookingStatus.PendingHostApproval &&
                    !b.IsApprovedByHost &&
                    b.Room.HostId == hostId &&
                    !b.IsDeleted)
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync(cancellation);
        }

        public async Task AddBookingAsync(Booking booking, CancellationToken cancellation = default)
        {
            await _context.Set<Booking>().AddAsync(booking, cancellation);
        }

        public Task UpdateBookingAsync(Booking booking, CancellationToken cancellation = default)
        {
            _context.Set<Booking>().Update(booking);
            return Task.CompletedTask;
        }

        public async Task DeleteBookingAsync(Guid bookingId, CancellationToken cancellation = default)
        {
            var booking = await _context.Set<Booking>().FirstOrDefaultAsync(b => b.Id == bookingId, cancellation);
            if (booking != null)
            {
                booking.SoftDelete();
                _context.Set<Booking>().Update(booking);
            }
        }

        public Task<bool> IsRoomAvailableAsync(Guid roomId, DateTime checkIn, DateTime checkOut, CancellationToken cancellation = default)
        {
            return IsRoomAvailableAsync(roomId, checkIn, checkOut, null, cancellation);
        }

        public async Task<bool> IsRoomAvailableAsync(Guid roomId, DateTime checkIn, DateTime checkOut, Guid? excludeBookingId = null, CancellationToken cancellation = default)
        {
            var query = _context.Set<Booking>()
                .Where(b => b.RoomId == roomId &&
                            b.Status != BookingStatus.Cancelled &&
                            !b.IsDeleted);

            if (excludeBookingId.HasValue)
            {
                query = query.Where(b => b.Id != excludeBookingId.Value);
            }

            return !await query.AnyAsync(b =>
                checkIn < b.CheckOutDate && checkOut > b.CheckInDate,
                cancellation);
        }

        public async Task<bool> IsRoomAvailableAsync(
            Guid roomId,
            DateTime checkIn,
            DateTime checkOut,
            IEnumerable<BookingStatus> blockingStatuses,
            Guid? excludeBookingId = null,
            CancellationToken cancellation = default)
        {
            var statuses = blockingStatuses?.ToArray() ?? Array.Empty<BookingStatus>();

            var query = _context.Set<Booking>()
                .Where(b => b.RoomId == roomId &&
                            statuses.Contains(b.Status) &&
                            !b.IsDeleted);

            if (excludeBookingId.HasValue)
            {
                query = query.Where(b => b.Id != excludeBookingId.Value);
            }

            return !await query.AnyAsync(b =>
                checkIn < b.CheckOutDate && checkOut > b.CheckInDate,
                cancellation);
        }

        public async Task<IEnumerable<Booking>> GetOverlappingPendingRequestsAsync(
            Guid roomId,
            DateTime checkIn,
            DateTime checkOut,
            Guid excludedBookingId,
            CancellationToken cancellation = default)
        {
            return await _context.Set<Booking>()
                .Where(b => b.RoomId == roomId
                            && b.Status == BookingStatus.PendingHostApproval
                            && b.Id != excludedBookingId
                            && !b.IsDeleted
                            && checkIn < b.CheckOutDate
                            && checkOut > b.CheckInDate)
                .ToListAsync(cancellation);
        }
    }
}