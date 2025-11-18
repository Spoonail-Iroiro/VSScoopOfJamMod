using ScoopOfJamMod.Config;
using ScoopOfJamMod.Items;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace ScoopOfJamMod {
    public class ScoopOfJamModModSystem : ModSystem {

        public ScoopOfJamModConfig? Config { get; private set; }

        public string ConfigName {
            get {
                return Mod.Info.ModID + ".json";
            }
        }

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api) {
            api.RegisterItemClass(Mod.Info.ModID + "." + nameof(ItemJamSpoon), typeof(ItemJamSpoon));
            api.RegisterItemClass(Mod.Info.ModID + "." + nameof(ItemJamBread), typeof(ItemJamBread));
            api.RegisterItemClass(Mod.Info.ModID + "." + nameof(ItemScoopOfJam), typeof(ItemScoopOfJam));

            var rootCommand = api.ChatCommands
                .Create("soj")
                .RequiresPrivilege(Privilege.chat)
                .WithDescription("Scoop of Jam mod commands");

            var parsers = api.ChatCommands.Parsers;

            var debugCommand = rootCommand
                .BeginSubCommand("debug")
                .WithDescription("Enable/disable debug mode")
                .RequiresPrivilege(api.Side.IsServer() ? Privilege.controlserver : Privilege.chat)
                .WithArgs(parsers.OptionalBool("enabled"))
                .HandleWith(args => {
                    if (args.Parsers[0].IsMissing) {
                        return TextCommandResult.Success($"Current value: {IsDebugMode(api)}");
                    }

                    var enable = (bool)args.Parsers[0].GetValue();
                    if (api.Side.IsServer()) {
                        if (Config != null) {
                            Config.isDebugMode = enable;
                            api.StoreModConfig(Config, ConfigName);
                            isServerDebugMode = Config.isDebugMode;
                        }
                    }
                    else {
                        // Client debug mode is current session only
                        isClientDebugMode = enable;
                    }

                    var enableStr = enable ? "enabled" : "disabled";
                    return TextCommandResult.Success($"{Mod.Info.ModID} debug mode {enableStr}");
                });
        }

        public override void StartServerSide(ICoreServerAPI api) {
            // Load or initialize the config file
            Config = api.LoadModConfig<ScoopOfJamModConfig>(ConfigName);
            if (Config == null) {
                Config = new ScoopOfJamModConfig();
                api.StoreModConfig(Config, ConfigName);
            }
            isServerDebugMode = Config.isDebugMode;

            var parsers = api.ChatCommands.Parsers;

            var rootCommand = api.ChatCommands
                .Get("soj");

            var subCommand1 = rootCommand
                .BeginSubCommand("strict-jam-check")
                .WithDescription("Enable/disable strict jam check")
                .RequiresPrivilege(Privilege.controlserver)
                .WithArgs(parsers.OptionalBool("enabled"))
                .HandleWith(args => {
                    if (Config == null) {
                        return TextCommandResult.Error($"Can't change server config");
                    }
                    if (args.Parsers[0].IsMissing) {
                        return TextCommandResult.Success($"Current value: {Config.isJamCheckStrict}");
                    }

                    var enable = (bool)args.Parsers[0].GetValue();

                    Config.isJamCheckStrict = enable;
                    api.StoreModConfig(Config, ConfigName);

                    var enableStr = enable ? "enabled" : "disabled";
                    return TextCommandResult.Success($"{Mod.Info.ModID} strict jam check {enableStr}");
                });
        }

        public override void StartClientSide(ICoreClientAPI api) {
        }

        static bool isClientDebugMode = false;
        static bool isServerDebugMode = false;

        public static bool IsDebugMode(ICoreAPI api) {
            return api.Side.IsServer() ? isServerDebugMode : isClientDebugMode;
        }

        public override void Dispose() {
            base.Dispose();
            isClientDebugMode = false;
            isServerDebugMode = false;
        }

    }
}
