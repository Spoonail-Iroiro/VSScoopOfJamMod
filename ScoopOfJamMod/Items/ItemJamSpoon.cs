using ScoopOfJamMod.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace ScoopOfJamMod.Items;
public class ItemJamSpoon : Item {
    public bool IsStrictRecipeCheck { get; set; } = true;

    // Handles jam -> scoop processing and holds some settings
    public JamItemizer JamItemizer { get; private set; } = new JamItemizer();

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling) {
        if (blockSel?.Position == null || byEntity is not EntityPlayer byPlayer) {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            return;
        }

        Block block = api.World.BlockAccessor.GetBlock(blockSel.Position);
        if (block?.Attributes?.IsTrue("mealContainer") == true) {

            ScoopFromBlock(block, blockSel.Position, byEntity.World);

            handling = EnumHandHandling.PreventDefault;
            return;
        }

        if (block is BlockGroundStorage) {

            var begs = api.World.BlockAccessor.GetBlockEntity<BlockEntityGroundStorage>(blockSel.Position);

            if (begs == null) return;

            var selectedSlot = begs.GetSlotAt(blockSel);

            if (selectedSlot is not ItemSlot gsSlot || gsSlot.Empty) return;

            if (gsSlot.Itemstack?.ItemAttributes?.IsTrue("mealContainer") == true) {
                var scooped = ScoopFromSlot(gsSlot, byEntity.World, byPlayer.Player);
                if (scooped) {
                    gsSlot.MarkDirty();
                    //begs.updateMeshes();
                    begs.MarkDirty(true);
                    handling = EnumHandHandling.PreventDefault;
                }

                return;
            }


        }

        base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
    }

    void ScoopFromBlock(Block selectedBlock, BlockPos pos, IWorldAccessor world) {
        api.Logger.Warning($"Trying to scoop from block but currently not supported! Report details and ask the author to support if you want");
    }

    bool ScoopFromSlot(ItemSlot mealContainerSlot, IWorldAccessor world, IPlayer player) {
        if (mealContainerSlot.Itemstack?.Block is BlockCookedContainerBase mealContainer) {
            string? recipeCode = mealContainer.GetRecipeCode(world, mealContainerSlot.Itemstack);
            ItemStack[] ingredientStacks = mealContainer.GetNonEmptyContents(world, mealContainerSlot.Itemstack);
            float servings = mealContainer.GetQuantityServings(world, mealContainerSlot.Itemstack);

            var debugMode = ScoopOfJamModModSystem.IsDebugMode(api);

            // Must be jam with at least 1 serving
            if (recipeCode != "jam" || servings < 1.0f) return false;

            // Validate ingredients
            var ingredientInfo = JamItemizer.GetJamIngredientInfoFromIngredientStacks(api, ingredientStacks, debugMode);

            if (ingredientInfo == null) return false;

            // Create scoop of jam item stack
            var scoopOfJamItemStack = JamItemizer.GetScoopOfJam(api, ingredientInfo, world, debugMode);

            if (scoopOfJamItemStack == null) return false;

            // Client side only performs validation
            if (world.Side.IsClient()) return true;

            // Update freshness and validate recipe again
            mealContainer.UpdateAndGetTransitionStates(world, mealContainerSlot);
            if (recipeCode != "jam") return true; // Return true because mealContainerSlot is updated

            // Transfer freshness from jam 
            TransitionableProperties[] tprops = scoopOfJamItemStack.Collectible.GetTransitionableProperties(api.World, scoopOfJamItemStack, null);
            var perishProps = tprops?.FirstOrDefault(p => p.Type == EnumTransitionType.Perish);
            var freshnessSlot = GetSlotForFreshness(mealContainerSlot, world);
            if (perishProps != null && freshnessSlot != null) {
                perishProps.TransitionedStack.Resolve(api.World, "scooping jam");
                // The default CarryOverTransition reduces spoilage for some reason
                // so we use our own CarryOverFreshness to transfer transitioned rate as is
                //CarryOverFreshness(api, periSlot, scoopOfJamItemStack, perishProps);
                FoodUtil.CarryOverFreshness(api, freshnessSlot, scoopOfJamItemStack, perishProps);
            }

            // Consume 1 serving using dummy bowl to invoke necessary processes such as replacing 0 serving crock with empty crock
            var dummySlot = new DummySlot();
            var bowlItemStack = new ItemStack(world.GetBlock(new AssetLocation("bowl-blue-fired")));
            dummySlot.Itemstack = bowlItemStack;
            var served = mealContainer.ServeIntoStack(dummySlot, mealContainerSlot, world);
            if (!served) {
                api.Logger.Warning($"Couldn't take 1 serving from the crock");
                return false;
            }

            // Give scoop of jam to player
            if (!player.InventoryManager.TryGiveItemstack(scoopOfJamItemStack, true)) {
                world.SpawnItemEntity(scoopOfJamItemStack, player.Entity.Pos.XYZ.AddCopy(0, 0.5, 0));
            }

            world.PlaySoundFor(
                new AssetLocation("sounds/player/collect"),
                player,
                randomizePitch: false
            );

            return true;

        }

        return false;
    }

    ItemSlot? GetSlotForFreshness(ItemSlot mealContainerSlot, IWorldAccessor world) {
        if (mealContainerSlot.Itemstack?.Block is BlockCookedContainerBase mealContainer) {
            var contentStacks = mealContainer.GetNonEmptyContents(world, mealContainerSlot.Itemstack);
            var dummyInv = new DummyInventory(api);
            //dummyInv.OnAcquireTransitionSpeed += (transType, stack, mul) => {
            //    float val = mul * 1.0f;// GetContainingTransitionModifierContained(world, inSlot, transType);

            //    if (mealContainerSlot.Inventory != null) val *= mealContainerSlot.Inventory.GetTransitionSpeedMul(transType, mealContainerSlot.Itemstack);

            //    return val;
            //};
            var periSlot = BlockCrock.GetDummySlotForFirstPerishableStack(world, contentStacks, null, dummyInv);
            if (periSlot.Empty) return null;
            return periSlot;
            //var sb = new StringBuilder();
            //var spoilage = periSlot.Itemstack?.Collectible.AppendPerishableInfoText(periSlot, sb, world);
        }

        return null;
    }


    void COFreshness(ICoreAPI api, ItemSlot inputSlot, ItemStack outStack, TransitionableProperties perishProps) {
        COFreshness(api, [inputSlot], [outStack], perishProps);
    }

    void COFreshness(ICoreAPI api, ItemSlot[] inputSlots, ItemStack[] outStacks, TransitionableProperties perishProps) {

        float transitionedHoursRelative = 0;

        float spoilageRelMax = 0;
        float spoilageRel = 0;
        int quantity = 0;

        for (int i = 0; i < inputSlots.Length; i++) {
            ItemSlot slot = inputSlots[i];
            if (slot.Empty) continue;
            TransitionState? state = slot.Itemstack?.Collectible?.UpdateAndGetTransitionState(api.World, slot, EnumTransitionType.Perish);
            if (state == null) continue;

            quantity++;
            float val = state.TransitionedHours / (state.TransitionHours + state.FreshHours);

            float spoilageRelOne = Math.Max(0, (state.TransitionedHours - state.FreshHours) / state.TransitionHours);
            spoilageRelMax = Math.Max(spoilageRelOne, spoilageRelMax);

            transitionedHoursRelative += val;
            spoilageRel += spoilageRelOne;
        }

        transitionedHoursRelative /= Math.Max(1, quantity);
        spoilageRel /= Math.Max(1, quantity);

        for (int i = 0; i < outStacks.Length; i++) {
            if (outStacks[i] == null) continue;

            if (!(outStacks[i].Attributes["transitionstate"] is ITreeAttribute)) {
                outStacks[i].Attributes["transitionstate"] = new TreeAttribute();
            }

            float transitionHours = perishProps.TransitionHours.nextFloat(1, api.World.Rand);
            float freshHours = perishProps.FreshHours.nextFloat(1, api.World.Rand);

            ITreeAttribute attr = (ITreeAttribute)outStacks[i].Attributes["transitionstate"];
            attr.SetDouble("createdTotalHours", api.World.Calendar.TotalHours);
            attr.SetDouble("lastUpdatedTotalHours", api.World.Calendar.TotalHours);

            attr["freshHours"] = new FloatArrayAttribute(new float[] { freshHours });
            attr["transitionHours"] = new FloatArrayAttribute(new float[] { transitionHours });

            if (spoilageRel > 0) {
                // If already spoiled: Take away 40% spoilage and 2 hours
                spoilageRel *= 0.6f;
                attr["transitionedHours"] = new FloatArrayAttribute(new float[] { freshHours + Math.Max(0, transitionHours * spoilageRel - 2) });

            }
            else {
                // If not yet spoiled: Weird formula :D
                attr["transitionedHours"] = new FloatArrayAttribute(new float[] { Math.Max(0, transitionedHoursRelative * (0.8f + (2 + quantity) * spoilageRelMax) * (transitionHours + freshHours)) });
            }


        }
    }

}
