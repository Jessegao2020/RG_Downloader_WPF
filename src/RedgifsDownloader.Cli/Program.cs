using Microsoft.Extensions.DependencyInjection;
using RedgifsDownloader.ApplicationLayer.Interfaces;
using RedgifsDownloader.Infrastructure;
using System.Reflection;

var services = new ServiceCollection();
services.AddSingleton<ISecretProtector, PlainTextSecretProtector>();

using var provider = services.BuildServiceProvider();
_ = provider.GetRequiredService<ISecretProtector>();

Console.WriteLine("RedgifsDownloader.Cli");
Console.WriteLine($"Core loaded: {Assembly.Load("RedgifsDownloader.Core").GetName().Name}");
Console.WriteLine("CLI scaffold is ready for Linux core download testing.");
