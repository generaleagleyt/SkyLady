# SkyLady - A Synthesis Patcher for Skyrim SE, AE, and VR

## Description is WIP - not Final

## Overview
**SkyLady** is a Synthesis patcher for Skyrim SE, AE and VR, that transforms male NPCs into females, using appearance templates from your load order. Utilizing the power of the Synthesis framework, SkyLady is lightweight, script-free, and can process a large load order in minutes, making it an efficient tool for feminizing your Skyrim world.

## Description
SkyLady is a Synthesis patcher that transforms male NPCs across your load order into females, using appearance templates from existing female NPCs. It adjusts gender flags, assigns equivalent female voices, and forwards changes from other mods, ensuring seamless integration. While designed to patch entire load orders, SkyLady offers flexible settings like single NPC patching, mod and NPC blacklists, and template preservation for consistent results across runs.

Inspired by SkyFem, an xEdit-based patcher, SkyLady overcomes the 254-master limit with ESP splitting. It processes large load orders (tested with 4000+ plugins) in minutes, making it an efficient tool to feminize your Skyrim world.

ESP splitting is a WIP feature of Synthesis. It will eventually be integrated into Synthesis itself. Currently it works but the patching process will end with an exception. This is intended behaviour and it means the patcher was successful.

## Features
- Transforms male NPCs into females, setting gender flags, applying female voices (configurable) and forwarding other mods' changes. NPCs will use your default body (e.g., 3BA, BHUNP, UBE). Compatible with OBody NG.
- Single NPC patching lets you manually select NPCs for new appearances. Ideal when you don't like how a certain NPC looks after patching, or for debugging.
- Blacklist mods (e.g., Skyrim.esm) to exclude from template collection, prioritizing custom appearances.
- Blacklist mods or specific NPCs to prevent patching, leaving males unchanged.
- Add mods to "Target Mods" list to patch only selected mods.
- Preserve previous appearances when adding new mods, patching only new NPCs.
- Lock specific NPCs to retain their templates across runs for consistent looks.
- Custom races handled via `SkyLady races.txt` and template sharing through `SkyLady Race Compatibility.txt`.
- Handles massive load orders (4000+ plugins) via ESP splitting, bypassing the 254-master limit.
- Uses `SkyLadyMarker.txt` to locate your SkyLady mod folder for file generation and facegen copying.

