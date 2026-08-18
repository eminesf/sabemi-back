using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Sabemi.Webhooks.Service.Interfaces;
using Sabemi.Webhooks.Repository.BackgroundProcessing;
using Sabemi.Webhooks.Repository.Persistence;
using Sabemi.Webhooks.Repository.Repositories;

namespace Sabemi.Webhooks.Repository.DependencyInjection;

public static class RepositoryLayerExtensions
{
    public static IServiceCollection AddRepositoryLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<SabemiDbContext>(options =>
            options.UseNpgsql(ResolveConnectionString(configuration)));

        services.AddScoped<IEventoBrutoLogRepository, EventoBrutoLogRepository>();
        services.AddScoped<IStatusContratoRepository, StatusContratoRepository>();

        services.AddSingleton<IBackgroundTaskQueue, ChannelBackgroundTaskQueue>();
        services.AddHostedService<PagamentoProcessingBackgroundService>();

        return services;
    }

    /// <summary>
    /// Resolve a connection string a partir de ConnectionStrings:Default (appsettings)
    /// ou, na ausência dela, da variável DATABASE_URL no formato URI — padrão usado
    /// por provedores como Railway e Supabase (postgres://user:pass@host:port/db).
    /// </summary>
    private static string ResolveConnectionString(IConfiguration configuration)
    {
        var fromConfig = configuration.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(fromConfig))
            return fromConfig;

        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (string.IsNullOrWhiteSpace(databaseUrl))
        {
            throw new InvalidOperationException(
                "Nenhuma connection string configurada. Defina 'ConnectionStrings:Default' (appsettings) " +
                "ou a variável de ambiente 'DATABASE_URL'.");
        }

        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            Database = uri.AbsolutePath.TrimStart('/'),
            SslMode = SslMode.Require
        };

        return connectionStringBuilder.ConnectionString;
    }
}
