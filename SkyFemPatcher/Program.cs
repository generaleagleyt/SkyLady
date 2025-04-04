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
            var humanoidRaces = new HashSet<string>(File.ReadAllLines(racesPath).Select(line => line.Split(' ')[0].Trim()));
            var partsToCopy = File.ReadAllLines(partsToCopyPath).ToHashSet();
            var femaleTemplatesByRace = new Dictionary<string, List<INpcGetter>>();
            var random = new Random();
            var requiemKey = ModKey.FromNameAndExtension("Coldhaven.esm");

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
                        femaleTemplatesByRace[race] = femaleTemplatesByRace.GetValueOrDefault(race, new List<INpcGetter>());
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

            // Patch Coldhaven male NPCs
            foreach (var npc in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
            {
                var race = npc.Race.TryResolve(state.LinkCache)?.EditorID;
                if (race == null || !humanoidRaces.Contains(race) || npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female) || npc.FormKey.ModKey != requiemKey)
                    continue;

                var npcFid = npc.FormKey.IDString();
                if (femaleTemplatesByRace.TryGetValue(race, out var templates) && templates.Count > 0)
                {
                    var patchedNpc = state.PatchMod.Npcs.GetOrAddAsOverride(npc);
                    INpcGetter? template = null; // Nullable INpcGetter
                    bool facegenCopied = false;

                    // Try templates until facegen copies or exhaust options
                    foreach (var candidate in templates.OrderBy(x => random.Next()))
                    {
                        template = candidate;

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

                        // Copy facegen files
                        var templateFid = template.FormKey.IDString();
                        var templateFileName = template.FormKey.ModKey.FileName.ToString();
                        var templateNifPath = Path.Combine(state.DataFolderPath, "meshes", "actors", "character", "facegendata", "facegeom", templateFileName, $"00{templateFid}.nif");
                        var templateDdsPath = Path.Combine(state.DataFolderPath, "textures", "actors", "character", "facegendata", "facetint", templateFileName, $"00{templateFid}.dds");
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
                            Console.WriteLine($"Patched Male NPC: {npc.EditorID ?? "Unnamed"} with {template.EditorID ?? "Unnamed"} (Race: {race})");
                            break; // Stop trying templates if both files copied
                        }
                    }

                    if (!facegenCopied)
                    {
                        Console.WriteLine($"Failed to patch {npc.EditorID ?? "Unnamed"} ({npcFid}) with facegen - no suitable template found.");
                    }
                }
                else
                {
                    Console.WriteLine($"No female templates found for race {race} for NPC {npc.EditorID ?? "Unnamed"} ({npcFid})");
                }
            }
            Console.WriteLine($"Successfully patched {successfulPatches} out of {maleNpcCount} male NPCs with facegen.");
        }
    }
}