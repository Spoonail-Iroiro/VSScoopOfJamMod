using ScoopOfJamMod.Items;
using System.Collections.Generic;
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

            var rootCommand = api.ChatCommands
                .Create("soj")
                .WithDescription("Scoop of Jam mod commands");

            var parsers = api.ChatCommands.Parsers;

            var debugCommand = rootCommand
                .BeginSubCommand("debug")
                .WithDescription("Enable/disable debug mode")
                .WithArgs(parsers.Bool("enabled"))
                .HandleWith(args => {
                    var enable = (bool)args.Parsers[0].GetValue();
                    if (enable) {
                        isDebugMode[api.Side] = true;
                        return TextCommandResult.Success($"{Mod.Info.ModID} debug mode enabled");
                    }
                    else {
                        isDebugMode[api.Side] = false;
                        return TextCommandResult.Success($"{Mod.Info.ModID} debug mode disabled");
                    }
                });
        }

        public override void StartServerSide(ICoreServerAPI api) {
        }

        public override void StartClientSide(ICoreClientAPI api) {
        }

        static Dictionary<EnumAppSide, bool> isDebugMode = new() {
            [EnumAppSide.Client] = false,
            [EnumAppSide.Server] = false,
            [EnumAppSide.Universal] = false,
        };

        static void resetDebugModeFlags() {
            isDebugMode = new() {
                [EnumAppSide.Client] = false,
                [EnumAppSide.Server] = false,
                [EnumAppSide.Universal] = false,
            };
        }

        public static bool IsDebugMode(ICoreAPI api) {
            return isDebugMode[api.Side];
        }

        public override void Dispose() {
            base.Dispose();
            resetDebugModeFlags();
        }

    }
}
