# Celeste Tag

Play touch tag inside a Celeste chapter with up to 16 players on a private host. Other players appear as tinted ghosts. The HUD shows who is it, and the chaser's ghost is gold.

## Play a round

[Download the mod and host ZIPs](releases/v0.1.0/) or [give these installation instructions to a coding agent](INSTALL-AGENT.md).

1. On every player's computer, install [Everest](https://everestapi.github.io/#installing-everest) 1.6696.0 or later. Drop `CelesteTag-0.1.0.zip` into Celeste's `Mods` folder and start the Everest version of the game.
2. On the host computer, install the [.NET 8 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) if needed. Extract `CelesteTag-Host-0.1.0.zip`, open a terminal in the extracted folder, and run the following, replacing `192.168.1.20` with the host computer's LAN IP address.

   ```sh
   dotnet CelesteTag.Host.dll 192.168.1.20
   ```

   The listener uses TCP port `27864`. Allow connections to that port on the host's private network if its firewall asks. For a local test on one computer, run the host without an address to listen on `127.0.0.1`.

3. In Celeste, open **Mod Options → Celeste Tag**. Enter the same address in **Host Address**, then select **Join tag game**. Alternatively, enable Everest's debug console and enter `tag_join 192.168.1.20 27864`.
4. Enter the same chapter side and room. Choose a room with enough space to chase. Two active players are enough to start, and the first connected active player starts as it.

Overlap another player's hitbox to pass the tag. After a 1.25-second cooldown, the new chaser can tag someone. They must separate from the previous chaser before tagging them back. Paused, dead, or transitioning players cannot tag or be tagged. If the chaser disconnects or stays inactive for five seconds, the host can assign a new chaser.

Select **Leave tag game** or enter `tag_leave` to disconnect. The host can stop the session with Ctrl+C. The mod shares player locations and tag status. Level progress and objects such as moving platforms still run independently on each player's copy.

## Build

With the .NET 8 SDK, Bash and `zip` available, set `CELESTE_DIR` to a patched Everest installation and run:

```sh
CELESTE_DIR="/path/to/Celeste" ./build.sh
```

The two installable ZIPs appear in `dist/`. The `TAG_DOTNET` environment variable can point to a specific `dotnet` executable.
