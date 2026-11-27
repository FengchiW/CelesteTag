using System.Diagnostics;

namespace CelesteTag;

internal sealed class TagRound
{
    private sealed class Participant
    {
        public Presence Position = Presence.Away;
        public long LastActiveAt;
    }

    private const int Capacity = 16;
    private static readonly long FreshFor = Stopwatch.Frequency / 2;
    private static readonly long TagDelay = Stopwatch.Frequency * 5 / 4;
    private static readonly long ChaserGrace = Stopwatch.Frequency * 5;
    private readonly Dictionary<int, Participant> players = [];
    private int nextId;
    private int? it;
    private int? previousIt;
    private long tagAfter;

    public int? Join()
    {
        lock (players)
        {
            if (players.Count >= Capacity)
                return null;

            int id = ++nextId;
            players.Add(id, new Participant());
            return id;
        }
    }

    public void Leave(int id)
    {
        lock (players)
        {
            players.Remove(id);
            if (it == id)
            {
                it = null;
                previousIt = null;
            }
            else if (previousIt == id)
            {
                previousIt = null;
            }
        }
    }

    public TagSnapshot Update(int id, Presence position)
    {
        lock (players)
        {
            long now = Stopwatch.GetTimestamp();
            Participant sender = players[id];
            sender.Position = position.Active && position.IsValid() ? position : Presence.Away;
            if (sender.Position.Active)
                sender.LastActiveAt = now;

            TagPlayer[] active = players
                .Where(pair => pair.Value.Position.Active && now - pair.Value.LastActiveAt <= FreshFor)
                .Select(pair => new TagPlayer(pair.Key, pair.Value.Position))
                .OrderBy(player => player.Id)
                .ToArray();

            if (it is int current && now - players[current].LastActiveAt > ChaserGrace)
            {
                it = null;
                previousIt = null;
            }

            if (it is null && active.Length >= 2)
            {
                it = active[0].Id;
                tagAfter = now + TagDelay;
            }

            TagPlayer? chaser = active.FirstOrDefault(player => player.Id == it);
            if (chaser is not null)
            {
                TagPlayer? previous = active.FirstOrDefault(player => player.Id == previousIt);
                if (previous is not null && !chaser.Position.Touches(previous.Position))
                    previousIt = null;

                if (now >= tagAfter)
                {
                    TagPlayer? target = active.FirstOrDefault(player => player.Id != it &&
                        player.Id != previousIt && chaser.Position.Touches(player.Position));
                    if (target is not null)
                    {
                        previousIt = it;
                        it = target.Id;
                        tagAfter = now + TagDelay;
                    }
                }
            }

            return new TagSnapshot(id, it, Math.Max(0, tagAfter - now) / (float) Stopwatch.Frequency, active);
        }
    }
}
