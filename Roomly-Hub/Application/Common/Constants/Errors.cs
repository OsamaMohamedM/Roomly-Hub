namespace Application.Common.Constants
{
    public static class Errors
    {
        public static class Codes
        {
            public static class Common
            {
                public const string ValidationError = "VALIDATION_ERROR";
                public const string UserNotFound = "USER_NOT_FOUND";
                public const string PermissionDenied = "PERMISSION_DENIED";
                public const string UnauthorizedAction = "UNAUTHORIZED_ACTION";
                public const string InvalidState = "INVALID_STATE";
                public const string InternalError = "INTERNAL_ERROR";
                public const string RequestFailed = "REQUEST_FAILED";
            }

            public static class Room
            {
                public const string RoomNotFound = "ROOM_NOT_FOUND";
                public const string ModeratorNotFound = "MODERATOR_NOT_FOUND";
                public const string InvalidRoomStatus = "INVALID_ROOM_STATUS";
                public const string DateAlreadyBlocked = "DATE_ALREADY_BLOCKED";
                public const string DateNotBlocked = "DATE_NOT_BLOCKED";
            }

            public static class Auth
            {
                public const string InvalidCredentials = "INVALID_CREDENTIALS";
                public const string InvalidRefreshToken = "INVALID_REFRESH_TOKEN";
                public const string InvalidGoogleToken = "INVALID_GOOGLE_TOKEN";
                public const string InvalidOtp = "INVALID_OTP";
                public const string OtpInvalidated = "OTP_INVALIDATED";
                public const string InvalidRequest = "INVALID_REQUEST";
                public const string EmailAlreadyExists = "EMAIL_ALREADY_EXISTS";
                public const string EmailAlreadyVerified = "EMAIL_ALREADY_VERIFIED";
                public const string EmailNotVerified = "EMAIL_NOT_VERIFIED";
                public const string EmailSendFailed = "EMAIL_SEND_FAILED";
                public const string OtpCooldown = "OTP_COOLDOWN";
                public const string AccountInactive = "ACCOUNT_INACTIVE";
                public const string AccountLocked = "ACCOUNT_LOCKED";
                public const string SecurityAlert = "SECURITY_ALERT";
                public const string TokenExpired = "TOKEN_EXPIRED";
            }

            public static class Booking
            {
                public const string BookingNotFound = "BOOKING_NOT_FOUND";
                public const string RoomNotAvailable = "ROOM_NOT_AVAILABLE";
                public const string InvalidBookingState = "INVALID_BOOKING_STATE";
                public const string InvalidBookingDateRange = "INVALID_BOOKING_DATE_RANGE";
                public const string PaymentFailed = "PAYMENT_FAILED";
                public const string CancellationNotAllowed = "CANCELLATION_NOT_ALLOWED";
                public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
            }

            public static class Kyc
            {
                public const string AccountInactive = "ACCOUNT_INACTIVE";
                public const string KycLimitReached = "KYC_LIMIT_REACHED";
                public const string PendingKycExists = "PENDING_KYC_EXISTS";
                public const string SubmissionNotFound = "SUBMISSION_NOT_FOUND";
                public const string ForbiddenReview = "FORBIDDEN";
            }
        }

        public static class Messages
        {
            public static class Common
            {
                public const string RequestBodyRequired = "Request body is required.";
                public const string RequestValidationFailed = "Request validation failed.";
                public const string UserNotFound = "User not found.";
                public const string UserIdRequired = "User ID is required.";
                public const string EmailRequired = "Email is required.";
            }

            public static class Room
            {
                public const string UserNotFound = "The specified user was not found.";
                public const string ModeratorNotFound = "The specified moderator was not found.";
                public const string PermissionDenied = "User does not have permission to perform this action.";
                public const string UnauthorizedAction = "You do not have permission to perform this action.";
                public const string RoomNotFound = "The specified room was not found.";
                public const string CannotCreateListing = "User does not have permission to create a listing.";
                public const string CannotUpdateListing = "User does not have permission to update this listing.";
                public const string CannotDeactivateListing = "User does not have permission to deactivate this listing.";
                public const string CannotSubmitForReview = "User does not have permission to submit this listing for review.";
                public const string RoomCannotBeEdited = "Room cannot be edited in its current state.";
                public const string RoomCannotBeDeactivated = "Room cannot be deactivated in its current state.";
                public const string OnlyPendingRoomsCanBeApproved = "Only rooms that are pending review can be approved.";
                public const string OnlyPendingRoomsCanBeRejected = "Only rooms that are pending review can be rejected.";
                public const string DateAlreadyBlocked = "A date in the selected range is already blocked.";
                public const string DateNotBlocked = "A date in the selected range is not blocked.";
            }

            public static class Auth
            {
                public const string InvalidCredentials = "Invalid email or password.";
                public const string InvalidRefreshToken = "Invalid or expired refresh token.";
                public const string InvalidGoogleToken = "Invalid or expired Google token.";
                public const string InvalidOtp = "The provided OTP is invalid.";
                public const string OtpInvalidated = "OTP invalidated.";
                public const string InvalidRequest = "Try again.";
                public const string EmailAlreadyExists = "Email is already in use.";
                public const string EmailAlreadyVerified = "Email is already verified.";
                public const string EmailNotVerified = "Please verify your email before logging in.";
                public const string AccountInactive = "Your account is inactive.";
                public const string AccountLocked = "Your account is locked. Please contact support.";
                public const string SecurityAlert = "Token reuse detected. Please login again.";
                public const string TokenExpired = "Your session has expired. Please login again.";
                public const string RefreshTokenNotFound = "refresh not found.";
                public const string EmailSendFailed = "Failed to send verification email.";
            }

            public static class Booking
            {
                public const string BookingNotFound = "The specified booking was not found.";
                public const string RoomNotAvailable = "The room is not available for the selected dates.";
                public const string InvalidBookingState = "Booking cannot be processed in its current state.";
                public const string InvalidBookingDateRange = "Check-out date must be after check-in date.";
                public const string PaymentFailed = "Booking payment could not be processed.";
                public const string CancellationNotAllowed = "Booking cancellation is not allowed in the current state.";
                public const string ConcurrencyConflict = "The booking was changed by another request. Please retry.";
            }

            public static class Kyc
            {
                public const string AccountInactive = "Your account is inactive.";
                public const string KycLimitReached = "Maximum KYC attempts reached.";
                public const string PendingKycExists = "You already have a pending KYC submission.";
                public const string SubmissionNotFound = "KYC submission not found.";
                public const string ForbiddenReview = "Only admins can review KYC submissions.";
            }
        }
    }
}