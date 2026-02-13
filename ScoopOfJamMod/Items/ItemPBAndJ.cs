using Newtonsoft.Json.Linq;
using ScoopOfJamMod.Util;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace ScoopOfJamMod.Items;
public class ItemPBAndJ : ItemJamBreadAttributeVariants {
    public override string GetHeldItemName(ItemStack itemStack) {
        var variant = AttributeVariantsHelper.GetVariant(itemStack);
        var firstFruit = variant?["fruit"];
        var grain = variant?["type"];
        var nutPaste = variant?["nutpaste"];
        if (firstFruit == null || grain == null || nutPaste == null) return base.GetHeldItemName(itemStack);

        var scoopOfJamAttribute = ScoopOfJamAttribute.FromTreeAttribute(itemStack.Attributes);

        var langCode = TrUtil.GetTranslateLocale();

        var jamIngredientText = TrUtil.GetJamIngredientText(api, langCode, firstFruit, scoopOfJamAttribute);
        var nutPasteText = Lang.GetL(langCode, TrUtil.LK($"item-nutpaste-{nutPaste}"));
        //jamIngredientText = TrUtil.ToHeadUpperAutoCase(langCode, jamIngredientText);

        var breadName = Lang.GetL(langCode, TrUtil.LK($"pbandj-{grain}-perfect-template"), jamIngredientText, nutPasteText);

        return breadName;
    }

    protected override void AddCreativeInventoryStacks(ICoreAPI api) {
        var allVariants = AttributeVariantsHelper.GatherAllVariants(this);
        if (allVariants == null) {
            api.Logger.Warning($"Couldn't load AttributeVariants");
            return;
        }

        var stacks = new List<JsonItemStack>();

        foreach (var variant in allVariants) {
            var fruit = variant["fruit"];
            var grain = variant["type"];

            var attrJToken = new JObject {
                ["types"] = new JObject {
                    ["type"] = grain,
                    ["fruit"] = fruit,
                    ["nutpaste"] = "peanutbutter"
                },
                ["secondFruitCode"] = $"game:fruit-{fruit}",
                ["extraNutrition"] = new JObject {
                    ["Fruit"] = NutritionUtil.VanillaFruitCodeToJamScoopSatiety(fruit!),
                    ["Grain"] = NutritionUtil.VanillaGrainCodeToBreadSatiety(grain!),
                    ["Protein"] = 160.0
                }
            };

            var jsonStack = new JsonItemStack() {
                Code = this.Code,
                Type = EnumItemClass.Item,
                Attributes = new JsonObject(attrJToken)
            };
            jsonStack.Resolve(api.World, this.Code + " type");
            stacks.Add(jsonStack);
        }

        this.CreativeInventoryStacks = [
            new CreativeTabAndStackList
            {
                Stacks = stacks.ToArray(),
                Tabs = new string[] { "general", "decorative", "scoopofjammod" }
            }
        ];
    }
}


