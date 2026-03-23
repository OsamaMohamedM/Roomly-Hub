namespace Application.Common.Constants
{
    public static class RoomErrorCodes
    {
        public const string UserNotFound = "USER_NOT_FOUND";
        public const string ModeratorNotFound = "MODERATOR_NOT_FOUND";
        public const string PermissionDenied = "PERMISSION_DENIED";
        public const string UnauthorizedAction = "UNAUTHORIZED_ACTION";

        public const string RoomNotFound = "ROOM_NOT_FOUND";

        public const string InvalidState = "INVALID_STATE";
        public const string InvalidRoomStatus = "INVALID_ROOM_STATUS";

        public const string ValidationFailed = "VALIDATION_FAILED";

        public const string RequestFailed = "REQUEST_FAILED";
        public const string InternalError = "INTERNAL_ERROR";
    }

    public static class RoomErrorMessages
    {
        public const string UserNotFoundMessage = "The specified user was not found.";
        public const string ModeratorNotFoundMessage = "The specified moderator was not found.";
        public const string PermissionDeniedMessage = "User does not have permission to perform this action.";
        public const string UnauthorizedActionMessage = "You do not have permission to perform this action.";

        public const string RoomNotFoundMessage = "The specified room was not found.";
        public const string CannotCreateListingMessage = "User does not have permission to create a listing.";
        public const string CannotUpdateListingMessage = "User does not have permission to update this listing.";
        public const string CannotDeactivateListingMessage = "User does not have permission to deactivate this listing.";
        public const string CannotSubmitForReviewMessage = "User does not have permission to submit this listing for review.";

        public const string RoomCannotBeEditedMessage = "Room cannot be edited in its current state.";
        public const string RoomCannotBeDeactivatedMessage = "Room cannot be deactivated in its current state.";
        public const string OnlyPendingRoomsCanBeApprovedMessage = "Only rooms that are pending review can be approved.";
        public const string OnlyPendingRoomsCanBeRejectedMessage = "Only rooms that are pending review can be rejected.";
    }
}