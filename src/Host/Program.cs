using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using CelesteTag;

IPAddress address = IPAddress.Loopback;
int port = TagWire.DefaultPort;

if (args.Length > 2 ||
    (args.Length > 0 && !IPAddress.TryParse(args[0], out address!)) ||
    (args.Length > 1 && (!int.TryParse(args[1], out port) || port is < 1 or > 65535)))
{
    Console.Error.WriteLine("Usage: CelesteTag.Host [listen IP] [port]");
    return 2;
}

using var shutdown = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    shutdown.Cancel();
};

TcpListener? listener = null;
var round = new TagRound();
try
{
    listener = new TcpListener(address, port);
    listener.Start();
    Console.WriteLine($"Celeste Tag listening on {listener.LocalEndpoint}. Press Ctrl+C to stop.");
    while (!shutdown.IsCancellationRequested)
    {
        TcpClient client = await listener.AcceptTcpClientAsync(shutdown.Token);
        _ = HandlePlayer(client, round, shutdown.Token);
    }
}
catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
{
}
catch (SocketException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
finally
{
    listener?.Stop();
}
return 0;

static async Task HandlePlayer(TcpClient client, TagRound round, CancellationToken stopping)
{
    using (client)
    {
        if (round.Join() is not int id)
            return;

        try
        {
            client.NoDelay = true;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stopping);
            NetworkStream stream = client.GetStream();
            while (true)
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(3));
                TagUpdate update = await TagWire.Read<TagUpdate>(stream, timeout.Token);
                if (update.Protocol != TagWire.Protocol || update.Position is null)
                    return;

                TagSnapshot snapshot = round.Update(id, update.Position);
                await TagWire.Write(stream, snapshot, timeout.Token);
                await Task.Delay(35, timeout.Token);
            }
        }
        catch (Exception ex) when (ex is IOException or SocketException or
            OperationCanceledException or JsonException or InvalidDataException)
        {
            // A disconnected or malformed client leaves the round.
        }
        finally
        {
            round.Leave(id);
        }
    }
}
