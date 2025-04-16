# SkyLady - Ladies of Skyrim

## This README is WIP.

## Overview
**SkyLady** is a Synthesis patcher designed to transform all male NPCs in your load order into females, using appearance templates from your modlist. Utilizing the power of the Synthesis framework, SkyLady is lightweight, script-free, and can process a large load order in minutes, making it an efficient tool for feminizing your Skyrim world.

## Description
SkyLady is a Synthesis patcher that transforms male NPCs across your load order into females, using appearance templates from existing female NPCs. It adjusts gender flags, assigns equivalent female voices, and forwards changes from other mods, ensuring seamless integration. While designed to patch entire load orders, SkyLady offers flexible settings like single NPC patching, NPC and mod blacklists, and template preservation for consistent results across runs.

Inspired by SkyFem, an xEdit-based patcher, SkyLady overcomes the 254-master limit with ESP splitting. It processes large load orders (tested with 4000+ plugins) in minutes, making it an efficient tool to feminize your Skyrim world.

ESP splitting, a work-in-progress Synthesis feature, automatically handles the 254-master limit by dividing the output into multiple ESP files. When the limit is reached, SkyLady creates a new ESP (e.g., SkyLadyPatcher_2.esp) and continues patching seamlessly. Once the patcher is done, an intentional error with a success message will appear. Future Synthesis updates will make this process fully automatic.

