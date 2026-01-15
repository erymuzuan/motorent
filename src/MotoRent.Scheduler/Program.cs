using System.Diagnostics;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MotoRent.Domain.Messaging;
using MotoRent.Messaging;
using MotoRent.Scheduler;
using MotoRent.Scheduler.Runners;
using Spectre.Console;

await SchedulerProgram.RunAsync(args);

namespace MotoRent.Scheduler
{
    /// <summary>
    /// Marker class for Scheduler logger.
    /// </summary>
    public class SchedulerLogger { }

    public static class SchedulerProgram
    {
        public static async Task RunAsync(string[] args)
        {
            string? runnerName = null;
            var printHelp = false;

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    runnerName = o.Runner;
                    printHelp = o.PrintHelp;
                });

            if (printHelp)
            {
                PrintHelp();
                return;
            }

            AnsiConsole.Write(new FigletText($"MotoRent Scheduler")
                .Centered()
                .Color(Color.Teal));
            AnsiConsole.WriteLine();

            // Build host
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.SetMinimumLevel(LogLevel.Information);
                    });

                    services.AddSingleton<IMessageBroker>(sp =>
                    {
                        var logger = sp.GetRequiredService<ILogger<RabbitMqMessageBroker>>();
                        return new RabbitMqMessageBroker(logger);
                    });

                    // Register task runners
                    services.AddScoped<MaintenanceService>();
                    services.AddScoped<MaintenanceAlertService>();
                    services.AddTransient<ITaskRunner, RentalExpiryRunner>();
                    services.AddTransient<ITaskRunner, MaintenanceAlertRunner>();
                })
                .Build();

            var logger = host.Services.GetRequiredService<ILogger<SchedulerLogger>>();
            var broker = host.Services.GetRequiredService<IMessageBroker>();

            // Connect to RabbitMQ
            try
            {
                await broker.ConnectAsync((msg, _) =>
                {
                    logger.LogWarning("RabbitMQ disconnected: {Message}", msg);
                });

                logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}/{VHost}",
                    RabbitMqConfigurationManager.Host,
                    RabbitMqConfigurationManager.Port,
                    RabbitMqConfigurationManager.VirtualHost);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to connect to RabbitMQ");
                return;
            }

            // Get available runners
            var runners = host.Services.GetServices<ITaskRunner>().ToList();

            if (string.IsNullOrEmpty(runnerName))
            {
                // Interactive mode - let user select runner
                var runnerNames = runners.Select(r => $"{r.Name} - {r.Description}").ToList();
                runnerNames.Add("Exit");

                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select a [green]task runner[/] to execute:")
                        .AddChoices(runnerNames));

                if (selection == "Exit")
                    return;

                runnerName = selection.Split(" - ")[0];
            }

            var runner = runners.FirstOrDefault(r => r.Name.Equals(runnerName, StringComparison.OrdinalIgnoreCase));
            if (runner == null)
            {
                logger.LogError("Runner not found: {RunnerName}", runnerName);
                logger.LogInformation("Available runners: {Runners}", string.Join(", ", runners.Select(r => r.Name)));
                return;
            }

            // Run the task
            var sw = Stopwatch.StartNew();
            try
            {
                logger.LogInformation("Starting {Runner}...", runner.Name);
                await runner.RunAsync();
                sw.Stop();
                logger.LogInformation("{Runner} completed in {Elapsed}ms", runner.Name, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                logger.LogError(ex, "{Runner} failed after {Elapsed}ms", runner.Name, sw.ElapsedMilliseconds);
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("MotoRent Scheduler - Scheduled Task Runner");
            Console.WriteLine();
            Console.WriteLine("Usage: MotoRent.Scheduler [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  /r:<runner>      Runner name to execute");
            Console.WriteLine("  /?               Show this help");
            Console.WriteLine();
            Console.WriteLine("Available Runners:");
            Console.WriteLine("  RentalExpiryRunner    Check for expiring rentals");
            Console.WriteLine();
            Console.WriteLine("Environment Variables:");
            Console.WriteLine("  MOTORENT_RabbitMqHost       RabbitMQ host (default: localhost)");
            Console.WriteLine("  MOTORENT_RabbitMqPort       RabbitMQ port (default: 5672)");
        }
    }

    public class Options
    {
        [Option('r', "runner", HelpText = "Runner name to execute")]
        public string? Runner { get; set; }

        [Option('?', "help", HelpText = "Print help")]
        public bool PrintHelp { get; set; }
    }
}
