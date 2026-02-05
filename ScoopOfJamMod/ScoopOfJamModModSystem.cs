using System;
using ScoopOfJamMod.Config;
using ScoopOfJamMod.Items;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using ScoopOfJamMod.CollectibleBehaviors;
using System.Text;
using ScoopOfJamMod.Util;
using System.Linq;

namespace ScoopOfJamMod {
    public class ScoopOfJamModModSystem : ModSystem {
        ICoreClientAPI? capi;
        ICoreServerAPI? sapi;

        public string NetworkChannelName { get { return Mod.Info.ModID + "-" + nameof(ScoopOfJamModModSystem); } }

        // Server config
        // Server should restart after change because syncing to client is only performed on join
        public ScoopOfJamModConfig? Config { get; private set; }

        public static string ModID { get; private set; } = "";

        public string ConfigName {
            get {
                return Mod.Info.ModID + ".json";
            }
        }
        public override void StartPre(ICoreAPI api) {
            ModID = Mod.Info.ModID;
        }

        public override void Start(ICoreAPI api) {
            api.RegisterItemClass(Mod.Info.ModID + "." + nameof(ItemJamSpoon), typeof(ItemJamSpoon));
            api.RegisterItemClass(Mod.Info.ModID + "." + nameof(ItemJamBread), typeof(ItemJamBread));
            api.RegisterItemClass(Mod.Info.ModID + "." + nameof(ItemScoopOfJam), typeof(ItemScoopOfJam));
            api.RegisterItemClass(Mod.Info.ModID + "." + nameof(ItemPBAndJ), typeof(ItemPBAndJ));

            api.RegisterCollectibleBehaviorClass(Mod.Info.ModID + ".ExtraSatiety", typeof(BehaviorExtraSatiety));
            api.RegisterCollectibleBehaviorClass(Mod.Info.ModID + ".AttributeVariants", typeof(BehaviorAttributeVariants));

            api.Network
                .RegisterChannel(NetworkChannelName)
                .RegisterMessageType<ScoopOfJamModConfig>();

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
#if DEBUG
            var testLoader = rootCommand
                .BeginSubCommand("attr-variant")
                .WithArgs(parsers.OptionalWordRange("cmd", ["show", "all"]))
                .HandleWith(args => {
                    var stack = args.Caller.Player.InventoryManager?.ActiveHotbarSlot?.Itemstack;
                    if (stack == null) {
                        return TextCommandResult.Error($"Needs some item in your hand");
                    }

                    if (!AttributeVariantsHelper.HasAttributeVariants(stack)) {
                        return TextCommandResult.Error($"No AttributeVariants");
                    }

                    var cmdSB = new StringBuilder();

                    var cmd = "show";

                    if (!args.Parsers[0].IsMissing) {
                        cmd = args.Parsers[0].GetValue().ToString();
                    }

                    switch (cmd) {
                        case "show":
                            var variant = AttributeVariantsHelper.GetVariant(stack);

                            if (variant == null) {
                                cmdSB.AppendLine("No variant");
                            }
                            else {
                                foreach (var kv in variant) {
                                    cmdSB.AppendLine($"{kv.Key}: {kv.Value}");
                                }
                            }
                            break;
                        case "all":
                            var allVars = AttributeVariantsHelper.GatherAllVariants(stack.Collectible);
                            foreach (var v in allVars) {
                                cmdSB.AppendLine(string.Join("-", v.Select(kv => kv.Value)));
                            }
                            break;
                    }
                    return TextCommandResult.Success(cmdSB.ToString());
                });
#endif

        }

        public override void StartServerSide(ICoreServerAPI api) {
            sapi = api;
            // Load or initialize the config file
            Config = api.LoadModConfig<ScoopOfJamModConfig>(ConfigName);
            if (Config == null) {
                Config = new ScoopOfJamModConfig();
                api.StoreModConfig(Config, ConfigName);
            }
            isServerDebugMode = Config.isDebugMode;

            // Sync server config to client on join
            api.Event.PlayerJoin += Event_PlayerJoin;

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
                    return TextCommandResult.Success($"{Mod.Info.ModID} strict jam check {enableStr}. A server restart is required to properly apply this change.");
                });
        }

        public override void StartClientSide(ICoreClientAPI api) {
            capi = api;

            capi.Network
                .GetChannel(NetworkChannelName)
                .SetMessageHandler<ScoopOfJamModConfig>(OnReceivedConfig);
        }

        protected void Event_PlayerJoin(IServerPlayer player) {
            sapi!.Network.GetChannel(NetworkChannelName).SendPacket(Config, player);
        }

        protected void OnReceivedConfig(ScoopOfJamModConfig config) {
            Config = config;
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
