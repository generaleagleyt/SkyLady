// START OF SECTION 1
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
        [SynthesisTooltip("The NPC whose template should be locked. Search and select an NPC to lock its template.")]
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
        [SynthesisSettingName("Patch Single NPC Only")]
        [SynthesisTooltip("If enabled, the patcher will only process the NPCs selected below. If disabled, it will patch all male NPCs in the target mods or entire load order.")]
        public bool PatchSingleNpcOnly { get; set; } = false;

        [SynthesisSettingName("NPCs to Patch")]
        [SynthesisTooltip("Select the NPCs to patch. Leave empty to patch all NPCs in the load order.")]
        public List<SkyLadyNpc> NpcsToPatch { get; set; } = [];

        [SynthesisSettingName("Use Default Race Fallback")]
        [SynthesisTooltip("If enabled, custom races with no female templates will use NordRace and ImperialRace templates as a fallback. If disabled, a matching race is required.")]
        public bool UseDefaultRaceFallback { get; set; } = false;

        [SynthesisSettingName("NPCs with Locked Templates")]
        [SynthesisTooltip("Specify NPCs whose templates should be locked. The patcher will automatically cache the last applied template for these NPCs and reuse it on subsequent runs.")]
        public List<LockedNpcTemplate> LockedTemplates { get; set; } = [];

        [SynthesisSettingName("Template Mod Blacklist")]
        [SynthesisTooltip("Mods to exclude from template collection (e.g., mods with vanilla looks or .nif issues).")]
        public HashSet<ModKey> TemplateModBlacklist { get; set; } = [];

        [SynthesisSettingName("Target Mods to Patch")]
        [SynthesisTooltip("Select the mods to patch. Leave empty to patch the entire load order.")]
        public HashSet<ModKey> TargetModsToPatch { get; set; } = [];

        [SynthesisSettingName("Mods to Exclude from Patching")]
        [SynthesisTooltip("Select mods to exclude from patching (e.g., mods with unique NPCs you want to preserve).")]
        public HashSet<ModKey> ModsToExcludeFromPatching { get; set; } = [];

        [SynthesisSettingName("NPCs to Exclude from Patching")]
        [SynthesisTooltip("Select individual NPCs to exclude from patching (e.g., unique NPCs you want to preserve).")]
        public List<IFormLinkGetter<INpcGetter>> NpcsToExcludeFromPatching { get; set; } = [];

        [SynthesisSettingName("Flag Output Plugins as ESL")]
        [SynthesisTooltip("If enabled, the output plugins will be flagged as ESL (Light Master) if they meet the eligibility criteria (max 2048 new records).")]
        public bool FlagOutputAsEsl { get; set; } = false;

        [SynthesisSettingName("Append Suffix to Output Filenames")]
        [SynthesisTooltip("If enabled, the output plugins will have the specified suffix (or a timestamp if none is provided) appended to their filenames (e.g., SkyLady Patcher_Main.esp). If disabled, the default naming will be used (e.g., SkyLady Patcher.esp).")]
        public bool AppendSuffixToOutput { get; set; } = false;

        [SynthesisSettingName("Output Filename Suffix")]
        [SynthesisTooltip("Enter a custom suffix to append to the output filenames (e.g., 'Main' for SkyLady Patcher_Main.esp). Leave empty to use a timestamp (YYYYMMDD_HHmm).")]
        public string OutputNameSuffix { get; set; } = "";

        // Deprecated: Kept for backward compatibility, but hidden from GUI
        [SynthesisIgnoreSetting]
        public string SingleNpcBaseId { get; set; } = "";
    }

    public class Program
    {
        private static readonly char[] LineSeparators = ['\n', '\r'];
        private static readonly FormKey SkyLadyPatched = FormKey.Factory("000800:SkyLadyKeywords.esp");

        // Define a variable to hold the settings
        static Lazy<PatcherSettings> Settings = null!;

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(Patch)
                // Register the settings class and specify the settings file path
                .SetAutogeneratedSettings(
                    nickname: "Settings",
                    path: "settings.json",
                    out Settings)
                .SetTypicalOpen(GameRelease.SkyrimSE, "SkyLady.esp")
                .Run(args);
        }

        // Helper method to perform batch file copying
        private static void BatchCopyFiles(List<(string SourcePath, string DestPath)> fileCopyOperations)
        {
            if (fileCopyOperations.Count == 0) return;

            // Extract unique destination directories
            var directories = fileCopyOperations
                .Select(op => Path.GetDirectoryName(op.DestPath))
                .Distinct()
                .ToList();

            // Create all directories
            foreach (var dir in directories)
            {
                if (dir != null)
                {
                    Directory.CreateDirectory(dir);
                }
            }

            // Copy all files
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

            // Dynamically locate the SkyLady mod folder by searching for SkyLadyKeywords.esp
            var modsBasePath = Path.Combine(state.DataFolderPath, "..", "..", "mods");
            if (!Directory.Exists(modsBasePath))
            {
                throw new Exception($"Cannot find mods directory at {modsBasePath}. Ensure the patcher is running in the correct environment.");
            }

            string? modFolderPath = null;
            foreach (var dir in Directory.GetDirectories(modsBasePath))
            {
                var espPath = Path.Combine(dir, "SkyLadyKeywords.esp");
                if (File.Exists(espPath))
                {
                    modFolderPath = dir;
                    break;
                }
            }

            if (modFolderPath == null)
            {
                throw new Exception("Cannot find SkyLady mod folder containing SkyLadyKeywords.esp. Ensure the mod is installed correctly.");
            }

            // Define paths using the dynamically located mod folder
            var racesPath = Path.Combine(modFolderPath, "SkyLady races.txt");
            var raceCompatibilityPath = Path.Combine(modFolderPath, "SkyLady Race Compatibility.txt");
            var partsToCopyPath = Path.Combine(modFolderPath, "SkyLady partsToCopy.txt");
            var tempTemplatesPath = Path.Combine(modFolderPath, "SkyLadyTempTemplates.json"); // Store in mod folder
            var humanoidRaces = new HashSet<string>(File.ReadAllLines(racesPath).Select(line => line.Trim()));
            var partsToCopy = File.ReadAllLines(partsToCopyPath).ToHashSet();
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
                if (entry.Template.IsNull && tempTemplates.TryGetValue(entry.Npc.FormKey.ToString(), out string? templateKey) && templateKey != null)
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
            foreach (var npc in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
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
            var overrideCache = state.LoadOrder.PriorityOrder.Npc()
                .WinningOverrides()
                .ToDictionary(n => n.FormKey, n => n);

            // START OF SECTION 2
            // Race compatibility mapping - Load from SkyLady Race Compatibility.txt if it exists
            var raceCompatibilityMap = new Dictionary<string, List<string>>();
            if (File.Exists(raceCompatibilityPath))
            {
                try
                {
                    var lines = File.ReadAllLines(raceCompatibilityPath);
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#")) continue; // Skip empty lines or comments

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
                    Console.WriteLine($"Error reading SkyLady Race Compatibility.txt: {ex.Message}. Falling back to default race compatibility map.");
                }
            }

            // If the file doesn't exist or failed to load, use the default hardcoded map
            if (raceCompatibilityMap.Count == 0)
            {
                Console.WriteLine("Using default race compatibility map.");
                raceCompatibilityMap = new Dictionary<string, List<string>>
        {
            { "NordRace", new List<string> { "NordRace", "NordRaceVampire", "HothRace", "ImperialRace", "ImperialRaceVampire" } },
            { "NordRaceVampire", new List<string> { "NordRace", "NordRaceVampire", "HothRace", "ImperialRace", "ImperialRaceVampire" } },
            { "HothRace", new List<string> { "NordRace", "NordRaceVampire", "HothRace", "ImperialRace", "ImperialRaceVampire" } },
            { "ImperialRace", new List<string> { "ImperialRace", "ImperialRaceVampire", "NordRace", "NordRaceVampire", "HothRace" } },
            { "ImperialRaceVampire", new List<string> { "ImperialRace", "ImperialRaceVampire", "NordRace", "NordRaceVampire", "HothRace" } },
            { "DarkElfRace", new List<string> { "DarkElfRace", "DarkElfRaceVampire", "_00DwemerRace", "MASNerevarineRace" } },
            { "DarkElfRaceVampire", new List<string> { "DarkElfRace", "DarkElfRaceVampire", "_00DwemerRace", "MASNerevarineRace" } },
            { "_00DwemerRace", new List<string> { "DarkElfRace", "DarkElfRaceVampire", "_00DwemerRace", "MASNerevarineRace" } },
            { "MASNerevarineRace", new List<string> { "DarkElfRace", "DarkElfRaceVampire", "_00DwemerRace", "MASNerevarineRace" } },
            { "ArgonianRace", new List<string> { "ArgonianRace", "ArgonianRaceVampire" } },
            { "ArgonianRaceVampire", new List<string> { "ArgonianRace", "ArgonianRaceVampire" } },
            { "KhajiitRace", new List<string> { "KhajiitRace", "KhajiitRaceVampire" } },
            { "KhajiitRaceVampire", new List<string> { "KhajiitRace", "KhajiitRaceVampire" } },
            { "HighElfRace", new List<string> { "HighElfRace", "HighElfRaceVampire", "SnowElfRace", "WB_ConjureCraftlord_Race" } },
            { "HighElfRaceVampire", new List<string> { "HighElfRace", "HighElfRaceVampire", "SnowElfRace", "WB_ConjureCraftlord_Race" } },
            { "SnowElfRace", new List<string> { "HighElfRace", "HighElfRaceVampire", "SnowElfRace", "WB_ConjureCraftlord_Race" } },
            { "WB_ConjureCraftlord_Race", new List<string> { "HighElfRace", "HighElfRaceVampire", "SnowElfRace", "WB_ConjureCraftlord_Race" } },
            { "WoodElfRace", new List<string> { "WoodElfRace", "WoodElfRaceVampire" } },
            { "WoodElfRaceVampire", new List<string> { "WoodElfRace", "WoodElfRaceVampire" } },
            { "BretonRace", new List<string> { "BretonRace", "BretonRaceVampire" } },
            { "BretonRaceVampire", new List<string> { "BretonRace", "BretonRaceVampire" } },
            { "RedguardRace", new List<string> { "RedguardRace", "RedguardRaceVampire" } },
            { "RedguardRaceVampire", new List<string> { "RedguardRace", "RedguardRaceVampire" } },
            { "OrcRace", new List<string> { "OrcRace", "OrcRaceVampire" } },
            { "OrcRaceVampire", new List<string> { "OrcRace", "OrcRaceVampire" } },
            { "ElderRace", new List<string> { "ElderRace", "ElderRaceVampire" } },
            { "ElderRaceVampire", new List<string> { "ElderRace", "ElderRaceVampire" } },
            { "DremoraRace", new List<string> { "DremoraRace" } },
            { "DA13AfflictedRace", new List<string> { "DA13AfflictedRace" } }
        };
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

                // Validate the race key
                if (!validRaceEditorIDs.Contains(race))
                {
                    Console.WriteLine($"Warning: Race EditorID '{race}' in race compatibility map does not exist in the load order. Skipping this entry.");
                    continue;
                }

                // Validate each compatible race
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

            // Voice type mapping
            var voiceTypeMap = new Dictionary<string, string>
            {
                { "MaleArgonian", "FemaleArgonian" },
                { "MaleBandit", "FemaleCommoner" },
                { "MaleBrute", "FemaleCommander" },
                { "MaleChild", "FemaleChild" },
                { "MaleCommander", "FemaleCommander" },
                { "MaleCommoner", "FemaleCommoner" },
                { "MaleCommonerAccented", "FemaleCommoner" },
                { "MaleCondescending", "FemaleCondescending" },
                { "MaleCoward", "FemaleCoward" },
                { "MaleDarkElf", "FemaleDarkElf" },
                { "MaleDrunk", "FemaleSultry" },
                { "MaleElfHaughty", "FemaleElfHaughty" },
                { "MaleEvenToned", "FemaleEvenToned" },
                { "MaleEvenTonedAccented", "FemaleEvenToned" },
                { "MaleGuard", "FemaleCommander" },
                { "MaleKhajiit", "FemaleKhajiit" },
                { "MaleNord", "FemaleNord" },
                { "MaleNordCommander", "FemaleNord" },
                { "MaleOldGrumpy", "FemaleOldGrumpy" },
                { "MaleOldKindly", "FemaleOldKindly" },
                { "MaleOrc", "FemaleOrc" },
                { "MaleSlyCynical", "FemaleSultry" },
                { "MaleSoldier", "FemaleCommander" },
                { "MaleUniqueGhost", "FemaleUniqueGhost" },
                { "MaleWarlock", "FemaleCondescending" },
                { "MaleYoungEager", "FemaleYoungEager" },
                { "DLC1MaleVampire", "DLC1FemaleVampire" },
                { "DLC2MaleDarkElfCommoner", "DLC2FemaleDarkElfCommoner" },
                { "DLC2MaleDarkElfCynical", "FemaleDarkElf" }
            };

            // Race to fallback voice list
            var raceVoiceFallbacks = new Dictionary<string, List<string>>
            {
                { "NordRace", new List<string> { "FemaleNord", "FemaleEvenToned", "FemaleCommander" } },
                { "NordRaceVampire", new List<string> { "FemaleNord", "FemaleEvenToned", "FemaleCommander" } },
                { "DarkElfRace", new List<string> { "FemaleDarkElf", "DLC2FemaleDarkElfCommoner", "FemaleCondescending" } },
                { "DarkElfRaceVampire", new List<string> { "FemaleDarkElf", "DLC2FemaleDarkElfCommoner", "FemaleCondescending" } },
                { "ArgonianRace", new List<string> { "FemaleArgonian", "FemaleSultry" } },
                { "ArgonianRaceVampire", new List<string> { "FemaleArgonian", "FemaleSultry" } },
                { "KhajiitRace", new List<string> { "FemaleKhajiit", "FemaleSultry" } },
                { "KhajiitRaceVampire", new List<string> { "FemaleKhajiit", "FemaleSultry" } },
                { "HighElfRace", new List<string> { "FemaleElfHaughty", "FemaleEvenToned" } },
                { "HighElfRaceVampire", new List<string> { "FemaleElfHaughty", "FemaleEvenToned" } },
                { "WoodElfRace", new List<string> { "FemaleEvenToned", "FemaleYoungEager" } },
                { "WoodElfRaceVampire", new List<string> { "FemaleEvenToned", "FemaleYoungEager" } },
                { "BretonRace", new List<string> { "FemaleEvenToned", "FemaleYoungEager" } },
                { "BretonRaceVampire", new List<string> { "FemaleEvenToned", "FemaleYoungEager" } },
                { "ImperialRace", new List<string> { "FemaleEvenToned", "FemaleCommander" } },
                { "ImperialRaceVampire", new List<string> { "FemaleEvenToned", "FemaleCommander" } },
                { "RedguardRace", new List<string> { "FemaleEvenToned", "FemaleSultry" } },
                { "RedguardRaceVampire", new List<string> { "FemaleEvenToned", "FemaleSultry" } },
                { "OrcRace", new List<string> { "FemaleOrc", "FemaleCommander" } },
                { "OrcRaceVampire", new List<string> { "FemaleOrc", "FemaleCommander" } }
            };

            // Collect female templates and count male NPCs (excluding Player and presets)
            int maleNpcCount = 0;
            int eligibleMaleNpcCount = 0; // Count of male NPCs eligible for patching (excludes blacklisted NPCs)
            int successfulPatches = 0;
            int skippedDueToPatch = 0; // Add counter for debugging
            int skippedDueToFilter = 0; // Add counter for debugging
            var blacklistedMaleNpcsByMod = new Dictionary<ModKey, int>(); // Track blacklisted male NPCs by mod
            foreach (var npc in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
            {
                var race = npc.Race.TryResolve(state.LinkCache)?.EditorID;
                if (race != null && humanoidRaces.Contains(race))
                {
                    if (npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female))
                    {
                        // Pre-filter templates when building femaleTemplatesByRace
                        bool isSkyrimEsm = npc.FormKey.ModKey.FileName.Equals("Skyrim.esm");
                        bool isAfflicted = race == "DA13AfflictedRace";
                        bool notBlacklisted = !blacklistedMods.Contains(npc.FormKey.ModKey.FileName);
                        var (nifExists, ddsExists) = facegenCache[(npc.FormKey.ModKey.FileName.ToString(), npc.FormKey.IDString())];
                        bool hasBeenPatched = npc.Keywords?.Any(k => k.FormKey == SkyLadyPatched) ?? false;
                        bool condition = !hasBeenPatched && ((isAfflicted && isSkyrimEsm) || (notBlacklisted && (nifExists && ddsExists)));
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
                            skippedDueToFilter++; // Increment counter
                            continue;
                        }
                        // Check if NPC has already been patched
                        bool hasBeenPatched = npc.Keywords?.Any(k => k.FormKey == SkyLadyPatched) ?? false;
                        if (hasBeenPatched)
                        {
                            skippedDueToPatch++; // Increment counter
                            continue;
                        }
                        maleNpcCount++;
                        // Check if NPC is blacklisted
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

            int totalSingleNpcs = settings.NpcsToPatch.Count;
            var patchedNpcs = new HashSet<FormKey>();

            foreach (var npc in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
            {
                if (patchedNpcs.Contains(npc.FormKey))
                    continue;

                if (npc.Keywords?.Any(k => k.FormKey == SkyLadyPatched) ?? false)
                {
                    skippedDueToPatch++;
                    continue;
                }

                var isFemale = npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female);
                var isPlayer = npc.EditorID?.Equals("Player", StringComparison.OrdinalIgnoreCase) ?? false;
                var isPreset = npc.EditorID?.ToLowerInvariant().Contains("preset") ?? false;
                var race = npc.Race.TryResolve(state.LinkCache)?.EditorID;

                if (race == null || !humanoidRaces.Contains(race) || isFemale || isPlayer || isPreset)
                {
                    skippedDueToFilter++;
                    continue;
                }

                if (settings.PatchSingleNpcOnly)
                {
                    if (!settings.NpcsToPatch.Any(n => n.Npc.FormKey == npc.FormKey))
                        continue;
                }
                else
                {
                    if (!patchEntireLoadOrder && !requiemKeys.Contains(npc.FormKey.ModKey))
                        continue;
                }

                if (settings.ModsToExcludeFromPatching.Contains(npc.FormKey.ModKey)) continue;
                if (settings.NpcsToExcludeFromPatching.Any(ex => ex.FormKey == npc.FormKey)) continue;

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
                    templates = new List<string> { "NordRace", "ImperialRace" }
                        .SelectMany(r => femaleTemplatesByRace.TryGetValue(r, out var t) ? t : [])
                        .ToList();
                }

                if (templates.Count > 0)
                {
                    INpcGetter? template = null;
                    bool useLockedTemplate = false;
                    bool facegenCopied = false;
                    Npc? patchedNpc = null;

                    // Check temp template for locked NPCs
                    if (lockedNpcs.TryGetValue(npc.FormKey, out var lockedEntry))
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
                                Console.WriteLine($"[Locked] Reusing temp template for {npc.EditorID ?? "Unnamed"}: {template.EditorID ?? "Unnamed"} ({template.FormKey}) from {template.FormKey.ModKey.FileName}");
                            }
                            else
                            {
                                Console.WriteLine($"[Locked] Temp template {tempTemplateKey} is invalid for {npc.EditorID ?? "Unnamed"} (race {race}). Will assign new template now.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[Locked] No valid temp template found for {npc.EditorID ?? "Unnamed"}. Will assign new template now.");
                        }
                    }

                    // Delete old facegen
                    DeleteExistingFacegenFiles(modFolderPath, npc.FormKey);

                    // Patch NPC
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

                    // Template selection
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

                            if (File.Exists(bsaPath) || (blacklistedMods.Contains(templateFileName) &&
                                !(templateFileName.Equals("Skyrim.esm") && templateRace == "DA13AfflictedRace")))
                            {
                                Console.WriteLine($"Skipping template {template.EditorID ?? "Unnamed"} ({template.FormKey}) from {templateFileName} (BSA or blacklisted)");
                                continue;
                            }

                            // Apply template properties
                            if (partsToCopy.Contains("PNAM") && template.HeadParts != null) patchedNpc.HeadParts.SetTo(template.HeadParts);
                            if (partsToCopy.Contains("WNAM") && template.WornArmor != null) patchedNpc.WornArmor.SetTo(template.WornArmor);
                            if (partsToCopy.Contains("QNAM")) patchedNpc.TextureLighting = template.TextureLighting;
                            if (partsToCopy.Contains("NAM9") && template.FaceMorph != null) patchedNpc.FaceMorph = template.FaceMorph.DeepCopy();
                            if (partsToCopy.Contains("NAMA") && template.FaceParts != null) patchedNpc.FaceParts = template.FaceParts.DeepCopy();
                            if (partsToCopy.Contains("Tint Layers") && template.TintLayers != null) patchedNpc.TintLayers.SetTo(template.TintLayers.Select(t => t.DeepCopy()));
                            if (partsToCopy.Contains("FTST") && template.HeadTexture != null) patchedNpc.HeadTexture.SetTo(template.HeadTexture);
                            if (partsToCopy.Contains("HCLF") && template.HairColor != null) patchedNpc.HairColor.SetTo(template.HairColor);

                            patchedNpc.Configuration.Flags |= NpcConfiguration.Flag.Female;

                            // Set voice
                            if (npc.Voice != null)
                            {
                                var voiceType = npc.Voice.TryResolve(state.LinkCache)?.EditorID;
                                if (!string.IsNullOrEmpty(voiceType))
                                {
                                    bool isFemaleVoice = voiceType.Contains("Female", StringComparison.OrdinalIgnoreCase) ||
                                                         voiceTypeMap.Values.Any(v => v.Equals(voiceType)) ||
                                                         raceVoiceFallbacks.Values.Any(list => list.Contains(voiceType));

                                    if (!isFemaleVoice)
                                    {
                                        if (voiceTypeMap.TryGetValue(voiceType, out var femaleVoiceID))
                                        {
                                            var femaleVoice = state.LoadOrder.PriorityOrder.VoiceType().WinningOverrides()
                                                .FirstOrDefault(vt => vt.EditorID == femaleVoiceID);
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
                        // Apply locked template properties
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
                                                         voiceTypeMap.Values.Any(v => v.Equals(voiceType)) ||
                                                         raceVoiceFallbacks.Values.Any(list => list.Contains(voiceType));

                                    if (!isFemaleVoice)
                                    {
                                        if (voiceTypeMap.TryGetValue(voiceType, out var femaleVoiceID))
                                        {
                                            var femaleVoice = state.LoadOrder.PriorityOrder.VoiceType().WinningOverrides()
                                                .FirstOrDefault(vt => vt.EditorID == femaleVoiceID);
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

                    // Copy facegen for both locked and new templates
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

                        if (templateFileName.Equals("Skyrim.esm") && templateRace == "DA13AfflictedRace")
                        {
                            var nifDir = Path.GetDirectoryName(patchedNif) ?? throw new InvalidOperationException("NIF path directory is null");
                            var ddsDir = Path.GetDirectoryName(patchedDds) ?? throw new InvalidOperationException("DDS path directory is null");
                            Directory.CreateDirectory(nifDir);
                            Directory.CreateDirectory(ddsDir);
                            facegenCopied = true;
                            Console.WriteLine($"Assumed vanilla facegen for Afflicted template {template.EditorID ?? "Unnamed"} ({templateFid}) from {templateFileName}");
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

                        if (!(patchedNpc.Keywords?.Any(k => k.FormKey == SkyLadyPatched) ?? false))
                        {
                            patchedNpc.Keywords ??= [];
                            patchedNpc.Keywords.Add(SkyLadyPatched);
                        }

                        currentRunTemplates[npc.FormKey.ToString()] = template?.FormKey.ToString() ?? ""; // Store all assigned templates

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
            Thread.Sleep(5000);

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

                string? suffix = null;
                if (settings.AppendSuffixToOutput)
                {
                    suffix = string.IsNullOrWhiteSpace(settings.OutputNameSuffix)
                        ? DateTime.Now.ToString("yyyyMMdd_HHmm")
                        : settings.OutputNameSuffix;
                }

                for (int i = 0; i < splitMods.Count; i++)
                {
                    var mod = splitMods[i];
                    var originalFileName = mod.ModKey.FileName.ToString();

                    string outputFileName;
                    if (suffix != null)
                    {
                        outputFileName = i == 0
                            ? $"SkyLady Patcher_{suffix}.esp"
                            : $"SkyLady Patcher_{suffix}_{i}.esp";
                        Console.WriteLine($"Renaming output file from {originalFileName} to {outputFileName}");
                    }
                    else
                    {
                        outputFileName = i == 0
                            ? "SkyLady Patcher.esp"
                            : $"SkyLady Patcher_{i + 1}.esp";
                        Console.WriteLine($"Using Synthesis default naming: {outputFileName}");
                    }

                    var splitMasterCount = mod.MasterReferences.Count;
                    var recordCount = mod.EnumerateMajorRecords().Count();
                    Console.WriteLine($"Mod {i}: {outputFileName}, Masters: {splitMasterCount}, Records: {recordCount}");

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