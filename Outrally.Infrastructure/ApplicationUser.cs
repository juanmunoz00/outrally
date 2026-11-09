using Microsoft.AspNetCore.Identity;
using Outrally.Core;

namespace Outrally.Infrastructure;

public enum UserRole { OE, TL, AD }

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.OE;
    public int OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
