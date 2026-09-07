using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Workbench.Data;
using Workbench.Modules.Auth.Models;

namespace Workbench.Tests.Helpers;

public static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    public static async Task<ApplicationUser> SeedUser(AppDbContext db, int id, string username)
    {
        var user = new ApplicationUser { Id = id, UserName = username, Email = $"{username}@test.com" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }
}
