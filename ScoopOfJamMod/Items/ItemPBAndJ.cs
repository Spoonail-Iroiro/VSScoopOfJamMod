using ScoopOfJamMod.Util;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ScoopOfJamMod.Items;
public class ItemPBAndJ : ItemJamBread {
    protected override string TranslationKeyNoNutritionNote => "note-pbandj-missing-nutrition";

    public override void OnLoaded(ICoreAPI api) {
    }

    public override string GetHeldItemName(ItemStack itemStack) {
        var firstFruit = itemStack.Item?.Variant["fruit"];
        var grain = itemStack.Item?.Variant["type"];
        var nutPaste = itemStack.Item?.Variant["nutpaste"];
        if (firstFruit == null || grain == null) return base.GetHeldItemName(itemStack);

        var scoopOfJamAttribute = ScoopOfJamAttribute.FromTreeAttribute(itemStack.Attributes);

        var langCode = TrUtil.GetTranslateLocale();

        var jamIngredientText = TrUtil.GetJamIngredientText(api, langCode, firstFruit, scoopOfJamAttribute);
        var nutPasteText = Lang.GetL(langCode, TrUtil.LK($"item-nutpaste-{nutPaste}"));
        //jamIngredientText = TrUtil.ToHeadUpperAutoCase(langCode, jamIngredientText);

        var breadName = Lang.GetL(langCode, TrUtil.LK($"pbandj-{grain}-perfect-template"), jamIngredientText, nutPasteText);

        return breadName;
    }

    protected override void AddCreativeInventoryStacks(ICoreAPI api) {
    }
}