## Features
- Transforms male NPCs into females, setting gender flags, applying female voices (configurable) and forwarding other mods' changes. NPCs will use your default body (e.g., 3BA, BHUNP, UBE) and physics. Compatible with OBody NG.
- Single NPC patching lets you manually select NPCs for new appearances. Ideal when you don't like how a certain NPCs ended up looking, or for debugging.
- Blacklist mods (e.g., Skyrim.esm) to exclude them from template collection (explained below), prioritizing custom appearances.
- Blacklist mods or specific NPCs to leave males unchanged.
- Add mods to "Target Mods" list to only patch the selected mods.
- Preserve previous appearances when adding new mods, patching only new NPCs.
- Lock specific NPCs to retain their templates across runs for consistent looks.
- Custom races are handled via `SkyLady races.txt` and template sharing through `SkyLady Race Compatibility.txt`.
- Handles massive load orders (4000+ plugins) via ESP splitting, bypassing the 254-master limit.
- Uses optional `SkyLadyMarker.txt` to locate your SkyLady mod folder for file generation and facegen copying.
- Option to automatically ESL flag split ESP plugins to save load order slots (as currently flagging through Synthesis Profile settings doesn't work on split ESPs.)

## Understanding Templates
- **What is a Template?**: The term "template" has many meanings in Skyrim modding, but for the purpose of SkyLady, we will use our own. By a "template" we understand a female NPC from your load order used as a source for appearance data (e.g., head parts, tint layers, facegen files, hair color, height, weight). In other words, it's a look of a female NPC.
- **How Are Templates Selected?**:
  - Valid templates are female NPCs from humanoid races (defined in `SkyLady races.txt`).
  - They must not be from blacklisted mods (Template Mods Blacklist), and require loose facegen files (except for vanilla plugins like Skyrim.esm, Dawnguard.esm, etc.).
  - Templates are grouped by race, with selection based on `SkyLady Race Compatibility.txt`.
- **Adding New Templates**: To expand the template pool, install mods with female NPCs (e.g., follower mods or female replacer mods), ensuring they have loose facegen files.
- **Custom Races**:
  - Add the race’s EditorID to `SkyLady races.txt` to recognize it.
  - Ensure female NPCs of the race exist with loose facegen files, or configure `SkyLady Race Compatibility.txt` to use templates from other races.

## Compatibility
SkyLady was tested on Skyrim SE, but should work with AE and VR.

Appearance templates from vanilla Skyrim plugins (Skyrim.esm, Dawnguard.esm, etc.) are supported without loose facegen files. Modded templates require loose facegen files. For modded setups, blacklist vanilla mods to prioritize custom templates.

Custom races aren’t patched by default unless added to `SkyLady races.txt` in your SkyLady mod folder. Add the race’s EditorID (found in xEdit under the RACE record), and ensure female NPCs of that race exist with loose facegen files. If only male NPCs exist or the mod uses BSAs, configure `SkyLady Race Compatibility.txt` to allow templates from other races.

Voice mappings for patched NPCs can be customized via `SkyLady Voice Compatibility.txt` in the SkyLady mod folder. Edit the `[VoiceMap]` section to set multiple female voice options for each male voice (e.g., `MaleNord: FemaleNord, FemaleSultry`), randomly selected by the patcher, or the `[RaceVoiceFallbacks]` section for race-specific fallback voices (e.g., `NordRace: FemaleNord, FemaleEvenToned`). Use xEdit to find voice EditorIDs.

## Installation
(This guide is for Mod Organizer 2 users and assumes you have Synthesis already installed. If not, you can follow this (or any other) guide on Youtube (for MO2): [Synthesis Guide on YT](https://www.youtube.com/watch?v=s7luh0hMMAU). 
I have never used other mod managers, so I might not be able to help you if you encounter issues during installation.)

1. Create an empty mod folder inside your MO2 mods folder. Name it SkyLady, then enable it. (Optionally, if you don't want to manually move files from Overwrite into this folder later, you can create an empty TXT file called `SkyLadyMarker.txt` inside the folder. The patcher will detect it and move all created files inside the folder. The final ESP file still needs to be moved manually.)

2. Download `SkyLady - Ladies of Skyrim` main file from Nexus (link) and extract the `SkyLady.synth` into a temporary folder. Optionally, download the `SkyLady - TXT settings` from Optional files and extract them into your SkyLady mod folder. They allow you to adjust settings like patched races and voice types immediately (otherwise, they are created during the first run).

3. Launch Synthesis through MO2. Once it loads, create a new group called SkyLady Patcher. Double click on the `SkyLady.synth` file you extracted earlier and add it to the group.

Alternatively, click on the SkyLady Patcher group, then choose `Git Repository` and search for `SkyLady`. If it doesn't show up, click on `Input` tab and enter the following link: `https://github.com/generaleagleyt/SkyLady`. Click on the `Project` dropdown and select the option shown. Click `Confirm` in bottom right.

4. Make sure the `Versioning` page looks like this: (image).

5. Click on `Settings` tab and configure the settings to your liking (see **Settings** section below for detailed explanations on what each setting does).

6. Run the patcher.

7. Once the patcher is done, one of two outcomes will occur:

   a) The patcher completes normally.
   
   b) If ESP splits occur (ESP hit 254-master limit), an intentional error will indicate success. 
   
8. Check the console for the amount of patched NPCs (e.g. 1000 out of 1000). If the numbers are not equal, I recommend you right click in the console, select all, copy and save it inside a TXT file. Check the **Troubleshooting** section below for more details. Close Synthesis.
   
9. MO2 might inform you that load order has been changed and ask if you want to keep changes. You can pick yes or no, it doesn't matter. `SkyLady Patcher.esp` file (or multiple if split) should now appear at the bottom of your load order. You might need to refresh MO2.

10. If you created `SkyLadyMarker.txt` during step 1 or downloaded the TXT settings from Optional files, then all files generated by SkyLady should already be present inside your SkyLady mod folder. You only need to move the created ESP file(s) into it.

   Otherwise, check your dedicated Synthesis Output folder, or Overwrite folder. All generated files should be here. Open the appropriate folder and inside you should see `SkyLady` folder and one or more `SkyLady Patcher.esp` files. Move the ESP file(s) and the **content** of the SkyLady folder inside your SkyLady mod folder you created during step 1. Delete the empty SkyLady folder inside Synthesis Output folder/Overwrite.

*Note: For future runs, all files should be generated directly inside your SkyLady mod folder.*

9. Refresh MO2, make sure both SkyLady mod folder and all SkyLady Patcher.esp plugins are active. They should be placed below other NPC-altering mods/patches in both the left side and right side of MO2, for it's changes to apply correctly.

10. You are good to go. If you encounter any issues in game, check the **Troubleshooting** section below.

After a successful run, your SkyLady mod folder should look like this:
- **meshes**
- **textures**
- SkyLadyMarker.txt
- SkyLady partsToCopy.txt
- SkyLady races.txt
- SkyLady Race Compatibility.txt
- SkyLady Voice Compatibility.txt
- SkyLadyTempTemplates.json
- SkyLady Patcher.esp (if split, also SkyLady Patcher_2.esp, etc.)

## Patcher's Settings
- **Patch Single NPC Only**:
Usage: This option allows you to select individual NPCs and assign them random female appearances, without touching other NPCs (whether patched or unpatched). This is useful when you don't like how certain NPCs ended up looking after your last SkyLady run. It allows you to select such NPCs and "roll the dice" again, giving them another random look. Or if you wish to "feminize" only hand-selected NPCs, this is the option to use.

  Instructions: Tick the `Patch Single NPC Only` option, then click on `NPCs to Patch`. Click on the `+` sign. A new entry will be created. Click inside the left field and search for the NPC you wish to patch. You need to enter the EditorID of the NPC. You can check EditorIDs of NPCs in xEdit. After you select all NPCs you want to patch, click on the `Top Level` to return back to all settings. You don't need to tick `Preserve Last Run Appearances`, as it's always active if `Patch Single NPC Only` is enabled. You can run the patcher.

- **NPCs to Patch**: Part of `Patch Single NPC Only`. See above.

- **Preserve Last Run Appearances**: This option is always internally enabled when using `Patch Single NPC Only`. Enabling this option will ensure that all previously patched NPCs will keep their last assigned appearance. If you install a new mod and want to patch it with SkyLady, without loosing current NPC looks, enable this option. New NPCs will get patched while the others remain the same. 

- **Use Default Race Fallback**: Use Nord and Imperial templates for custom races. SkyLady works by searching your load order for elligible NPCs based on their race, and assigning them random female templates of the same or compatible race. If an NPC has a race that is not included inside `SkyLady races.txt`, it won't be patched (will remain male). If the race is inside the TXT file, the NPC will be patched with a female template of the same race, or of a compatible race (based on `SkyLady Race Compatibility.txt`). If a race **IS** inside `SkyLady races.txt`, but it doesn't have a valid female template nor a valid compatible race, this setting will make sure it's patched with a random template chosen from NordRace or ImperialRace template pools. Otherwise the NPC won't be patched (stays male).

- **NPCs with Locked Templates**: Lock NPCs’ templates for consistency. If you really like how certain patched NPCs look, you can add them to this list to lock their appearance. This means they will keep their last assigned appearance until you unlock them (remove from the list). This setting overrides all other settings and blacklists (except Template Mod Blacklist).

- **Template Mod Blacklist**: Exclude mods from template collection. The patcher scans your load order for compatible female templates and randomly assigns them to male NPCs. If you want to prevent certain templates from being used (like vanilla-looking templates from Skyrim.esm), add their mods to this list.
- For best visual results I recommend adding Skyrim.esm and all DLC plugins, as well as CC plugins. If you do this, make sure you have plenty of other mods that contain valid female templates, like NPC replacers, female follower mods etc. Otherwise you might get repeating looks or unpatched NPCs.

- **Target Mods to Patch**: Limit patching to specific mods. If you don't want to patch your entire load order, but only a handful of mods, add them to this list. Only male NPCs present in listed mods will be patched.

- **Mods to Exclude from Patching**: Skip mods from patching. If you want male NPCs from some mods to remain male, add them here. 

- **NPCs to Exclude from Patching**: Skip specific NPCs. If you want specific NPCs to remain male, add them here. You need to know their EditorID.

- **Flag Output Plugins as ESL**: Automatically flag split ESP plugins as ESL (ESP-FE). Synthesis Profile Setting that does this doesn't work on split ESP plugins yet, so you can use this option instead. Only applies if ESP was split.

## Managing Mods with SkyLady
- **Adding/Updating Mods**: If new mods introduce male NPCs, rerun SkyLady with `Preserve Last Run Appearances` setting enabled to keep existing appearances.
- **Removing Patched Mods**: Disable SkyLady plugin, then rerun SkyLady to update the patch. Enable `Preserve Last Run Appearances` setting to keep existing appearances.
- **Uninstalling SkyLady**: Disable all `SkyLady Patcher.esp` plugins and delete the SkyLady mod folder.

## Troubleshooting

**Always save the Synthesis log after a run (right-click console, select all, copy, paste into a TXT file) to diagnose issues later.**

- **Some NPCs are not patched**:
  - In-game, approach the unpatched NPC, open the console, click it to view its IDs, and note the EditorID (screenshot or write it down).
  - Search your saved Synthesis log for the NPC’s EditorID to find why it wasn’t patched. Possible reasons:
    - **Race not listed**: The NPC’s race isn’t in `SkyLady races.txt`.
      - *Solution*: Use xEdit to find the race's name, add it to the `SkyLady races.txt`, and update `SkyLady Race Compatibility.txt` (instructions are inside the TXT files).
    - **No templates**: No compatible female templates for the NPC’s race. Check the `Understandign Templates` section for more info.
      - *Solution*: Add compatible races in `SkyLady Race Compatibility.txt` or install more mods with female NPCs of that race.
    - **Blacklisted**: The NPC or its mod is excluded from patching.
      - *Solution*: Check the patcher's blacklist settings.
    - **Target mods**: If using `Target Mods to Patch`, the NPC’s mod isn’t included.
      - *Solution*: Add the NPC’s mod to `Target Mods to Patch`.

- **Templates From Some Mods Are Not Being Used**:
  - Check the saved Synthesis log for "Skipped Templates".
  - Templates won't be used if the mods FaceGen files are inside BSA, or if the mod doesn't have it's own FaceGen files.
  
- **Patched NPC Has a Weird Appearance (Purple face, deformed head, floating eyes and mouth**:
  - Cause: The template used for that NPC is broken. It usually happens with templates from older follower mods. 
  - Solution: Note the EditorID of the affected NPC. Open Synthesis, check `Patch Single NPC Only` and select the affected NPC in `NPCs to Patch`. Run the patcher. The selected NPC will get new random look.
  - Prevention: To prevent the broken template from being used in the future, before you rerun the patcher, open your saved Synthesis log and search for the affected NPC's EditorID. You should find a line:
  *Patched: EditorID (Formkey) using EditorIDofUsedTemplate from PluginName.esp*
  Note the name of the plugin from which the template was used, and add it to the `Template Mod Blacklist` next time you run the patcher. This will make sure it's templates won't be used.
  
- **My Game Crashed and CrashLog Mentions NPC Patched by SkyLady**:
  - Identify the NPC via crash log. Find it's EditorID, then open your saved Synthesis log. Search for the EditorID. You should find a line:
  *Patched: EditorID (Formkey) using EditorIDofUsedTemplate from PluginName.esp*
  - The template used is probably broken, so blacklist the source mod of the template. Then, rerun SkyLady patcher for that NPC using `Patch Single NPC Only` option.
  - If it happens again, temporarily disable SkyLady Patcher.esp and check the NPC in game. If you get CTD again, it's not caused by SkyLady. CTDs caused by applied broken templates would happen the moment you enter the same cell as the affected NPC. If disabling SkyLady helps, another broken template might have been assigned and you should repeat this process again (find template - blacklist source mod - assign new look). 
  - If you use OBody NG, and you notice it being mentioned in your crashlog, try disabling it's `Performance mode` MCM setting. It might sometimes cause crashes if there are too many female NPCs in one cell.

- **About "broken" templates**:
  - When I say a template is "broken", it doesn't necessarily mean that it's bad. During my tests, many of those "broken" templates worked fine on their original NPCs. But once they were copied and assigned to someone else, they started to cause issues. After checking their .nif files in NifSkope, their head meshes looked distorted (like neck sticking up through the head). Blacklisting these mods is the best solution.

## Credits
- Thanks to **Corsec**, for his SkyFem mod ([Loverslab link](https://www.loverslab.com/files/file/3485-skyfem-all-npcs-now-female-or-futanari/)), and to the author of improved Special Edition version ([Loverslab link](https://www.loverslab.com/files/file/7549-skyfem-all-npcs-now-female-special-edition/)), from which I took inspiration. 
- **Mutagen/Synthesis Team**: For the framework enabling SkyLady’s functionality.
- **Bethesda**: For creating Skyrim and its modding possibilities.

## Support
Report issues on Nexus or GitHub (`https://github.com/generaleagleyt/SkyLady`).
