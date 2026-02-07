using ScoopOfJamMod.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace ScoopOfJamMod.CollectibleBehaviors;
public class BehaviorExtraSatiety : CollectibleBehavior {
    public BehaviorExtraSatiety(CollectibleObject collObj) : base(collObj) {
    }

    public void ProcessTryEatStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, Action baseTryEatStop) {
        var stackSizePre = slot.StackSize;
        var extraNutritionProps = ExtraNutritionPropsAttribute.FromTreeAttribute(slot.Itemstack?.Attributes);
        baseTryEatStop();
        //base.tryEatStop(secondsUsed, slot, byEntity);
        if (byEntity.World.Side.IsServer()) {
            // Eaten
            if (stackSizePre > slot.StackSize && extraNutritionProps != null) {
                GiveExtraSatiety(extraNutritionProps, byEntity);
            }
        }
    }

    public static void GiveExtraSatiety(ExtraNutritionPropsAttribute extraNutritionPropsAttribute, EntityAgent byEntity) {
        foreach (var (cat, val) in extraNutritionPropsAttribute.extraNutrition) {
            byEntity.ReceiveSaturation(val, cat);
        }
    }

    public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe, ref EnumHandling bhHandling) {
        base.OnCreatedByCrafting(allInputslots, outputSlot, byRecipe, ref bhHandling);

        if (outputSlot.Itemstack == null) return;

        var extraNut = new Dictionary<EnumFoodCategory, float>();

        var existingNutrition = outputSlot.Itemstack?.Collectible.NutritionProps;

        foreach (var inputSlot in allInputslots) {
            if (inputSlot.Itemstack == null) continue;

            var nutritionProps = inputSlot.Itemstack.Collectible.NutritionProps;
            if (nutritionProps == null) continue;
            // TODO: calc from the recipe (unable because we can't refer to ingredients on server side) 
            var quantity = 1;

            // TODO: GetNutritionProperties() (how to get world here)


            // Skip existing nutrition category in the output item
            // TODO: Clean up? Maybe food with extraNutrition shouldn't have vanilla nutrition
            if (nutritionProps.FoodCategory == existingNutrition?.FoodCategory && existingNutrition.Satiety > 0) continue;
            // Set extra nutrition
            if (!extraNut.ContainsKey(nutritionProps.FoodCategory)) extraNut[nutritionProps.FoodCategory] = 0.0f;
            extraNut[nutritionProps.FoodCategory] += nutritionProps.Satiety * quantity;
            //extraNut[inputSlot.Itemstack.NutritionProps.FoodCategory] = scoopOfJamItem.NutritionProps.Satiety * quantity;
        }

        if (extraNut.Count > 0) {
            var extraNutProps = new ExtraNutritionPropsAttribute(extraNut);
            extraNutProps.ToTreeAttribute(outputSlot.Itemstack.Attributes);
        }
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        if (inSlot.Itemstack == null) return;

        // Split to lines
        var lines = dsc
                .ToString()
                .TrimEnd()
                .Split('\n')
                .Select(l => l.TrimEnd('\r'))
                .ToList();

        var nutritionFacts = GetNutritionFactsDesc(inSlot.Itemstack);
        if (nutritionFacts == null) return;

        if (lines.Count >= 2) {
            var deleteIndex = lines.Count - 2;
            lines.RemoveAt(deleteIndex);
            lines.RemoveAt(deleteIndex);
            lines.Insert(deleteIndex, nutritionFacts);

            dsc.Clear();
            foreach (var line in lines) {
                dsc.AppendLine(line);
            }
        }
        else {
            dsc.AppendLine(nutritionFacts);
        }

        //api.Logger.Event(dsc.ToString());
        //sb.AppendLine(Lang.Get("Nutrition Facts"));
        //sb.AppendLine(Lang.Get("nutrition-facts-line-satiety", Lang.Get("foodcategory-" + val.Key.ToString().ToLowerInvariant()), Math.Round(val.Value)));
    }

    public static string? GetNutritionFactsDesc(ItemStack stack) {
        var totalNutrition = new Dictionary<EnumFoodCategory, float>();
        var baseNut = stack.Collectible?.NutritionProps;
        if (baseNut != null) {
            totalNutrition[baseNut.FoodCategory] = baseNut.Satiety;
        }

        var extraNutritionProps = ExtraNutritionPropsAttribute.FromTreeAttribute(stack.Attributes);
        bool noExtraNutrition = false;

        if (extraNutritionProps != null) {
            foreach (var (cat, sat) in extraNutritionProps.extraNutrition) {
                if (totalNutrition.ContainsKey(cat)) {
                    totalNutrition[cat] += sat;
                }
                else {
                    totalNutrition[cat] = sat;
                }
            }
        }
        else {
            noExtraNutrition = true;
        }

        if (totalNutrition.Count == 0) return null;

        var sb = new StringBuilder();

        sb.AppendLine(Lang.Get("Nutrition Facts"));
        foreach (var (cat, sat) in totalNutrition) {
            sb.AppendLine(Lang.Get("nutrition-facts-line-satiety", Lang.Get("foodcategory-" + cat.ToString().ToLowerInvariant()), Math.Round(sat)));
        }

        return sb.ToString().TrimEnd();
    }

}

public record class ExtraNutritionPropsAttribute(
    Dictionary<EnumFoodCategory, float> extraNutrition
// Health, etc...
) {
    public void ToTreeAttribute(ITreeAttribute attr) {
        if (extraNutrition.Count == 0) return;
        var extraNutDictTree = new TreeAttribute();

        foreach (var kv in extraNutrition) {
            // To make it compatible with json item stack (in creativeInventoryStacks), we store value as double
            extraNutDictTree.SetDouble(kv.Key.ToString(), kv.Value);
        }

        attr["extraNutrition"] = extraNutDictTree;
    }

    public static ExtraNutritionPropsAttribute? FromTreeAttribute(ITreeAttribute? attr) {
        var tree = attr?["extraNutrition"];
        if (tree == null || tree is not TreeAttribute extraNutDictTree) return null;

        var extraNut = new Dictionary<EnumFoodCategory, float>();
        foreach (var key in extraNutDictTree.Keys) {
            if (Enum.TryParse(key, out EnumFoodCategory cat)) {
                var satVal = extraNutDictTree.GetDouble(key);
                extraNut[cat] = (float)satVal;
            }
        }

        return new ExtraNutritionPropsAttribute(extraNut);
    }
}
