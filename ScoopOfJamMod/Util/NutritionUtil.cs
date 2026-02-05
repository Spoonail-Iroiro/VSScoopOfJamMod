using System.Collections.Generic;
using System.Linq;

namespace ScoopOfJamMod.Util;
public class NutritionUtil {
    public static double VanillaFruitCodeToJamScoopSatiety(string fruit) {
        var sat = 200.0;
        switch (fruit) {
            case "saguaro":
            case "cranberry":
                sat = 170.0;
                break;
            case "cherry":
            case "lychee":
                sat = 140.0;
                break;
            case "breadfruit":
                sat = 330.0;
                break;
        }
        return sat;
    }

    public static double VanillaGrainCodeToBreadSatiety(string grain) {
        var sat = 300.0;
        switch (grain) {
            case "flax":
                sat = 160;
                break;
            case "rice":
                sat = 330;
                break;
        }
        return sat;
    }
}

