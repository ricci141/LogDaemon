using Worker.Models;
using Worker.Services;

namespace Worker.Extensions;

public static class AppExtensions
{
    public static HostApplicationBuilder ConfigureRageApp(this HostApplicationBuilder builder)
    {
        string userConfigPath = Path.Combine(AppContext.BaseDirectory, "appsettings.user.json");
        builder.Configuration.AddJsonFile(userConfigPath, optional: true, reloadOnChange: true);

        var settings = builder.Configuration.GetSection("RageSettings").Get<AppSettings>()
                       ?? new AppSettings();
        
        builder.Services.AddSingleton(settings);

        builder.Services.AddSingleton<IRageProcessMonitor>(_ => new RageProcessMonitor(settings.ProcessName));

        builder.Services.AddSingleton<IChatLogBackupService, ChatLogBackupService>();

        builder.Services.AddHostedService<RageWorker>();

        builder.Services.AddLogging();
        
        return builder;
    }
}