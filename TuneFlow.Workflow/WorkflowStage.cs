namespace TuneFlow.Workflow;

public enum WorkflowStage
{
    Started, 
    Decrypted,
    GotLyrics,
    GotCover,
    SavedToFile,
    EmbeddedInfo,
    Finished,
}