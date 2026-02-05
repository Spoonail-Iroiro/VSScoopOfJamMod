using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using System.Collections.Specialized;
using System.Threading.Tasks.Dataflow;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.ServerMods;
using System.Diagnostics;

namespace ScoopOfJamMod.CollectibleBehaviors;
public class BehaviorAttributeVariants : CollectibleBehavior {
    public RegistryObjectVariantGroup[]? AttributeVariants { get; private set; }

    public BehaviorAttributeVariants(CollectibleObject collObj) : base(collObj) {
    }

    public override void Initialize(JsonObject properties) {
        base.Initialize(properties);

        AttributeVariants = properties["attributeVariants"].AsObject<RegistryObjectVariantGroup[]>();

        if (AttributeVariants == null) {
            throw new ArgumentException($"{nameof(BehaviorAttributeVariants)} needs attributeVariants property: {collObj.Code}");
        }
    }

    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);

        // Resolve loadFromProperties
        foreach (var group in AttributeVariants!) {
            if (group.LoadFromProperties != null) {
                var prop = api.Assets.TryGet(group.LoadFromProperties.WithPathPrefixOnce("worldproperties/").WithPathAppendixOnce(".json"))
                    .ToObject<StandardWorldProperty>();
                var newStates = prop.Variants.Select(p => p.Code.Path).ToArray().Append(group.States);

                // if the group has only `loadFromProperties`, code is detemined automatically
                // Like, for { "loadFromProperties":"block/fruit" }, its code is "fruit"
                if (group.Code == null) group.Code = prop.Code.Path;
                group.States = newStates;
                // Resolved
                group.LoadFromProperties = null;
            }
        }
    }
}
