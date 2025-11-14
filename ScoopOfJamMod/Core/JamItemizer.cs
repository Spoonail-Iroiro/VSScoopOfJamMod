using System.Collections.Generic;
using System.Linq;
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

    public JamIngredientInfo? GetJamIngredientInfoFromIngredientStacks(ItemStack[] ingredientStacks) {
        int honeyCount = 0;
        int fruitCount = 0;
        var rtn = new JamIngredientInfo();
        bool strictCheck = IsStrictRecipeCheck;

        foreach (var ingredient in ingredientStacks) {
            if (ingredient.Collectible == null) return null;
            var code = ingredient.Collectible.Code;

            if (code.FirstCodePart() == "fruit" && code.Domain == "game") {
                //ingredient.Collectible.NutritionProps.
                if (fruitCount == 0) {
                    rtn.FirstFruitCode = code;
                    rtn.FirstFruitSatiety = ingredient.Collectible.NutritionProps.Satiety;
                }
                else if (fruitCount == 1) {
                    rtn.SecondFruitCode = code;
                    rtn.SecondFruitSatiety = ingredient.Collectible.NutritionProps.Satiety;

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
            if (fruitCount != 2 || honeyCount != 2 || !isFirstAndSecondSatietySame) return null;
        }

        return rtn;
    }

    public ItemStack? GetScoopOfJam(JamIngredientInfo ingredientInfo, IWorldAccessor world) {
        var fruitItem = world.GetItem(ingredientInfo.FirstFruitCode);
        var fruitCode = fruitItem?.Variant["fruit"];
        if (fruitCode == null) return null;
        var itemType = world.GetItem(new AssetLocation("scoopofjammod", $"scoopofjam-{fruitCode}-same"));
        if (itemType == null) return null;
        if (IsStrictRecipeCheck && ingredientInfo.SecondFruitCode == null) return null;
        var scoopOfJamItemStack = new ItemStack(itemType);
        var secondFruitCode = ingredientInfo.SecondFruitCode ?? ingredientInfo.FirstFruitCode;
        var secondFruitNutrition = ingredientInfo.SecondFruitCode != null ? ingredientInfo.SecondFruitSatiety : ingredientInfo.FirstFruitSatiety;
        scoopOfJamItemStack.Attributes.SetString("secondFruitCode", secondFruitCode);
        scoopOfJamItemStack.Attributes.SetFloat("secondFruitNutrition", secondFruitNutrition);
        scoopOfJamItemStack.StackSize = ScoopCountPerJam;

        return scoopOfJamItemStack;

    }

}
