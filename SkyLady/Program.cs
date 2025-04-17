using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;
using Mutagen.Bethesda.Plugins.Analysis.DI;
using System.IO;
using Mutagen.Bethesda.Synthesis.Settings;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using System.Text.Json;

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
        [SynthesisSettingName("SkyLady Mod Folder")]
        [SynthesisTooltip("Paste here a path to your SkyLady mod folder containing SkyLadyMarker.txt (e.g., C:\\Skyrim\\ModlistName\\mods\\SkyLady). Required for file creation.")]
        public string SkyLadyModFolder { get; set; } = "";

        [SynthesisSettingName("Patch Single NPC Only")]
        [SynthesisTooltip("If enabled, only selected NPCs from target mods get new random templates. Non-selected NPCs preserve their last run appearances.")]
        public bool PatchSingleNpcOnly { get; set; } = false;

        [SynthesisSettingName("NPCs to Patch")]
        [SynthesisTooltip("Select NPCs to receive new random templates when 'Patch Single NPC Only' is enabled.")]
        public List<SkyLadyNpc> NpcsToPatch { get; set; } = [];

        [SynthesisSettingName("Preserve Last Run Appearances")]
        [SynthesisTooltip("In bulk mode, enables non-locked NPCs to reuse last run templates. In Single NPC mode, non-selected NPCs always preserve appearances.")]
        public bool PreserveLastRunAppearances { get; set; } = false;

        [SynthesisSettingName("Use Default Race Fallback")]
        [SynthesisTooltip("If enabled, custom races with no female templates will use NordRace and ImperialRace templates as a fallback. If disabled, a matching race is required.")]
        public bool UseDefaultRaceFallback { get; set; } = false;

        [SynthesisSettingName("NPCs with Locked Templates")]
        [SynthesisTooltip("Select NPCs to lock their current templates, ensuring the patcher reuses them in future runs.")]
        public List<LockedNpcTemplate> LockedTemplates { get; set; } = [];

        [SynthesisSettingName("Template Mod Blacklist")]
        [SynthesisTooltip("Mods to exclude from template collection (e.g., Skyrim.esm for modded setups to avoid vanilla looks). Vanilla mods like Skyrim.esm are included by default.")]
        public HashSet<ModKey> TemplateModBlacklist { get; set; } = [];

        [SynthesisSettingName("Target Mods to Patch")]
        [SynthesisTooltip("Select the mods to patch. Leave empty to patch the entire load order.")]
        public HashSet<ModKey> TargetModsToPatch { get; set; } = [];

        [SynthesisSettingName("Mods to Exclude from Patching")]
        [SynthesisTooltip("Select mods to skip patching (e.g., mods with unique NPCs or custom appearances to preserve).")]
        public HashSet<ModKey> ModsToExcludeFromPatching { get; set; } = [];

        [SynthesisSettingName("NPCs to Exclude from Patching")]
        [SynthesisTooltip("Select specific NPCs to skip patching (e.g., unique NPCs or those with custom appearances to preserve).")]
        public List<IFormLinkGetter<INpcGetter>> NpcsToExcludeFromPatching { get; set; } = [];

        [SynthesisSettingName("Flag Output Plugins as ESL")]
        [SynthesisTooltip("If enabled, output plugins are flagged as ESL (Light Master) if they have 2048 or fewer new records.")]
        public bool FlagOutputAsEsl { get; set; } = false;

        // Deprecated: Kept for backward compatibility, but hidden from GUI
        [SynthesisIgnoreSetting]
        public string SingleNpcBaseId { get; set; } = "";
    }

    public class Program
    {
        private static readonly char[] LineSeparators = ['\n', '\r'];

        // Define a variable to hold the settings
        static Lazy<PatcherSettings> Settings = null!;

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(Patch)
                .SetAutogeneratedSettings(
                    nickname: "Settings",
                    path: "settings.json",
                    out Settings)
                .SetTypicalOpen(GameRelease.SkyrimSE, "SkyLadyPatcher.esp")
                .Run(args);
        }

        // Helper method to perform batch file copying
        private static void BatchCopyFiles(List<(string SourcePath, string DestPath)> fileCopyOperations)
        {
            if (fileCopyOperations.Count == 0) return;

            var directories = fileCopyOperations
                .Select(op => Path.GetDirectoryName(op.DestPath))
                .Distinct()
                .ToList();

            foreach (var dir in directories)
            {
                if (dir != null)
                {
                    Directory.CreateDirectory(dir);
                }
            }

            foreach (var (sourcePath, destPath) in fileCopyOperations)
            {
                File.Copy(sourcePath, destPath, true);
                Console.WriteLine($"Copied file to: {destPath}");
            }
        }

        // Helper method to delete existing facegen files for an NPC
        private static void DeleteExistingFacegenFiles(string outputModFolder, FormKey npcFormKey)
        {
            var modKey = npcFormKey.ModKey.FileName.ToString();
            var formId = npcFormKey.IDString();
            var nifPath = Path.Combine(outputModFolder, "meshes", "actors", "character", "facegendata", "facegeom", modKey, $"00{formId}.nif");
            var ddsPath = Path.Combine(outputModFolder, "textures", "actors", "character", "facegendata", "facetint", modKey, $"00{formId}.dds");

            if (File.Exists(nifPath))
            {
                File.Delete(nifPath);
                Console.WriteLine($"Deleted existing facegen .nif: {nifPath}");
            }
            if (File.Exists(ddsPath))
            {
                File.Delete(ddsPath);
                Console.WriteLine($"Deleted existing facegen .dds: {ddsPath}");
            }
        }

        public static void Patch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var settings = Settings.Value;
            Console.WriteLine("SkyLady (Side) running on .NET 8.0...");

            // Locate the SkyLady mod folder using the user-specified setting
            string modFolderPath;
            if (!string.IsNullOrEmpty(settings.SkyLadyModFolder) &&
                Directory.Exists(settings.SkyLadyModFolder) &&
                File.Exists(Path.Combine(settings.SkyLadyModFolder, "SkyLadyMarker.txt")))
            {
                modFolderPath = settings.SkyLadyModFolder;
                Console.WriteLine($"Using user-specified SkyLady mod folder at {modFolderPath}.");
            }
            else
            {
                modFolderPath = Path.Combine(state.DataFolderPath, "SkyLady");
                Directory.CreateDirectory(modFolderPath);
                Console.WriteLine($"SkyLadyMarker.txt not found or invalid mod folder specified. Created default SkyLady mod folder at {modFolderPath}.");
            }

            // Clear previous facegen files before patching
            try
            {
                var facegeomPath = Path.Combine(modFolderPath, "meshes", "actors", "character", "facegendata", "facegeom");
                var facetintPath = Path.Combine(modFolderPath, "textures", "actors", "character", "facegendata", "facetint");

                if (Directory.Exists(facegeomPath))
                {
                    foreach (var dir in Directory.GetDirectories(facegeomPath))
                    {
                        Directory.Delete(dir, true);
                    }
                }

                if (Directory.Exists(facetintPath))
                {
                    foreach (var dir in Directory.GetDirectories(facetintPath))
                    {
                        Directory.Delete(dir, true);
                    }
                }

                Console.WriteLine("Cleared previous facegen files before patching.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to clear previous facegen files: {ex.Message}. Continuing with patching.");
            }

            // Define paths using the mod folder
            var racesPath = Path.Combine(modFolderPath, "SkyLady races.txt");
            var raceCompatibilityPath = Path.Combine(modFolderPath, "SkyLady Race Compatibility.txt");
            var partsToCopyPath = Path.Combine(modFolderPath, "SkyLady partsToCopy.txt");
            var voiceCompatibilityPath = Path.Combine(modFolderPath, "SkyLady Voice Compatibility.txt");
            var tempTemplatesPath = Path.Combine(modFolderPath, "SkyLadyTempTemplates.json");

            // Create .txt files with defaults if they don't exist
            if (!File.Exists(racesPath))
            {
                try
                {
                    File.WriteAllLines(racesPath,
                    [
                    "# SkyLady Races Configuration",
                "# Lists the races eligible for patching by SkyLady (e.g., NordRace, ArgonianRace).",
                "# Format: One race EditorID per line.",
                "# These races determine which NPCs can be transformed with female templates.",
                "# Add custom races from mods to include them, or remove races to exclude.",
                "# Back up this file before editing.",
                "# Lines starting with # or empty lines are ignored.",
                "",
                "ArgonianRace",
                "BretonRace",
                "DarkElfRace",
                "DremoraRace",
                "ElderRace",
                "HighElfRace",
                "ImperialRace",
                "KhajiitRace",
                "NordRace",
                "OrcRace",
                "RedguardRace",
                "WoodElfRace",
                "ArgonianRaceVampire",
                "BretonRaceVampire",
                "DarkElfRaceVampire",
                "ElderRaceVampire",
                "HighElfRaceVampire",
                "ImperialRaceVampire",
                "NordRaceVampire",
                "KhajiitRaceVampire",
                "OrcRaceVampire",
                "RedguardRaceVampire",
                "WoodElfRaceVampire",
                "DA13AfflictedRace",
                "COTRRace",
                "ArgonianRaceKZ",
                "KhajiitRaceKZ"
                    ]);
                    Console.WriteLine($"Created default SkyLady races.txt at {racesPath}.");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to create SkyLady races.txt at {racesPath}. Check write permissions: {ex.Message}");
                }
            }

            if (!File.Exists(partsToCopyPath))
            {
                try
                {
                    File.WriteAllLines(partsToCopyPath,
                    [
                    "# SkyLady Parts to Copy Configuration",
                "# Lists NPC appearance components to copy from female templates (e.g., PNAM, Tint Layers).",
                "# Format: One component identifier per line.",
                "# These settings control which visual aspects are applied to patched NPCs.",
                "# Adjusting these is not recommended, as it may cause unintended behavior.",
                "# Back up this file before editing.",
                "# Lines starting with # or empty lines are ignored.",
                "",
                "PNAM",
                "HEDP",
                "WNAM",
                "QNAM",
                "NAM9",
                "NAMA",
                "Tint Layers",
                "FTST",
                "HCLF",
                "NAM7",
                "NAM6"
                    ]);
                    Console.WriteLine($"Created default SkyLady partsToCopy.txt at {partsToCopyPath}.");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to create SkyLady partsToCopy.txt at {partsToCopyPath}. Check write permissions: {ex.Message}");
                }
            }

            if (!File.Exists(raceCompatibilityPath))
            {
                try
                {
                    File.WriteAllLines(raceCompatibilityPath,
                    [
                    "# SkyLady Race Compatibility Configuration",
                "# Defines which races can share female templates (e.g., NordRace: NordRace, ImperialRace).",
                "# Format: Race: CompatibleRace1, CompatibleRace2, ...",
                "# Controls template matching for patched NPCs; broader mappings increase variety.",
                "# Add mod races or adjust mappings to suit your load order.",
                "# Back up this file before editing.",
                "# Lines starting with # or empty lines are ignored.",
                "",
                "NordRace: NordRace",
                "NordRaceVampire: NordRace, NordRaceVampire",
                "ImperialRace: ImperialRace",
                "ImperialRaceVampire: ImperialRace, ImperialRaceVampire",
                "DarkElfRace: DarkElfRace, _00DwemerRace, MASNerevarineRace",
                "DarkElfRaceVampire: DarkElfRace, DarkElfRaceVampire, _00DwemerRace, MASNerevarineRace",
                "ArgonianRace: ArgonianRace",
                "ArgonianRaceVampire: ArgonianRace, ArgonianRaceVampire",
                "KhajiitRace: KhajiitRace",
                "KhajiitRaceVampire: KhajiitRace, KhajiitRaceVampire",
                "HighElfRace: HighElfRace",
                "HighElfRaceVampire: HighElfRace, HighElfRaceVampire",
                "WoodElfRace: WoodElfRace",
                "WoodElfRaceVampire: WoodElfRace, WoodElfRaceVampire",
                "BretonRace: BretonRace",
                "BretonRaceVampire: BretonRace, BretonRaceVampire",
                "RedguardRace: RedguardRace",
                "RedguardRaceVampire: RedguardRace, RedguardRaceVampire",
                "OrcRace: OrcRace",
                "OrcRaceVampire: OrcRace, OrcRaceVampire",
                "ElderRace: ElderRace",
                "ElderRaceVampire: ElderRace, ElderRaceVampire",
                "DremoraRace: DremoraRace",
                "DA13AfflictedRace: DA13AfflictedRace",
                "COTRRace: COTRRace, NordRace, ImperialRace",
                "ArgonianRaceKZ: ArgonianRaceKZ",
                "KhajiitRaceKZ: KhajittRaceKZ"
                    ]);
                    Console.WriteLine($"Created default SkyLady Race Compatibility.txt at {raceCompatibilityPath}.");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to create SkyLady Race Compatibility.txt at {raceCompatibilityPath}. Check write permissions: {ex.Message}");
                }
            }

            if (!File.Exists(voiceCompatibilityPath))
            {
                try
                {
                    File.WriteAllLines(voiceCompatibilityPath,
                    [
                    "# SkyLady Voice Compatibility Configuration",
                "# Format:",
                "# [VoiceMap] for male-to-female voice mappings. If an NPC used a male voice on the left side before using SkyLady, it will end up with the female voice on the right side.",
                "# [RaceVoiceFallbacks] for race-specific fallback voices. If an NPC of the race on the left side contains a voice not mapped in [VoiceMap] section, it will randomly choose one of the race's voices on the right side.",
                "# Back up this file before editing.",
                "# Lines starting with # or empty lines are ignored.",
                "",
                "[VoiceMap]",
                "MaleArgonian: FemaleArgonian",
                "MaleBandit: FemaleCommoner",
                "MaleBrute: FemaleCommander",
                "MaleChild: FemaleChild",
                "MaleCommander: FemaleCommander",
                "MaleCommoner: FemaleCommoner",
                "MaleCommonerAccented: FemaleCommoner",
                "MaleCondescending: FemaleCondescending",
                "MaleCoward: FemaleCoward",
                "MaleDarkElf: FemaleDarkElf",
                "MaleDrunk: FemaleSultry",
                "MaleElfHaughty: FemaleElfHaughty",
                "MaleEvenToned: FemaleEvenToned",
                "MaleEvenTonedAccented: FemaleEvenToned",
                "MaleGuard: FemaleCommander",
                "MaleKhajiit: FemaleKhajiit",
                "MaleNord: FemaleNord",
                "MaleNordCommander: FemaleNord",
                "MaleOldGrumpy: FemaleOldGrumpy",
                "MaleOldKindly: FemaleOldKindly",
                "MaleOrc: FemaleOrc",
                "MaleSlyCynical: FemaleSultry",
                "MaleSoldier: FemaleCommander",
                "MaleUniqueGhost: FemaleUniqueGhost",
                "MaleWarlock: FemaleCondescending",
                "MaleYoungEager: FemaleYoungEager",
                "DLC1MaleVampire: DLC1FemaleVampire",
                "DLC2MaleDarkElfCommoner: DLC2FemaleDarkElfCommoner",
                "DLC2MaleDarkElfCynical: FemaleDarkElf",
                "",
                "[RaceVoiceFallbacks]",
                "NordRace: FemaleNord, FemaleEvenToned, FemaleCommander",
                "NordRaceVampire: FemaleNord, FemaleEvenToned, FemaleCommander",
                "DarkElfRace: FemaleDarkElf, DLC2FemaleDarkElfCommoner, FemaleCondescending",
                "DarkElfRaceVampire: FemaleDarkElf, DLC2FemaleDarkElfCommoner, FemaleCondescending",
                "ArgonianRace: FemaleArgonian, FemaleSultry",
                "ArgonianRaceVampire: FemaleArgonian, FemaleSultry",
                "KhajiitRace: FemaleKhajiit, FemaleSultry",
                "KhajiitRaceVampire: FemaleKhajiit, FemaleSultry",
                "HighElfRace: FemaleElfHaughty, FemaleEvenToned",
                "HighElfRaceVampire: FemaleElfHaughty, FemaleEvenToned",
                "WoodElfRace: FemaleEvenToned, FemaleYoungEager",
                "WoodElfRaceVampire: FemaleEvenToned, FemaleYoungEager",
                "BretonRace: FemaleEvenToned, FemaleYoungEager",
                "BretonRaceVampire: FemaleEvenToned, FemaleYoungEager",
                "ImperialRace: FemaleEvenToned, FemaleCommander",
                "ImperialRaceVampire: FemaleEvenToned, FemaleCommander",
                "RedguardRace: FemaleEvenToned, FemaleSultry",
                "RedguardRaceVampire: FemaleEvenToned, FemaleSultry",
                "OrcRace: FemaleOrc, FemaleCommander",
                "OrcRaceVampire: FemaleOrc, FemaleCommander"
                    ]);
                    Console.WriteLine($"Created default SkyLady Voice Compatibility.txt at {voiceCompatibilityPath}.");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to create SkyLady Voice Compatibility.txt at {voiceCompatibilityPath}. Check write permissions: {ex.Message}");
                }
            }

            // Load configuration files
            var humanoidRaces = new HashSet<string>(File.ReadAllLines(racesPath).Select(line => line.Trim()).Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith("#")));
            var partsToCopy = File.ReadAllLines(partsToCopyPath).ToHashSet().Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith("#")).ToHashSet();
            HashSet<string> blacklistedMods = [.. settings.TemplateModBlacklist.Select(modKey => modKey.FileName.String)];
            var femaleTemplatesByRace = new Dictionary<string, List<INpcGetter>>();
            var successfulTemplatesByRace = new Dictionary<string, List<INpcGetter>>();
            var skippedTemplates = new Dictionary<string, (string ModName, string Reason)>();
            var unpatchedNpcs = new Dictionary<string, string>();
            var filteredNpcs = new Dictionary<string, string>();
            var random = new Random();
            var currentRunTemplates = new Dictionary<string, string>(); // Track all templates assigned in this run

            // Load temporary templates from SkyLadyTempTemplates.json (all NPCs from last run)
            Dictionary<string, string> tempTemplates;
            if (File.Exists(tempTemplatesPath))
            {
                try
                {
                    var json = File.ReadAllText(tempTemplatesPath);
                    tempTemplates = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading SkyLadyTempTemplates.json: {ex.Message}. Starting with an empty temp cache.");
                    tempTemplates = [];
                }
            }
            else
            {
                tempTemplates = [];
            }

            // Build a dictionary of locked NPCs for quick lookup
            var lockedNpcs = settings.LockedTemplates
                .Where(n => !n.Npc.IsNull)
                .ToDictionary(n => n.Npc.FormKey, n => n);

            // Update LockedTemplates with temp templates for display in the UI
            foreach (var entry in lockedNpcs.Values)
            {
                if (entry.Template.IsNull && tempTemplates.TryGetValue(entry.Npc.FormKey.ToString(), out var templateKey) && templateKey != null)
                {
                    if (FormKey.TryFactory(templateKey, out var templateFormKey))
                    {
                        entry.Template = new FormLink<INpcGetter>(templateFormKey);
                    }
                }
            }

            // List to store file copy operations
            var fileCopyOperations = new List<(string SourcePath, string DestPath)>();

            // Load target mods from settings
            HashSet<ModKey> requiemKeys = settings.TargetModsToPatch;
            bool patchEntireLoadOrder = true;
            if (requiemKeys.Any())
            {
                patchEntireLoadOrder = false;
                Console.WriteLine("Patching specific mods from settings:");
                foreach (var mod in requiemKeys)
                {
                    Console.WriteLine($"  {mod.FileName}");
                }
            }
            else
            {
                Console.WriteLine("No target mods specified in settings - patching entire load order.");
            }

            // Cache facegen file existence for NPCs with humanoid races only
            Console.WriteLine("Caching facegen file existence...");
            var facegenCache = new Dictionary<(string ModKey, string FormID), (bool NifExists, bool DdsExists)>();
            foreach (var npc in state.LoadOrder.PriorityOrder.WinningOverrides<INpcGetter>())
            {
                var race = npc.Race.TryResolve(state.LinkCache)?.EditorID;
                if (race != null && humanoidRaces.Contains(race))
                {
                    var modKey = npc.FormKey.ModKey.FileName.ToString();
                    var formId = npc.FormKey.IDString();
                    var nifPath = Path.Combine(state.DataFolderPath, "meshes", "actors", "character", "facegendata", "facegeom", modKey, $"00{formId}.nif");
                    var ddsPath = Path.Combine(state.DataFolderPath, "textures", "actors", "character", "facegendata", "facetint", modKey, $"00{formId}.dds");
                    facegenCache[(modKey, formId)] = (File.Exists(nifPath), File.Exists(ddsPath));
                }
            }
            Console.WriteLine($"Cached facegen existence for {facegenCache.Count} NPCs.");

            // Cache all NPC overrides
            Console.WriteLine("Building NPC override cache...");
            var overrideCache = state.LoadOrder.PriorityOrder.WinningOverrides<INpcGetter>()
                .ToDictionary(n => n.FormKey, n => n);

            // START OF SECTION 2
            // Race compatibility mapping - Load from SkyLady Race Compatibility.txt
            var raceCompatibilityMap = new Dictionary<string, List<string>>();
            try
            {
                var lines = File.ReadAllLines(raceCompatibilityPath);
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#")) continue;

                    var parts = trimmedLine.Split(':');
                    if (parts.Length != 2)
                    {
                        Console.WriteLine($"Invalid race compatibility entry in SkyLady Race Compatibility.txt: {trimmedLine}. Expected format: Race: CompatibleRace1, CompatibleRace2, ...");
                        continue;
                    }

                    var race = parts[0].Trim();
                    var compatibleRaces = parts[1].Split(',')
                        .Select(r => r.Trim())
                        .Where(r => !string.IsNullOrEmpty(r))
                        .ToList();

                    if (compatibleRaces.Count == 0)
                    {
                        Console.WriteLine($"No compatible races defined for {race} in SkyLady Race Compatibility.txt. Skipping entry.");
                        continue;
                    }

                    raceCompatibilityMap[race] = compatibleRaces;
                    Console.WriteLine($"Loaded race compatibility for {race}: {string.Join(", ", compatibleRaces)}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading SkyLady Race Compatibility.txt at {raceCompatibilityPath}. Ensure the file is accessible: {ex.Message}");
            }

            if (raceCompatibilityMap.Count == 0)
            {
                throw new Exception("SkyLady Race Compatibility.txt is empty or invalid. At least one valid race mapping is required.");
            }

            // Validate race EditorIDs against the load order
            Console.WriteLine("Validating race EditorIDs from race compatibility map...");
            var validRaceEditorIDs = state.LoadOrder.PriorityOrder.Race().WinningOverrides()
                .Select(race => race.EditorID)
                .Where(id => !string.IsNullOrEmpty(id))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var validatedRaceCompatibilityMap = new Dictionary<string, List<string>>();
            foreach (var entry in raceCompatibilityMap)
            {
                var race = entry.Key;
                var compatibleRaces = entry.Value;

                if (!validRaceEditorIDs.Contains(race))
                {
                    Console.WriteLine($"Warning: Race EditorID '{race}' in race compatibility map does not exist in the load order. Skipping this entry.");
                    continue;
                }

                var validCompatibleRaces = compatibleRaces
                    .Where(compatibleRace =>
                    {
                        if (validRaceEditorIDs.Contains(compatibleRace))
                        {
                            return true;
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Compatible race EditorID '{compatibleRace}' for race '{race}' in race compatibility map does not exist in the load order. This race will be skipped.");
                            return false;
                        }
                    })
                    .ToList();

                if (validCompatibleRaces.Count == 0)
                {
                    Console.WriteLine($"Warning: No valid compatible races remain for race '{race}' after validation. Skipping this entry.");
                    continue;
                }

                validatedRaceCompatibilityMap[race] = validCompatibleRaces;
            }

            raceCompatibilityMap = validatedRaceCompatibilityMap;
            Console.WriteLine($"Race compatibility map validation complete. {raceCompatibilityMap.Count} valid race entries remain.");

            // Voice compatibility mapping - Load from SkyLady Voice Compatibility.txt
            var voiceTypeMap = new Dictionary<string, List<string>>();
            var raceVoiceFallbacks = new Dictionary<string, List<string>>();
            try
            {
                var lines = File.ReadAllLines(voiceCompatibilityPath);
                bool parsingVoiceMap = false;
                bool parsingRaceFallbacks = false;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#")) continue;

                    if (trimmedLine.Equals("[VoiceMap]", StringComparison.OrdinalIgnoreCase))
                    {
                        parsingVoiceMap = true;
                        parsingRaceFallbacks = false;
                        continue;
                    }
                    else if (trimmedLine.Equals("[RaceVoiceFallbacks]", StringComparison.OrdinalIgnoreCase))
                    {
                        parsingVoiceMap = false;
                        parsingRaceFallbacks = true;
                        continue;
                    }

                    if (parsingVoiceMap)
                    {
                        var parts = trimmedLine.Split(':');
                        if (parts.Length != 2)
                        {
                            Console.WriteLine($"Invalid voice map entry in SkyLady Voice Compatibility.txt: {trimmedLine}. Expected format: MaleVoice: FemaleVoice1, FemaleVoice2, ...");
                            continue;
                        }

                        var maleVoice = parts[0].Trim();
                        var femaleVoices = parts[1].Split(',')
                            .Select(v => v.Trim())
                            .Where(v => !string.IsNullOrEmpty(v))
                            .ToList();

                        if (femaleVoices.Count == 0)
                        {
                            Console.WriteLine($"No female voices defined for {maleVoice} in SkyLady Voice Compatibility.txt. Skipping entry.");
                            continue;
                        }

                        voiceTypeMap[maleVoice] = femaleVoices;
                        Console.WriteLine($"Loaded voice mapping for {maleVoice}: {string.Join(", ", femaleVoices)}");
                    }
                    else if (parsingRaceFallbacks)
                    {
                        var parts = trimmedLine.Split(':');
                        if (parts.Length != 2)
                        {
                            Console.WriteLine($"Invalid race voice fallback entry in SkyLady Voice Compatibility.txt: {trimmedLine}. Expected format: Race: Voice1, Voice2, ...");
                            continue;
                        }

                        var race = parts[0].Trim();
                        var voices = parts[1].Split(',')
                            .Select(v => v.Trim())
                            .Where(v => !string.IsNullOrEmpty(v))
                            .ToList();

                        if (voices.Count == 0)
                        {
                            Console.WriteLine($"No voices defined for race {race} in SkyLady Voice Compatibility.txt. Skipping entry.");
                            continue;
                        }

                        raceVoiceFallbacks[race] = voices;
                        Console.WriteLine($"Loaded race voice fallbacks for {race}: {string.Join(", ", voices)}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading SkyLady Voice Compatibility.txt at {voiceCompatibilityPath}. Ensure the file is accessible: {ex.Message}");
            }

            if (voiceTypeMap.Count == 0 && raceVoiceFallbacks.Count == 0)
            {
                throw new Exception("SkyLady Voice Compatibility.txt is empty or invalid. At least one valid voice mapping or fallback is required.");
            }

            // Collect female templates and count male NPCs (excluding Player and presets)
            int maleNpcCount = 0;
            int eligibleMaleNpcCount = 0;
            int successfulPatches = 0;
            int skippedDueToPatch = 0;
            int skippedDueToFilter = 0;
            var blacklistedMaleNpcsByMod = new Dictionary<ModKey, int>();
            foreach (var npc in state.LoadOrder.PriorityOrder.WinningOverrides<INpcGetter>())
            {
                var race = npc.Race.TryResolve(state.LinkCache)?.EditorID;
                if (race != null && humanoidRaces.Contains(race))
                {
                    if (npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female))
                    {
                        bool isVanillaPlugin = npc.FormKey.ModKey.FileName.Equals("Skyrim.esm") ||
                            npc.FormKey.ModKey.FileName.Equals("Dawnguard.esm") ||
                            npc.FormKey.ModKey.FileName.Equals("Dragonborn.esm") ||
                            npc.FormKey.ModKey.FileName.Equals("Update.esm") ||
                            npc.FormKey.ModKey.FileName.Equals("HearthFires.esm");
                        bool notBlacklisted = !blacklistedMods.Contains(npc.FormKey.ModKey.FileName);
                        var (nifExists, ddsExists) = facegenCache[(npc.FormKey.ModKey.FileName.ToString(), npc.FormKey.IDString())];
                        bool condition = notBlacklisted && (isVanillaPlugin || (nifExists && ddsExists));
                        if (condition)
                        {
                            femaleTemplatesByRace[race] = femaleTemplatesByRace.GetValueOrDefault(race, []);
                            femaleTemplatesByRace[race].Add(npc);
                        }
                    }
                    else if (patchEntireLoadOrder || requiemKeys.Contains(npc.FormKey.ModKey))
                    {
                        if (npc.EditorID != null && (npc.EditorID.Equals("Player", StringComparison.OrdinalIgnoreCase) ||
                            npc.EditorID.Contains("preset", StringComparison.OrdinalIgnoreCase)))
                        {
                            filteredNpcs[npc.EditorID + " (" + npc.FormKey.IDString() + ")"] = "Filtered (Player/Preset)";
                            skippedDueToFilter++;
                            continue;
                        }
                        maleNpcCount++;
                        if (settings.ModsToExcludeFromPatching.Contains(npc.FormKey.ModKey))
                        {
                            blacklistedMaleNpcsByMod[npc.FormKey.ModKey] = blacklistedMaleNpcsByMod.GetValueOrDefault(npc.FormKey.ModKey, 0) + 1;
                            continue;
                        }
                        if (settings.NpcsToExcludeFromPatching.Any(excludedNpc => excludedNpc.FormKey == npc.FormKey))
                        {
                            blacklistedMaleNpcsByMod[npc.FormKey.ModKey] = blacklistedMaleNpcsByMod.GetValueOrDefault(npc.FormKey.ModKey, 0) + 1;
                            continue;
                        }
                        eligibleMaleNpcCount++;
                        Console.WriteLine($"Found male NPC: {npc.EditorID ?? "Unnamed"} ({npc.FormKey.IDString()}) (Race: {race})");
                    }
                }
            }
            Console.WriteLine($"Collected templates for {femaleTemplatesByRace.Count} races.");
            Console.WriteLine($"Total male humanoid NPCs in {(patchEntireLoadOrder ? "entire load order" : "target mods")}: {maleNpcCount}");
            Console.WriteLine($"Skipped due to already patched: {skippedDueToPatch}");
            Console.WriteLine($"Skipped due to Player/Preset filter: {skippedDueToFilter}");

            foreach (var race in femaleTemplatesByRace.Keys.OrderBy(r => r))
            {
                Console.WriteLine($"Found {femaleTemplatesByRace[race].Count} female templates for race {race}");
            }

            // SECTION 3 and 4: Unified NPC patching loop
            int totalSingleNpcs = settings.NpcsToPatch.Count;
            var patchedNpcs = new HashSet<FormKey>();

            foreach (var npc in state.LoadOrder.PriorityOrder.WinningOverrides<INpcGetter>())
            {
                if (patchedNpcs.Contains(npc.FormKey))
                    continue;

                var isFemale = npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female);
                var isPlayer = npc.EditorID?.Equals("Player", StringComparison.OrdinalIgnoreCase) ?? false;
                var isPreset = npc.EditorID?.ToLowerInvariant().Contains("preset") ?? false;
                var race = npc.Race.TryResolve(state.LinkCache)?.EditorID;

                if (race == null || !humanoidRaces.Contains(race) || isFemale || isPlayer || isPreset)
                {
                    skippedDueToFilter++;
                    continue;
                }

                bool shouldPatchNew = true;
                bool isLocked = lockedNpcs.ContainsKey(npc.FormKey);

                // Skip mode and blacklist checks for locked NPCs
                if (!isLocked)
                {
                    if (!patchEntireLoadOrder && !requiemKeys.Contains(npc.FormKey.ModKey))
                        continue;

                    if (settings.PatchSingleNpcOnly)
                    {
                        if (!settings.NpcsToPatch.Any(n => n.Npc.FormKey == npc.FormKey))
                        {
                            shouldPatchNew = false;
                        }
                    }
                    else
                    {
                        if (settings.PreserveLastRunAppearances)
                        {
                            shouldPatchNew = false;
                        }
                    }

                    if (settings.ModsToExcludeFromPatching.Contains(npc.FormKey.ModKey))
                        continue;
                    if (settings.NpcsToExcludeFromPatching.Any(ex => ex.FormKey == npc.FormKey))
                        continue;
                }

                var npcFid = npc.FormKey.IDString();
                if (race == "DA13AfflictedRace")
                {
                    Console.WriteLine($"Processing Afflicted NPC: {npc.EditorID ?? "Unnamed"} ({npcFid})");
                }

                var compatibleRaces = raceCompatibilityMap.TryGetValue(race, out var races) ? races : [race];
                var templates = compatibleRaces
                    .SelectMany(r => femaleTemplatesByRace.TryGetValue(r, out var t) ? t : [])
                    .ToList();

                if (templates.Count == 0 && settings.UseDefaultRaceFallback)
                {
                    Console.WriteLine($"No templates found for race {race} for NPC {npc.EditorID ?? "Unnamed"} ({npc.FormKey}). Using default race fallback (NordRace, ImperialRace).");
                    templates = [.. new List<string> { "NordRace", "ImperialRace" }.SelectMany(r => femaleTemplatesByRace.TryGetValue(r, out var t) ? t : [])];
                }

                if (templates.Count > 0)
                {
                    INpcGetter? template = null;
                    bool useLockedTemplate = false;
                    bool facegenCopied = false;
                    Npc? patchedNpc = null;

                    if (isLocked || !shouldPatchNew)
                    {
                        if (tempTemplates.TryGetValue(npc.FormKey.ToString(), out var tempTemplateKey)
                            && FormKey.TryFactory(tempTemplateKey, out var tempFormKey)
                            && state.LinkCache.TryResolve<INpcGetter>(tempFormKey, out var tempLockedTemplate))
                        {
                            var tempLockedTemplateRace = tempLockedTemplate.Race.TryResolve(state.LinkCache)?.EditorID;
                            if (tempLockedTemplateRace != null && compatibleRaces.Contains(tempLockedTemplateRace))
                            {
                                template = tempLockedTemplate;
                                useLockedTemplate = true;
                                Console.WriteLine($"[{(isLocked ? "Locked" : "Preserved")}] Reusing temp template for {npc.EditorID ?? "Unnamed"}: {template.EditorID ?? "Unnamed"} ({template.FormKey}) from {template.FormKey.ModKey.FileName}");
                            }
                            else
                            {
                                Console.WriteLine($"[{(isLocked ? "Locked" : "Preserved")}] Temp template {tempTemplateKey} is invalid for {npc.EditorID ?? "Unnamed"} (race {race}). Will assign new template now.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[{(isLocked ? "Locked" : "Preserved")}] No valid temp template found for {npc.EditorID ?? "Unnamed"}. Will assign new template now.");
                        }
                    }

                    DeleteExistingFacegenFiles(modFolderPath, npc.FormKey);

                    patchedNpc = state.PatchMod.Npcs.GetOrAddAsOverride(npc);
                    if (patchedNpc == null)
                    {
                        Console.WriteLine($"Failed to create patched NPC for {npc.EditorID ?? "Unnamed"} ({npc.FormKey}) — GetOrAddAsOverride returned null");
                        continue;
                    }

                    if (overrideCache.TryGetValue(npc.FormKey, out var cachedOverride) && !cachedOverride.Equals(patchedNpc))
                    {
                        patchedNpc.Configuration = cachedOverride.Configuration.DeepCopy();
                        patchedNpc.Keywords = (cachedOverride.Keywords ?? []).ToExtendedList();
                        if (patchedNpc.Items != null && cachedOverride.Items != null)
                        {
                            patchedNpc.Items.Clear();
                            patchedNpc.Items.AddRange(cachedOverride.Items.Select(i => i.DeepCopy()));
                        }
                        patchedNpc.Packages.Clear();
                        patchedNpc.Packages.AddRange(cachedOverride.Packages);
                        if (patchedNpc.Perks != null && cachedOverride.Perks != null)
                        {
                            patchedNpc.Perks.Clear();
                            patchedNpc.Perks.AddRange(cachedOverride.Perks.Select(p => p.DeepCopy()));
                        }
                        patchedNpc.Factions.Clear();
                        if (cachedOverride.Factions != null)
                            patchedNpc.Factions.AddRange(cachedOverride.Factions.Select(f => f.DeepCopy()));
                    }

                    if (!useLockedTemplate)
                    {
                        var validTemplates = templates.ToList();
                        if (validTemplates.Count > 1)
                        {
                            for (int i = validTemplates.Count - 1; i > 0; i--)
                            {
                                int j = random.Next(i + 1);
                                (validTemplates[i], validTemplates[j]) = (validTemplates[j], validTemplates[i]);
                            }
                        }

                        foreach (var candidate in validTemplates)
                        {
                            template = candidate;
                            var templateFid = template.FormKey.IDString();
                            var templateFileName = template.FormKey.ModKey.FileName.ToString();
                            var templateRace = template.Race.TryResolve(state.LinkCache)?.EditorID;
                            var bsaPath = Path.Combine(state.DataFolderPath, templateFileName.Replace(".esm", ".bsa").Replace(".esp", ".bsa"));

                            // Allow vanilla plugins to bypass facegen/BSA check
                            bool isVanillaPlugin = templateFileName.Equals("Skyrim.esm") ||
                                templateFileName.Equals("Dawnguard.esm") ||
                                templateFileName.Equals("Dragonborn.esm") ||
                                templateFileName.Equals("Update.esm") ||
                                templateFileName.Equals("HearthFires.esm");

                            if (!isVanillaPlugin && (File.Exists(bsaPath) || blacklistedMods.Contains(templateFileName)))
                            {
                                Console.WriteLine($"Skipping template {template.EditorID ?? "Unnamed"} ({template.FormKey}) from {templateFileName} (BSA or blacklisted)");
                                continue;
                            }

                            if (partsToCopy.Contains("PNAM") && template.HeadParts != null) patchedNpc.HeadParts.SetTo(template.HeadParts);
                            if (partsToCopy.Contains("WNAM") && template.WornArmor != null) patchedNpc.WornArmor.SetTo(template.WornArmor);
                            if (partsToCopy.Contains("QNAM")) patchedNpc.TextureLighting = template.TextureLighting;
                            if (partsToCopy.Contains("NAM9") && template.FaceMorph != null) patchedNpc.FaceMorph = template.FaceMorph.DeepCopy();
                            if (partsToCopy.Contains("NAMA") && template.FaceParts != null) patchedNpc.FaceParts = template.FaceParts.DeepCopy();
                            if (partsToCopy.Contains("Tint Layers") && template.TintLayers != null) patchedNpc.TintLayers.SetTo(template.TintLayers.Select(t => t.DeepCopy()));
                            if (partsToCopy.Contains("FTST") && template.HeadTexture != null) patchedNpc.HeadTexture.SetTo(template.HeadTexture);
                            if (partsToCopy.Contains("HCLF") && template.HairColor != null) patchedNpc.HairColor.SetTo(template.HairColor);

                            patchedNpc.Configuration.Flags |= NpcConfiguration.Flag.Female;

                            if (npc.Voice != null)
                            {
                                var voiceType = npc.Voice.TryResolve(state.LinkCache)?.EditorID;
                                if (!string.IsNullOrEmpty(voiceType))
                                {
                                    bool isFemaleVoice = voiceType.Contains("Female", StringComparison.OrdinalIgnoreCase) ||
                                        voiceTypeMap.Any(kvp => kvp.Value.Contains(voiceType)) ||
                                        raceVoiceFallbacks.Any(kvp => kvp.Value.Contains(voiceType));

                                    if (!isFemaleVoice)
                                    {
                                        if (voiceTypeMap.TryGetValue(voiceType, out var femaleVoiceIDs) && femaleVoiceIDs.Count > 0)
                                        {
                                            var selectedFemaleVoiceID = femaleVoiceIDs[random.Next(femaleVoiceIDs.Count)];
                                            var femaleVoice = state.LoadOrder.PriorityOrder.VoiceType().WinningOverrides()
                                                .FirstOrDefault(vt => vt.EditorID == selectedFemaleVoiceID);
                                            if (femaleVoice != null)
                                                patchedNpc.Voice.SetTo(femaleVoice);
                                        }
                                        else if (raceVoiceFallbacks.TryGetValue(race, out var fallbackVoices) && fallbackVoices.Count > 0)
                                        {
                                            var selectedVoice = fallbackVoices[random.Next(fallbackVoices.Count)];
                                            var fallback = state.LoadOrder.PriorityOrder.VoiceType().WinningOverrides()
                                                .FirstOrDefault(vt => vt.EditorID == selectedVoice);
                                            if (fallback != null)
                                                patchedNpc.Voice.SetTo(fallback);
                                        }
                                    }
                                }
                            }

                            patchedNpc.Height = template.Height != 0.0f ? template.Height : 1.0f;
                            patchedNpc.Weight = template.Weight != 0.0f ? template.Weight : 50.0f;

                            break;
                        }
                    }
                    else
                    {
                        if (template != null)
                        {
                            if (partsToCopy.Contains("PNAM") && template.HeadParts != null) patchedNpc.HeadParts.SetTo(template.HeadParts);
                            if (partsToCopy.Contains("WNAM") && template.WornArmor != null) patchedNpc.WornArmor.SetTo(template.WornArmor);
                            if (partsToCopy.Contains("QNAM")) patchedNpc.TextureLighting = template.TextureLighting;
                            if (partsToCopy.Contains("NAM9") && template.FaceMorph != null) patchedNpc.FaceMorph = template.FaceMorph.DeepCopy();
                            if (partsToCopy.Contains("NAMA") && template.FaceParts != null) patchedNpc.FaceParts = template.FaceParts.DeepCopy();
                            if (partsToCopy.Contains("Tint Layers") && template.TintLayers != null) patchedNpc.TintLayers.SetTo(template.TintLayers.Select(t => t.DeepCopy()));
                            if (partsToCopy.Contains("FTST") && template.HeadTexture != null) patchedNpc.HeadTexture.SetTo(template.HeadTexture);
                            if (partsToCopy.Contains("HCLF") && template.HairColor != null) patchedNpc.HairColor.SetTo(template.HairColor);

                            patchedNpc.Configuration.Flags |= NpcConfiguration.Flag.Female;

                            if (npc.Voice != null)
                            {
                                var voiceType = npc.Voice.TryResolve(state.LinkCache)?.EditorID;
                                if (!string.IsNullOrEmpty(voiceType))
                                {
                                    bool isFemaleVoice = voiceType.Contains("Female", StringComparison.OrdinalIgnoreCase) ||
                                        voiceTypeMap.Any(kvp => kvp.Value.Contains(voiceType)) ||
                                        raceVoiceFallbacks.Any(kvp => kvp.Value.Contains(voiceType));

                                    if (!isFemaleVoice)
                                    {
                                        if (voiceTypeMap.TryGetValue(voiceType, out var femaleVoiceIDs) && femaleVoiceIDs.Count > 0)
                                        {
                                            var selectedFemaleVoiceID = femaleVoiceIDs[random.Next(femaleVoiceIDs.Count)];
                                            var femaleVoice = state.LoadOrder.PriorityOrder.VoiceType().WinningOverrides()
                                                .FirstOrDefault(vt => vt.EditorID == selectedFemaleVoiceID);
                                            if (femaleVoice != null)
                                                patchedNpc.Voice.SetTo(femaleVoice);
                                        }
                                        else if (raceVoiceFallbacks.TryGetValue(race, out var fallbackVoices) && fallbackVoices.Count > 0)
                                        {
                                            var selectedVoice = fallbackVoices[random.Next(fallbackVoices.Count)];
                                            var fallback = state.LoadOrder.PriorityOrder.VoiceType().WinningOverrides()
                                                .FirstOrDefault(vt => vt.EditorID == selectedVoice);
                                            if (fallback != null)
                                                patchedNpc.Voice.SetTo(fallback);
                                        }
                                    }
                                }
                            }

                            patchedNpc.Height = template.Height != 0.0f ? template.Height : 1.0f;
                            patchedNpc.Weight = template.Weight != 0.0f ? template.Weight : 50.0f;
                        }
                    }

                    if (template != null)
                    {
                        var templateFid = template.FormKey.IDString();
                        var templateFileName = template.FormKey.ModKey.FileName.ToString();
                        var templateRace = template.Race.TryResolve(state.LinkCache)?.EditorID;

                        var patchedNif = Path.Combine(modFolderPath, "meshes", "actors", "character", "facegendata", "facegeom", npc.FormKey.ModKey.FileName, $"00{npcFid}.nif");
                        var patchedDds = Path.Combine(modFolderPath, "textures", "actors", "character", "facegendata", "facetint", npc.FormKey.ModKey.FileName, $"00{npcFid}.dds");
                        var templateNif = Path.Combine(state.DataFolderPath, "meshes", "actors", "character", "facegendata", "facegeom", templateFileName, $"00{templateFid}.nif");
                        var templateDds = Path.Combine(state.DataFolderPath, "textures", "actors", "character", "facegendata", "facetint", templateFileName, $"00{templateFid}.dds");

                        bool nifExists = facegenCache[(templateFileName, templateFid)].NifExists;
                        bool ddsExists = facegenCache[(templateFileName, templateFid)].DdsExists;

                        // Allow vanilla plugins to bypass facegen copying
                        bool isVanillaPlugin = templateFileName.Equals("Skyrim.esm") ||
                            templateFileName.Equals("Dawnguard.esm") ||
                            templateFileName.Equals("Dragonborn.esm") ||
                            templateFileName.Equals("Update.esm") ||
                            templateFileName.Equals("HearthFires.esm");

                        if (isVanillaPlugin)
                        {
                            var nifDir = Path.GetDirectoryName(patchedNif) ?? throw new InvalidOperationException("NIF path directory is null");
                            var ddsDir = Path.GetDirectoryName(patchedDds) ?? throw new InvalidOperationException("DDS path directory is null");
                            Directory.CreateDirectory(nifDir);
                            Directory.CreateDirectory(ddsDir);
                            facegenCopied = true;
                            Console.WriteLine($"Assumed vanilla facegen for template {template.EditorID ?? "Unnamed"} ({templateFid}) from {templateFileName}");
                        }
                        else if (nifExists && ddsExists)
                        {
                            fileCopyOperations.Add((templateNif, patchedNif));
                            fileCopyOperations.Add((templateDds, patchedDds));
                            facegenCopied = true;
                        }
                        else
                        {
                            Console.WriteLine($"Skipping template {template.EditorID ?? "Unnamed"} ({template.FormKey}) from {templateFileName} — missing facegen files (.nif: {nifExists}, .dds: {ddsExists})");
                            continue;
                        }
                    }

                    if (facegenCopied)
                    {
                        successfulPatches++;
                        patchedNpcs.Add(npc.FormKey);

                        currentRunTemplates[npc.FormKey.ToString()] = template?.FormKey.ToString() ?? "";
                        Console.WriteLine($"Patched: {npc.EditorID ?? "Unnamed"} ({npc.FormKey}) using {template?.EditorID ?? "Unknown"} from {template?.FormKey.ModKey.FileName}");
                    }
                    else
                    {
                        Console.WriteLine($"Skipped: {npc.EditorID ?? "Unnamed"} ({npc.FormKey}) — no facegen copied");
                    }
                }
            }

            // START OF SECTION 5
            // Perform final batch copy for any remaining files
            if (fileCopyOperations.Count > 0)
            {
                Console.WriteLine($"Performing final batch file copy for {fileCopyOperations.Count} files...");
                BatchCopyFiles(fileCopyOperations);
                fileCopyOperations.Clear();
            }

            // Save current run templates to SkyLadyTempTemplates.json
            if (currentRunTemplates.Any())
            {
                try
                {
                    var directory = Path.GetDirectoryName(tempTemplatesPath) ?? throw new Exception("Cannot determine directory for SkyLadyTempTemplates.json.");
                    Directory.CreateDirectory(directory);
                    var json = JsonSerializer.Serialize(currentRunTemplates, new JsonSerializerOptions { WriteIndented = true });
                    using (var stream = new FileStream(tempTemplatesPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(json);
                    }
                    Console.WriteLine($"Saved temporary templates to {tempTemplatesPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving SkyLadyTempTemplates.json: {ex.Message}");
                }
            }

            if (skippedTemplates.Count != 0)
            {
                Console.WriteLine("\nSummary of Skipped Templates:");
                foreach (KeyValuePair<string, (string ModName, string Reason)> entry in skippedTemplates)
                {
                    string templateId = entry.Key;
                    (string modName, string reason) = entry.Value;
                    Console.WriteLine($"- Template: {templateId}, Mod: {modName}, Reason: {reason}");
                }
                Console.WriteLine("If you encounter issues with these templates, consider adding the listed mods to the 'Template Mod Blacklist' in the patcher settings.");
            }

            if (filteredNpcs.Count != 0)
            {
                Console.WriteLine("\nFiltered NPCs (Excluded from Patching):");
                foreach (KeyValuePair<string, string> entry in filteredNpcs)
                {
                    string npcId = entry.Key;
                    string reason = entry.Value;
                    Console.WriteLine($"- NPC: {npcId}, Reason: {reason}");
                }
            }

            if (unpatchedNpcs.Count != 0)
            {
                Console.WriteLine("\nUnpatched NPCs:");
                foreach (var (npcId, reason) in unpatchedNpcs)
                {
                    Console.WriteLine($"- NPC: {npcId}, Reason: {reason}");
                }
            }

            if (settings.PatchSingleNpcOnly)
            {
                Console.WriteLine($"Successfully patched {successfulPatches} out of {totalSingleNpcs} selected NPCs.");
            }
            else
            {
                Console.WriteLine($"Successfully patched {successfulPatches} out of {eligibleMaleNpcCount} male NPCs with facegen.");
                if (blacklistedMaleNpcsByMod.Count > 0)
                {
                    Console.WriteLine("\nBlacklisted Male NPCs:");
                    foreach (var entry in blacklistedMaleNpcsByMod.OrderBy(e => e.Key.FileName.String))
                    {
                        Console.WriteLine($"- {entry.Key.FileName}: {entry.Value}");
                    }
                    int totalBlacklisted = blacklistedMaleNpcsByMod.Values.Sum();
                    Console.WriteLine($"Total Blacklisted Male NPCs: {totalBlacklisted}");
                    Console.WriteLine($"Total Male Humanoid NPCs in Entire Load Order: {maleNpcCount}");
                }
            }

            // Add a small delay to ensure file handles are released before Synthesis writes the output
            Thread.Sleep(2000);

            // Check if splitting is needed based on the number of masters
            var contributingMods = new HashSet<ModKey>();
            foreach (var rec in state.PatchMod.EnumerateMajorRecords())
            {
                if (!rec.FormKey.ModKey.Equals(state.PatchMod.ModKey))
                {
                    contributingMods.Add(rec.FormKey.ModKey);
                }
            }
            var masterCount = contributingMods.Count;
            Console.WriteLine($"Calculated master count: {masterCount} (based on contributing mods)");
            if (masterCount <= 250)
            {
                // No splitting needed; let Synthesis handle the output naturally
                Console.WriteLine("No ESP splitting needed (master count under 250). Letting Synthesis write the output ESP.");

                if (settings.FlagOutputAsEsl)
                {
                    bool canBeEsl = true;
                    uint newRecordCount = 0;

                    foreach (var rec in state.PatchMod.EnumerateMajorRecords())
                    {
                        if (rec.FormKey.ModKey.Equals(state.PatchMod.ModKey))
                        {
                            newRecordCount++;
                            if (rec.FormKey.ID < 0x800 || rec.FormKey.ID > 0xFFF)
                            {
                                canBeEsl = false;
                                Console.WriteLine($"Cannot flag output ESP as ESL: New record {rec.FormKey} has FormID outside ESL range (0x800 to 0xFFF).");
                                break;
                            }
                        }
                    }

                    if (newRecordCount > 2048)
                    {
                        canBeEsl = false;
                        Console.WriteLine($"Cannot flag output ESP as ESL: Exceeds 2048 new records (found {newRecordCount}).");
                    }

                    if (canBeEsl)
                    {
                        state.PatchMod.ModHeader.Flags |= SkyrimModHeader.HeaderFlag.Small;
                        Console.WriteLine($"Flagged output ESP as ESL.");
                    }
                }

                var recordCount = state.PatchMod.EnumerateMajorRecords().Count();
                Console.WriteLine($"Prepared single ESP for Synthesis output: Masters: {masterCount}, Records: {recordCount}");
            }
            else
            {
                var splitter = new MultiModFileSplitter();
                var splitMods = splitter.Split<ISkyrimMod, ISkyrimModGetter>(state.PatchMod, 250).ToList();
                Console.WriteLine($"Split into {splitMods.Count} mods:");

                foreach (var mod in splitMods)
                {
                    mod.MasterReferences.Clear();
                    mod.MasterReferences.AddRange(state.PatchMod.MasterReferences.Select(m => m.DeepCopy()));
                }

                if (settings.FlagOutputAsEsl)
                {
                    foreach (var mod in splitMods)
                    {
                        bool canBeEsl = true;
                        uint newRecordCount = 0;

                        foreach (var rec in mod.EnumerateMajorRecords())
                        {
                            if (rec.FormKey.ModKey.Equals(mod.ModKey))
                            {
                                newRecordCount++;
                                if (rec.FormKey.ID < 0x800 || rec.FormKey.ID > 0xFFF)
                                {
                                    canBeEsl = false;
                                    Console.WriteLine($"Cannot flag split ESP as ESL: New record {rec.FormKey} has FormID outside ESL range (0x800 to 0xFFF).");
                                    break;
                                }
                            }
                        }

                        if (newRecordCount > 2048)
                        {
                            canBeEsl = false;
                            Console.WriteLine($"Cannot flag split ESP as ESL: Exceeds 2048 new records (found {newRecordCount}).");
                        }

                        if (canBeEsl)
                        {
                            mod.ModHeader.Flags |= SkyrimModHeader.HeaderFlag.Small;
                            Console.WriteLine($"Flagged split ESP as ESL.");
                        }
                    }
                }

                for (int i = 0; i < splitMods.Count; i++)
                {
                    var mod = splitMods[i];
                    var originalFileName = mod.ModKey.FileName.ToString();

                    string outputFileName = i == 0
                        ? state.PatchMod.ModKey.FileName.ToString()
                        : $"{state.PatchMod.ModKey.FileName.ToString().Replace(".esp", "")}_{i + 1}.esp";
                    Console.WriteLine($"Using Synthesis naming: {outputFileName}");

                    var splitMasterCount = mod.MasterReferences.Count;
                    var recordCount = mod.EnumerateMajorRecords().Count();
                    Console.WriteLine($"Mod {i}: {outputFileName}, Records: {recordCount}");

                    mod.WriteToBinary(
                        Path.Combine(state.DataFolderPath, outputFileName),
                        new BinaryWriteParameters { ModKey = ModKeyOption.NoCheck });
                }
                Console.WriteLine("Data Folder Path: " + state.DataFolderPath);
                throw new Exception("This error indicates that the patcher ran successfully. The final ESP was split due to the 254-master limit. This error is intentional to prevent Synthesis from crashing and will be removed once ESP splitting is officially implemented in the Synthesis application.");
            }
        }
    }
}