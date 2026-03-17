using Application.Common.Results;
using Application.DTOs.Rooms;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services.RoomCRUD
{
    public class RoomModerationService : IRoomModerationService
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RoomModerationService(IRoomRepository roomRepository, IUnitOfWork unitOfWork)
        {
            _roomRepository = roomRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> ApproveRoomAsync(Guid moderatorId, Guid roomId, CancellationToken cancellationToken = default)
        {
            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
            {
                return Result.Failure("RoomNotFound", "The specified room does not exist.");
            }
            room.Approve();
            await _roomRepository.UpdateAsync(room);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result<List<PendingRoomDto>>> GetPendingRoomsAsync(CancellationToken cancellationToken = default)
        {
            var rooms = await _roomRepository.GetPendingReviewRoomsAsync(cancellationToken); ;
            var pendingRoomDTOs = rooms.Select(r => new PendingRoomDto
            {
                Id = r.Id,
                HostId = r.HostId,
                SubmittedAt = r.CreatedAt,
                Room = r
            }).ToList();
            return Result<List<PendingRoomDto>>.Success(pendingRoomDTOs);
        }

        public async Task<Result> RejectRoomAsync(Guid moderatorId, Guid roomId, string reason, CancellationToken cancellationToken = default)
        {
            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if(room == null)
            {
                return Result.Failure("RoomNotFound", "The specified room does not exist.");
            }

            room.Reject(reason);
            await _roomRepository.UpdateAsync(room, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();

        }
    }
}