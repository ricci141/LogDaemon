using Workers;
using Workers.Models;
using Workers.Services;

var builder = Host.CreateApplicationBuilder(args);

var settings = builder.Configuration
    .GetSection("RageSettings")
    .Get<AppSettings>()!;

builder.Services.AddSingleton(settings);

builder.Services.AddSingleton<IRageProcessMonitor>(_ => new RageProcessMonitor(settings.ProcessName));

builder.Services.AddSingleton<IChatLogBackupService, ChatLogBackupService>();

builder.Services.AddHostedService<RageWorker>();

builder.Services.AddLogging();

var app = builder.Build();

await app.RunAsync();