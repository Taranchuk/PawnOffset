using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PawnOffset
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {

        public static Dictionary<ThingDef, List<PawnOffsetDef>> allPawnOffsetSittableTriggers = new Dictionary<ThingDef, List<PawnOffsetDef>>();
        static HarmonyPatches()
        {
            var harmony = new Harmony("PawnOffset.Mod");
            harmony.PatchAll();
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (thingDef.building != null && (thingDef.building.isSittable || thingDef.building.multiSittable) && thingDef.graphicData != null)
                {
                    if (thingDef.graphicData.drawOffset != null)
                    {
                        thingDef.graphicData.drawOffset.y += 0.1f;
                    }
                    else
                    {
                        thingDef.graphicData.drawOffset = new Vector3(0, 0.1f, 0);
                    }
                }

                else if (thingDef.race != null)
                {
                    if (!allPawnOffsetSittableTriggers.ContainsKey(thingDef))
                    {
                        allPawnOffsetSittableTriggers[thingDef] = new List<PawnOffsetDef>();
                    }

                    foreach (var def in DefDatabase<PawnOffsetDef>.AllDefs)
                    {
                        if (thingDef.race.Humanlike && def.anyHumanlikes || def.races != null && def.races.Contains(thingDef))
                        {
                            allPawnOffsetSittableTriggers[thingDef].Add(def);
                        }
                    }

                    var things = allPawnOffsetSittableTriggers[thingDef].Where(x => x.things != null);
                    var anySittables = allPawnOffsetSittableTriggers[thingDef].Where(x => x.onAnySittable);
                    allPawnOffsetSittableTriggers[thingDef] = allPawnOffsetSittableTriggers[thingDef].OrderByDescending(x => x.things != null).ToList();
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_DrawTracker), "DrawPos", MethodType.Getter)]
    public class DrawPos_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Pawn_DrawTracker __instance, Pawn ___pawn, ref Vector3 __result)
        {
            if (!___pawn.pather.Moving)
            {
                if (HarmonyPatches.allPawnOffsetSittableTriggers.TryGetValue(___pawn.def, out var sittableOffsetList))
                {
                    var firstBuilding = ___pawn.Position.GetFirstBuilding(___pawn.Map);
                    if (firstBuilding != null)
                    {
                        foreach (var entry in sittableOffsetList)
                        {
                            if (entry.onAnySittable && (firstBuilding.def.building.isSittable || firstBuilding.def.building.multiSittable))
                            {
                                __result += entry.GetDrawOffset(___pawn.Rotation);
                                return;
                            }
                            else if (entry.thingDefsHash != null && entry.thingDefsHash.Contains(firstBuilding.def))
                            {
                                __result += entry.GetDrawOffset(___pawn.Rotation);
                                return;
                            }
                        }
                    }
                }
            }
        }
    }


    public class PawnOffsetDef : Def
    {
        public bool anyHumanlikes;
        public bool onAnySittable;
        public List<ThingDef> races;
        public List<ThingDef> things;
        public Vector3 drawOffsetWest;
        public Vector3 drawOffsetEast;
        public Vector3 drawOffsetSouth;
        public Vector3 drawOffsetNorth;
        public HashSet<ThingDef> thingDefsHash;
        public Vector3 GetDrawOffset(Rot4 rot)
        {
            switch (rot.AsInt)
            {
                case 0: return drawOffsetNorth;
                case 1: return drawOffsetEast;
                case 2: return drawOffsetSouth;
                case 3: return drawOffsetWest;
            }
            return Vector3.zero;
        }

        public override IEnumerable<string> ConfigErrors()
        {
            if (things != null)
            {
                thingDefsHash = things.ToHashSet();
                Log.Message(thingDefsHash?.Count() + " - " + things?.Count());
            }
            return base.ConfigErrors();
        }
    }
}
