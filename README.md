# SkyLady - A Synthesis Patcher for Skyrim SE, AE, and VR

## Description is WIP - not Final

## Overview
**SkyLady** is a Synthesis patcher for Skyrim Special Edition, Anniversary Edition, and VR that transforms male NPCs into females, using appearance templates from your modlist. Utilizing the power of the Synthesis framework, SkyLady is lightweight, script-free, and can process a large load order in minutes, making it an efficient tool for feminizing your Skyrim world.

## Description
SkyLady is a Synthesis patcher that transforms male NPCs across your load order into females, using appearance templates from existing female NPCs. It adjusts gender flags, assigns equivalent female voices (where possible), and forwards changes from other mods, ensuring seamless integration. Configuration files are automatically generated on first run for easy setup, or can be downloaded manually to adjust first. While designed to patch entire load orders, SkyLady offers flexible settings like single NPC patching, mod and NPC blacklists, and template preservation for consistent results across runs.

Inspired by SkyFem, an xEdit-based patcher, SkyLady overcomes the 254-master limit with ESP splitting. It processes large load orders (tested with 4000+ plugins) in minutes, making it an efficient tool to feminize your Skyrim world.

## Features
- Transforms male NPCs into females, setting gender flags, applying female voices (where compatible), and using your default body (e.g., 3BA, BHUNP, UBE) with physics like CBPC. Compatible with OBody NG.
- Single NPC patching lets you manually select NPCs for new appearances, ideal when you don't like certain assigned appearance, or for debugging.
- Blacklist mods (e.g., Skyrim.esm) to exclude from template collection, prioritizing custom appearances.
- Blacklist mods or specific NPCs to prevent patching, leaving males unchanged.
- Add mods to "Target Mods" list to patch only selected mods.
- Preserve previous appearances when adding new mods, patching only new NPCs.
- Lock specific NPCs to retain their templates across runs for consistent looks.
- Custom races handled via `SkyLady races.txt` and template sharing through `SkyLady Race Compatibility.txt`.
- Handles massive load orders (4000+ plugins) via ESP splitting, bypassing the 254-master limit.
- Automatically creates configuration files (`races.txt`, `partsToCopy.txt`, etc.) in your mod folder on first run.
- Uses `SkyLadyMarker.txt` to locate your mod folder for file generation and facegen copying.
## Advanced Features
- Optional `SkyLadyKeywords.esp` enables multi-ESP mode to track patched NPCs across suffixed plugins (e.g., `SkyLadyPatcher_Main.esp`).
- Single-ESP mode respects prior multi-ESP patches if `SkyLadyKeywords.esp` is present, skipping patched NPCs.
- Option to ESL flag split ESP plugins to save load order slots if under 2048 records.

