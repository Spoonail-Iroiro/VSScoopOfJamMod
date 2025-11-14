using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;

namespace ScoopOfJamMod.Core;
public class FoodUtil {
    public static void CarryOverFreshness(ICoreAPI api, ItemSlot inputSlot, ItemStack outputStack, TransitionableProperties perishProps) {
        var transitionedRate = GetTransitionedRateSafe(api, inputSlot);

        if (!(outputStack.Attributes["transitionstate"] is ITreeAttribute)) {
            outputStack.Attributes["transitionstate"] = new TreeAttribute();
        }

        float transitionHours = perishProps.TransitionHours.nextFloat(1, api.World.Rand);
        float freshHours = perishProps.FreshHours.nextFloat(1, api.World.Rand);

        ITreeAttribute attr = (ITreeAttribute)outputStack.Attributes["transitionstate"];
        attr.SetDouble("createdTotalHours", api.World.Calendar.TotalHours);
        attr.SetDouble("lastUpdatedTotalHours", api.World.Calendar.TotalHours);

        attr["freshHours"] = new FloatArrayAttribute([freshHours]);
        attr["transitionHours"] = new FloatArrayAttribute([transitionHours]);

        var totalHours = transitionHours + freshHours;

        attr["transitionedHours"] = new FloatArrayAttribute([totalHours * transitionedRate]);
    }

    /// <summary>
    /// Calculate transitioned rate of item in slot. Returns 0 if input is invalid
    /// </summary>
    /// <param name="api"></param>
    /// <param name="slot"></param>
    /// <returns></returns>
    static float GetTransitionedRateSafe(ICoreAPI api, ItemSlot slot) {
        if (slot.Empty) return 0.0f;

        TransitionState? state = slot.Itemstack?.Collectible?.UpdateAndGetTransitionState(api.World, slot, EnumTransitionType.Perish);
        if (state == null) return 0.0f;

        var totalHours = state.TransitionHours + state.FreshHours;

        if (totalHours <= 0) return 0.0f;

        var rate = GameMath.Clamp(state.TransitionedHours / totalHours, 0.0f, 1.0f);

        return rate;
    }
}
