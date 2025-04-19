# SkyLady - Ladies of Skyrim

![SkyLady Main Image](https://github.com/user-attachments/assets/75b2031b-2822-4570-98f6-e4642a45caf2)
## Overview
**SkyLady** is a Synthesis patcher designed to turn all male NPCs into females. Utilizing the power of Synthesis framework, SkyLady is lightweight, script-free, and can process large load orders in minutes, making it an efficient tool for feminizing your Skyrim world.

![SkyLady_Example_Ulfric(1)](https://github.com/user-attachments/assets/94823a5e-3837-496d-a694-4d82294f97ac)
*The name change is not part of the patcher.*
## Description
**SkyLady** is a Synthesis patcher that transforms male NPCs into females, using appearance templates from existing female NPCs in your load order. It adjusts gender flags, assigns equivalent female voices (configurable), and forwards changes from other mods, ensuring seamless integration. While designed to patch entire load orders, SkyLady offers flexible settings like single NPC patching, NPC and mod blacklists, and template preservation for consistent results across runs.

Inspired by SkyFem, an xEdit-based patcher, SkyLady overcomes the 254-master limit with ESP splitting. It processes large load orders (tested with 4000+ plugins) in minutes, making it an efficient tool to feminize your Skyrim world.

**ESP splitting**, a work-in-progress Synthesis feature, automatically handles the 254-master limit by dividing the output into multiple ESP files. When the limit is reached, SkyLady creates a new ESP (e.g., SkyLady_2.esp) and continues patching seamlessly. Once the patcher is done, an intentional error with a success message will appear. Future Synthesis updates will make this process fully automatic.

## Features
- Transforms male NPCs into females, setting gender flags, applying female voices (configurable) and forwarding other mods' changes. NPCs will use your default body and physics. Compatible with OBody NG.
- Single NPC patching lets you manually select NPCs for new appearances. Ideal when you don't like how a certain NPCs ended up looking, or for debugging.
- Blacklist mods (e.g., Skyrim.esm) to exclude them from template collection (explained below), prioritizing custom appearances.
- Blacklist mods or specific NPCs to leave males unchanged.
- Add mods to "Target Mods" list to only patch the selected mods.
- Preserve previous appearances when adding new mods, patching only new NPCs.
- Lock specific NPCs to retain their templates across runs for consistent looks.
- Custom races are handled via `SkyLady races.txt` and template sharing through `SkyLady Race Compatibility.txt`.
- Handles massive load orders (4000+ plugins) via ESP splitting, bypassing the 254-master limit.
- Option to automatically ESL flag split ESP plugins to save load order slots (as currently flagging through Synthesis Profile settings doesn't work on split ESP files.)

## Installation
This guide is for Mod Organizer 2 users and assumes you have Synthesis already installed. If not, you can follow one of many guides available on the internet, like this [Synthesis Guide on YT](https://www.youtube.com/watch?v=s7luh0hMMAU) (for MO2).

I have never used other mod managers, so I might not be able to help you if you encounter any issues during installation.

1. Create an empty mod folder inside MO2. Name it SkyLady, then enable it. Inside this folder, create an empty TXT file called `SkyLadyMarker.txt`. Refresh MO2. The patcher will detect it and move all created files inside this folder automatically. The final ESP file will still need to be moved manually.

2. Download `SkyLady - Ladies of Skyrim` main file [Nexus](https://www.nexusmods.com/skyrimspecialedition/mods/147559?tab=files) and extract the `SkyLady.synth` file into a temporary folder. Optionally, download the `SkyLady - TXT Settings` file from Optional files and extract it into your SkyLady mod folder. They will allow you to adjust settings like patched races and voice types immediately (otherwise, they are created by patcher during the first run).

3. Launch Synthesis through MO2. Once it loads, create a new group called `SkyLady` or similar. Find the `SkyLady.synth` file you extracted earlier and double click it to add it to the group.

Alternatively, you can get the patcher from Synthesis. Click on the SkyLady group, choose `Git Repository` and search for `SkyLady`. If it doesn't show up, click on Input tab and enter the following link: `https://github.com/generaleagleyt/SkyLady`. Click on the `Project` dropdown and select the option shown. Click `Confirm` in bottom right.

4. Make sure the `Versioning` page looks like this:
![SkyLady_Installation_Setting up the Patcher_Versioning](https://github.com/user-attachments/assets/ca5842a4-6205-41b4-b865-d99e59fa50bc)

5. Click on `Settings` tab. Paste the path to your SkyLady mod folder (where `SkyLadyMarker.txt` is located) inside the `SkyLady Mod Folder` field, as shown on the picture below.
![WKG79Hc](https://github.com/user-attachments/assets/5b195dc7-9c89-4b3b-8e6a-747e61d9111a)

6. Configure the rest of the settings to your liking (see `Patcher Settings` section below for detailed explanations on what each setting does).

7. Run the patcher.

8. Once the patcher is done, one of two outcomes will occur:

   a) The patcher completes normally. Continue to **Step 9.** 
   
   b) You get a Synthesis error "Too Many Masters". This happens when the patcher hits Skyrim's 254-master limit. If thats the case, click on the arrow in the top left to go back to the settings tab, and enable `Force ESP Splitting`. This will make sure the final ESP is split into multiple files, overcoming the master limit. `Run the patcher again.`

*(This ESP splitting was automatic in the previous version, but turns out it's master count detection was not reliable. Synthesis will soon integrate ESP splitting into it's application, making it fully automatic. Until then, use the "Force ESP Splitting" setting to resolve the 254-master issue.)*
![0wiJl4a](https://github.com/user-attachments/assets/74955131-4518-4459-bd1d-d3048a1f6d71)

After that, you should get an error that reads: "This error indicates that the patcher run successfully. The final ESP was split..."
![SkyLady_Installation_Succesfully Split ESP Files](https://github.com/user-attachments/assets/801402c4-a0fd-407d-94da-6bc51f6fd0c2)

The error means that the patcher was successful. Continue to **Step 9.** 

*(Explanation: The "System.Exception" is intentional because the splitting is not fully integrated into Synthesis yet. It works, but without it Synthesis would still try to create a single output ESP, hitting the master limit and throwing an exception, after the split ESP files were already created successfully. The above Exception is safe and indicates success.*
   
9. Check the console for the amount of patched NPCs (e.g. 1000 out of 1000). If the numbers are not the same, I recommend you right click in the console, select all, copy and save it inside a TXT file. Check the **Troubleshooting** section below for more details. Close Synthesis.
   
10. MO2 might inform you that load order has been changed and ask if you want to keep changes. You can pick yes or no, it doesn't matter. `SkyLady.esp` file (or multiple if split) should now appear at the bottom of your load order. If not, refresh MO2.

11. All files generated by SkyLady should now be present inside your SkyLady mod folder. You only need to move the created ESP file(s) into it. Check your dedicated Synthesis Output folder, or Overwrite folder. Move the `SkyLady.esp` file(s) inside your SkyLady mod folder that you created during step 1.

*Note: For future runs, all files should be generated directly inside your SkyLady mod folder, so you don't have to move anything.*

12. Refresh MO2, make sure both SkyLady mod folder and all SkyLady.esp plugins are active. They should be placed below other NPC-altering mods/patches in both the left side and right side of MO2, for it's changes to apply correctly.

13. You are good to go. If you encounter any issues in game, check the **Troubleshooting** section below.

<ins>After a successful run, your SkyLady mod folder should look like this:</ins>
- **meshes**
- **textures**
- SkyLadyMarker.txt
- SkyLady partsToCopy.txt
- SkyLady races.txt
- SkyLady Race Compatibility.txt
- SkyLady Voice Compatibility.txt
- SkyLadyTempTemplates.json
- SkyLady.esp (if split, also SkyLady_2.esp, etc.)

## Rerunning the Patcher

1. Before you launch Synthesis, disable all active SkyLady plugins (.esp files) on the right side of MO2.

2. Launch Synthesis through MO2.

3. Configure the settings to your liking. If you want to keep previously assigned looks, make sure to enable appropriate settings.

4. Run the patcher.

5. After it's done, copy and save the Synthesis log if necessary. Close Synthesis.

## Patcher's Settings

**Important Note:** SkyLady, as any other Synthesis Patcher, overwrites its previous output ESP with each run. For example, if you first patch NPCs from Skyrim.esm and later patch only Dawnguard.esm, the Skyrim.esm changes won’t automatically persist. To maintain previous appearances, SkyLady saves patched NPCs and their templates inside `SkyLadyTempTemplates.json`. Use the `Preserve Last Run Appearances` and `NPCs with Locked Templates` settings explained below to reapply the stored templates in your next run. Check each setting’s description to control which NPCs keep their prior looks versus receiving new ones. If unsure, ask in the Posts section.

**WARNING:** The SkyLadyTempTemplates.json file only keeps track of your last session. Any previous sessions are lost.

By default, SkyLady will patch ALL mods in your load order. If that's not what you want, you can customize this by using settings explained bellow.

- **Force ESP Splitting**:
Used to overcome the 254-master limit. Enable this if Synthesis gives you "Too Many Masters" error.

- **Patch Single NPC Only** (or more):
This option allows you to select individual NPCs and assign them random female appearances, without touching other NPCs (whether patched or unpatched). This is useful when you only want to "feminize" certain NPCs or if you don't like how certain NPCs ended up looking after your last SkyLady run. It allows you to select such NPCs and "roll the dice" again, giving them new random looks.

**Instructions:** Tick the `Patch Single NPC Only` option, then click on `NPCs to Patch`. Click the `+` button. A new entry will be created. Click inside the left field and search for the EditorID of the NPC you wish to patch. You can check EditorIDs of vanilla NPCs online, or in xEdit if modded. After you added all NPCs you want to patch, click on the `Top Level` to return back to all settings. You don't need to tick `Preserve Last Run Appearances`, as it's always active when `Patch Single NPC Only` is enabled (but no harm if you do). You can run the patcher.

- **NPCs to Patch**: Part of `Patch Single NPC Only`. See above.

- **Preserve Last Run Appearances**: *(This option is always internally enabled when using `Patch Single NPC Only`.)*
  Enabling this option will ensure that all previously patched NPCs will keep their last assigned appearance. If you install a new mod and want to patch it with SkyLady, while keeping current NPC looks, enable this option. New NPCs will get patched while the others remain the same.

- **Use Default Race Fallback**: Use Nord and Imperial templates for custom races. SkyLady works by searching your load order for elligible NPCs based on their race, and assigning them random female templates of the same or compatible race. If an NPC has a race that is not included inside `SkyLady races.txt`, it won't be patched (will stay male). If the race **IS** inside the TXT file, the NPC will be patched with a female template of the same race, or of a compatible race (based on the `SkyLady Race Compatibility.txt`). If a race IS inside `SkyLady races.txt`, but it doesn't have a valid female template nor a valid compatible race, this setting will make sure it's patched using a random template chosen from NordRace or ImperialRace template pools. Otherwise the NPC won't be patched (stays male).

Alternatively locate unpatched NPCs, find their race in xEdit, and add it to `SkyLady races.txt`.

- **NPCs with Locked Templates**: Lock NPCs’ templates for consistency. If you really like how certain patched NPCs look, you can add them to this list to lock their appearance. This means they will keep their last assigned appearance during subsequent runs until you unlock them (remove from the list). This setting overrides all other settings and blacklists (except `Template Mod Blacklist`).

- **Template Mod Blacklist**: Exclude mods from template collection. The patcher scans your load order for compatible female templates and randomly assigns them to male NPCs. If you want to prevent certain templates from being used (like vanilla-looking templates from quest mods without NPC replacers), add those mods to this list.

SkyLady currently doesn't support templates from mods that store their facegen files inside BSA, so they are automatically skipped. This means you don't have to exclude mods like Skyrim.esm (and DLCs) as their vanilla templates are excluded by default.

That means you should make sure that you have plenty of other mods that contain valid female templates, like NPC replacers, female follower mods etc. Otherwise you might get repeating looks or unpatched NPCs. 

- **Target Mods to Patch**: Limit patching to specific mods. If you don't want to patch your entire load order, but only a handful of mods, add them to this list. Only male NPCs present in the listed mods will be patched.

- **Mods to Exclude from Patching**: Skip mods from patching. If you want male NPCs from certain mods to remain male, add them here.

- **NPCs to Exclude from Patching**: Skip specific NPCs. If you want specific NPCs to remain male, add them here. You need to know their EditorID.

- **Flag Output Plugins as ESL**: Automatically flag split ESP plugins as ESL (ESP-FE). Synthesis Profile Setting that does this doesn't work on split ESP plugins yet, so you can use this option instead. Only applies if the ESP was split.

## Understanding Templates
- **What is a Template?**: The term "template" has many meanings in Skyrim modding, but for the purpose of SkyLady, we will use our own. By a "template" we understand a female NPC from your load order used as a source for appearance data (e.g. head parts, tint layers, facegen files, hair color, height, weight). In other words, it's a look of a female NPC.
- **How Are Templates Selected?**:
  - Valid templates are female NPCs from humanoid races (defined in `SkyLady races.txt`).
  - They must not be from blacklisted mods (Template Mods Blacklist), and require loose facegen files (templates from plugins like Skyrim.esm are automatically excluded, because they use BSA).
  - Templates are grouped by race, with selection based on `SkyLady Race Compatibility.txt`.
- **Adding New Templates**: To expand the template pool, install mods with female NPCs (e.g., follower mods or female replacer mods), ensuring they have loose facegen files.
- **Custom Races**:
  - Add the race’s EditorID to `SkyLady races.txt` to recognize it.
  - Ensure female NPCs of the race exist with loose facegen files, or configure `SkyLady Race Compatibility.txt` to use templates from other races.

## Managing Mods with SkyLady
- **Adding/Updating Mods**: If you install new mods with male NPCs, rerun SkyLady with `Preserve Last Run Appearances` setting enabled to keep existing appearances.
- **Removing Patched Mods**: Disable SkyLady plugin, then rerun SkyLady to update the patch. Enable `Preserve Last Run Appearances` setting to keep existing appearances.
- **Uninstalling SkyLady**: Disable all `SkyLady.esp` plugins and delete the SkyLady mod folder.

## Compatibility
SkyLady was tested using Skyrim SE, but should work with AE and VR.

SkyLady should be compatible with nearly all mods, as it's a Synthesis patcher. Male NPC replacers should be disabled or placed above SkyLady.esp to avoid conflicts. Male NPC replacers based on SkyPatcher might cause problems and should be disabled (if patching the same NPCs)

Appearance templates from base Skyrim plugins (Skyrim.esm, Dawnguard.esm, etc.) are NOT supported, as they don't have loose facegen files (they are in BSA). All valid templates require loose facegen files.

Custom races aren’t patched by default unless added to `SkyLady races.txt` in your SkyLady mod folder. Add the race’s EditorID (found in xEdit under the RACE record), and ensure female NPCs of that race exist with loose facegen files. If only male NPCs of that race exist or the mod uses BSAs, configure `SkyLady Race Compatibility.txt` to allow templates from other races.

Voice mappings for patched NPCs can be customized via `SkyLady Voice Compatibility.txt` in the SkyLady mod folder. Follow the instructions inside the file if you wish to make changes. Use xEdit to find voice EditorIDs.

## Recommended Mods
- [Fuz Ro D-oh - Silent Voice](https://www.nexusmods.com/skyrimspecialedition/mods/15109): To be able to skip unvoiced dialogue.
- [Random Physics Hair for NPCs](https://www.nexusmods.com/skyrimspecialedition/mods/86800): Uses SPID to give all NPCs random wigs with physics. Further enhances patched NPCs.

## Troubleshooting

**Always save the Synthesis log after a run (right-click console, select all, copy, paste into a TXT file) to diagnose issues later.**

- **Some NPCs are not patched**:
  - In-game, approach the unpatched NPC, open the console, click on it to view its details, and note the EditorID (screenshot or write it down). Search your saved Synthesis log for the NPC’s EditorID to find why it wasn’t patched. Possible reasons:
    
    - **Race not listed**: The NPC’s race isn’t in `SkyLady races.txt`.
      - *Solution*: Use xEdit to find the race's name, add it to the `SkyLady races.txt`, and update `SkyLady Race Compatibility.txt` (instructions are inside those TXT files).
     
    - **No templates**: No compatible female templates for the NPC’s race. Check the `Understandign Templates` section above for more info.
      - *Solution*: Add compatible races in `SkyLady Race Compatibility.txt` or install more mods with female NPCs of that race.
        
    - **Blacklisted**: The NPC or its mod is excluded from patching.
      - *Solution*: Check the patcher's blacklist settings to make sure it's not blacklisted.
        
    - **Target mods**: **IF** using `Target Mods to Patch` option, the NPC’s mod might not be included.
      - *Solution*: Add the NPC’s mod to `Target Mods to Patch`.

- **Templates From Some Mods Are Not Being Used**:
  - Check the saved Synthesis log for "Skipped Templates".
  - Templates won't be used if the mod's facegen files are inside BSA, or if the mod doesn't have it's own facegen files.
  
- **Patched NPC Has a Weird Appearance (Purple face, deformed head, floating eyes and mouth**:
  - Cause: The template used for that NPC is broken. It usually happens with templates from older follower mods.
  - Solution: Note the EditorID of the affected NPC. Open Synthesis, check `Patch Single NPC Only` and select the affected NPC in `NPCs to Patch`. Run the patcher. The selected NPC will get new random look.
  - Prevention: To prevent the broken template from being used in the future, before you rerun the patcher, open your saved Synthesis log and search for the affected NPC's EditorID. You should find a line:
    
  "Patched: **EditorID** (**Formkey**) using **EditorIDofUsedTemplate** from **PluginName.esp**"
(EditorID - patched NPC's EditorID, Formkey - patched NPC's Formkey, EditorIDofUsedTemplate - EditorID of an NPC whose template was used, PluginName.esp - Source mod of the used template.)

  Note the name of the plugin from which the template was used, and add it to the `Template Mod Blacklist` next time you run the patcher. This will make sure it's templates won't be used again.
  
- **My Game Crashed and CrashLog Mentions NPC Patched by SkyLady**:
  - Identify the NPC via crash log. Find it's EditorID, then open your saved Synthesis log. Search for the EditorID. You should find a line:
 
  "Patched: **EditorID** (**Formkey**) using **EditorIDofUsedTemplate** from **PluginName.esp**"
(EditorID - patched NPC's EditorID, Formkey - patched NPC's Formkey, EditorIDofUsedTemplate - EditorID of an NPC whose template was used, PluginName.esp - Source mod of the used template.)

  - The template used is probably broken, so blacklist the source mod of the template in `Template Mod Blacklist`. Then, rerun SkyLady for that NPC only using `Patch Single NPC Only` option. Leave other settings as they were during last run.
  - If it happens again, temporarily disable SkyLady.esp and check the NPC in game. If you get CTD again, it's probably not caused by SkyLady. CTDs caused by applied broken templates would happen the moment you enter the same cell as the affected NPC. If disabling SkyLady helps, another broken template might have been assigned and you should repeat the steps above (find the template - blacklist source mod - patch again).
  - If you use **OBody NG**, and you notice it being mentioned inside the crashlog, try disabling it's `Performance mode` MCM setting. It might sometimes cause crashes if there are too many female NPCs in one cell.

- **About "broken" templates**:
  - When I say that a template is "broken", it doesn't necessarily mean it's bad. During my tests, many of these "broken" templates worked fine on their original NPCs. But once they were copied and assigned to someone else, they started to cause issues. After checking their original .nif files in NifSkope, their head meshes looked distorted (like neck sticking up through the head). Blacklisting such mods is the easiest way to solve this.

## Keep in Mind:
Dialogue of patched NPCs won't change, so they will still refer to themselves as male.

Patched NPCs will have their voices changed to their female equivalent (per `SkyLady Voice Compatibility.txt`). This means the dialogue of those NPCs will be silent, especially during quests. A potential solution would be to generate vanilla voicelines through tools like xVASynth.

## FAQ:
**Q: Should I run SkyLady before or after other patchers?**
A: It mostly depends on your preferrence. It can be run before/after other patchers, but I recommend to run it after other patchers at first. The main reason is that if one of your NPCs gets assigned a broken template (see Troubleshooting section above) from one of your other mods (like old follower mods), they might cause CTD when you enter their cell. This can be easily fixed by assigning a new template to only that NPC using Patch Single NPC Only option. And it's easier to do if you run this patcher last.

**Q: Does SkyLady work with unmodded vanilla Skyrim?**
**A:** At the moment, no. You need to have mods with loose facegen files installed, like female NPC replacers or female follower mods. I'm working on a way to allow usage of templates from BSA files.

**Q: Does it require a new game?**
**A:** No, technically it shouldn't cause any problems.

**Q: Can I manually assign an NPC a specific appearance?**
**A:** You should the following Synthesis patcher: NPC-Appearance-Copier. It was created exactly for that purpose.

**Q: Does this mod also adjust NPC dialogue lines to reflect their new nature?**
**A:** No.

## Support
Report issues on [Nexus](https://www.nexusmods.com/skyrimspecialedition/mods/147559) or GitHub.

## Credits
Thanks to:
- **Corsec**, for his original SkyFem mod ([Nexus link﻿](https://www.nexusmods.com/skyrim/mods/87315)), and to the author of it's improved Special Edition version ([Loverslab link](https://www.loverslab.com/files/file/7549-skyfem-all-npcs-now-female-special-edition/)), from which I took inspiration. 
- **Mutagen/Synthesis Team**: For the framework enabling SkyLady’s functionality.
- **Bethesda**: For creating Skyrim and its modding ecosystem.
