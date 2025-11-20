using ScoopOfJamMod.Util;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
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

        return new ScoopOfJamAttribute(secondFruitCode);
    }
}

public class ItemScoopOfJam : Item {
    public override string GetHeldItemName(ItemStack itemStack) {

        var firstFruit = itemStack.Item?.Variant["fruit"];
        if (firstFruit == null) {
            return base.GetHeldItemName(itemStack);
        }

        var scoopOfJamAttribute = ScoopOfJamAttribute.FromTreeAttribute(itemStack.Attributes);

        var langCode = TrUtil.GetTranslateLocale();

        var jamIngredientText = TrUtil.GetJamIngredientText(api, langCode, firstFruit, scoopOfJamAttribute);

        //jamIngredientText = TrUtil.ToLowerAutoCase(langCode, jamIngredientText);

        var itemName = Lang.GetL(langCode, TrUtil.LK($"scoopofjam-template"), jamIngredientText);

        return itemName;
    }

    public ScoopOfJamAttribute? GetScoopOfJamAttribute(ItemStack stack) {
        return ScoopOfJamAttribute.FromTreeAttribute(stack.Attributes);
    }

    public void SetScoopOfJamAttribute(ItemStack stack, ScoopOfJamAttribute attr) {
        //stack.Attributes.SetFloat("secondFruitNutrition", attr.secondFruitNutrition);
        attr.ToTreeAttribute(stack.Attributes);
    }

}
