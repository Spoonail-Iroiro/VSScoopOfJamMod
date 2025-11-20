using ScoopOfJamMod.Items;
using System.Collections.Generic;
using System.Linq;
using System;
using Vintagestory.API.Common;

namespace ScoopOfJamMod.Core;

//public enum JamCheckFailReason {
//    Success,
//    InvalidFruit,
//    FirstAndSecondFruitNutritionNotEqual,
//    InvalidJamIngredient,
//    Unexpected
//}

public enum JamErrorLevel {
    Fatal,
    StrictCheckFailed,
    FutureSupport
}

public class InvalidJamException : Exception {
    Dictionary<JamErrorLevel, string> errorLevelToErrorCode = new() {
        [JamErrorLevel.Fatal] = "scoopofjammod-jamcheckfatal",
        [JamErrorLevel.StrictCheckFailed] = "scoopofjammod-jamcheckstrictfailed",
        [JamErrorLevel.FutureSupport] = "scoopofjammod-jamcheckdifferentfruitnutritions"
    };

    public JamErrorLevel ErrorLevel { get; }

    public string InGameErrorCode {
        get {
            return errorLevelToErrorCode[ErrorLevel];
        }
    }

    public InvalidJamException(string message, JamErrorLevel errorLevel) : base(message) {
        ErrorLevel = errorLevel;
    }
}

public class JamIngredientInfo {
    public AssetLocation? FirstFruitCode { get; set; }

    public float FirstFruitSatiety { get; set; }

    public AssetLocation? SecondFruitCode { get; set; }

    public float SecondFruitSatiety { get; set; }
}

public class JamItemizer {

    public bool IsJamCheckStrict { get; set; } = true;

    public int ScoopCountPerJam { get; set; } = 2;

    public JamIngredientInfo GetJamIngredientInfoFromIngredientStacks(ICoreAPI api, ItemStack[] ingredientStacks, bool isDebugMode = false) {
        int honeyCount = 0;
        int fruitCount = 0;
        var rtn = new JamIngredientInfo();
        bool strictCheck = IsJamCheckStrict;

        foreach (var ingredient in ingredientStacks) {
            if (ingredient.Collectible == null) continue;

            var code = ingredient.Collectible.Code;

            if (code.FirstCodePart() == "fruit" && code.Domain == "game") {
                var sat = ingredient.ItemAttributes?["nutritionPropsWhenInMeal"]?.AsObject<FoodNutritionProperties>()?.Satiety;
                if (sat == null) {
                    throw new InvalidJamException($"Unexpected jam: Ingredient {code} has no nutritionPropsWhenInMeal though it seems like vanilla fruit.", JamErrorLevel.Fatal);
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

        if (fruitCount == 0) {
            throw new InvalidJamException($"Unexpected jam: No fruits recognized.", JamErrorLevel.Fatal);
        }

        if (strictCheck) {
            // First and second fruit satiety should match: technical restriction to supress the number of jam pattern
            bool isFirstAndSecondSatietySame = (rtn.FirstFruitSatiety == rtn.SecondFruitSatiety);
            if (fruitCount != 2 || honeyCount != 2) {
                throw new InvalidJamException($"fruitCount={fruitCount}, honeyCount={honeyCount}", JamErrorLevel.StrictCheckFailed);
            }

            if (!isFirstAndSecondSatietySame) {
                throw new InvalidJamException($"({rtn.FirstFruitCode} ({rtn.FirstFruitSatiety}), {rtn.SecondFruitCode} ({rtn.SecondFruitSatiety}))", JamErrorLevel.FutureSupport);
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
        if (IsJamCheckStrict && ingredientInfo.SecondFruitCode == null) return null;
        var scoopOfJamItemStack = new ItemStack(itemType);
        var secondFruitCode = ingredientInfo.SecondFruitCode ?? ingredientInfo.FirstFruitCode!;
        //var secondFruitNutrition = ingredientInfo.SecondFruitCode != null ? ingredientInfo.SecondFruitSatiety : ingredientInfo.FirstFruitSatiety;

        sojItem.SetScoopOfJamAttribute(scoopOfJamItemStack, new ScoopOfJamAttribute(secondFruitCode));
        scoopOfJamItemStack.StackSize = ScoopCountPerJam;

        return scoopOfJamItemStack;

    }


}
