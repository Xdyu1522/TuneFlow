using NcmFox.Models;
using TuneFlow.Workflow.Options;

namespace TuneFlow.Workflow.Abstractions;

public interface ICoverProvider: IResourcesProvider<byte[]>
{
    public CoverSourceStrategy Strategy { get; }
}