using PrepPilot.Application.Abstractions;
using PrepPilot.Application.Dtos;
using PrepPilot.Application.Exceptions;
using PrepPilot.Domain.Entities;

namespace PrepPilot.Application.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);
    Task<UserDto> GetMeAsync(int userId, CancellationToken ct = default);
}

public class AuthService(
    IUserRepository users,
    IPasswordHasher hasher,
    ITokenService tokens,
    IUnitOfWork unitOfWork,
    TimeProvider time) : IAuthService
{
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        if (dto.Password.Length < 8)
            throw new BusinessRuleException("Password must be at least 8 characters.");

        var email = NormalizeEmail(dto.Email);
        if (await users.GetByEmailAsync(email, ct) is not null)
            throw new ConflictException("An account with this email already exists.");

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            PasswordHash = hasher.Hash(dto.Password),
            CreatedAtUtc = time.GetUtcNow().UtcDateTime
        };

        await users.AddAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return ToResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var user = await users.GetByEmailAsync(NormalizeEmail(dto.Email), ct);

        // Same message for "no such user" and "wrong password" so accounts cannot be enumerated.
        if (user is null || !hasher.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        return ToResponse(user);
    }

    public async Task<UserDto> GetMeAsync(int userId, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("User not found.");
        return new UserDto(user.Id, user.FullName, user.Email);
    }

    private AuthResponseDto ToResponse(User user)
    {
        var token = tokens.Create(user);
        return new AuthResponseDto(token.Token, token.ExpiresAtUtc, new UserDto(user.Id, user.FullName, user.Email));
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
