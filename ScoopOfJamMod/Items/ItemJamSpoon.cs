using ScoopOfJamMod.Core;
using ScoopOfJamMod.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace ScoopOfJamMod.Items;
public class ItemJamSpoon : Item {
    // Handles jam -> scoop processing and holds some settings
    public JamItemizer JamItemizer { get; private set; } = new JamItemizer();

    bool configLoaded = false;

    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);
    }

    public bool LoadConfig(ICoreAPI api) {
        if (configLoaded) return true;
        var mod = api.ModLoader.GetModSystem<ScoopOfJamModModSystem>();
        if (mod.Config == null) return false;
        // Set same value for both server and client side; config value is already synced from server to client
        JamItemizer.IsJamCheckStrict = mod.Config.isJamCheckStrict;
        configLoaded = true;
        return true;
    }

    public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot) {
        WorldInteraction[] newHelps = [
            new WorldInteraction() {
                ActionLangCode = TrUtil.LK("heldhelp-scoopjam"),
                MouseButton = EnumMouseButton.Right
            }
        ];
        return newHelps;
    }

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling) {
        if (blockSel?.Position == null || byEntity is not EntityPlayer byPlayer) {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            return;
        }

        var configLoaded = LoadConfig(api);
        if (!configLoaded) {
            if (ScoopOfJamModModSystem.IsDebugMode(api)) {
                api.Logger.Warning($"Server config not loaded! Can't scoop.");
            }
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
        else {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        }
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
            JamIngredientInfo ingredientInfo;
            try {
                ingredientInfo = JamItemizer.GetJamIngredientInfoFromIngredientStacks(api, ingredientStacks, debugMode);
            }
            catch (InvalidJamException je) {
                string seeThis = Lang.Get("scoopofjammod:strict-jam-check-see-this");
                string debugInfo = (je.ErrorLevel == JamErrorLevel.Fatal || debugMode) ? je.Message + " " : "";

                var userMessage = Lang.Get("ingameerror-" + je.InGameErrorCode, debugInfo, seeThis);
                if (api is ICoreClientAPI capi) {
                    capi.TriggerIngameError(this, je.InGameErrorCode, userMessage);
                }
                else {
                    api.Logger.Warning(userMessage);
                }

                return false;
            }

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
            var periSlot = BlockCrock.GetDummySlotForFirstPerishableStack(world, contentStacks, null, dummyInv);
            if (periSlot.Empty) return null;
            return periSlot;
        }

        return null;
    }
}
