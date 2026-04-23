using Application.Common.Constants;
using Application.Common.Results;
using Application.DTOs.Auctions;
using Application.Interfaces.BackgroundJobs;
using Application.Interfaces.Persistence;
using Domain.Constants;
using Domain.Entities.Auctions;
using Domain.enums.Auction;
using Domain.enums.Room;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Services.Auctions.Handlers
{
 
    public sealed class CreateAuctionHandler
    {
        private readonly ILogger<CreateAuctionHandler> _logger;
        private readonly IValidator<CreateAuctionRequestDto> _validator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRoomRepository _roomRepository;
        private readonly IAuctionRepository _auctionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly IBackgroundJob _backgroundJob;

        public CreateAuctionHandler(
            ILogger<CreateAuctionHandler> logger,
            IValidator<CreateAuctionRequestDto> validator,
            IUnitOfWork unitOfWork,
            IRoomRepository roomRepository,
            IAuctionRepository auctionRepository,
            IUserRepository userRepository,
            IConfiguration configuration,
            IBackgroundJob backgroundJob)
        {
            _logger = logger;
            _validator = validator;
            _unitOfWork = unitOfWork;
            _roomRepository = roomRepository;
            _auctionRepository = auctionRepository;
            _userRepository = userRepository;
            _configuration = configuration;
            _backgroundJob = backgroundJob;
        }

        public async Task<Result<AuctionResponseDto>> HandleAsync(Guid hostId, CreateAuctionRequestDto dto, CancellationToken ct)
        {
            _logger.LogInformation("CreateAuction started — HostId: {HostId}, RoomId: {RoomId}", hostId, dto.RoomId);

            var validationResult = await ValidateRequestAsync(dto, ct);
            if (validationResult.IsFailure)
                return Result<AuctionResponseDto>.Failure(validationResult.ErrorCode!, validationResult.ErrorMessage!, validationResult.Errors!);

            var rulesResult = await ValidateBusinessRulesAsync(hostId, dto, ct);
            if (rulesResult.IsFailure)
                return Result<AuctionResponseDto>.Failure(rulesResult.ErrorCode!, rulesResult.ErrorMessage!);

            var datesResult = CalculateDates(dto);
            if (datesResult.IsFailure)
                return Result<AuctionResponseDto>.Failure(datesResult.ErrorCode!, datesResult.ErrorMessage!);

            var (startTime, endTime, durationEnum) = datesResult.Value;
            var insuranceRate = ReadInsuranceRate();
            var roomTitle = rulesResult.Value!;

            var auction = Auction.Create(
                dto.RoomId, hostId, dto.CheckInDate, dto.CheckOutDate,
                dto.StartingPrice, IncrementType.FixedAmount, dto.MinBidIncrementValue,
                insuranceRate, durationEnum, startTime, endTime);

            await _auctionRepository.AddAsync(auction, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            ScheduleJobs(auction, startTime, endTime);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Auction created — AuctionId: {AuctionId}, EndTime: {EndTime}", auction.Id, endTime);
            return Result<AuctionResponseDto>.Success(auction.ToDto(roomTitle));
        }

        private async Task<Result> ValidateRequestAsync(CreateAuctionRequestDto dto, CancellationToken ct)
        {
            var result = await _validator.ValidateAsync(dto, ct);
            if (result.IsValid) return Result.Success();

            _logger.LogError("DTO validation failed: {Errors}", result.Errors);
            var errors = new Dictionary<string, string[]>
            {
                { "ValidationErrors", result.Errors.Select(e => e.ErrorMessage).ToArray() }
            };
            return Result.Failure(Errors.Codes.Common.ValidationError, "Validation Error in request", errors);
        }

        private async Task<Result<string>> ValidateBusinessRulesAsync(Guid hostId, CreateAuctionRequestDto dto, CancellationToken ct)
        {
            var user = await _userRepository.GetByIdAsync(hostId);
            if (user == null)
            {
                _logger.LogError("Host {HostId} not found.", hostId);
                return Result<string>.Failure(Errors.Codes.Common.UserNotFound, Errors.Messages.Common.UserNotFound);
            }

            if (user.KycStatus != SubmissionStatus.Approved)
            {
                _logger.LogError("Host {HostId} KYC not approved.", hostId);
                return Result<string>.Failure(Errors.Codes.Common.UnauthorizedAction, "Host has not completed KYC verification.");
            }

            var room = await _roomRepository.GetByIdAsync(dto.RoomId);
            if (room == null)
            {
                _logger.LogError("Room {RoomId} not found.", dto.RoomId);
                return Result<string>.Failure(Errors.Codes.Room.RoomNotFound, Errors.Messages.Room.RoomNotFound);
            }

            if (room.Status != RoomListingStatus.Published)
            {
                _logger.LogError("Room {RoomId} is not published.", dto.RoomId);
                return Result<string>.Failure(Errors.Codes.Auction.RoomNotActive, "Room is not published/active.");
            }

            if (room.HostId != hostId)
            {
                _logger.LogError("Host {HostId} does not own room {RoomId}.", hostId, dto.RoomId);
                return Result<string>.Failure(Errors.Codes.Common.UnauthorizedAction, "You cannot create an auction for someone else's room.");
            }

            var existing = await _auctionRepository.GetActiveByRoomIdAsync(dto.RoomId, ct);
            if (existing != null)
            {
                _logger.LogError("Duplicate active auction for room {RoomId}.", dto.RoomId);
                return Result<string>.Failure(Errors.Codes.Auction.DuplicateAuction, "An active auction already exists for this room.");
            }

            var activeCount = await _auctionRepository.CountActiveByHostIdAsync(hostId, ct);
            if (activeCount >= 5)
            {
                _logger.LogError("Host {HostId} reached max active auctions.", hostId);
                return Result<string>.Failure(Errors.Codes.Auction.MaxAuctionsReached, "Host has reached the maximum number of active auctions.");
            }

            return Result<string>.Success(room.Title);
        }

        private Result<(DateTime StartTime, DateTime EndTime, AuctionDuration DurationEnum)> CalculateDates(CreateAuctionRequestDto dto)
        {
            if (!Enum.TryParse<AuctionDuration>(dto.Duration, true, out var durationEnum))
                return Result<(DateTime, DateTime, AuctionDuration)>.Failure(
                    Errors.Codes.Common.ValidationError, "Invalid duration value.");

            var startTime = DateTime.UtcNow;
            var endTime = startTime.AddHours((int)durationEnum);

            var checkInUtc = DateTime.SpecifyKind(dto.CheckInDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var latestEnd = checkInUtc.AddHours(-AuctionConstants.EndTimePriorCheckInHours);

            if (endTime > latestEnd)
            {
                _logger.LogError("EndTime {EndTime} too close to check-in {CheckIn}.", endTime, dto.CheckInDate);
                return Result<(DateTime, DateTime, AuctionDuration)>.Failure(
                    Errors.Codes.Auction.EndTimeTooLate,
                    $"Auction must end at least {AuctionConstants.EndTimePriorCheckInHours} hours before check-in.");
            }

            return Result<(DateTime, DateTime, AuctionDuration)>.Success((startTime, endTime, durationEnum));
        }

        private decimal ReadInsuranceRate()
        {
            var raw = _configuration["InsuranceDepositRate"]
                      ?? throw new InvalidOperationException("InsuranceDepositRate config key is missing.");
            return decimal.Parse(raw);
        }

        private void ScheduleJobs(Auction auction, DateTime startTime, DateTime endTime)
        {
            var duration = endTime - startTime;
            _backgroundJob.ScheduleAuctionSettlement(auction.Id, duration);
            _backgroundJob.ScheduleAuctionEndingSoon(
                auction.Id,
                duration - TimeSpan.FromMinutes(AuctionConstants.EndingSoonNotificationMin));
        }
    }
}
