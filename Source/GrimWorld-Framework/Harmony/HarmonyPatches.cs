﻿using System;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GW_Frame
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        private static readonly Type patchType = typeof(HarmonyPatches);
        private static readonly Color FulfilledPrerequisiteColor = ColorLibrary.Green;
        private static readonly Color MissingPrerequisiteColor = ColorLibrary.RedReadable;
        
        static HarmonyPatches()
        {
            Harmony harmony = new("Rimworld.Grimworld.Framework.main");
            
            harmony.Patch(AccessTools.Method(typeof(EquipmentUtility), nameof(EquipmentUtility.CanEquip), 
                    new[] { typeof(Thing), typeof(Pawn), typeof(string).MakeByRefType(), typeof(bool) }),
                postfix: new HarmonyMethod(patchType, nameof(CanEquipPostfix)));
            
            harmony.Patch(AccessTools.Method(typeof(MainTabWindow_Research), "DrawResearchPrerequisites"),
                prefix: new HarmonyMethod(patchType, nameof(DrawResearchPrerequisitesPrefix)));
            
            harmony.Patch(AccessTools.PropertyGetter(typeof(ResearchProjectDef), nameof(ResearchProjectDef.PrerequisitesCompleted)),
                postfix: new HarmonyMethod(patchType, nameof(PrerequisitesCompletedPostFix)));
            
            harmony.Patch(AccessTools.Method(typeof(MainTabWindow_Research), "DrawBottomRow"),
                prefix: new HarmonyMethod(patchType, nameof(DrawBottomRowPreFix)));
            
            harmony.Patch(AccessTools.Method(typeof(Pawn_EquipmentTracker), "MakeRoomFor"),
                postfix: new HarmonyMethod(patchType, nameof(DropShieldIfEquippedTwoHandedPostFix)));
            
            harmony.Patch(AccessTools.Method(typeof(Pawn_ApparelTracker), "Wear"),
                postfix: new HarmonyMethod(patchType, nameof(DropTwoHandedIfEquippedShieldPostFix)));
            
            //TODO: Temporarily disabled  until it's purpose can be ascertained (genuinely WTF does this do?) 
            //harmony.Patch(AccessTools.Method(typeof(Log), "ResetMessageCount"),
            //    postfix: new HarmonyMethod(patchType, nameof(ResetMessageCountPostfix)));
        }
        
        public static void ResetMessageCountPostfix()
        {
            Settings.Settings.Instance?.CastChanges();
            ThingCategoryDef.Named("GW_Shield").ResolveReferences();
            ThingCategoryDef.Named("GW_TwoHanded").ResolveReferences();
        }
        
        public static void CanEquipPostfix(ref bool __result, Thing thing, Pawn pawn, ref string cantReason)
        {
            EquipRestrictExtension extension = thing.def.GetModExtension<EquipRestrictExtension>();
            if (extension != null && __result)
            {       // Attempt to get the various limiting lists
                List<GeneDef> requiredGenesToEquip = extension.requiredGenesToEquip;
                List<GeneDef> requireOneOfGenesToEquip = extension.requireOneOfGenesToEquip;
                List<GeneDef> forbiddenGenesToEquip = extension.forbiddenGenesToEquip;
                List<XenotypeDef> requireOneOfXenotypeToEquip = extension.requireOneOfXenotypeToEquip;
                List<XenotypeDef> forbiddenXenotypesToEquip = extension.forbiddenXenotypesToEquip;
                List<HediffDef> requiredHediffsToEquip = extension.requiredHediffsToEquip;
                List<HediffDef> requireOneOfHediffsToEquip = extension.requireOneOfHediffsToEquip;
                List<HediffDef> forbiddenHediffsToEquip = extension.forbiddenHediffsToEquip;
                // Gene Check
                if (!pawn.genes.GenesListForReading.NullOrEmpty())
                {
                    Pawn_GeneTracker currentGenes = pawn.genes;
                    if (!requiredGenesToEquip.NullOrEmpty() || !requireOneOfGenesToEquip.NullOrEmpty() || !forbiddenGenesToEquip.NullOrEmpty() ||
                        !requireOneOfXenotypeToEquip.NullOrEmpty() || !forbiddenXenotypesToEquip.NullOrEmpty())
                    {
                        bool flag = true;
                        if (!requireOneOfXenotypeToEquip.NullOrEmpty() && !requireOneOfXenotypeToEquip.Contains(pawn.genes.Xenotype) && flag)
                        {
                            if (requireOneOfXenotypeToEquip.Count > 1) cantReason = "GW_XenoRestrictedEquipment_AnyOne".Translate();
                            else cantReason = "GW_XenoRestrictedEquipment_One".Translate(requireOneOfXenotypeToEquip[0].label);
                            flag = false;
                        }
                        if (!forbiddenXenotypesToEquip.NullOrEmpty() && forbiddenXenotypesToEquip.Contains(pawn.genes.Xenotype) && flag)
                        {
                            cantReason = "GW_XenoRestrictedEquipment_None".Translate(pawn.genes.Xenotype.label);
                            flag = false;
                        }
                        if (!forbiddenGenesToEquip.NullOrEmpty() && flag)
                        {
                            foreach (Gene gene in currentGenes.GenesListForReading)
                            {
                                if (forbiddenGenesToEquip.Contains(gene.def))
                                {
                                    cantReason = "GW_GeneRestrictedEquipment_None".Translate(gene.def.label);
                                    flag = false;
                                    break;
                                }
                            }
                        }
                        if (!requiredGenesToEquip.NullOrEmpty() && flag)
                        {
                            foreach (Gene gene in currentGenes.GenesListForReading)
                            {
                                if (requiredGenesToEquip.Contains(gene.def)) requiredGenesToEquip.Remove(gene.def);
                            }
                            if (!requiredGenesToEquip.NullOrEmpty())
                            {
                                if (extension.requiredGenesToEquip.Count > 1) cantReason = "GW_GeneRestrictedEquipment_All".Translate();
                                else cantReason = "GW_GeneRestrictedEquipment_One".Translate(extension.requiredGenesToEquip[0].label);
                                flag = false;
                            }
                        }
                        if (!requireOneOfGenesToEquip.NullOrEmpty() && flag)
                        {
                            flag = false;
                            if (requireOneOfGenesToEquip.Count > 1) cantReason = "GW_GeneRestrictedEquipment_AnyOne".Translate();
                            else cantReason = "GW_GeneRestrictedEquipment_One".Translate(requireOneOfGenesToEquip[0].label);
                            foreach (Gene gene in currentGenes.GenesListForReading)
                            {
                                if (requiredGenesToEquip.Contains(gene.def))
                                {
                                    flag = true;
                                    cantReason = null;
                                    break;
                                }
                            }
                        }
                        __result = flag;
                    }
                }
                else
                {
                    if (!requiredGenesToEquip.NullOrEmpty() || !requireOneOfGenesToEquip.NullOrEmpty() || !requireOneOfXenotypeToEquip.NullOrEmpty())
                    {
                        cantReason = "GW_GenesNotFound".Translate();
                        __result = false;
                    }
                }

                // Hediff Check
                HediffSet hediffSet = pawn.health.hediffSet;
                if (__result && !hediffSet.hediffs.NullOrEmpty())
                {
                    if (!requiredHediffsToEquip.NullOrEmpty() || !requireOneOfHediffsToEquip.NullOrEmpty() || !forbiddenHediffsToEquip.NullOrEmpty())
                    {
                        bool flag = true;
                        if (!forbiddenHediffsToEquip.NullOrEmpty())
                        {
                            foreach (HediffDef hediffDef in forbiddenHediffsToEquip)
                            {
                                if (hediffSet.HasHediff(hediffDef))
                                {
                                    cantReason = "GW_HediffRestrictedEquipment_None".Translate(hediffDef.label);
                                    flag = false;
                                    break;
                                }
                            }
                        }

                        if (flag && !requireOneOfHediffsToEquip.NullOrEmpty())
                        {
                            flag = false;
                            foreach (HediffDef hediffDef in requireOneOfHediffsToEquip)
                            {
                                if (hediffSet.HasHediff(hediffDef))
                                {
                                    flag = true;
                                    break;
                                }
                            }
                            if (!flag)
                            {
                                if (requireOneOfHediffsToEquip.Count > 1) cantReason = "GW_HediffRestrictedEquipment_AnyOne".Translate();
                                else cantReason = "GW_HediffRestrictedEquipment_One".Translate(requireOneOfHediffsToEquip[0].label);
                            }
                        }

                        if (flag && !requiredHediffsToEquip.NullOrEmpty())
                        {
                            foreach (Hediff hediff in hediffSet.hediffs)
                            {
                                if (requiredHediffsToEquip.Contains(hediff.def)) requiredHediffsToEquip.Remove(hediff.def);
                            }
                            if (!requiredHediffsToEquip.NullOrEmpty())
                            {
                                if (extension.requiredHediffsToEquip.Count > 1) cantReason = "GW_HediffRestrictedEquipment_All".Translate();
                                else "GW_HediffRestrictedEquipment_One".Translate(extension.requiredHediffsToEquip[0].label);
                                flag = false;
                            }
                        }

                        __result = flag;
                    }
                }
            }
        }
        
        public static bool DrawResearchPrerequisitesPrefix(MainTabWindow_Research __instance, Rect rect, ref float y, ResearchProjectDef project)
        {
            bool flag = false;
            if (project.HasModExtension<DefModExtension_ExtraPrerequisiteActions>())
            {
                float xMin = rect.xMin;
                var modExtension = project.GetModExtension<DefModExtension_ExtraPrerequisiteActions>();
                if (!project.prerequisites.NullOrEmpty())
                {
                    Widgets.LabelCacheHeight(ref rect, "Prerequisites".Translate() + ":");
                    rect.yMin += rect.height;
                    rect.xMin += 6f;
                    for (int i = 0; i < project.prerequisites.Count; i++)
                    {
                        SetPrerequisiteStatusColor(project.prerequisites[i].IsFinished, project);
                        Widgets.LabelCacheHeight(ref rect, project.prerequisites[i].LabelCap);
                        rect.yMin += rect.height;
                    }
                    GUI.color = Color.white;
                    rect.xMin -= 6f;
                }
                if (!Find.World.GetComponent<WorldComponent_StudyManager>().CompletedAllRequirements(project))
                {
                    Widgets.LabelCacheHeight(ref rect, "GW_RequiresStudyOf".Translate());
                    rect.xMin += 6f;
                    rect.yMin += rect.height;

                    var stcManager = Find.World.GetComponent<WorldComponent_StudyManager>();
                    GUI.color = MissingPrerequisiteColor;
                    foreach (var req in modExtension.ItemStudyRequirements)
                    {
                        if (stcManager.CompletedRequirement(project, req.StudyObject))
                            GUI.color = FulfilledPrerequisiteColor;
                        else
                            GUI.color = MissingPrerequisiteColor;

                        string numRequired = "GW_MoreNeeded".Translate(req.NumberRequired);
                        string reqLabel = req.StudyObject.LabelCap;
                        string atCogitator;
                        if (modExtension.StudyLocation != null)
                        {
                            atCogitator = "GW_StudyAt".Translate(modExtension.StudyLocation.LabelCap);
                        }
                        else
                        {
                            atCogitator = "GW_StudyAt".Translate("nowhere. Please set StudyLocation.");
                        }
                        var labelPart1Size = Text.CalcSize(numRequired);
                        var stcFragmentsSize = Text.CalcSize(reqLabel);
                        var num = labelPart1Size.y;
                        rect.height = num;
                        Widgets.Label(rect, numRequired);
                        rect.x += labelPart1Size.x;
                        Dialog_InfoCard.Hyperlink hyperlink = new Dialog_InfoCard.Hyperlink(req.StudyObject);
                        Widgets.ButtonText(rect, reqLabel, drawBackground: false, doMouseoverSound: false, active: false);
                        if (Widgets.ButtonInvisible(rect))
                        {
                            hyperlink.ActivateHyperlink();
                        }
                        rect.x += stcFragmentsSize.x;
                        Widgets.Label(rect, atCogitator);
                        rect.x -= stcFragmentsSize.x + labelPart1Size.x;

                        rect.yMin += rect.height;
                    }
                }
                else
                {
                    GUI.color = FulfilledPrerequisiteColor;
                    Widgets.LabelCacheHeight(ref rect, "GW_DiscoveredResearch".Translate());
                    rect.yMin += rect.height;
                }
                GUI.color = Color.white;
                rect.xMin = xMin;
                
                y = rect.yMin;
                flag = true;
            }
            return !flag;
        }

        private static void SetPrerequisiteStatusColor(bool present, ResearchProjectDef project)
        {
            if (!project.IsFinished)
            {
                if (present)
                {
                    GUI.color = FulfilledPrerequisiteColor;
                }
                else
                {
                    GUI.color = MissingPrerequisiteColor;
                }
            }
        }

        public static void PrerequisitesCompletedPostFix(ref bool __result, ResearchProjectDef __instance)
        {
            if (__result && __instance.HasModExtension<DefModExtension_ExtraPrerequisiteActions>())
                __result = Find.World.GetComponent<WorldComponent_StudyManager>().CompletedAllRequirements(__instance);
        }

        public static bool DrawBottomRowPreFix(MainTabWindow_Research __instance, Rect rect, ResearchProjectDef project, Color techprintColor, Color studiedColor)
        {
            if (!project.HasModExtension<DefModExtension_ExtraPrerequisiteActions>() || project.TechprintCount > 0 || project.RequiredAnalyzedThingCount > 0)
                return true;

            DefModExtension_ExtraPrerequisiteActions modExtension = project.GetModExtension<DefModExtension_ExtraPrerequisiteActions>();
            WorldComponent_StudyManager stcManager = Find.World.GetComponent<WorldComponent_StudyManager>();

            Color color = GUI.color;
            TextAnchor anchor = Text.Anchor;

            float num = rect.width / 2;

            Rect rect2 = rect;
            rect2.x = rect.x;
            rect2.width = num;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect2, project.CostApparent.ToString(CultureInfo.CurrentCulture));
            rect2.x += num;
            foreach (StudyRequirement req in modExtension.ItemStudyRequirements)
            {
                bool complete = stcManager.CompletedRequirement(project, req.StudyObject);
                string text = $"{(complete ? req.NumberRequired : 0)} / {req.NumberRequired}";
                Vector2 textAreaSize = Text.CalcSize(text);
                Rect rect3 = rect2;
                rect3.xMin = rect2.xMax - textAreaSize.x - 10f;
                Rect rect4 = rect2;
                rect4.width = rect4.height;
                rect4.x = rect3.x - rect4.width;
                GUI.color = complete ? Color.green : ColorLibrary.RedReadable;
                Widgets.Label(rect3, text);
                GUI.color = Color.white;
                GUI.DrawTexture(rect4.ContractedBy(3f), req.StudyObject.uiIcon);
                rect2.x += num;
                GUI.color = color;
                Text.Anchor = anchor;
                return false;
            }

            return true;
        }

        public static void DropShieldIfEquippedTwoHandedPostFix(Pawn_EquipmentTracker __instance, ThingWithComps eq)
        {
            if (!eq.HasThingCategory(DefDatabase<ThingCategoryDef>.GetNamed("GW_TwoHanded"))) return;

            var pawnApparelTracker = __instance.pawn.apparel;
            var allWornApparels = pawnApparelTracker.WornApparel;

            foreach (var wornApparel in allWornApparels)
            {
                if (wornApparel.HasThingCategory(DefDatabase<ThingCategoryDef>.GetNamed("GW_Shield")))
                {
                    pawnApparelTracker.TryDrop(wornApparel);
                    break;
                }
            }
        }

        public static void DropTwoHandedIfEquippedShieldPostFix(Pawn_ApparelTracker __instance, Apparel newApparel)
        {
            if (!newApparel.HasThingCategory(DefDatabase<ThingCategoryDef>.GetNamed("GW_Shield"))) return;

            var pawnPrimaryWeapon = __instance.pawn.equipment.Primary;

            if(pawnPrimaryWeapon != null && pawnPrimaryWeapon.HasThingCategory(DefDatabase<ThingCategoryDef>.GetNamed("GW_TwoHanded"))) {
                __instance.pawn.equipment.TryDropEquipment(pawnPrimaryWeapon, out var resultEq, __instance.pawn.Position, false);
            }
        }
    }
}
