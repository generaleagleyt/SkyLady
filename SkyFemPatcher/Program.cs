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
            var humanoidRaces = new HashSet<string>(File.ReadAllLines(racesPath).Select(line => line.Trim()));
            var partsToCopy = File.ReadAllLines(partsToCopyPath).ToHashSet();
            HashSet<string> blacklistedMods = File.Exists(blacklistPath) ? [.. File.ReadAllLines(blacklistPath).Select(line => line.Trim())] : [];
            // Add known problematic mods to the blacklist by default
            blacklistedMods.Add("Harem - Volume 3.esp");
            blacklistedMods.Add("SexyBanditCaptives.esp");
            blacklistedMods.Add("Harem 2 AIO Armored.esp");
            blacklistedMods.Add("Damsels The Caged Rose.esp");
            blacklistedMods.Add("WindhelmSSE.esp");
            blacklistedMods.Add("Deviously Cursed Loot.esp");
            blacklistedMods.Add("Skyrim.esm");
            blacklistedMods.Add("[AMI] COCO Succubus.esp");
            var femaleTemplatesByRace = new Dictionary<string, List<INpcGetter>>();
            var successfulTemplatesByRace = new Dictionary<string, List<INpcGetter>>();
            var skippedTemplates = new Dictionary<string, (string ModName, string Reason)>();
            var random = new Random();
            var requiemKey = ModKey.FromNameAndExtension("Coldhaven.esm");

            // Race compatibility mapping (e.g., NordRace and NordRaceVampire are compatible)
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

            // Voice type mapping (male to female)
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
                // DLC voices
                { "DLC1MaleVampire", "DLC1FemaleVampire" },
                { "DLC2MaleDarkElfCommoner", "DLC2FemaleDarkElfCommoner" },
                { "DLC2MaleDarkElfCynical", "FemaleDarkElf" }
            };

            // Race to fallback voice list (preferred voice first)
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

            // Collect female templates and count male NPCs
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
                    else if (npc.FormKey.ModKey == requiemKey)
                    {
                        maleNpcCount++;
                        Console.WriteLine($"Found male NPC: {npc.EditorID ?? "Unnamed"} ({npc.FormKey.IDString()}) (Race: {race})");
                    }
                }
            }
            Console.WriteLine($"Collected templates for {femaleTemplatesByRace.Count} races.");
            Console.WriteLine($"Total male humanoid NPCs in Coldhaven.esm: {maleNpcCount}");

            foreach (var race in femaleTemplatesByRace.Keys.OrderBy(r => r))
            {
                Console.WriteLine($"Found {femaleTemplatesByRace[race].Count} female templates for race {race}");
            }

            // Patch Coldhaven male NPCs
            foreach (var npc in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
            {
                var race = npc.Race.TryResolve(state.LinkCache)?.EditorID;
                if (race == null || !humanoidRaces.Contains(race) || npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female) || npc.FormKey.ModKey != requiemKey)
                    continue;

                var npcFid = npc.FormKey.IDString();
                var compatibleRaces = raceCompatibilityMap.TryGetValue(race, out var races) ? races : [race];
                var templates = compatibleRaces
                    .SelectMany(r => femaleTemplatesByRace.TryGetValue(r, out var t) ? t : [])
                    .ToList();
                if (templates.Count > 0)
                {
                    var patchedNpc = state.PatchMod.Npcs.GetOrAddAsOverride(npc);
                    INpcGetter? template = null;
                    bool facegenCopied = false;

                    // Try all templates randomly
                    foreach (var candidate in templates.OrderBy(x => random.Next()))
                    {
                        template = candidate;
                        var templateFid = template.FormKey.IDString();
                        var templateFileName = template.FormKey.ModKey.FileName.ToString();
                        var templateNifPath = Path.Combine(state.DataFolderPath, "meshes", "actors", "character", "facegendata", "facegeom", templateFileName, $"00{templateFid}.nif");
                        var templateDdsPath = Path.Combine(state.DataFolderPath, "textures", "actors", "character", "facegendata", "facetint", templateFileName, $"00{templateFid}.dds");

                        // Skip templates from blacklisted mods
                        if (blacklistedMods.Contains(templateFileName))
                        {
                            Console.WriteLine($"Skipping template {template.EditorID ?? "Unnamed"} ({templateFid}) from blacklisted mod {templateFileName} for NPC {npc.EditorID ?? "Unnamed"} - mod contains problematic facegen files. To manage blacklisted mods, edit 'SkyFem blacklist.txt' in the SkyFem Patcher mod folder.");
                            skippedTemplates[template.EditorID ?? "Unnamed"] = (templateFileName, "Mod is blacklisted");
                            continue;
                        }

                        // Check if the mod has meshes or textures folders
                        var modMeshesPath = Path.Combine(state.DataFolderPath, "meshes", "actors", "character", "facegendata", "facegeom", templateFileName);
                        var modTexturesPath = Path.Combine(state.DataFolderPath, "textures", "actors", "character", "facegendata", "facetint", templateFileName);
                        if (!Directory.Exists(modMeshesPath) && !Directory.Exists(modTexturesPath))
                        {
                            Console.WriteLine($"Skipping template {template.EditorID ?? "Unnamed"} ({templateFid}) from {templateFileName} for NPC {npc.EditorID ?? "Unnamed"} - mod does not contain meshes or textures folders, likely missing facegen files.");
                            skippedTemplates[template.EditorID ?? "Unnamed"] = (templateFileName, "Mod lacks meshes or textures folders");
                            continue;
                        }

                        // Check for .bsa archives
                        var bsaPath = Path.Combine(state.DataFolderPath, templateFileName.Replace(".esm", ".bsa").Replace(".esp", ".bsa"));
                        if (File.Exists(bsaPath))
                        {
                            Console.WriteLine($"Skipping template {template.EditorID ?? "Unnamed"} ({templateFid}) from {templateFileName} for NPC {npc.EditorID ?? "Unnamed"} - mod uses a .bsa archive, which the patcher cannot access.");
                            skippedTemplates[template.EditorID ?? "Unnamed"] = (templateFileName, "Mod uses a .bsa archive");
                            continue;
                        }

                        // Validate template facegen files
                        if (!File.Exists(templateNifPath) || new FileInfo(templateNifPath).Length == 0)
                        {
                            Console.WriteLine($"Invalid .nif for template {template.EditorID ?? "Unnamed"} ({templateFid}) at {templateNifPath} - skipping.");
                            skippedTemplates[template.EditorID ?? "Unnamed"] = (templateFileName, "Invalid .nif file (missing or empty)");
                            continue;
                        }
                        if (!File.Exists(templateDdsPath) || new FileInfo(templateDdsPath).Length == 0)
                        {
                            Console.WriteLine($"Invalid .dds for template {template.EditorID ?? "Unnamed"} ({templateFid}) at {templateDdsPath} - skipping.");
                            skippedTemplates[template.EditorID ?? "Unnamed"] = (templateFileName, "Invalid .dds file (missing or empty)");
                            continue;
                        }

                        // Check for male texture references and missing textures in the .nif file
                        try
                        {
                            var nifContent = File.ReadAllText(templateNifPath);
                            // Extract active texture paths from BSShaderTextureSet blocks
                            var texturePaths = new List<string>();
                            var lines = nifContent.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries);
                            bool inTextureSet = false;
                            foreach (var line in lines)
                            {
                                if (line.Contains("BSShaderTextureSet"))
                                {
                                    inTextureSet = true;
                                    continue;
                                }
                                if (inTextureSet && line.Contains("textures\\"))
                                {
                                    var pathStart = line.IndexOf("textures\\");
                                    var pathEnd = line.IndexOf(".dds") + 4;
                                    if (pathStart >= 0 && pathEnd > pathStart)
                                    {
                                        string texturePath = line[pathStart..pathEnd];
                                        texturePaths.Add(texturePath);
                                    }
                                }
                                if (inTextureSet && line.Contains('}'))
                                {
                                    inTextureSet = false;
                                }
                            }

                            // Check if any active texture path references male textures (excluding BlankDetailmap.dds and KhajiitMouth textures)
                            if (texturePaths.Any(path => path.Contains(@"textures\actors\character\male\", StringComparison.OrdinalIgnoreCase) &&
                                !path.Contains(@"textures\actors\character\male\blankdetailmap.dds", StringComparison.OrdinalIgnoreCase) &&
                                !(path.Contains(@"textures\actors\character\khajiitmale\", StringComparison.OrdinalIgnoreCase) && path.Contains("khajiitmouth", StringComparison.OrdinalIgnoreCase))))
                            {
                                Console.WriteLine($"Skipping template {template.EditorID ?? "Unnamed"} ({templateFid}) from {templateFileName} for NPC {npc.EditorID ?? "Unnamed"} - .nif file references male textures, which may cause rendering issues. Consider adding '{templateFileName}' to SkyFem blacklist.txt.");
                                skippedTemplates[template.EditorID ?? "Unnamed"] = (templateFileName, "Male texture references detected");
                                continue;
                            }

                            // Check if each texture exists
                            foreach (var texturePath in texturePaths)
                            {
                                var fullTexturePath = Path.Combine(state.DataFolderPath, texturePath.Replace('/', '\\'));
                                if (!File.Exists(fullTexturePath))
                                {
                                    Console.WriteLine($"Skipping template {template.EditorID ?? "Unnamed"} ({templateFid}) from {templateFileName} for NPC {npc.EditorID ?? "Unnamed"} - .nif file references missing texture: {texturePath}. Consider adding '{templateFileName}' to SkyFem blacklist.txt.");
                                    skippedTemplates[template.EditorID ?? "Unnamed"] = (templateFileName, $"Missing texture: {texturePath}");
                                    continue;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to read .nif file for template {template.EditorID ?? "Unnamed"} ({templateFid}) at {templateNifPath} - skipping due to error: {ex.Message}");
                            skippedTemplates[template.EditorID ?? "Unnamed"] = (templateFileName, $"Failed to read .nif file: {ex.Message}");
                            continue;
                        }

                        // Copy specific fields from partsToCopy
                        if (partsToCopy.Contains("PNAM")) patchedNpc.HeadParts.SetTo(template.HeadParts);
                        if (partsToCopy.Contains("WNAM")) patchedNpc.WornArmor.SetTo(template.WornArmor);
                        if (partsToCopy.Contains("QNAM")) patchedNpc.TextureLighting = template.TextureLighting;
                        if (partsToCopy.Contains("NAM9") && template.FaceMorph != null) patchedNpc.FaceMorph = template.FaceMorph.DeepCopy();
                        if (partsToCopy.Contains("NAMA")) patchedNpc.FaceParts = template.FaceParts?.DeepCopy();
                        if (partsToCopy.Contains("Tint Layers") && template.TintLayers != null) patchedNpc.TintLayers.SetTo(template.TintLayers.Select(t => t.DeepCopy()));
                        if (partsToCopy.Contains("FTST")) patchedNpc.HeadTexture.SetTo(template.HeadTexture);
                        if (partsToCopy.Contains("HCLF")) patchedNpc.HairColor.SetTo(template.HairColor);

                        // Set Female flag
                        patchedNpc.Configuration.Flags |= NpcConfiguration.Flag.Female;

                        // Swap voice type
                        if (npc.Voice != null)
                        {
                            var voiceTypeGetter = npc.Voice.TryResolve(state.LinkCache);
                            var voiceType = voiceTypeGetter?.EditorID;
                            if (voiceType != null && voiceTypeMap.TryGetValue(voiceType, out var femaleVoiceType))
                            {
                                // Direct mapping found
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
                                // No mapping found, pick a random fallback voice for the race
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

                        // Set height and weight (female norms or from template)
                        patchedNpc.Height = template.Height != 0.0f ? template.Height : 1.0f; // Default female height
                        patchedNpc.Weight = template.Weight != 0.0f ? template.Weight : 50.0f; // Default female weight

                        // Copy facegen files
                        var outputModFolder = "G:\\LoreRim\\mods\\SkyFem Patcher";
                        var patchedNifPath = Path.Combine(outputModFolder, "meshes", "actors", "character", "facegendata", "facegeom", state.PatchMod.ModKey.FileName, $"00{npcFid}.nif");
                        var patchedDdsPath = Path.Combine(outputModFolder, "textures", "actors", "character", "facegendata", "facetint", state.PatchMod.ModKey.FileName, $"00{npcFid}.dds");

                        Console.WriteLine($"Checking facegen - Geom Src: {templateNifPath}, Exists: {File.Exists(templateNifPath)}");
                        Console.WriteLine($"Checking facegen - Tint Src: {templateDdsPath}, Exists: {File.Exists(templateDdsPath)}");

                        bool nifCopied = false, ddsCopied = false;
                        var nifDir = Path.GetDirectoryName(patchedNifPath) ?? throw new InvalidOperationException("NIF path directory is null");
                        var ddsDir = Path.GetDirectoryName(patchedDdsPath) ?? throw new InvalidOperationException("DDS path directory is null");

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

                    // Fallback if all templates fail
                    if (!facegenCopied)
                    {
                        if (successfulTemplatesByRace.TryGetValue(race, out var successfulTemplates) && successfulTemplates.Count > 0)
                        {
                            template = successfulTemplates[random.Next(successfulTemplates.Count)];
                            var fallbackNifPath = Path.Combine(state.DataFolderPath, "meshes", "actors", "character", "facegendata", "facegeom", template.FormKey.ModKey.FileName, $"00{template.FormKey.IDString()}.nif");
                            var fallbackDdsPath = Path.Combine(state.DataFolderPath, "textures", "actors", "character", "facegendata", "facetint", template.FormKey.ModKey.FileName, $"00{template.FormKey.IDString()}.dds");
                            var outputModFolder = "G:\\LoreRim\\mods\\SkyFem Patcher";
                            var patchedNifPath = Path.Combine(outputModFolder, "meshes", "actors", "character", "facegendata", "facegeom", state.PatchMod.ModKey.FileName, $"00{npcFid}.nif");
                            var patchedDdsPath = Path.Combine(outputModFolder, "textures", "actors", "character", "facegendata", "facetint", state.PatchMod.ModKey.FileName, $"00{npcFid}.dds");
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
                            // No successful templates available for this race; skip patching entirely
                            Console.WriteLine($"Failed to patch {npc.EditorID ?? "Unnamed"} ({npcFid}) - no valid templates or successful fallbacks available for race {race}. NPC will remain unchanged.");
                            continue;
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"No female templates found for race {race} for NPC {npc.EditorID ?? "Unnamed"} ({npcFid})");
                }
            }

            // Log a summary of skipped templates
            if (skippedTemplates.Count != 0)
            {
                Console.WriteLine("\nSummary of Skipped Templates:");
                foreach (var (templateId, (modName, reason)) in skippedTemplates)
                {
                    Console.WriteLine($"- Template: {templateId}, Mod: {modName}, Reason: {reason}");
                }
                Console.WriteLine("If you encounter issues with these templates, consider adding the listed mods to 'SkyFem blacklist.txt' in the SkyFem Patcher mod folder.");
            }

            Console.WriteLine($"Successfully patched {successfulPatches} out of {maleNpcCount} male NPCs with facegen.");
        }
    }
}