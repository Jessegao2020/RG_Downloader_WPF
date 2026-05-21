using Microsoft.Extensions.DependencyInjection;
using RedgifsDownloader.ApplicationLayer.Interfaces;
using RedgifsDownloader.Cli;

var services = new ServiceCollection();
services.AddSingleton<ISecretProtector, PlainTextSecretProtector>();
using var provider = services.BuildServiceProvider();

Console.WriteLine("RedgifsDownloader.Cli");
Console.WriteLine("CLI scaffold is ready for Linux core download testing.");
