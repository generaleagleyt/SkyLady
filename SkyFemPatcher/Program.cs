using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;
using Mutagen.Bethesda.Plugins.Assets;

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

                    // Debug facegen assets
                    var npcFid = npc.FormKey.IDString();
                    var templateFid = template.FormKey.IDString();
                    Console.WriteLine($"Template: {template.EditorID ?? "Unnamed"} ({templateFid})");
                    var listedAssets = template.EnumerateAssetLinks(AssetLinkQuery.Listed).ToList();
                    var inferredAssets = template.EnumerateAssetLinks(AssetLinkQuery.Inferred).ToList();
                    Console.WriteLine($"Listed Assets ({listedAssets.Count}):");
                    if (listedAssets.Count == 0)
                    {
                        Console.WriteLine("  No listed asset links found.");
                    }
                    else
                    {
                        foreach (var asset in listedAssets)
                        {
                            Console.WriteLine($"  Listed Asset: {asset} (Type: {asset.GetType().Name})");
                        }
                    }
                    Console.WriteLine($"Inferred Assets ({inferredAssets.Count}):");
                    if (inferredAssets.Count == 0)
                    {
                        Console.WriteLine("  No inferred asset links found.");
                    }
                    else
                    {
                        foreach (var asset in inferredAssets)
                        {
                            Console.WriteLine($"  Inferred Asset: {asset} (Type: {asset.GetType().Name})");
                        }
                    }

                    Console.WriteLine($"Patched Male NPC: {npc.EditorID ?? "Unnamed"} with {template.EditorID ?? "Unnamed"} (Race: {race})");
                }
            }
        }
    }
}