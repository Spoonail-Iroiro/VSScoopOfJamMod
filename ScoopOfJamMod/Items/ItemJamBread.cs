using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;

namespace ScoopOfJamMod.Items;
public class ItemJamBread : Item {
    // TODO: Inherit nutrition and perish on crafting

    protected override void tryEatStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity) {
        var stackSizePre = slot.StackSize;
        base.tryEatStop(secondsUsed, slot, byEntity);
        if (byEntity.World.Side.IsServer()) {
            // Eaten
            if (stackSizePre > slot.StackSize) {
                GiveExtraSatiety();
            }
        }
    }

    void GiveExtraSatiety() {
        api.Logger.Event($"Extra satiety given!");
    }
}
