using CleanDistrict.Domain;
using Microsoft.AspNetCore.Identity;

namespace CleanDistrict.Infrastructure;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = "";
    public Guid? ClientId { get; set; }
    public Client? Client { get; set; }
}
