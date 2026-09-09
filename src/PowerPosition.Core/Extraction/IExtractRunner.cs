namespace PowerPosition.Core.Extraction;

public interface IExtractRunner
{
    Task<ExtractResult> RunAsync(CancellationToken cancellationToken);
}
