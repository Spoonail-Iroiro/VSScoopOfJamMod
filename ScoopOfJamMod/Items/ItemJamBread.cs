using ScoopOfJamMod.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace ScoopOfJamMod.Items;

public record class ExtraNutritionPropsAttribute(
    Dictionary<EnumFoodCategory, float> extraNutrition
// Health, etc...
) {
    public void ToTreeAttribute(ITreeAttribute attr) {
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

public class ItemJamBread : Item {
    public override string GetHeldItemName(ItemStack itemStack) {
        var firstFruit = itemStack.Item?.Variant["fruit"];
        var grain = itemStack.Item?.Variant["type"];
        if (firstFruit == null || grain == null) return base.GetHeldItemName(itemStack);

        var scoopOfJamAttribute = ScoopOfJamAttribute.FromTreeAttribute(itemStack.Attributes);

        var langCode = TrUtil.GetTranslateLocale();

        var jamIngredientText = TrUtil.GetJamIngredientText(api, langCode, firstFruit, scoopOfJamAttribute);
        //jamIngredientText = TrUtil.ToHeadUpperAutoCase(langCode, jamIngredientText);

        var breadName = Lang.GetL(langCode, TrUtil.LK($"jambread-{grain}-perfect-template"), jamIngredientText);

        return breadName;
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        if (inSlot.Itemstack == null) return;

        var lines = dsc
                .ToString()
                .TrimEnd()
                .Split('\n')
                .Select(l => l.TrimEnd('\r'))
                .ToList();

        var nutritionFacts = GetNutritionFactsDesc(inSlot.Itemstack);
        if (nutritionFacts == null) return;

        if (lines.Count >= 3) {
            var deleteIndex = lines.Count - 3;
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

    string? GetNutritionFactsDesc(ItemStack stack) {
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

        if (noExtraNutrition) {
            sb.AppendLine($"Also contains fruit nutrition from jam! (not shown in handbook)");
        }

        return sb.ToString().TrimEnd();
    }


    protected override void tryEatStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity) {
        var stackSizePre = slot.StackSize;
        var extraNutritionProps = ExtraNutritionPropsAttribute.FromTreeAttribute(slot.Itemstack?.Attributes);
        base.tryEatStop(secondsUsed, slot, byEntity);
        if (byEntity.World.Side.IsServer()) {
            // Eaten
            if (stackSizePre > slot.StackSize && extraNutritionProps != null) {
                GiveExtraSatiety(extraNutritionProps, byEntity);
            }
        }
    }

    void GiveExtraSatiety(ExtraNutritionPropsAttribute extraNutritionPropsAttribute, EntityAgent byEntity) {
        foreach (var (cat, val) in extraNutritionPropsAttribute.extraNutrition) {
            byEntity.ReceiveSaturation(val, cat);
        }
    }

    public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe) {
        base.OnCreatedByCrafting(allInputslots, outputSlot, byRecipe);

        if (outputSlot.Itemstack == null) return;

        /*
        // Prevent derp in the handbook
        if (outputSlot is DummySlot) return;
        */

        foreach (var inputSlot in allInputslots) {
            if (inputSlot.Itemstack?.Collectible is ItemScoopOfJam scoopOfJamItem) {
                // Copy ScoopOfJamAttribute into jam bread
                var jamAttr = scoopOfJamItem.GetScoopOfJamAttribute(inputSlot.Itemstack);
                if (jamAttr != null) {
                    scoopOfJamItem.SetScoopOfJamAttribute(outputSlot.Itemstack, jamAttr);
                }

                // TODO: calc from scoop per jam (unable because we can't refer to ingredients on server side) 
                var quantity = 1;

                // Set extra nutrition
                var extraNut = new Dictionary<EnumFoodCategory, float>();
                extraNut[scoopOfJamItem.NutritionProps.FoodCategory] = scoopOfJamItem.NutritionProps.Satiety * quantity;
                var extraNutProps = new ExtraNutritionPropsAttribute(extraNut);
                extraNutProps.ToTreeAttribute(outputSlot.Itemstack.Attributes);
            }
        }
    }

}
