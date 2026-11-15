using Worker;
using Worker.Extensions;
using Worker.Models;
using Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.ConfigureRageApp();

var app = builder.Build();

await app.RunAsync();