using HarmonyLib;
using Merthsoft.DesignatorShapes.Defs;
using Verse;

namespace Merthsoft.DesignatorShapes.Patches;

[HarmonyPatch(typeof(DesignatorManager), "Select", new[] { typeof(Designator) })]
internal class DesignatorManager_Select
{
    public static Designator SelectedDesignator { get; private set; }

    public static void Postfix(DesignatorManager __instance)
    {
        if (!DesignatorShapes.ShowControls || __instance.SelectedDesignator == SelectedDesignator)
        {
            return;
        }

        SelectedDesignator = __instance.SelectedDesignator;
        if (SelectedDesignator == null)
        {
            return;
        }

        // Ensure settings and cache are not null before using them
        if (DesignatorShapes.Settings?.CachedShapes == null)
        {
            // Log.Error("Designator Shapes settings or cache not initialized.");
        }

        var shape = DesignatorShapes.Settings.CachedShapes.TryGetShape(SelectedDesignator.GetType().FullName);

        // Final safety check: if shape is somehow still null, fall back to a default.
        if (shape == null)
        {
            // Ensure defs are loaded before accessing them
            if (Defs.DesignatorShapeDefOf.Rectangle == null)
            {
                // Log.Warning("DesignatorShapeDefOf.Rectangle is null, attempting to load defs");
                DesignatorShapes.LoadDefs();
            }
            
            // Log.Warning($"A shape could not be found, falling back to default. Rectangle null? {Defs.DesignatorShapeDefOf.Rectangle == null}");
            shape = Defs.DesignatorShapeDefOf.Rectangle ?? Defs.DesignatorShapeDefOf.RectangleFilled;
            
            // If both are null, try to get any available shape
            if (shape == null)
            {
                var allShapes = DefDatabase<DesignatorShapeDef>.AllDefsListForReading;
                if (allShapes.Count > 0)
                {
                    shape = allShapes[0];
                    // Log.Message($"Falling back to first available shape: {shape.defName}");
                }
            }
        }
        
        // Final check to ensure we don't pass null to SelectTool
        if (shape != null)
        {
            DesignatorShapes.SelectTool(shape);
        }
        else
        {
            // Log.Error("Unable to find any valid shape for selected designator, not selecting tool");
        }
    }
}
