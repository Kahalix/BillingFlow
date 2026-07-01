namespace BillingFlow.Infrastructure.Outbox.Models;

public enum DispatchStatus
{
    /// <summary>Worker successfully claimed the lock and is currently executing the side effect.</summary>
    Processing = 1,

    /// <summary>The external side-effect completed successfully.</summary>
    Completed = 2,

    /// <summary>The external side-effect failed (e.g., network timeout). Lock is released for retry.</summary>
    Failed = 3
}
