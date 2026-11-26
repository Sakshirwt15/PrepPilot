using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PrepPilot.Application.Dtos;
using PrepPilot.Application.Exceptions;

namespace PrepPilot.Tests;

public class AuthServiceTests
{
    private static RegisterDto Reg(string email = "sakshi@example.com", string password = "Passw0rd!") =>
        new() { FullName = "Sakshi Rawat", Email = email, Password = password };

    [Fact]
    public async Task Register_ThenLogin_Works_AndEmailIsCaseInsensitive()
    {
        using var db = TestSupport.NewDb();
        var auth = TestSupport.Auth(db);

        var registered = await auth.RegisterAsync(Reg("Sakshi@Example.com"));
        var loggedIn = await auth.LoginAsync(new LoginDto { Email = "SAKSHI@example.COM", Password = "Passw0rd!" });

        Assert.Equal("sakshi@example.com", registered.User.Email);
        Assert.Equal(registered.User.Id, loggedIn.User.Id);
    }

    [Fact]
    public async Task Register_DoesNotStorePlainTextPassword()
    {
        using var db = TestSupport.NewDb();
        await TestSupport.Auth(db).RegisterAsync(Reg());

        var user = await db.Users.SingleAsync();
        Assert.NotEqual("Passw0rd!", user.PasswordHash);
        Assert.StartsWith("$2", user.PasswordHash); // bcrypt
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsConflict()
    {
        using var db = TestSupport.NewDb();
        var auth = TestSupport.Auth(db);
        await auth.RegisterAsync(Reg());

        await Assert.ThrowsAsync<ConflictException>(() => auth.RegisterAsync(Reg("SAKSHI@example.com")));
    }

    [Fact]
    public async Task Register_ShortPassword_IsRejected()
    {
        using var db = TestSupport.NewDb();
        await Assert.ThrowsAsync<BusinessRuleException>(() => TestSupport.Auth(db).RegisterAsync(Reg(password: "short")));
    }

    [Theory]
    [InlineData("sakshi@example.com", "wrong-password")]
    [InlineData("nobody@example.com", "Passw0rd!")]
    public async Task Login_WithBadCredentials_ThrowsUnauthorized(string email, string password)
    {
        using var db = TestSupport.NewDb();
        var auth = TestSupport.Auth(db);
        await auth.RegisterAsync(Reg());

        await Assert.ThrowsAsync<UnauthorizedException>(() => auth.LoginAsync(new LoginDto { Email = email, Password = password }));
    }

    [Fact]
    public async Task Token_ContainsUserIdClaim()
    {
        using var db = TestSupport.NewDb();
        var result = await TestSupport.Auth(db).RegisterAsync(Reg());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        var idClaim = jwt.Claims.First(c => c.Type is "nameid" or ClaimTypes.NameIdentifier);

        Assert.Equal(result.User.Id.ToString(), idClaim.Value);
        Assert.True(result.ExpiresAtUtc > DateTime.UtcNow);
    }
}

public class AttemptServiceTests
{
    [Fact]
    public async Task FullFlow_AllCorrect_PassesWithFullReview()
    {
        using var db = await TestSupport.SeededDbAsync();
        var user = await TestSupport.AddUserAsync(db);
        var service = TestSupport.Attempts(db);
        var test = await TestSupport.LoadTestAsync(db, "TCS NQT Style Mock 1");

        var started = await service.StartAsync(user.Id, new StartAttemptDto { MockTestId = test.Id });
        var result = await service.SubmitAsync(user.Id, started.AttemptId, TestSupport.AnswerAll(test, correct: true));

        Assert.Equal(test.Questions.Count, result.Score);
        Assert.Equal(100, result.Percent);
        Assert.True(result.Passed);
        Assert.Equal(test.Questions.Count, result.Review.Count);
        Assert.All(result.Review, r => Assert.True(r.IsCorrect));
        Assert.All(result.Topics, t => Assert.Equal(100, t.Percent));
        // After submitting, the review shows which option was correct.
        Assert.All(result.Review, r => Assert.Single(r.Options, o => o.IsCorrect));
    }

    [Fact]
    public async Task AllWrong_FailsAndReportsTopicWeakness()
    {
        using var db = await TestSupport.SeededDbAsync();
        var user = await TestSupport.AddUserAsync(db);
        var service = TestSupport.Attempts(db);
        var test = await TestSupport.LoadTestAsync(db, "Infosys Online Assessment Mock 1");

        var started = await service.StartAsync(user.Id, new StartAttemptDto { MockTestId = test.Id });
        var result = await service.SubmitAsync(user.Id, started.AttemptId, TestSupport.AnswerAll(test, correct: false));

        Assert.Equal(0, result.Score);
        Assert.False(result.Passed);
        Assert.All(result.Topics, t => Assert.Equal(0, t.Percent));
    }

