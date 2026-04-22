namespace Domain.Constants
{
    public static class AuctionConstants
    {
        public const int WinnerPaymentWindowMinutes = 120;
        public const int CascadePaymentWindowMinutes = 30;
        public const int MaxCascadeDepth = 4;
        public const int AntiSnipingTriggerSeconds = 60;
        public const int AntiSnipingExtensionSeconds = 120;
        public const int EndTimePriorCheckInHours = 24;
        public const int EndingSoonNotificationMin = 10;
    }
}
