using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;

namespace ScoopOfJamMod.Util;
public class HandbookUtil {
    public static bool IsIncludedInHandBookGeneral(CollectibleObject self) {
        if (self.Code == null) return false;
        var handbookAttributes = self.Attributes?["handbook"];
        if (handbookAttributes?["exclude"].AsBool() == true) return false;

        bool inCreativeTab = self.CreativeInventoryTabs != null && self.CreativeInventoryTabs.Length > 0;
        bool inCreativeTabStack = self.CreativeInventoryStacks != null && self.CreativeInventoryStacks.Length > 0;
        if (!inCreativeTab && !inCreativeTabStack) {
            if (true != handbookAttributes?["include"].AsBool()) return false;
        }

        return true;
    }
}