    [Fact]
    public async Task SkippedQuestions_AreMarkedUnanswered()
    {
        using var db = await TestSupport.SeededDbAsync();
        var user = await TestSupport.AddUserAsync(db);
        var service = TestSupport.Attempts(db);
        var test = await TestSupport.LoadTestAsync(db, "Wipro Aptitude Mock 1");

        var started = await service.StartAsync(user.Id, new StartAttemptDto { MockTestId = test.Id });
        var result = await service.SubmitAsync(user.Id, started.AttemptId, new SubmitAttemptDto());

        Assert.Equal(0, result.Score);
        Assert.All(result.Review, r => Assert.False(r.Answered));
    }

    [Fact]
    public async Task Submit_Twice_ThrowsConflict()
    {
        using var db = await TestSupport.SeededDbAsync();
        var user = await TestSupport.AddUserAsync(db);
        var service = TestSupport.Attempts(db);
        var test = await TestSupport.LoadTestAsync(db, "TCS NQT Style Mock 1");

        var started = await service.StartAsync(user.Id, new StartAttemptDto { MockTestId = test.Id });
        await service.SubmitAsync(user.Id, started.AttemptId, new SubmitAttemptDto());

        await Assert.ThrowsAsync<ConflictException>(
            () => service.SubmitAsync(user.Id, started.AttemptId, new SubmitAttemptDto()));
    }

    [Fact]
    public async Task OtherUsers_CannotSeeOrSubmitYourAttempt()
    {
        using var db = await TestSupport.SeededDbAsync();
        var owner = await TestSupport.AddUserAsync(db, "Owner");
        var intruder = await TestSupport.AddUserAsync(db, "Intruder");
        var service = TestSupport.Attempts(db);
        var test = await TestSupport.LoadTestAsync(db, "TCS NQT Style Mock 1");

        var started = await service.StartAsync(owner.Id, new StartAttemptDto { MockTestId = test.Id });
        await service.SubmitAsync(owner.Id, started.AttemptId, new SubmitAttemptDto());

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetResultAsync(intruder.Id, started.AttemptId));
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.SubmitAsync(intruder.Id, started.AttemptId, new SubmitAttemptDto()));
    }

    [Fact]
    public async Task Submit_OptionFromAnotherQuestion_IsRejected()
    {
        using var db = await TestSupport.SeededDbAsync();
        var user = await TestSupport.AddUserAsync(db);
        var service = TestSupport.Attempts(db);
        var test = await TestSupport.LoadTestAsync(db, "TCS NQT Style Mock 1");

        var started = await service.StartAsync(user.Id, new StartAttemptDto { MockTestId = test.Id });
        var dto = new SubmitAttemptDto
        {
            Answers = new List<AnswerDto>
            {
                new() { QuestionId = test.Questions[0].Id, OptionId = test.Questions[1].Options[0].Id }
            }
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.SubmitAsync(user.Id, started.AttemptId, dto));
    }

    [Fact]
    public async Task GetResult_BeforeSubmitting_IsRejected()
    {
        using var db = await TestSupport.SeededDbAsync();
        var user = await TestSupport.AddUserAsync(db);
        var service = TestSupport.Attempts(db);
        var test = await TestSupport.LoadTestAsync(db, "TCS NQT Style Mock 1");

        var started = await service.StartAsync(user.Id, new StartAttemptDto { MockTestId = test.Id });

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.GetResultAsync(user.Id, started.AttemptId));
    }

    [Fact]
    public async Task Start_UnknownTest_ThrowsNotFound()
    {
        using var db = await TestSupport.SeededDbAsync();
        var user = await TestSupport.AddUserAsync(db);

        await Assert.ThrowsAsync<NotFoundException>(
            () => TestSupport.Attempts(db).StartAsync(user.Id, new StartAttemptDto { MockTestId = 99999 }));
    }

    [Fact]
    public async Task History_ListsOnlySubmittedAttempts_NewestFirst()
    {
        using var db = await TestSupport.SeededDbAsync();
        var user = await TestSupport.AddUserAsync(db);
        var service = TestSupport.Attempts(db);
        var test = await TestSupport.LoadTestAsync(db, "TCS NQT Style Mock 1");

        var a = await service.StartAsync(user.Id, new StartAttemptDto { MockTestId = test.Id });
        await service.SubmitAsync(user.Id, a.AttemptId, TestSupport.AnswerAll(test, true));
        await service.StartAsync(user.Id, new StartAttemptDto { MockTestId = test.Id }); // never submitted

        var history = await service.GetHistoryAsync(user.Id);

        Assert.Single(history);
        Assert.Equal("TCS", history[0].CompanyName);
    }
}
