using System.Reflection;

Console.WriteLine("RedgifsDownloader.Cli");
Console.WriteLine($"Core loaded: {Assembly.Load("RedgifsDownloader.Core").GetName().Name}");
Console.WriteLine("CLI scaffold is ready for Linux core download testing.");
