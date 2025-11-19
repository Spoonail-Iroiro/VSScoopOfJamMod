using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace ScoopOfJamMod.Items;

public record class ScoopOfJamAttribute(
        AssetLocation secondFruitCode
) {
    public void ToTreeAttribute(ITreeAttribute tree) {
        tree.SetString("secondFruitCode", secondFruitCode);
    }

    public static ScoopOfJamAttribute? FromTreeAttribute(ITreeAttribute tree) {
        var secondFruitCodeString = tree.GetString("secondFruitCode");
        if (secondFruitCodeString == null) return null;
        var secondFruitCode = new AssetLocation(secondFruitCodeString);
        //var secondFruitNutrition = stack.Attributes.GetFloat("secondFruitNutrition");

        return new ScoopOfJamAttribute(secondFruitCode);
    }
}

public class ItemScoopOfJam : Item {
    public ScoopOfJamAttribute? GetScoopOfJamAttribute(ItemStack stack) {
        return ScoopOfJamAttribute.FromTreeAttribute(stack.Attributes);
    }

    public void SetScoopOfJamAttribute(ItemStack stack, ScoopOfJamAttribute attr) {
        //stack.Attributes.SetFloat("secondFruitNutrition", attr.secondFruitNutrition);
        attr.ToTreeAttribute(stack.Attributes);
    }

}
