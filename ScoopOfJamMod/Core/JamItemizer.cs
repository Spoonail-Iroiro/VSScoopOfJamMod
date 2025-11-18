using ScoopOfJamMod.Items;
using System.Collections.Generic;
using System.Linq;
using System;
using Vintagestory.API.Common;

namespace ScoopOfJamMod.Core;
public class JamIngredientInfo {
    public AssetLocation? FirstFruitCode { get; set; }

    public float FirstFruitSatiety { get; set; }

    public AssetLocation? SecondFruitCode { get; set; }

    public float SecondFruitSatiety { get; set; }
}
public class JamItemizer {

    public bool IsStrictRecipeCheck { get; set; } = true;

    public int ScoopCountPerJam { get; set; } = 2;

    public JamIngredientInfo? GetJamIngredientInfoFromIngredientStacks(ICoreAPI api, ItemStack[] ingredientStacks, bool isDebugMode = false) {
        int honeyCount = 0;
        int fruitCount = 0;
        var rtn = new JamIngredientInfo();
        bool strictCheck = IsStrictRecipeCheck;

        foreach (var ingredient in ingredientStacks) {
            if (ingredient.Collectible == null) return null;
            var code = ingredient.Collectible.Code;

            if (code.FirstCodePart() == "fruit" && code.Domain == "game") {
                var sat = ingredient?.ItemAttributes?["nutritionPropsWhenInMeal"]?.AsObject<FoodNutritionProperties>()?.Satiety;
                if (sat == null) {
                    if (isDebugMode) {
                        api.Logger.Error($"Ingredient {code} has no nutritionPropsWhenInMeal though it seems like vanilla fruits. Aborting getting ingredient info.");
                    }

                    return null;
                }

                if (fruitCount == 0) {
                    rtn.FirstFruitCode = code;
                    rtn.FirstFruitSatiety = sat.Value;
                }
                else if (fruitCount == 1) {
                    rtn.SecondFruitCode = code;
                    rtn.SecondFruitSatiety = sat.Value;

                }
                ++fruitCount;
            }
            if (code.FirstCodePart() == "jamhoneyportion" && code.Domain == "game") {
                ++honeyCount;
            }
        }

        if (fruitCount == 0) return null;

        if (strictCheck) {
            // First and second fruit satiety should match: technical restriction to supress the number of jam pattern
            bool isFirstAndSecondSatietySame = (rtn.FirstFruitSatiety == rtn.SecondFruitSatiety);
            if (fruitCount != 2 || honeyCount != 2 || !isFirstAndSecondSatietySame) {
                if (isDebugMode) {
                    api.Logger.Warning($"IsStrictRecipeCheck is enabled and check failed. fruitCount={fruitCount}, honeyCount={honeyCount}, isFirstAndSecondSatietySame={isFirstAndSecondSatietySame}");
                }
                return null;
            }
        }

        return rtn;
    }

    public ItemStack? GetScoopOfJam(ICoreAPI api, JamIngredientInfo ingredientInfo, IWorldAccessor world, bool isDebugMode = false) {
        var fruitItem = world.GetItem(ingredientInfo.FirstFruitCode);
        var fruitCode = fruitItem?.Variant["fruit"];
        if (fruitCode == null) return null;
        var sojCode = $"scoopofjam-{fruitCode}-equal";
        var itemType = world.GetItem(new AssetLocation("scoopofjammod", sojCode));
        if (itemType == null || itemType is not ItemScoopOfJam sojItem) {
            if (isDebugMode) {
                api.Logger.Error($"Couldn't construct scoop of jam from fruit code. {sojCode} is unknown.");
            }
            return null;
        }
        if (IsStrictRecipeCheck && ingredientInfo.SecondFruitCode == null) return null;
        var scoopOfJamItemStack = new ItemStack(itemType);
        var secondFruitCode = ingredientInfo.SecondFruitCode ?? ingredientInfo.FirstFruitCode!;
        //var secondFruitNutrition = ingredientInfo.SecondFruitCode != null ? ingredientInfo.SecondFruitSatiety : ingredientInfo.FirstFruitSatiety;

        sojItem.SetScoopOfJamAttribute(scoopOfJamItemStack, new ScoopOfJamAttribute(secondFruitCode));
        scoopOfJamItemStack.StackSize = ScoopCountPerJam;

        return scoopOfJamItemStack;

    }


}
