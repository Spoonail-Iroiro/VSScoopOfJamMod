using HarmonyLib;
using ScoopOfJamMod.CollectibleBehaviors;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.ServerMods;

using VariantType = Vintagestory.API.Datastructures.OrderedDictionary<string, string?>;
using VariantSafeType = Vintagestory.API.Util.RelaxedReadOnlyDictionary<string, string?>;

namespace ScoopOfJamMod.Util;

public static class AttributeVariantsHelper {
    public static bool HasAttributeVariants(ItemStack stack) {
        var beh = stack.Collectible.GetBehavior<BehaviorAttributeVariants>();
        return beh != null;
    }

    public static VariantSafeType? GetVariant(ItemStack stack) {
        var beh = stack.Collectible.GetBehavior<BehaviorAttributeVariants>();
        if (beh == null) return null;
        if (stack?.Attributes == null) return null;
        var typeAttr = stack.Attributes.GetTreeAttribute("types");
        if (typeAttr == null) return null;

        var variant = new VariantType();
        foreach (var group in beh.AttributeVariants!) {
            var value = typeAttr.GetString(group.Code);
            variant[group.Code] = value;
        }


        return new VariantSafeType(variant);
    }

    public static void SetVariant(ItemStack stack, VariantType variant) {
        var beh = stack.Collectible.GetBehavior<BehaviorAttributeVariants>();
        if (beh == null) return;
        var typeAttr = stack.Attributes.GetTreeAttribute("types");
        if (typeAttr == null) {
            typeAttr = new TreeAttribute();
            stack.Attributes["types"] = typeAttr;
        }

        foreach (var group in beh.AttributeVariants!) {
            var value = variant.Get(group.Code);
            typeAttr.SetString(group.Code, value);
        }
    }

    public static IEnumerable<VariantSafeType> GatherAllVariants(CollectibleObject colobj) {
        var beh = colobj.GetBehavior<BehaviorAttributeVariants>();
        if (beh == null) return Enumerable.Empty<VariantSafeType>();
        var remainingGroups = beh.AttributeVariants!.Select(GatherStates).ToList();
        var currentSelections = new List<Tuple<string, string>>();
        return GatherAllVariantsInternal(remainingGroups, 0, currentSelections);
    }

    private static IEnumerable<VariantSafeType> GatherAllVariantsInternal(List<(string Code, string[] States)> groups, int groupIndex, List<Tuple<string, string>> currentSelections) {
        var group = groups[groupIndex];

        foreach (var state in group.States) {
            currentSelections.Add(Tuple.Create(group.Code, state));
            if (groupIndex + 1 < groups.Count) {
                foreach (var childVariant in GatherAllVariantsInternal(groups, groupIndex + 1, currentSelections)) {
                    yield return childVariant;
                }
            }
            else {
                var variant = new VariantType();
                foreach ((var code, var stateForGroup) in currentSelections) {
                    variant[code] = stateForGroup;
                }
                yield return new VariantSafeType(variant);
            }
            currentSelections.RemoveAt(currentSelections.Count - 1);
        }
    }

    private static (string Code, string[] States) GatherStates(RegistryObjectVariantGroup variantGroup) {
        var rtnStates = variantGroup.States.ToList();
        return (variantGroup.Code, rtnStates.ToArray());

    }
}
