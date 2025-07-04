﻿using GW4KArmor.Data;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace GW4KArmor.Patches
{
    internal static class HarmonyPatches
    {
        [HarmonyPatch(typeof(ApparelGraphicRecordGetter), nameof(ApparelGraphicRecordGetter.TryGetGraphicApparel))]
        public static class ApparelGraphicRecordGetter_TryGetGraphicApparel
        {
            public static void Postfix(Apparel apparel, bool __result, ref ApparelGraphicRecord rec)
            {
                bool flag = !__result;

                if (flag)
                    return;
                
                Comp_TriColorMask triColorMaskComp = apparel?.GetComp<Comp_TriColorMask>();
                bool flag2 = triColorMaskComp == null;
                
                if (!flag2)
                {
                    rec.graphic = TriMaskGraphicPool.GraphicFromComp<Graphic_TriColorMask>(triColorMaskComp);
                }
            }
        }

        [HarmonyPatch(typeof(GraphicData), nameof(GraphicData.GraphicColoredFor))]
        public static class GraphicData_GraphicColoredFor
        {
            // Token: 0x06000069 RID: 105 RVA: 0x00004454 File Offset: 0x00002654
            public static bool Prefix(Thing t, ref Graphic __result)
            {
                if (t is not ThingWithComps thingWithComps)
                    return true;

                Comp_TriColorMask comp = thingWithComps.GetComp<Comp_TriColorMask>();
                
                if (comp == null)
                    return true;

                Graphic graphic;
                
                if (t is Apparel)
                {
                    graphic = TriMaskGraphicPool.GraphicFromComp<Graphic_TriColorMask>(comp);
                }
                else
                {
                    graphic = TriMaskGraphicPool.GraphicFromComp<Graphic_TriColorMask_Single>(comp);
                }
                
                __result = graphic;
                return false;
            }
        }

        [HarmonyPatch(typeof(ShaderUtility), nameof(ShaderUtility.SupportsMaskTex))]
        public static class ShaderUtility_SupportsMaskTex
        {
            public static void Postfix(Shader shader, ref bool __result)
            {
                if (shader == Core.MaskShader)
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(ThingIDMaker), nameof(ThingIDMaker.GiveIDTo))]
        public static class ThingIDPatch
        {
            [HarmonyPriority(800)]
            private static bool Prefix(Thing t)
            {
                if (active == 0)
                    return true;
                
                t.thingIDNumber = 69420;
                return false;
            }

            private static int active;

            public readonly ref struct Scope
            {
                public Scope(bool uselessFlag = true)
                {
                    active++;
                }

                public void Dispose()
                {
                    bool flag = active > 0;
                    
                    if (flag)
                    {
                        active--;
                    }
                }
            }
        }

        //Inject Painting-Tool Gizmo on items with PaintableThingExtension
        [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
        public static class Pawn_GetGizmosPatch
        {
            private static List<ThingWithComps> tempList = [];
            
            public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
            {
                tempList.Clear();
                if (__instance.equipment == null || __instance.apparel == null) 
                    return;
                
                tempList.AddRange(__instance.equipment.AllEquipmentListForReading);
                tempList.AddRange(__instance.apparel.WornApparel);
                    
                var comps = tempList
                    .Select(a => 
                        a.GetComp<Comp_TriColorMask>())
                    .Where(c => c != null)?
                    .ToList();

                if (comps.Count < 1) 
                    return;
                    
                Comp_TriColorMask firstComp = comps.First();
                    
                switch (comps.Count)
                {
                    case 1:
                    {
                        Gizmo_Paintable gizmo = firstComp.PaintGizmo;
                        __result = __result.Append(gizmo);
                        break;
                    }
                    case > 1:
                    {
                        Gizmo_PaintableMulti gizmo = firstComp.PaintGizmoMulti;
                        gizmo.pawn = __instance;
                        __result = __result.Append(gizmo);
                        break;
                    }
                }
            }
        }
    }
}