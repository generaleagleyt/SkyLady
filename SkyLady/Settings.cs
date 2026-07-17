using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis.Settings;
using System.Collections.Generic;

namespace SkyLady.SkyLady
{
    // Class to represent an NPC in the GUI
    public class SkyLadyNpc
    {
        [SynthesisOrder]
        [SynthesisTooltip("The NPC to patch")]
        public IFormLinkGetter<INpcGetter> Npc { get; set; } = FormLink<INpcGetter>.Null;

        public override string ToString()
        {
            return Npc.IsNull ? "None" : Npc.FormKey.ToString();
        }
    }

    // Class to represent an NPC with a locked template
    public class LockedNpcTemplate
    {
        [SynthesisOrder]
        [SynthesisTooltip("Select NPCs to lock their current templates, ensuring the patcher reuses them in future runs.")]
        public IFormLinkGetter<INpcGetter> Npc { get; set; } = FormLink<INpcGetter>.Null;

        [SynthesisIgnoreSetting]
        public IFormLinkGetter<INpcGetter> Template { get; set; } = FormLink<INpcGetter>.Null;

        public override string ToString()
        {
            if (Npc.IsNull) return "None";
            return Template.IsNull ? Npc.FormKey.ToString() : $"{Npc.FormKey} (Template: {Template.FormKey})";
        }
    }

    // Settings class for GUI
    public class PatcherSettings
    {
        [SynthesisSettingName("Force ESP Splitting")]
        [SynthesisTooltip("If you encounter 'Too Many Masters' Synthesis error, enable this option to split the final ESP.")]
        public bool ForceEspSplitting { get; set; } = false;

        [SynthesisSettingName("SkyLady Mod Folder")]
        [SynthesisTooltip("Path to your (persistent) SkyLady mod folder where loose facegen files will be written, e.g. C:\\...\\mods\\SkyLady. If left empty, files go to <Data>\\SkyLady, which managed/Stock Game setups may wipe.")]
        public string SkyLadyModFolder { get; set; } = "";

        [SynthesisSettingName("Patch Single NPC Only")]
        [SynthesisTooltip("If enabled, only selected NPCs from target mods get new random templates. Non-selected NPCs preserve their last run appearances.")]
        public bool PatchSingleNpcOnly { get; set; } = false;

        [SynthesisSettingName("NPCs to Patch")]
        [SynthesisTooltip("Select NPCs to receive new random templates when 'Patch Single NPC Only' is enabled.")]
        public List<SkyLadyNpc> NpcsToPatch { get; set; } = new();

        [SynthesisSettingName("Preserve Last Run Appearances")]
        [SynthesisTooltip("In bulk mode, enables non-locked NPCs to reuse last run templates. In Single NPC mode, non-selected NPCs always preserve appearances.")]
        public bool PreserveLastRunAppearances { get; set; } = false;

        [SynthesisSettingName("Use Default Race Fallback")]
        [SynthesisTooltip("If enabled, custom races with no female templates will use NordRace and ImperialRace templates as a fallback. If disabled, a matching race is required. Note: the race still needs to be inside SkyLady races.txt.")]
        public bool UseDefaultRaceFallback { get; set; } = false;

        [SynthesisSettingName("Pseudo-Copy Race on Fallback")]
        [SynthesisTooltip("Only applies when 'Use Default Race Fallback' is triggered. Instead of changing the race to Nord/Imperial, create a hybrid race that KEEPS the custom race's stats/keywords/tweaks but takes its body/appearance from the fallback race (Nord/Imperial).")]
        public bool PseudoCopyRaceOnFallback { get; set; } = false;

        [SynthesisSettingName("Change Voices")]
        [SynthesisTooltip("If enabled, male voices will be changed to their female counterparts according to Voice Compatibility.txt. If disabled, original voices are preserved.")]
        public bool ChangeVoices { get; set; } = true;

        [SynthesisSettingName("Patch Non-Unique NPCs Only")]
        [SynthesisTooltip("If enabled, only NPCs without the IsUnique flag are patched, unless locked in 'NPCs with Locked Templates' or selected in 'NPCs to Patch' with 'Patch Single NPC Only' enabled.")]
        public bool PatchNonUniqueOnly { get; set; } = false;

        [SynthesisSettingName("NPCs with Locked Templates")]
        [SynthesisTooltip("Select NPCs to lock their current templates, ensuring the patcher reuses them in future runs.")]
        public List<LockedNpcTemplate> LockedTemplates { get; set; } = new();

        [SynthesisSettingName("Template Mod Blacklist")]
        [SynthesisTooltip("Mods to exclude from template collection (e.g., Skyrim.esm for modded setups to avoid vanilla looks). Vanilla mods require loose facegen files.")]
        public HashSet<ModKey> TemplateModBlacklist { get; set; } = new();

        [SynthesisSettingName("Template Mod Whitelist")]
        [SynthesisTooltip("Only female templates from these mods will be used. Leave empty to use templates from all mods (except those in Template Mod Blacklist).")]
        public HashSet<ModKey> TemplateModWhitelist { get; set; } = new();

        [SynthesisSettingName("Target Mods to Patch")]
        [SynthesisTooltip("Select the mods to patch. Leave empty to patch the entire load order.")]
        public HashSet<ModKey> TargetModsToPatch { get; set; } = new();

        [SynthesisSettingName("Mods to Exclude from Patching")]
        [SynthesisTooltip("Select mods to skip patching (e.g., mods with unique NPCs or custom appearances to preserve).")]
        public HashSet<ModKey> ModsToExcludeFromPatching { get; set; } = new();

        [SynthesisSettingName("NPCs to Exclude from Patching")]
        [SynthesisTooltip("Select specific NPCs to skip patching (e.g., unique NPCs or those with custom appearances to preserve).")]
        public List<IFormLinkGetter<INpcGetter>> NpcsToExcludeFromPatching { get; set; } = new();

        [SynthesisSettingName("Flag Output Plugins as ESL")]
        [SynthesisTooltip("If enabled, output plugins are flagged as ESL (Light Master) if they have 2048 or fewer new records.")]
        public bool FlagOutputAsEsl { get; set; } = false;

        [SynthesisSettingName("Patch Only Female NPCs")]
        [SynthesisTooltip("If enabled, the patcher will ONLY patch female NPCs from 'Female Target Mods' and will skip all male NPCs. If disabled, males are patched as usual (plus females if any mods are listed).")]
        public bool PatchOnlyFemaleNPCs { get; set; } = false;

        [SynthesisSettingName("Female Target Mods")]
        [SynthesisTooltip("If any mods are added here, female NPCs from these mods will also be patched with random different female appearances (same race only). Leave empty to disable female patching.")]
        public HashSet<ModKey> FemaleTargetMods { get; set; } = new();

        // Deprecated: Kept for backward compatibility, but hidden from GUI
        [SynthesisIgnoreSetting]
        public string SingleNpcBaseId { get; set; } = "";
    }
}