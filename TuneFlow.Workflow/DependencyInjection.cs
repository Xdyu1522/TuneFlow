using Microsoft.Extensions.DependencyInjection;
using TuneFlow.Workflow.Abstractions;
using TuneFlow.Workflow.Blocks;
using TuneFlow.Workflow.Providers;

namespace TuneFlow.Workflow;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkflow(this IServiceCollection services)
    {
        // 注册 Workflow 内部的服务
        services.AddTransient<StartBlock>();
        services.AddTransient<PrepareContextBlock>();
        services.AddTransient<DecryptBlock>();
        services.AddTransient<GetLyricsBlock>();
        services.AddTransient<GetCoverBlock>();
        services.AddTransient<EmbedBlock>();
        services.AddTransient<SaveToFileBlock>();
        services.AddTransient<WorkflowFactory>();
        services.AddTransient<WorkflowRunner>();

        services.AddTransient<ICoverProvider, CoverFromFileProvider>();
        
        services.AddHttpClient<ICoverProvider, CoverFromNetProvider>();
        services.AddHttpClient<ILyricsProvider, LyricsFromNetProvider>();
        
        return services;
    }
}
