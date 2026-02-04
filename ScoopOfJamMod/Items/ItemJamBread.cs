using ScoopOfJamMod.CollectibleBehaviors;
using ScoopOfJamMod.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ScoopOfJamMod.Items;


public class ItemJamBread : Item, IHandBookPageCodeProvider {
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

        var extraNut = ExtraNutritionPropsAttribute.FromTreeAttribute(inSlot.Itemstack.Attributes);

        if (extraNut == null) {
            // No extra nutrition info found (such as in handbook recipe output), but provides note anyway
            dsc.AppendLine(Lang.Get(TrUtil.LK("note-jambread-fruit-nutrition")));
        }

    }

    protected override void tryEatStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity) {
        var beh = GetCollectibleBehavior<BehaviorExtraSatiety>(true);
        if (beh != null) {
            beh.ProcessTryEatStop(secondsUsed, slot, byEntity, () => base.tryEatStop(secondsUsed, slot, byEntity));
        }
        else {
            base.tryEatStop(secondsUsed, slot, byEntity);
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
                // Allows missing secondFruitCode attribute, for crafting in handbook, which hates any pieable food with attributes (causes CtD)

                //// TODO: calc from scoop per jam (unable because we can't refer to ingredients on server side) 
                //var quantity = 1;

                //// Set extra nutrition
                //var extraNut = new Dictionary<EnumFoodCategory, float>();
                //extraNut[scoopOfJamItem.NutritionProps.FoodCategory] = scoopOfJamItem.NutritionProps.Satiety * quantity;
                //var extraNutProps = new ExtraNutritionPropsAttribute(extraNut);
                //extraNutProps.ToTreeAttribute(outputSlot.Itemstack.Attributes);
            }
        }
    }

    /// <summary>
    /// Returns new stack of jam bread suitable for registering/referring for handbook (mainly removes ignored attributes)
    /// </summary>
    /// <param name="origItemStack"></param>
    /// <returns></returns>
    public static ItemStack GetItemStackForHandbook(ItemStack origItemStack) {
        var rtnStack = origItemStack.Clone();
        rtnStack.Attributes.RemoveAttribute("secondFruitCode");
        return rtnStack;
    }

    public override List<ItemStack>? GetHandBookStacks(ICoreClientAPI capi) {
        if (!HandbookUtil.IsIncludedInHandBookGeneral(this)) return null;
        var baseStacks = base.GetHandBookStacks(capi);
        var handBookStacks = baseStacks
            .Select(GetItemStackForHandbook)
            .ToList();
        return handBookStacks;
    }

    public string HandbookPageCodeForStack(IWorldAccessor world, ItemStack stack) {
        var stackForPage = GetItemStackForHandbook(stack);
        var code = GuiHandbookItemStackPage.PageCodeForStack(stackForPage);
        return code;
    }
}
