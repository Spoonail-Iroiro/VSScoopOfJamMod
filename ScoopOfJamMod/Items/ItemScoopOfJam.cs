using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;

namespace ScoopOfJamMod.Items;

public record class ScoopOfJamAttribute(
        AssetLocation secondFruitCode
//float secondFruitNutrition
);

public class ItemScoopOfJam : Item {
    public ScoopOfJamAttribute? GetScoopOfJamAttribute(ItemStack stack) {
        var secondFruitCodeString = stack.Attributes.GetString("secondFruitCode");
        if (secondFruitCodeString == null) return null;
        var secondFruitCode = new AssetLocation(secondFruitCodeString);
        //var secondFruitNutrition = stack.Attributes.GetFloat("secondFruitNutrition");

        return new ScoopOfJamAttribute(secondFruitCode);
    }

    public void SetScoopOfJamAttribute(ItemStack stack, ScoopOfJamAttribute attr) {
        stack.Attributes.SetString("secondFruitCode", attr.secondFruitCode);
        //stack.Attributes.SetFloat("secondFruitNutrition", attr.secondFruitNutrition);
    }

}
