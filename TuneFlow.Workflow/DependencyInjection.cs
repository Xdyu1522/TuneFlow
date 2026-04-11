using Microsoft.Extensions.DependencyInjection;
using TuneFlow.Workflow.Abstractions;
using TuneFlow.Workflow.Blocks;
using TuneFlow.Workflow.Providers;

namespace TuneFlow.Workflow;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkflow(this IServiceCollection services)
    {
        services.AddTransient<WorkflowRunner>();

        services.AddTransient<GetLyricsBlock>();
        services.AddTransient<GetCoverBlock>();

        services.AddTransient<ICoverProvider, CoverFromFileProvider>();
        services.AddHttpClient<ICoverProvider, CoverFromNetProvider>();
        services.AddHttpClient<ILyricsProvider, LyricsFromNetProvider>();

        return services;
    }
}
