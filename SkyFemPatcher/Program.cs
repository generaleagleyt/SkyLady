using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;

namespace SkyFemPatcher.SkyFemPatcher
{
    public class Program
    {
        private static readonly char[] LineSeparators = ['\n', '\r'];

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(Patch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "SkyFem Patcher.esp")
                .Run(args);
        }

        public static void Patch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            Console.WriteLine("SkyFem Patcher (Side) running on .NET 8.0...");
            var racesPath = Path.Combine(state.DataFolderPath, "..", "..", "mods", "SkyFem Patcher", "SkyFem races.txt");
            var partsToCopyPath = Path.Combine(state.DataFolderPath, "..", "..", "mods", "SkyFem Patcher", "SkyFem partsToCopy.txt");
            var blacklistPath = Path.Combine(state.DataFolderPath, "..", "..", "mods", "SkyFem Patcher", "SkyFem blacklist.txt");
            var targetModsPath = Path.Combine(state.DataFolderPath, "..", "..", "mods", "SkyFem Patcher", "SkyFem target mods.txt");
            var humanoidRaces = new HashSet<string>(File.ReadAllLines(racesPath).Select(line => line.Trim()));
            var partsToCopy = File.ReadAllLines(partsToCopyPath).ToHashSet();
            HashSet<string> blacklistedMods = File.Exists(blacklistPath) ? [.. File.ReadAllLines(blacklistPath).Select(line => line.Trim())] : [];
            var femaleTemplatesByRace = new Dictionary<string, List<INpcGetter>>();
            var successfulTemplatesByRace = new Dictionary<string, List<INpcGetter>>();
            var skippedTemplates = new Dictionary<string, (string ModName, string Reason)>();
            var unpatchedNpcs = new Dictionary<string, string>(); // NPC ID -> Reason
            var filteredNpcs = new Dictionary<string, string>(); // Filtered NPCs (Player/Presets)
            var random = new Random();

