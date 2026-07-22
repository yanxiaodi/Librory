using Librory.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Librory.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(LibroryDbContext context)
    {
        if (await context.Families.AnyAsync())
            return;

        var family = new Family { Name = "Test Family" };
        family.Members.Add(new Member
        {
            DisplayName = "Admin",
            Role = MemberRole.Admin,
        });

        context.Families.Add(family);
        await context.SaveChangesAsync();
    }
}
