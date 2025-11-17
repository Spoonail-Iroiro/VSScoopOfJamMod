using ScoopOfJamMod.Items;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace ScoopOfJamMod {
    public class ScoopOfJamModModSystem : ModSystem {

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api) {
            api.RegisterItemClass(Mod.Info.ModID + "." + nameof(ItemJamSpoon), typeof(ItemJamSpoon));
            api.RegisterItemClass(Mod.Info.ModID + "." + nameof(ItemJamBread), typeof(ItemJamBread));
            api.RegisterItemClass(Mod.Info.ModID + "." + nameof(ItemScoopOfJam), typeof(ItemScoopOfJam));
        }

        public override void StartServerSide(ICoreServerAPI api) {
        }

        public override void StartClientSide(ICoreClientAPI api) {
        }

        public static bool IsDebug() {
            return false;
        }

    }
}
