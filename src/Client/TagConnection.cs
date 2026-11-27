using System.Net.Sockets;
using System.Text.Json;

namespace CelesteTag;

internal sealed class TagConnection : IDisposable
{
    private readonly CancellationTokenSource stopping = new();
    private readonly Task worker;
    private volatile Presence local = Presence.Away;
    private volatile TagSnapshot? snapshot;
    private volatile string status = "Connecting to tag host...";

    public TagSnapshot? Snapshot => snapshot;
    public string Status => status;

    public TagConnection(string host, int port)
    {
        worker = Run(host, port, stopping.Token);
    }

    public void Publish(Presence position) => local = position;

    public void Dispose()
    {
        stopping.Cancel();
        _ = DisposeAfterExit();
    }

    private async Task DisposeAfterExit()
    {
        try
        {
            await worker.ConfigureAwait(false);
        }
        finally
        {
            stopping.Dispose();
        }
    }

    private async Task Run(string host, int port, CancellationToken cancellation)
    {
        try
        {
            using var client = new TcpClient { NoDelay = true };
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            timeout.CancelAfter(TimeSpan.FromSeconds(4));
            await client.ConnectAsync(host, port, timeout.Token).ConfigureAwait(false);

            NetworkStream stream = client.GetStream();
            while (!cancellation.IsCancellationRequested)
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(4));
                await TagWire.Write(stream, new TagUpdate(TagWire.Protocol, local), timeout.Token)
                    .ConfigureAwait(false);
                snapshot = await TagWire.Read<TagSnapshot>(stream, timeout.Token).ConfigureAwait(false);
                await Task.Delay(50, cancellation).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is IOException or SocketException or
            OperationCanceledException or JsonException or InvalidDataException or ArgumentException)
        {
            status = ex is OperationCanceledException
                ? "Tag connection timed out."
                : "Tag connection closed or host unavailable.";
        }
        finally
        {
            snapshot = null;
        }
    }
}
