using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrepPilot.Application.Abstractions;
using PrepPilot.Infrastructure.Ai;
using PrepPilot.Infrastructure.Persistence;
using PrepPilot.Infrastructure.Security;

namespace PrepPilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString, string aiProvider = "Groq")
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IMockTestRepository, MockTestRepository>();
        services.AddScoped<IAttemptRepository, AttemptRepository>();
        services.AddScoped<IProblemRepository, ProblemRepository>();
        services.AddScoped<IStatsRepository, StatsRepository>();

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        // AI provider is chosen by configuration (Ai:Provider): Groq (free tier, default) or Anthropic (paid).
        if (string.Equals(aiProvider, "Anthropic", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IAiClient, AnthropicAiClient>(client =>
            {
                client.BaseAddress = new Uri("https://api.anthropic.com/");
                client.Timeout = TimeSpan.FromSeconds(120);
            });
        }
        else
        {
            services.AddHttpClient<IAiClient, GroqAiClient>(client =>
            {
                client.BaseAddress = new Uri("https://api.groq.com/");
                client.Timeout = TimeSpan.FromSeconds(120);
            });
        }

        return services;
    }
}
