using Microsoft.Extensions.DependencyInjection;
using PrepPilot.Application.Services;

namespace PrepPilot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IMockTestService, MockTestService>();
        services.AddScoped<IAttemptService, AttemptService>();
        services.AddScoped<IProblemService, ProblemService>();
        services.AddScoped<IAiPaperService, AiPaperService>();
        services.AddScoped<IAiCoachService, AiCoachService>();
        services.AddScoped<IDashboardService, DashboardService>();
        return services;
    }
}
