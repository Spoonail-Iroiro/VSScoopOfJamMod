using ScoopOfJamMod.Items;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ScoopOfJamMod;
public class TrUtil {
    public static string LK(string key) {
        return $"{ScoopOfJamModModSystem.ModID}:{key}";
    }

    // Get consistent dst language to prevent partial translation
    public static string GetTranslateLocale() {
        return Lang.HasTranslation(LK("jam-ingredient-single")) ? Lang.CurrentLocale : "en";
    }

    public static string GetJamIngredientText(ICoreAPI api, string langCode, string firstFruit, ScoopOfJamAttribute? attr) {
        var secondFruit = attr == null ? firstFruit : (api.World.GetItem(attr.secondFruitCode)?.Variant["fruit"] ?? firstFruit);

        var jamIngredientLangKey = (firstFruit == secondFruit) ? LK("jam-ingredient-single") : LK("jam-ingredient-double");

        var firstFruitName = Lang.GetL(langCode, firstFruit != "pineapple" ? $"item-fruit-{firstFruit}" : "pineapple-in-jam-name");
        var secondFruitName = Lang.GetL(langCode, secondFruit != "pineapple" ? $"item-fruit-{secondFruit}" : "pineapple-in-jam-name");

        var jamIngredient = Lang.GetL(langCode, jamIngredientLangKey, firstFruitName, secondFruitName);

        return jamIngredient;
    }
}
