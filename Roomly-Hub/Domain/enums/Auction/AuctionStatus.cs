namespace Domain.enums.Auction
{
    public enum AuctionStatus
    {
        Active = 1,
        Pending_Payment = 2,
        Completed = 3,
        Expired_NoBids = 4,
        Cancelled_By_Host = 5,
        Defaulted_Cascaded = 6,
        Fully_Defaulted = 7
    }
}