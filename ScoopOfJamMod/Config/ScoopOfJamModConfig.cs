using ProtoBuf;
using System.Collections.Generic;
using System.Linq;

namespace ScoopOfJamMod.Config;

[ProtoContract]
public record class ScoopOfJamModConfig {
    [ProtoMember(1, IsRequired = true)]
    public bool isJamCheckStrict { get; set; } = true;

    [ProtoMember(2, IsRequired = true)]
    public bool isDebugMode { get; set; } = false;
}
