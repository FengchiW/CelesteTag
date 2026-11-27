using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace CelesteTag;

public sealed class TagSettings : EverestModuleSettings
{
    [SettingMaxLength(96)]
    public string HostAddress { get; set; } = "127.0.0.1";
}

public sealed class TagModule : EverestModule
{
    private static TagModule? instance;
    private TagConnection? connection;
    private TagOverlay? overlay;

    public override Type SettingsType => typeof(TagSettings);

    public override void CreateModMenuSection(TextMenu menu, bool inGame, FMOD.Studio.EventInstance snapshot)
    {
        base.CreateModMenuSection(menu, inGame, snapshot);
        var settings = (TagSettings) _Settings;
        menu.Add(new TextMenu.Button("Join tag game").Pressed(() => Join(settings.HostAddress)));
        menu.Add(new TextMenu.Button("Leave tag game").Pressed(Leave));
    }

    public override void Load()
    {
        instance = this;
        On.Monocle.Engine.Update += OnUpdate;
    }

    public override void Unload()
    {
        On.Monocle.Engine.Update -= OnUpdate;
        Disconnect();
        instance = null;
    }

    [Command("tag_join", "Connect to a Celeste Tag host. Example: tag_join 127.0.0.1 27864")]
    public static void Join(string host, int port = TagWire.DefaultPort)
    {
        if (string.IsNullOrWhiteSpace(host) || port is < 1 or > 65535)
        {
            Engine.Commands.Log("Usage: tag_join <host> [port]");
            return;
        }

        if (instance is not { } module)
            return;

        module.Disconnect();
        module.connection = new TagConnection(host, port);
        Engine.Commands.Log($"Connecting to {host}:{port}. Enter a chapter to play.");
    }

    [Command("tag_leave", "Leave your Celeste Tag game")]
    public static void Leave()
    {
        instance?.Disconnect();
        Engine.Commands.Log("Left Celeste Tag.");
    }

    private void Disconnect()
    {
        connection?.Dispose();
        connection = null;
        overlay?.RemoveSelf();
        overlay = null;
    }

    private void OnUpdate(On.Monocle.Engine.orig_Update orig, Engine engine, GameTime gameTime)
    {
        orig(engine, gameTime);
        if (connection is not { } current)
            return;

        if (Engine.Scene is not Level level)
        {
            current.Publish(Presence.Away);
            overlay?.RemoveSelf();
            overlay = null;
            return;
        }

        if (overlay?.Scene != level)
        {
            overlay?.RemoveSelf();
            overlay = new TagOverlay(current);
            level.Add(overlay);
        }

        current.Publish(GetPresence(level));
    }

    private static Presence GetPresence(Level level)
    {
        Player? player = level.Tracker.GetEntity<Player>();
        if (level.Paused || level.Transitioning || player is not { Dead: false, InControl: true, Collider: not null })
            return Presence.Away;

        return new Presence(level.Session.Area.SID, (int) level.Session.Area.Mode, level.Session.Level,
            player.X, player.Y, player.Left, player.Top, player.Right, player.Bottom, (int) player.Facing, true);
    }

    private sealed class TagOverlay(TagConnection source) : Entity
    {
        public override void Added(Scene scene)
        {
            base.Added(scene);
            Depth = -1_000_000;
        }

        public override void Render()
        {
            var level = (Level) Scene;
            if (level.Transitioning)
                return;

            TagSnapshot? state = source.Snapshot;
            var hud = level.Camera.Position + new Vector2(8, 8);
            if (state is null)
            {
                DrawLabel(source.Status, hud, Vector2.Zero, Color.White);
                return;
            }

            string status = state.It switch
            {
                null => "TAG  /  Waiting for players",
                int it when it == state.YourId => "TAG  /  YOU'RE IT",
                int it => $"TAG  /  Player {it} is it",
            };
            if (state.It == state.YourId && state.It is not null && state.Cooldown > 0)
                status += "  /  Cooldown";

            Color tagged = new(255, 200, 89);
            DrawLabel(status, hud, Vector2.Zero, state.It == state.YourId ? tagged : Color.White);

            foreach (TagPlayer remote in state.Players)
            {
                Presence position = remote.Position;
                if (remote.Id == state.YourId || position.Area != level.Session.Area.SID ||
                    position.Side != (int) level.Session.Area.Mode || position.Room != level.Session.Level)
                    continue;

                bool isIt = remote.Id == state.It;
                Color color = isIt ? tagged : new Color(143, 225, 255);
                var feet = new Vector2(position.X, position.Y);
                if (GFX.Game.Has("characters/player/idle00"))
                {
                    GFX.Game["characters/player/idle00"].DrawJustified(feet, new Vector2(0.5f, 1f),
                        color, 1f, 0f, position.Facing < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
                }
                else
                {
                    Draw.Rect(position.Left, position.Top,
                        position.Right - position.Left, position.Bottom - position.Top, color);
                }

                string label = isIt ? $"P{remote.Id} - IT" : $"P{remote.Id}";
                DrawLabel(label, feet - new Vector2(0, 16), new Vector2(0.5f, 1f), color);
            }
        }

        private static void DrawLabel(string label, Vector2 position, Vector2 justify, Color color) =>
            ActiveFont.DrawOutline(label, position, justify, Vector2.One * 0.19f, color, 1f, Color.Black);
    }
}