## Advanced Features
- Optional `SkyLadyKeywords.esp` enables multi-ESP mode to track patched NPCs across suffixed plugins (e.g., `SkyLadyPatcher_Main.esp`).
- Single-ESP mode respects prior multi-ESP patches if `SkyLadyKeywords.esp` is present, skipping patched NPCs.
- Option to ESL flag split ESP plugins to save load order slots (as right now flagging through Synthesis Profile settings doesn't work on split ESP files.)

## Installation
(This guide is for Mod Organizer 2 users assumes you have Synthesis already installed. Otherwise you can follow this (or any other guide) on Youtube (for MO2): [Synthesis Guide on YT](https://www.youtube.com/watch?v=s7luh0hMMAU). I have never used other mod managers, so I might not be able to help you if you encounter issues during the installation.)

1. Create an empty mod folder inside your mods folder. Name it SkyLady or similar. (Optionally, if you later don't want to manually move files from Overwrite into this folder, you can create an empty TXT file called `SkyLadyMarker.txt`, and the patcher will detect it and create all files inside that folder. The final ESP file will still have to be moved manually.)
2. Download SkyLady.synth file from Nexus (main file, link) and extract it into a temporary folder.
2.5. (Optional, for advanced users only) For multi-ESP mode, download `SkyLadyKeywords.esp` from Nexus from Optional Files and place it in your SkyLady mod folder. Make sure it's active, load order position doesn’t matter. For normal users, YOU DON'T NEED THIS UNLESS YOU KNOW WHAT YOU ARE DOING.)
3. Make sure the Synthesis is added as an executable inside MO2 (if not it's part of the Youtube guide above). 
4. Launch Synthesis through MO2. Once it loads, create a new group called SkyLady Patcher. Locate the `SkyLady.synth` file you extracted earlier and double click on it. It should now open inside Synthesis. Alternatively, you can click on your new group and select "Git Repository" in top left corner, find `SkyLady` and add it to the group this way.
5. Configure settings to your liking (see **Settings** section below for detailed explanations on what each setting does).
5. Run the patcher via Synthesis.
6. Once the patcher is done, one of two outcomes will happen:
6.1. The patcher completes normally. Check the ammount of patched NPCs (e.g. 1000 out of 1000). If the numbers are not the same, I recommend you right click in the Synthesis console, select all, copy and save it inside a TXT file. Check the **Troubleshooting** section below for more details. Close Synthesis.
6.2. If ESP splits occur (254-master limit), an intentional error indicates success. Check the ammount of patched NPCs (e.g. 1000 out of 1000). If the numbers are not the same, I recommend you right click in the Synthesis console, select all, copy and save it inside a TXT file. Check the **Troubleshooting** section below for more details. Close Synthesis.
7. MO2 might inform you that load order has been changed and if you want to keep changes. You can pick yes, but it doesn't matter which option you choose. `SkyLadyPatcher.esp` (or multiple if split) should now appear at the bottom of your load order (right side).
8.1. If you have Synthesis Output folder set, all generated files should end up inside it. If you don't, they should be inside your Overwrite folder. Open the folder and you should see `SkyLady` folder and one or more `SkyLady Patcher.esp` files. Move the ESP file(s) inside the `SkyLady` folder, then open that folder, select all and move them to the SkyLady mod folder you created in step 1. (inside your `mods` folder). 
8.2. If you created `SkyLadyMarker.txt` during step 1, all files generated by SkyLady should already be present inside the SkyLady mod folder. You only need to move the created ESP file(s) into it.
8.3. Your SkyLady mod folder should now contain:
- meshes
- textures
- SkyLady partsToCopy.txt
- SkyLady races.txt
- SkyLady Race Compatibility.txt
- SkyLady Voice Compatibility.txt
- SkyLadyTempTemplates.json
- SkyLady Patcher.esp (if split, also SkyLady Patcher_2.esp, etc.)
9. Refresh MO2. Activate the ESP file(s) on the right side of MO2. SkyLady plugins should be placed below other NPC-altering mods/patches in your load order for it's changes to apply.

## Settings
- **Patch Single NPC Only**:
Usage: This option allows you to select individual NPCs and assign them random female appearances, without touching other NPCs (whether patched or unpatched). This is useful when you don't like how certain NPCs ended up looking after you patched your entire load order. This setting allows you to select such NPCs and "roll the dice" for them again, assigning them another random look. 

If you already run the patcher prior to doing this, all other NPCs will keep their previously assigned appearance. Or if you wish to only "feminize" hand-selected NPCs, this is the option to do so.

Instructions: 
1. Tick the `Patch Single NPC Only` option, then click on `NPCs to Patch`. A new window will open. Click on the `+` sign on top right. A new entry will be created. Click inside the first box and search for the NPC you want to patch. You need to find the EditorID of the NPC. You can check EditorID of NPCs with xEdit. After you select all NPCs you want to patch, click on the `Top Level` to return back to the main window. You don't need to tick `Preserve Last Run Appearances`, as it's always active if `Patch Single NPC Only` is ticked.

- **NPCs to Patch**: Part of `Patch Single NPC Only`. See above.

- **Preserve Last Run Appearances**: Enabling this option will ensure that all previously patched NPCs keep their assigned templates. If you install a new mod and want to patch it's male NPCs, without changing current appearances, enable this option and run the patcher. New NPCs will get patched while the other remain the same. This option is always enabled when using `Patch Single NPC Only`. 

- **Use Default Race Fallback**: Use Nord and Imperial templates for custom races. SkyLady works by searching the load order for elligible NPCs based on their race, and assigning them random female templates of the same or compatible race. If an NPC has a race that is not included inside `SkyLady races.txt`, it won't be patched (will stay male). If the race is inside the TXT file, the NPC will be patched with a female template of the same race, or of a compatible race (as set inside `SkyLady Race Compatibility.txt`). IF a race IS inside `SkyLady races.txt`, but it doesn't have a female template nor a compatible race, with this setting checked a random template will be chosen from races NordRace and ImperialRace. Otherwise the NPC would not be patched (remain male).

- **NPCs with Locked Templates**: Lock NPCs’ templates for consistency. If you really like how some patched NPCs look, and would like to keep their appearance even if you rerun the patcher in the future, add them to this list. 


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
