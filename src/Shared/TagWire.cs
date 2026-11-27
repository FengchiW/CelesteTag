using System.Buffers.Binary;
using System.Text.Json;

namespace CelesteTag;

internal sealed record Presence(string Area, int Side, string Room, float X, float Y,
    float Left, float Top, float Right, float Bottom, int Facing, bool Active)
{
    public static readonly Presence Away = new("", 0, "", 0, 0, 0, 0, 0, 0, 1, false);

    public bool SameRoom(Presence other) =>
        Area == other.Area && Side == other.Side && Room == other.Room;

    public bool Touches(Presence other) =>
        SameRoom(other) && Left < other.Right && Right > other.Left &&
        Top < other.Bottom && Bottom > other.Top;

    public bool IsValid() =>
        Area is { Length: > 0 and <= 256 } && Room is { Length: > 0 and <= 128 } &&
        Side is >= 0 and <= 2 && Facing is -1 or 1 &&
        X is >= -100000 and <= 100000 && Y is >= -100000 and <= 100000 &&
        Right - Left is > 0 and <= 16 && Bottom - Top is > 0 and <= 20 &&
        MathF.Abs((Left + Right) / 2 - X) <= 16 &&
        MathF.Abs(Bottom - Y) <= 16;
}

internal sealed record TagUpdate(int Protocol, Presence Position);
internal sealed record TagPlayer(int Id, Presence Position);
internal sealed record TagSnapshot(int YourId, int? It, float Cooldown, TagPlayer[] Players);

internal static class TagWire
{
    public const int Protocol = 1;
    public const int DefaultPort = 27864;
    private const int MaxPayload = 64 * 1024;

    public static async ValueTask Write<T>(Stream stream, T message, CancellationToken cancellation)
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(message);
        if (payload.Length > MaxPayload)
            throw new InvalidDataException("Tag message is too large.");

        var frame = new byte[sizeof(int) + payload.Length];
        BinaryPrimitives.WriteInt32BigEndian(frame, payload.Length);
        payload.CopyTo(frame, sizeof(int));
        await stream.WriteAsync(frame, cancellation);
    }

    public static async ValueTask<T> Read<T>(Stream stream, CancellationToken cancellation)
    {
        var header = new byte[sizeof(int)];
        await stream.ReadExactlyAsync(header, cancellation);
        int length = BinaryPrimitives.ReadInt32BigEndian(header);
        if (length is <= 0 or > MaxPayload)
            throw new InvalidDataException("Invalid tag message length.");

        var payload = new byte[length];
        await stream.ReadExactlyAsync(payload, cancellation);
        return JsonSerializer.Deserialize<T>(payload)
            ?? throw new InvalidDataException("Invalid tag message.");
    }
}
