namespace IPMan.Infrastructure.Common;

internal static class AtomicJsonFileWriter
{
    public static async Task WriteAsync(
        string destinationPath,
        bool overwrite,
        Func<Stream, CancellationToken, Task> serializeAsync,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        ArgumentNullException.ThrowIfNull(serializeAsync);
        cancellationToken.ThrowIfCancellationRequested();

        string fullDestinationPath = Path.GetFullPath(destinationPath);
        string? directory = Path.GetDirectoryName(fullDestinationPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new IOException("The destination directory could not be resolved.");
        }

        string fileName = Path.GetFileNameWithoutExtension(fullDestinationPath);
        string temporaryPath = Path.Combine(
            directory,
            $".{fileName}-{Guid.NewGuid():N}.tmp");

        try
        {
            Directory.CreateDirectory(directory);

            await using (FileStream stream = new(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await serializeAsync(stream, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporaryPath, fullDestinationPath, overwrite);
        }
        finally
        {
            TryDeleteTemporaryFile(temporaryPath);
        }
    }

    private static void TryDeleteTemporaryFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // The final file is never partial; an orphaned temp can be ignored.
        }
        catch (UnauthorizedAccessException)
        {
            // Preserve the original persistence result.
        }
    }
}
