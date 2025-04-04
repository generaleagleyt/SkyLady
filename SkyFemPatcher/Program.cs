using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;
using Noggog;

namespace SkyFemPatcher
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .Run(args, new RunPreferences());
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            Console.WriteLine("SkyFem Patcher (Side) running on .NET 8.0...");
            var racesPath = Path.Combine(state.DataFolderPath, "..", "..", "mods", "SkyFem Patcher", "SkyFem races.txt");
            var partsToCopyPath = Path.Combine(state.DataFolderPath, "..", "..", "mods", "SkyFem Patcher", "SkyFem partsToCopy.txt");
            var humanoidRaces = new HashSet<string>(File.ReadAllLines(racesPath).Select(line => line.Split(' ')[0].Trim()));
            var partsToCopy = File.ReadAllLines(partsToCopyPath).ToHashSet();
            var femaleTemplatesByRace = new Dictionary<string, List<INpcGetter>>();
            var random = new Random();
            var requiemKey = ModKey.FromNameAndExtension("Coldhaven.esm");

            // Collect female templates
            foreach (var npc in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
            {
                var race = npc.Race.TryResolve(state.LinkCache)?.EditorID;
                if (race != null && humanoidRaces.Contains(race) && npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female))
                {
                    femaleTemplatesByRace[race] = femaleTemplatesByRace.GetValueOrDefault(race, new List<INpcGetter>());
                    femaleTemplatesByRace[race].Add(npc);
                }
            }
            Console.WriteLine($"Collected templates for {femaleTemplatesByRace.Count} races.");

            // Patch Coldhaven male NPCs
            foreach (var npc in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
            {
                var race = npc.Race.TryResolve(state.LinkCache)?.EditorID;
                if (race == null || !humanoidRaces.Contains(race) || npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female) || npc.FormKey.ModKey != requiemKey)
                    continue;

                if (femaleTemplatesByRace.TryGetValue(race, out var templates) && templates.Count > 0)
                {
                    var template = templates[random.Next(templates.Count)];
                    var patchedNpc = state.PatchMod.Npcs.GetOrAddAsOverride(npc);

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
                    var npcFileName = npc.FormKey.ModKey.FileName;
                    var npcFid = npc.FormKey.IDString();
                    var templateFileName = template.FormKey.ModKey.FileName.ToString();
                    var templateFid = template.FormKey.IDString();

                    var faceGeomSrc = Path.Combine(state.DataFolderPath, "meshes", "actors", "character", "facegendata", "facegeom", templateFileName, $"00{templateFid}.nif");
                    var faceTintSrc = Path.Combine(state.DataFolderPath, "textures", "actors", "character", "facegendata", "facetint", templateFileName, $"00{templateFid}.dds");
                    var faceGeomDest = Path.Combine("G:\\LoreRim\\mods\\SkyFem Patcher", "meshes", "actors", "character", "facegendata", "facegeom", state.PatchMod.ModKey.FileName, $"00{npcFid}.nif");
                    var faceTintDest = Path.Combine("G:\\LoreRim\\mods\\SkyFem Patcher", "textures", "actors", "character", "facegendata", "facetint", state.PatchMod.ModKey.FileName, $"00{npcFid}.dds");

                    Console.WriteLine($"Checking facegen - Geom Src: {faceGeomSrc}, Exists: {File.Exists(faceGeomSrc)}");
                    Console.WriteLine($"Checking facegen - Tint Src: {faceTintSrc}, Exists: {File.Exists(faceTintSrc)}");

                    if (File.Exists(faceGeomSrc))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(faceGeomDest)!);
                        File.Copy(faceGeomSrc, faceGeomDest, true);
                        Console.WriteLine($"Copied facegen to: {faceGeomDest}");
                    }
                    else
                    {
                        Console.WriteLine($"Warning: No facegeom file found for {template.EditorID ?? "Unnamed"} ({templateFid}) in {templateFileName}");
                    }
                    if (File.Exists(faceTintSrc))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(faceTintDest)!);
                        File.Copy(faceTintSrc, faceTintDest, true);
                        Console.WriteLine($"Copied facegen to: {faceTintDest}");
                    }
                    else
                    {
                        Console.WriteLine($"Warning: No facetint file found for {template.EditorID ?? "Unnamed"} ({templateFid}) in {templateFileName}");
                    }

                    Console.WriteLine($"Patched Male NPC: {npc.EditorID ?? "Unnamed"} with {template.EditorID ?? "Unnamed"} (Race: {race})");
                }
            }
        }
    }
}