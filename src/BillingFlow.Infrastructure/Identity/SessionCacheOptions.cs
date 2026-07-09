using System;

namespace BillingFlow.Infrastructure.Identity;

public class SessionCacheOptions
{
    public const string SectionName = "SessionCache";
    public TimeSpan ValidationTtl { get; set; } = TimeSpan.FromMinutes(5);
}
