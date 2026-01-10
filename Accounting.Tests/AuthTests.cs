using Accounting.Application.Authentication.Commands.Login;
using Accounting.Application.Authentication.Commands.Register;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Accounting.Tests.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Accounting.Tests;

public class AuthTests
{
    private readonly Mock<IJwtTokenGenerator> _mockTokenGenerator;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;

    public AuthTests()
    {
        _mockTokenGenerator = new Mock<IJwtTokenGenerator>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        
        // Setup default behaviors
        _mockTokenGenerator.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<List<string>>()))
            .Returns("fake-token");
        _mockTokenGenerator.Setup(x => x.GenerateRefreshToken())
            .Returns(("fake-refresh-token", DateTime.UtcNow.AddDays(7)));
        
        _mockPasswordHasher.Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns((string pwd) => $"hashed-{pwd}");
        _mockPasswordHasher.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
             .Returns((string hash, string pwd) => hash == $"hashed-{pwd}");
    }

    private AppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // Auth tests usually don't need Audit logging, but we need to satisfy DI
        var fakeUserService = new FakeCurrentUserService(null);
        var interceptor = new Accounting.Infrastructure.Persistence.Interceptors.AuditSaveChangesInterceptor(fakeUserService);
        return new AppDbContext(options, interceptor, fakeUserService);
    }

    [Fact]
    public async Task Register_ShouldSucceed_WhenEmailIsUnique()
    {
        var db = GetDbContext();
        var handler = new RegisterCommandHandler(db, _mockPasswordHasher.Object, _mockTokenGenerator.Object);
        var command = new RegisterCommand("John", "Doe", "john@test.com", "Password123");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("John", result.FirstName);
        Assert.Equal("fake-token", result.AccessToken);
        
        // Use async lambda for checking db state
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "john@test.com");
        Assert.NotNull(user);
        Assert.Equal("hashed-Password123", user.PasswordHash);
    }

    [Fact]
    public async Task Register_ShouldThrow_WhenEmailExists()
    {
        var db = GetDbContext();
        db.Users.Add(new User { FirstName = "Existing", LastName = "User", Email = "john@test.com", PasswordHash = "hash" });
        await db.SaveChangesAsync();

        var handler = new RegisterCommandHandler(db, _mockPasswordHasher.Object, _mockTokenGenerator.Object);
        var command = new RegisterCommand("John", "Doe", "john@test.com", "Password123");

        await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Login_ShouldSucceed_WhenCredentialsCorrect()
    {
        var db = GetDbContext();
        var hashedPassword = "hashed-Password123";
        db.Users.Add(new User 
        { 
            FirstName = "John", LastName = "Doe", Email = "john@test.com", 
            PasswordHash = hashedPassword 
        });
        await db.SaveChangesAsync();

        var handler = new LoginCommandHandler(db, _mockPasswordHasher.Object, _mockTokenGenerator.Object);
        var command = new LoginCommand("john@test.com", "Password123");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("John", result.FirstName);
        Assert.NotNull(result.AccessToken);
    }

    [Fact]
    public async Task Login_ShouldThrow_WhenUserNotFound()
    {
        var db = GetDbContext();
        var handler = new LoginCommandHandler(db, _mockPasswordHasher.Object, _mockTokenGenerator.Object);
        var command = new LoginCommand("wrong@test.com", "Password123");

        await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Login_ShouldThrow_WhenPasswordIncorrect()
    {
        var db = GetDbContext();
        db.Users.Add(new User 
        { 
            FirstName = "John", LastName = "Doe", Email = "john@test.com", 
            PasswordHash = "hashed-CorrectPassword" 
        });
        await db.SaveChangesAsync();

        var handler = new LoginCommandHandler(db, _mockPasswordHasher.Object, _mockTokenGenerator.Object);
        var command = new LoginCommand("john@test.com", "WrongPassword");

        await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
    }
}