## Installation
(This guide assumes you have Synthesis installed. If not, look up one of many guides on internet. You can try this one (for MO2): https://www.youtube.com/watch?v=s7luh0hMMAU)
1. Download and install SkyLady (nexus link) via your mod manager (e.g., MO2, Vortex), including `SkyLadyMarker.txt` in the mod folder (e.g., `NPCs_SkyLady`).
2. In Synthesis, add SkyLady from GitHub (`https://github.com/generaleagleyt/SkyLady`) or use the .synth file (coming soon Nexus download page).
3. (Optional, for advanced users only) For multi-ESP mode, download `SkyLadyKeywords.esp` from Optional Files and place it in your SkyLady mod folder. Load order position doesn’t matter.
4. Configure settings in Synthesis (see Settings section below).
5. Run the patcher via Synthesis.
6. If ESP splits occur (254-master limit), an intentional error indicates success. Close Synthesis. Otherwise, it completes normally.
7. For MO2, accept load order changes. `SkyLadyPatcher.esp` (or multiple if split) appears at the load order bottom.
8. SkyLady generates necessary files inside the mod folder that contains `SkyLadyMarker.txt`, except the ESP. Move output ESP plugin to this folder. Check for `meshes`, `textures`, and `.json` to confirm.
9. Place SkyLady plugins below NPC-altering mods in your load order for correct appearances (verify with xEdit).
10. Back up `.txt` files before editing to preserve custom settings.

## Settings
- **Patch Single NPC Only**: Patch specific NPCs or all eligible males.
- **NPCs to Patch**: Choose NPCs for single NPC patching.
- **Preserve Last Run Appearances**: Keep previous templates in bulk mode.
- **Use Default Race Fallback**: Use Nord/Imperial templates for custom races.
- **NPCs with Locked Templates**: Lock NPCs’ templates for consistency.
- **Template Mod Blacklist**: Exclude mods from template collection.
- **Target Mods to Patch**: Limit patching to specific mods.
- **Mods to Exclude from Patching**: Skip mods from patching.
- **NPCs to Exclude from Patching**: Skip specific NPCs.
- **Flag Output Plugins as ESL**: Save load order slots for small plugins.
- **Append Suffix to Output Filenames**: Enable multi-ESP mode (needs `SkyLadyKeywords.esp`).
- **Output Filename Suffix**: Set custom suffix or timestamp.

## Understanding Templates
- **What is a Template?**: A female NPC from your load order used as a source for appearance data (e.g., head parts, tint layers, facegen files, hair color, height, weight).
- **How Are Templates Selected?**:
  - Templates are female NPCs from humanoid races (defined in `SkyLady races.txt`).
  - They must not be from blacklisted mods and require loose facegen files (except for vanilla plugins like Skyrim.esm, Dawnguard.esm, etc.).
  - Templates are grouped by race, with selection based on `SkyLady Race Compatibility.txt` or a default map.
- **Default Configuration**: On first run, SkyLady creates `races.txt` and `Race Compatibility.txt` with defaults, editable for custom setups.
- **Adding New Templates**: Install mods with female NPCs (e.g., follower mods) to add templates, ensuring they have loose facegen files (unless from vanilla plugins) and aren’t blacklisted.
- **Custom Races**:
  - Add the race’s EditorID to `SkyLady races.txt` to recognize it.
  - Ensure female NPCs of the race exist with loose facegen files (or are from vanilla plugins), or configure `SkyLady Race Compatibility.txt` to use templates from related races.

## Managing Mods with SkyLady
- **Adding/Updating Mods**: If new mods introduce male NPCs, rerun SkyLady with "Preserve Last Run Appearances" ticked to keep existing appearances. Blacklist unchanged mods if needed. Ensure `SkyLadyMarker.txt` stays in your mod folder to guide file generation.
- **Removing Patched Mods**: Disable or delete the mod’s plugin, then rerun SkyLady to update the patch, excluding the removed mods from Target Mods if listed.
- **Removing SkyLady**: Disable all `SkyLadyPatcher.esp` files and delete the SkyLady mod folder. Avoid uninstalling mid-save, though it’s generally safe due to Synthesis’s design.

## Troubleshooting
- **NPCs Not Being Patched**:
  - Check the log for "Unpatched NPCs" to identify reasons (e.g., no templates).
  - Ensure female templates exist for the NPC’s race.
  - Verify the NPC’s mod/NPC isn’t excluded.
  - Confirm custom races are in `SkyLady races.txt`.
- **Templates Not Being Used**:
  - Check the log for "Skipped Templates" (e.g., BSA usage, blacklisted mods).
  - Adjust the Template Mod Blacklist as needed.
- **CTD or Incorrect Appearance**:
  - Identify the NPC via crash logs (e.g., NetScriptFramework).
  - Open `SkyLadyTempTemplates.json` to find the template’s source mod, blacklist it, and rerun in Single NPC mode.
- **ESP Splitting**:
  - Split ESPs (e.g., `SkyLadyPatcher_Main.esp`, `SkyLadyPatcher_Main_1.esp`) handle large load orders, limited to 254 masters each.
- **Files in Overwrite**: If `.txt` files or `SkyLadyTempTemplates.json` appear in MO2’s Overwrite, move them to your SkyLady mod folder with `SkyLadyMarker.txt`.

## Compatibility
SkyLady supports Skyrim Special Edition, Anniversary Edition, and VR (test VR for full compatibility) as a Synthesis patcher, ensuring broad mod compatibility.

Appearance templates from vanilla Skyrim plugins (Skyrim.esm, Dawnguard.esm, etc.) are supported without loose facegen files. Modded templates require loose facegen files. For modded setups, blacklist vanilla mods to prioritize custom templates.

Custom races aren’t patched by default unless added to `SkyLady races.txt` in your SkyLady mod folder. Add the race’s EditorID (found in xEdit under the RACE record), and ensure female NPCs of that race exist with loose facegen files (or are from vanilla plugins). If only male NPCs exist or the mod uses BSAs, configure `SkyLady Race Compatibility.txt` to allow templates from related races.

Voice mappings for patched NPCs can be customized via `SkyLady Voice Compatibility.txt` in the SkyLady mod folder. Edit the `[VoiceMap]` section to set multiple female voice options for each male voice (e.g., `MaleNord: FemaleNord, FemaleSultry`), randomly selected by the patcher, or the `[RaceVoiceFallbacks]` section for race-specific fallback voices (e.g., `NordRace: FemaleNord, FemaleEvenToned`). Use xEdit to find voice EditorIDs.

## Requirements
- Skyrim Special Edition, Anniversary Edition, or VR
- Synthesis (latest version recommended)
- `SkyLadyKeywords.esp` (optional, for multi-ESP mode only)

## Credits
- **SkyFem team**: For inspiring SkyLady with the original xEdit-based mod.
- **Mutagen/Synthesis Team**: For the framework enabling SkyLady’s functionality.
- **Bethesda**: For creating Skyrim and its modding ecosystem.

## Version History
- **v1.0.0** (April 2025): Initial release with auto-generated configuration files and support for SE, AE, and VR.

## Support
Report issues on GitHub (`https://github.com/generaleagleyt/SkyLady`) or Nexus (once uploaded).

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
