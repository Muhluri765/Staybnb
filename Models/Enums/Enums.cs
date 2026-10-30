namespace Staybnb.Models.Enums
{
    public enum BookingStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled,
        CheckedIn,
        Completed
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Refunded
    }

    public enum ApplicationStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public enum CheckInStatus
    {
        Pending,
        InReview,
        Approved,
        Rejected
    }

    public enum DocumentStatus
    {
        Pending,
        Verified,
        Rejected
    }
}
