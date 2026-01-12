using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MotoRent.Domain.Messaging;
using MotoRent.Messaging;
using MotoRent.Worker;
using MotoRent.Worker.Infrastructure;
using Spectre.Console;

await ConsoleProgram.RunAsync(args);

namespace MotoRent.Worker
{
    /// <summary>
    /// Marker class for Worker logger.
    /// </summary>
    public class WorkerLogger { }

    public static class ConsoleProgram
    {
        public static async Task RunAsync(string[] args)
        {
            var debug = GetEnvironmentVariable("RX_DEBUG") is "1" or "True" or "true";
            var instance = "Default";
            var printHelp = false;
            string[] started = [];

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    if (!debug)
                        debug = o.Debug;

                    instance = o.Instance ?? instance;
                    printHelp = o.PrintHelp;

                    var subs = o.Subscribers;
                    if (GetEnvironmentVariable("RX_SUBS") is { Length: > 0 } st)
                        subs = st;
                    if (!string.IsNullOrWhiteSpace(subs))
                        started = subs.Split([",", ";", "|", ":", "-"], StringSplitOptions.RemoveEmptyEntries);
                });

            if (printHelp)
            {
                PrintHelp();
                return;
            }

            AnsiConsole.WriteLine("Use /? for help");
            AnsiConsole.Write(new FigletText($"MotoRent Worker")
                .Centered()
                .Color(Color.Teal));
            AnsiConsole.WriteLine();

            var workerProcess = System.Diagnostics.Process.GetCurrentProcess();
            Console.Title = $"MotoRent Worker [{workerProcess.Id}] {instance}";

            if (debug)
            {
                Console.WriteLine($"Attach your debugger to [{workerProcess.Id}] {workerProcess.ProcessName}");
                Console.WriteLine("Press [ENTER] to continue");
                Console.ReadLine();
            }

            // Build services
            var services = new ServiceCollection();
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

            var provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<ILogger<WorkerLogger>>();
            var broker = provider.GetRequiredService<IMessageBroker>();

            // Connect to RabbitMQ
            await broker.ConnectAsync((msg, _) =>
            {
                logger.LogWarning("RabbitMQ disconnected: {Message}", msg);
            });

            logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}/{VHost}",
                RabbitMqConfigurationManager.Host,
                RabbitMqConfigurationManager.Port,
                RabbitMqConfigurationManager.VirtualHost);

            // Discover subscribers
            List<SubscriberMetadata> metadata = [];
            using (var discoverer = new Discoverer())
            {
                metadata.AddRange(discoverer.Find());
            }

            logger.LogInformation("Found {Count} subscribers", metadata.Count);

            // Filter subscribers
            if (started is [_, ..])
                metadata.RemoveAll(x => !started.Contains(x.Name));

            if (GetEnvironmentVariable("RX_EX_SUBS") is { Length: > 0 } exc)
            {
                var excludes = exc.Split([",", ";", "|", ":", "-"], StringSplitOptions.RemoveEmptyEntries);
                metadata.RemoveAll(x => excludes.Contains(x.Name));
            }

            // Start subscribers
            var stopFlag = new AutoResetEvent(false);
            Console.CancelKeyPress += (_, _) =>
            {
                logger.LogInformation("Shutting down...");
                stopFlag.Set();
            };

            foreach (var sub in metadata)
            {
                if (sub.Type == null) continue;

                try
                {
                    var subscriber = (Subscriber?)Activator.CreateInstance(sub.Type);
                    if (subscriber != null)
                    {
                        var subLogger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(sub.Type);
                        await subscriber.RunAsync(broker, provider, subLogger);
                        logger.LogInformation("Started subscriber: {Name}", sub.Name);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to start subscriber: {Name}", sub.Name);
                }
            }

            Console.WriteLine("Press Ctrl+C to quit.");
            stopFlag.WaitOne();
        }

        private static void PrintHelp()
        {
            Console.WriteLine("MotoRent Worker - RabbitMQ Message Subscriber");
            Console.WriteLine();
            Console.WriteLine("Usage: MotoRent.Worker [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  /i:<instance>    Instance name for multiple workers");
            Console.WriteLine("  /subs:<names>    Comma-separated subscriber names to run");
            Console.WriteLine("  /debug           Pause for debugger attachment");
            Console.WriteLine("  /?               Show this help");
            Console.WriteLine();
            Console.WriteLine("Environment Variables:");
            Console.WriteLine("  MOTORENT_RabbitMqHost       RabbitMQ host (default: localhost)");
            Console.WriteLine("  MOTORENT_RabbitMqPort       RabbitMQ port (default: 5672)");
            Console.WriteLine("  MOTORENT_RabbitMqUserName   Username (default: guest)");
            Console.WriteLine("  MOTORENT_RabbitMqPassword   Password (default: guest)");
            Console.WriteLine("  MOTORENT_RabbitMqVirtualHost Virtual host (default: motorent)");
            Console.WriteLine("  RX_SUBS                     Subscribers to run");
            Console.WriteLine("  RX_EX_SUBS                  Subscribers to exclude");
        }

        private static string? GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process)
                ?? Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User)
                ?? Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);
        }
    }

    public class Options
    {
        [Option('i', "instance", HelpText = "Instance name")]
        public string? Instance { get; set; }

        [Option('s', "subs", HelpText = "Subscribers to run")]
        public string? Subscribers { get; set; }

        [Option('d', "debug", HelpText = "Enable debug mode")]
        public bool Debug { get; set; }

        [Option('?', "help", HelpText = "Print help")]
        public bool PrintHelp { get; set; }
    }
}
