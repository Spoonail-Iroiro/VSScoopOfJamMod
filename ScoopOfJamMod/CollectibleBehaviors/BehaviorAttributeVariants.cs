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

        // TODO: load from properties

        if (AttributeVariants == null) {
            throw new ArgumentException($"{nameof(BehaviorAttributeVariants)} needs attributeVariants property: {collObj.Code}");
        }

        Console.WriteLine($"Initializing {nameof(BehaviorAttributeVariants)}: {collObj.Code}");
    }
}
