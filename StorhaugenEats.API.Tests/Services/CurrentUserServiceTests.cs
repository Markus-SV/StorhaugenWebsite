using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenEats.API.Models;
using StorhaugenEats.API.Services;
using Xunit;

namespace StorhaugenEats.API.Tests.Services;

public class CurrentUserServiceTests
{
    [Fact]
    public async Task GetOrCreateUserIdAsync_RetriesWhenShareIdCollides()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new CollisionDbContext(options);

        var accessor = BuildAccessor(email: "collision@test.com", subject: "subject-1", name: "Collision User");
        var shareIds = new Queue<string>(["DUPLICATE01X2", "UNIQUECODE12"]);
        var service = new TestCurrentUserService(accessor, context, shareIds);

        var userId = await service.GetOrCreateUserIdAsync();

        var createdUser = await context.Users.SingleAsync(u => u.Email == "collision@test.com");
        createdUser.Id.Should().Be(userId);
        createdUser.UniqueShareId.Should().Be("UNIQUECODE12");
        createdUser.DisplayName.Should().Be("Collision User");
        createdUser.CreatedAt.Should().NotBe(default);
    }

    private static IHttpContextAccessor BuildAccessor(string email, string subject, string name)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.Email, email),
                        new Claim(ClaimTypes.NameIdentifier, subject),
                        new Claim("name", name)
                    },
                    authenticationType: "TestAuth"))
        };

        return new HttpContextAccessor { HttpContext = httpContext };
    }

    private class TestCurrentUserService : CurrentUserService
    {
        private readonly Queue<string> _shareIds;

        public TestCurrentUserService(IHttpContextAccessor httpContextAccessor, AppDbContext context, Queue<string> shareIds)
            : base(httpContextAccessor, context)
        {
            _shareIds = shareIds;
        }

        protected override string GenerateShareIdCandidate()
        {
            return _shareIds.Count > 0 ? _shareIds.Dequeue() : base.GenerateShareIdCandidate();
        }
    }

    private class CollisionDbContext : AppDbContext
    {
        private bool _collisionInjected;

        public CollisionDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (!_collisionInjected)
            {
                var collidingUser = ChangeTracker.Entries<User>()
                    .FirstOrDefault(e => e.State == EntityState.Added && e.Entity.UniqueShareId == "DUPLICATE01X2");

                if (collidingUser != null)
                {
                    _collisionInjected = true;

                    base.Users.Add(new User
                    {
                        Id = Guid.NewGuid(),
                        Email = "existing@test.com",
                        DisplayName = "Existing User",
                        UniqueShareId = collidingUser.Entity.UniqueShareId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });

                    base.SaveChanges();
                    throw new DbUpdateException("Simulated unique constraint violation for UniqueShareId");
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
