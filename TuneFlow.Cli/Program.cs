using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Extensions.DependencyInjection;
using TuneFlow.Workflow;

namespace TuneFlow.Cli;

internal class Program
{
    private static int Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddWorkflow();

        using var registrar = new DependencyInjectionRegistrar(services);
        var app = new CommandApp(registrar);

        app.Configure(config =>
        {
            config.AddCommand<InfoCommand>("info")
                .WithDescription("获取ncm文件的信息");
            config.AddCommand<WatchCommand>("watch")
                .WithDescription("监视目录并自动处理");
            config.SetApplicationName("TuneFlow");
        });

        return app.Run(args);
    }
}