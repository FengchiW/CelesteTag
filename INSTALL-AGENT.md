# Install Celeste Tag as a coding agent

## Prompt to give your agent

> Install Celeste Tag 0.1.0 in my Steam copy of Celeste. Follow this repository’s INSTALL-AGENT.md and verify that the game loads both Everest and Celeste Tag. I’ll join a friend’s host.

## Installation procedure

1. **Identify the game on this machine.** Use Steam → Celeste → Manage → Browse local files, or read Steam's `steamapps/libraryfolders.vdf` and locate `appmanifest_504230.acf` in the matching library. Confirm the game directory and use its actual operating system. If there are multiple plausible installs and Steam cannot identify the active one, ask which to use. If Celeste is running, have the player close it before changing files.
2. **Check Everest.** If Everest 1.6696.0 or newer already loads on that install, use it. Otherwise install or update through Olympus, or use the documented manual installer for the local OS from the [official Everest installation guide](https://everestapi.github.io/#installing-everest). Use the existing Steam installation and preserve saves, settings, and unrelated mods. Ask for any filesystem approval your environment actually requires before writing outside the workspace.
3. **Install the packaged mod.** Get `CelesteTag-0.1.0.zip` and `SHA256SUMS` from [the v0.1.0 downloads](releases/v0.1.0/). Compute the mod archive's SHA-256 and compare it to the matching entry. Make sure the ZIP contains `everest.yaml` and `CelesteTag.dll` at its root. Place the ZIP intact in the Everest `Mods` directory for that game. If an identical copy is already installed, reuse it. If replacing an older copy, move only the old Celeste Tag package outside `Mods` so it can be restored if needed. Leave the host archive for the person hosting.
4. **Verify by launching.** Start the game using its normal Steam or Olympus launch path. Check **Mod Options → Celeste Tag**, or confirm in the new game log that both Everest and `CelesteTag.TagModule` registered and startup completed. If verification fails, inspect the newest log for the relevant loader error and report anything you could not resolve.
5. **Hand off the game.** Report the install location, versions, and what verified. Ask the player for their friend's host address only if they want you to set it. To play, open **Mod Options → Celeste Tag**, enter the address in **Host Address**, choose **Join tag game**, and enter the same chapter side and room as the other player. The host listens on TCP port `27864` by default. See the [multiplayer setup](README.md#play-a-round) if this machine is hosting.