            // Load target mods from txt file
            HashSet<ModKey> requiemKeys = [];
            bool patchEntireLoadOrder = true;
            if (File.Exists(targetModsPath))
            {
                var targetMods = File.ReadAllLines(targetModsPath)
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrEmpty(line))
                    .Select(modName => ModKey.FromNameAndExtension(modName))
                    .ToList();
                if (targetMods.Any())
                {
                    requiemKeys.UnionWith(targetMods);
                    patchEntireLoadOrder = false;
                    Console.WriteLine("Patching specific mods from SkyFem target mods.txt:");
                    foreach (var mod in requiemKeys) Console.WriteLine($"  {mod.FileName}");
                }
                else
                {
                    Console.WriteLine("SkyFem target mods.txt is empty - patching entire load order.");
                }
            }
            else
            {
                Console.WriteLine("SkyFem target mods.txt not found - patching entire load order.");
            }

            // Cache all NPC overrides
            Console.WriteLine("Building NPC override cache...");
            var overrideCache = state.LoadOrder.PriorityOrder.Npc()
                .WinningOverrides()
                .ToDictionary(n => n.FormKey, n => n);

            // Race compatibility mapping
            var raceCompatibilityMap = new Dictionary<string, List<string>>
            {
                { "NordRace", new List<string> { "NordRace", "NordRaceVampire", "HothRace" } },
                { "NordRaceVampire", new List<string> { "NordRace", "NordRaceVampire", "HothRace" } },
                { "HothRace", new List<string> { "NordRace", "NordRaceVampire", "HothRace" } },
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
                { "ImperialRace", new List<string> { "ImperialRace", "ImperialRaceVampire" } },
                { "ImperialRaceVampire", new List<string> { "ImperialRace", "ImperialRaceVampire" } },
                { "RedguardRace", new List<string> { "RedguardRace", "RedguardRaceVampire" } },
                { "RedguardRaceVampire", new List<string> { "RedguardRace", "RedguardRaceVampire" } },
                { "OrcRace", new List<string> { "OrcRace", "OrcRaceVampire" } },
                { "OrcRaceVampire", new List<string> { "OrcRace", "OrcRaceVampire" } },
                { "ElderRace", new List<string> { "ElderRace", "ElderRaceVampire" } },
                { "ElderRaceVampire", new List<string> { "ElderRace", "ElderRaceVampire" } },
                { "DremoraRace", new List<string> { "DremoraRace" } },
                { "DA13AfflictedRace", new List<string> { "DA13AfflictedRace" } }
            };

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
            int successfulPatches = 0;
            foreach (var npc in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
            {
                var race = npc.Race.TryResolve(state.LinkCache)?.EditorID;
                if (race != null && humanoidRaces.Contains(race))
                {
                    if (npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female))
                    {
                        femaleTemplatesByRace[race] = femaleTemplatesByRace.GetValueOrDefault(race, []);
                        femaleTemplatesByRace[race].Add(npc);
                    }
                    else if (patchEntireLoadOrder || requiemKeys.Contains(npc.FormKey.ModKey))
                    {
                        // Exclude Player and presets from the count
                        if (npc.EditorID != null && (npc.EditorID.Equals("Player", StringComparison.OrdinalIgnoreCase) ||
                                                     npc.EditorID.Contains("preset", StringComparison.OrdinalIgnoreCase)))
                        {
                            filteredNpcs[npc.EditorID + " (" + npc.FormKey.IDString() + ")"] = "Filtered (Player/Preset)";
                            continue;
                        }
                        maleNpcCount++;
                        Console.WriteLine($"Found male NPC: {npc.EditorID ?? "Unnamed"} ({npc.FormKey.IDString()}) (Race: {race})");
                    }
                }
            }
            Console.WriteLine($"Collected templates for {femaleTemplatesByRace.Count} races.");
            Console.WriteLine($"Total male humanoid NPCs in {(patchEntireLoadOrder ? "entire load order" : "target mods")}: {maleNpcCount}");

            foreach (var race in femaleTemplatesByRace.Keys.OrderBy(r => r))
            {
                Console.WriteLine($"Found {femaleTemplatesByRace[race].Count} female templates for race {race}");
            }

            // Patch male NPCs from target mods or entire load order
            foreach (var npc in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
            {
                var race = npc.Race.TryResolve(state.LinkCache)?.EditorID;
                if (race == null || !humanoidRaces.Contains(race) ||
                    npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female) ||
                    (!patchEntireLoadOrder && !requiemKeys.Contains(npc.FormKey.ModKey)) ||
                    (npc.EditorID != null && (npc.EditorID.Equals("Player", StringComparison.OrdinalIgnoreCase) ||
                                              npc.EditorID.Contains("preset", StringComparison.OrdinalIgnoreCase))))
                    continue;

                var npcFid = npc.FormKey.IDString();

                // Debug log for Afflicted NPCs
                if (race == "DA13AfflictedRace")
                {
                    Console.WriteLine($"Processing Afflicted NPC: {npc.EditorID ?? "Unnamed"} ({npcFid})");
                }

                var compatibleRaces = raceCompatibilityMap.TryGetValue(race, out var races) ? races : [race];
                var templates = compatibleRaces
                    .SelectMany(r => femaleTemplatesByRace.TryGetValue(r, out var t) ? t : [])
                    .Where(t =>
                    {
                        bool isSkyrimEsm = t.FormKey.ModKey.FileName.Equals("Skyrim.esm");
                        bool isAfflicted = race == "DA13AfflictedRace";
                        bool notBlacklisted = !blacklistedMods.Contains(t.FormKey.ModKey.FileName);
                        bool nifExists = File.Exists(Path.Combine(state.DataFolderPath, "meshes", "actors", "character", "facegendata", "facegeom", t.FormKey.ModKey.FileName, $"00{t.FormKey.IDString()}.nif"));
                        bool ddsExists = File.Exists(Path.Combine(state.DataFolderPath, "textures", "actors", "character", "facegendata", "facetint", t.FormKey.ModKey.FileName, $"00{t.FormKey.IDString()}.dds"));
                        bool condition = (isAfflicted && isSkyrimEsm) || (notBlacklisted && (nifExists && ddsExists));
                        if (isAfflicted && isSkyrimEsm)
                        {
                            Console.WriteLine($"Checking template {t.EditorID ?? "Unnamed"} ({t.FormKey}): notBlacklisted={notBlacklisted}, nifExists={nifExists}, ddsExists={ddsExists}, passes={condition}");
                        }
                        return condition;
                    })
                    .ToList();

                if (templates.Count > 0)
                {
                    var validTemplates = templates.ToList();
                    if (validTemplates.Count > 1)
                    {
                        // Shuffle for full randomness
                        for (int i = validTemplates.Count - 1; i > 0; i--)
                        {
                            int j = random.Next(i + 1);
                            var temp = validTemplates[i];
                            validTemplates[i] = validTemplates[j];
                            validTemplates[j] = temp;
                        }
                    }

                    var patchedNpc = state.PatchMod.Npcs.GetOrAddAsOverride(npc);

                    var previousOverride = overrideCache.TryGetValue(npc.FormKey, out var cached) && !cached.Equals(patchedNpc) ? cached : null;
                    if (previousOverride != null)
                    {
                        patchedNpc.Configuration.Level = previousOverride.Configuration.Level.DeepCopy();
                        patchedNpc.Configuration.CalcMinLevel = previousOverride.Configuration.CalcMinLevel;
                        patchedNpc.Configuration.CalcMaxLevel = previousOverride.Configuration.CalcMaxLevel;
                        patchedNpc.Configuration.HealthOffset = previousOverride.Configuration.HealthOffset;
                        patchedNpc.Configuration.MagickaOffset = previousOverride.Configuration.MagickaOffset;
                        patchedNpc.Configuration.StaminaOffset = previousOverride.Configuration.StaminaOffset;
                        patchedNpc.Configuration.DispositionBase = previousOverride.Configuration.DispositionBase;
                        patchedNpc.Configuration.Flags = previousOverride.Configuration.Flags;

                        patchedNpc.Keywords = (previousOverride.Keywords ?? []).ToExtendedList();

                        patchedNpc.Items?.Clear();
                        if (previousOverride.Items != null)
                            patchedNpc.Items?.AddRange(previousOverride.Items.Select(i => i.DeepCopy()));

                        patchedNpc.Packages.Clear();
                        patchedNpc.Packages.AddRange(previousOverride.Packages);

                        patchedNpc.Perks?.Clear();
                        if (previousOverride.Perks != null)
                            patchedNpc.Perks?.AddRange(previousOverride.Perks.Select(p => p.DeepCopy()));

                        patchedNpc.Factions.Clear();
                        if (previousOverride.Factions != null)
                            patchedNpc.Factions.AddRange(previousOverride.Factions.Select(f => f.DeepCopy()));
                    }

                    INpcGetter? template = null;
                    bool facegenCopied = false;

                    foreach (var candidate in validTemplates)
                    {
                        template = candidate;
                        var templateFid = template.FormKey.IDString();
                        var templateFileName = template.FormKey.ModKey.FileName.ToString();
                        var templateNifPath = Path.Combine(state.DataFolderPath, "meshes", "actors", "character", "facegendata", "facegeom", templateFileName, $"00{templateFid}.nif");
                        var templateDdsPath = Path.Combine(state.DataFolderPath, "textures", "actors", "character", "facegendata", "facetint", templateFileName, $"00{templateFid}.dds");

                        var bsaPath = Path.Combine(state.DataFolderPath, templateFileName.Replace(".esm", ".bsa").Replace(".esp", ".bsa"));
                        if (File.Exists(bsaPath))
                        {
                            Console.WriteLine($"Skipping template {template.EditorID ?? "Unnamed"} ({templateFid}) from {templateFileName} for NPC {npc.EditorID ?? "Unnamed"} - mod uses a .bsa archive, which the patcher cannot access.");
                            skippedTemplates[template.EditorID ?? "Unnamed"] = (templateFileName, "Mod uses a .bsa archive");
                            continue;
                        }

                        if (partsToCopy.Contains("PNAM")) patchedNpc.HeadParts.SetTo(template.HeadParts);
                        if (partsToCopy.Contains("WNAM")) patchedNpc.WornArmor.SetTo(template.WornArmor);
                        if (partsToCopy.Contains("QNAM")) patchedNpc.TextureLighting = template.TextureLighting;
                        if (partsToCopy.Contains("NAM9") && template.FaceMorph != null) patchedNpc.FaceMorph = template.FaceMorph.DeepCopy();
                        if (partsToCopy.Contains("NAMA")) patchedNpc.FaceParts = template.FaceParts?.DeepCopy();
                        if (partsToCopy.Contains("Tint Layers") && template.TintLayers != null) patchedNpc.TintLayers.SetTo(template.TintLayers.Select(t => t.DeepCopy()));
                        if (partsToCopy.Contains("FTST")) patchedNpc.HeadTexture.SetTo(template.HeadTexture);
                        if (partsToCopy.Contains("HCLF")) patchedNpc.HairColor.SetTo(template.HairColor);

                        patchedNpc.Configuration.Flags |= NpcConfiguration.Flag.Female;

                        if (npc.Voice != null)
                        {
                            var voiceTypeGetter = npc.Voice.TryResolve(state.LinkCache);
                            var voiceType = voiceTypeGetter?.EditorID;
                            if (voiceType != null && voiceTypeMap.TryGetValue(voiceType, out var femaleVoiceType))
                            {
                                var femaleVoice = state.LoadOrder.PriorityOrder.VoiceType().WinningOverrides()
                                    .FirstOrDefault(vt => vt.EditorID == femaleVoiceType);
                                if (femaleVoice != null)
                                {
                                    patchedNpc.Voice.SetTo(femaleVoice);
                                    Console.WriteLine($"Swapped voice type for {npc.EditorID ?? "Unnamed"} from {voiceType} to {femaleVoiceType}");
                                }
                            }
                            else if (voiceType != null && raceVoiceFallbacks.TryGetValue(race, out var fallbackVoices) && fallbackVoices.Count > 0)
                            {
                                var selectedVoiceType = fallbackVoices[random.Next(fallbackVoices.Count)];
                                var fallbackVoice = state.LoadOrder.PriorityOrder.VoiceType().WinningOverrides()
                                    .FirstOrDefault(vt => vt.EditorID == selectedVoiceType);
                                if (fallbackVoice != null)
                                {
                                    patchedNpc.Voice.SetTo(fallbackVoice);
                                    Console.WriteLine($"No female voice mapping for {voiceType} - used fallback {selectedVoiceType} for {npc.EditorID ?? "Unnamed"} (Race: {race})");
                                }
                                else
                                {
                                    Console.WriteLine($"Failed to find fallback voice {selectedVoiceType} for {npc.EditorID ?? "Unnamed"} (Race: {race})");
                                }
                            }
                            else if (voiceType != null)
                            {
                                Console.WriteLine($"No fallback voices defined for race {race} for {npc.EditorID ?? "Unnamed"}");
                            }
                        }

                        patchedNpc.Height = template.Height != 0.0f ? template.Height : 1.0f;
                        patchedNpc.Weight = template.Weight != 0.0f ? template.Weight : 50.0f;

                        var outputModFolder = "G:\\LoreRim\\mods\\SkyFem Patcher";
                        var patchedNifPath = Path.Combine(outputModFolder, "meshes", "actors", "character", "facegendata", "facegeom", npc.FormKey.ModKey.FileName, $"00{npcFid}.nif");
                        var patchedDdsPath = Path.Combine(outputModFolder, "textures", "actors", "character", "facegendata", "facetint", npc.FormKey.ModKey.FileName, $"00{npcFid}.dds");

                        Console.WriteLine($"Checking facegen - Geom Src: {templateNifPath}, Exists: {File.Exists(templateNifPath)}");
                        Console.WriteLine($"Checking facegen - Tint Src: {templateDdsPath}, Exists: {File.Exists(templateDdsPath)}");

                        bool nifCopied = false, ddsCopied = false;
                        var nifDir = Path.GetDirectoryName(patchedNifPath) ?? throw new InvalidOperationException("NIF path directory is null");
                        var ddsDir = Path.GetDirectoryName(patchedDdsPath) ?? throw new InvalidOperationException("DDS path directory is null");

                        if (templateFileName.Equals("Skyrim.esm") && race == "DA13AfflictedRace")
                        {
                            Console.WriteLine($"Bypass triggered for template {template.EditorID ?? "Unnamed"} ({templateFid}) for NPC {npc.EditorID ?? "Unnamed"} ({npcFid})");
                            Directory.CreateDirectory(nifDir);
                            Directory.CreateDirectory(ddsDir);
                            nifCopied = true;
                            ddsCopied = true;
                            Console.WriteLine($"Assumed facegen for template {template.EditorID ?? "Unnamed"} ({templateFid}) from Skyrim.esm for Afflicted NPC");
                        }
                        else
                        {
                            if (File.Exists(templateNifPath))
                            {
                                Directory.CreateDirectory(nifDir);
                                File.Copy(templateNifPath, patchedNifPath, true);
                                Console.WriteLine($"Copied facegen .nif to: {patchedNifPath}");
                                nifCopied = true;
                            }
                            else
                            {
                                Console.WriteLine($"Warning: No facegen .nif found for template {template.EditorID ?? "Unnamed"} ({templateFid}) at {templateNifPath}");
                            }

                            if (File.Exists(templateDdsPath))
                            {
                                Directory.CreateDirectory(ddsDir);
                                File.Copy(templateDdsPath, patchedDdsPath, true);
                                Console.WriteLine($"Copied facegen .dds to: {patchedDdsPath}");
                                ddsCopied = true;
                            }
                            else
                            {
                                Console.WriteLine($"Warning: No facegen .dds found for template {template.EditorID ?? "Unnamed"} ({templateFid}) at {templateDdsPath}");
                            }
                        }

                        if (nifCopied && ddsCopied)
                        {
                            facegenCopied = true;
                            successfulPatches++;
                            if (!successfulTemplatesByRace.TryGetValue(race, out var templateList))
                            {
                                templateList = [];
                                successfulTemplatesByRace[race] = templateList;
                            }
                            templateList.Add(template);
                            Console.WriteLine($"Patched Male NPC: {npc.EditorID ?? "Unnamed"} with {template.EditorID ?? "Unnamed"} (Race: {race})");
                            break;
                        }
                    }

                    if (!facegenCopied)
                    {
                        if (successfulTemplatesByRace.TryGetValue(race, out var successfulTemplates) && successfulTemplates.Count > 0)
                        {
                            template = successfulTemplates[random.Next(successfulTemplates.Count)];
                            var fallbackNifPath = Path.Combine(state.DataFolderPath, "meshes", "actors", "character", "facegendata", "facegeom", template.FormKey.ModKey.FileName, $"00{template.FormKey.IDString()}.nif");
                            var fallbackDdsPath = Path.Combine(state.DataFolderPath, "textures", "actors", "character", "facegendata", "facetint", template.FormKey.ModKey.FileName, $"00{template.FormKey.IDString()}.dds");
                            var outputModFolder = "G:\\LoreRim\\mods\\SkyFem Patcher";
                            var patchedNifPath = Path.Combine(outputModFolder, "meshes", "actors", "character", "facegendata", "facegeom", npc.FormKey.ModKey.FileName, $"00{npcFid}.nif");
                            var patchedDdsPath = Path.Combine(outputModFolder, "textures", "actors", "character", "facegendata", "facetint", npc.FormKey.ModKey.FileName, $"00{npcFid}.dds");
                            var nifDir = Path.GetDirectoryName(patchedNifPath) ?? throw new InvalidOperationException("NIF path directory is null");
                            var ddsDir = Path.GetDirectoryName(patchedDdsPath) ?? throw new InvalidOperationException("DDS path directory is null");

                            Directory.CreateDirectory(nifDir);
                            Directory.CreateDirectory(ddsDir);
                            File.Copy(fallbackNifPath, patchedNifPath, true);
                            File.Copy(fallbackDdsPath, patchedDdsPath, true);
                            Console.WriteLine($"Used fallback facegen .nif from successful template: {patchedNifPath}");
                            Console.WriteLine($"Used fallback facegen .dds from successful template: {patchedDdsPath}");
                            successfulPatches++;
                            Console.WriteLine($"Patched Male NPC: {npc.EditorID ?? "Unnamed"} with Fallback Template {template.EditorID ?? "Unnamed"} (Race: {race})");
                        }
                        else
                        {
                            unpatchedNpcs[npc.EditorID ?? "Unnamed" + " (" + npcFid + ")"] = $"No valid templates or successful fallbacks for race {race}";
                            Console.WriteLine($"Failed to patch {npc.EditorID ?? "Unnamed"} ({npcFid}) - no valid templates or successful fallbacks available for race {race}. NPC will remain unchanged.");
                            continue;
                        }
                    }
                }
                else
                {
                    unpatchedNpcs[npc.EditorID ?? "Unnamed" + " (" + npcFid + ")"] = $"No female templates found for race {race}";
                    Console.WriteLine($"No female templates found for race {race} for NPC {npc.EditorID ?? "Unnamed"} ({npcFid})");
                }
            }

            if (skippedTemplates.Count != 0)
            {
                Console.WriteLine("\nSummary of Skipped Templates:");
                foreach (var (templateId, (modName, reason)) in skippedTemplates)
                {
                    Console.WriteLine($"- Template: {templateId}, Mod: {modName}, Reason: {reason}");
                }
                Console.WriteLine("If you encounter issues with these templates, consider adding the listed mods to 'SkyFem blacklist.txt' in the SkyFem Patcher mod folder.");
            }

            if (filteredNpcs.Count != 0)
            {
                Console.WriteLine("\nFiltered NPCs (Excluded from Patching):");
                foreach (var (npcId, reason) in filteredNpcs)
                {
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

            Console.WriteLine($"Successfully patched {successfulPatches} out of {maleNpcCount} male NPCs with facegen.");
        }
    }
}