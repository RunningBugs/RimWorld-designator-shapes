using System.Collections.Generic;
using Verse;
using System;
using RimWorld;
using System.Drawing;

namespace Merthsoft.DesignatorShapes;

public class DesignatorSelectionCache : IExposable
{
    private Dictionary<string, Defs.DesignatorShapeDef> shapesCache = new();
    private static Defs.DesignatorShapeDef defaultShape;

    public Defs.DesignatorShapeDef TryGetShape(string designatorClass)
    {
        // Ensure default shape is initialized
        if (defaultShape == null)
        {
            // Try to get RectangleFilled first
            defaultShape = Defs.DesignatorShapeDefOf.RectangleFilled;
        }

        // Add a null check for safety, though it shouldn't be necessary with the fix.
        if (shapesCache != null && shapesCache.TryGetValue(designatorClass, out var shapeDef))
        {
            // Log.Message($"DesignatorSelectionCache: Found shape for {designatorClass}: {shapeDef?.defName}");
            return shapeDef;
        }
        
        if (defaultShape != null)
        {
            // Log.Warning($"DesignatorSelectionCache: No shape found for {designatorClass}, returning default shape {defaultShape.defName}.");
        }
        else
        {
            // Log.Error($"DesignatorSelectionCache: No shape found for {designatorClass} and default shape is null!");
        }
        
        return defaultShape;
    }

    public void SetShape(string designatorClass, Defs.DesignatorShapeDef shapeDef)
    {
        if (shapesCache == null)
        {
            shapesCache = new();
        }
        shapesCache[designatorClass] = shapeDef;
    }

    public virtual void ExposeData()
    {
        Scribe_Collections.Look(ref shapesCache, "shapesCache", LookMode.Value, LookMode.Def);
    }
}

public class UserDefaultShapesConfig : DesignatorSelectionCache
{
    private bool initialized = false;

    public UserDefaultShapesConfig()
    {
        // Don't initialize here, wait for defs to be loaded
    }

    public void Initialize()
    {
        if (initialized) return;
        // Log.Message($"Initialize called".Colorize(UnityEngine.Color.cyan));

        // Only initialize if defs are loaded
        if (Defs.DesignatorShapeDefOf.Rectangle != null)
        {
            // This method populates the cache with your desired defaults.
            // Log.Message($"UserDefaultShapesConfig: Initializing default shapes. Rectangle is null? {Defs.DesignatorShapeDefOf.Rectangle == null}");

            // Log.Message($"Setting shape rectangle for {typeof(Designator_Build).FullName}");
            SetShape(typeof(Designator_Build).FullName, Defs.DesignatorShapeDefOf.Rectangle);
            initialized = true;
        }
        else
        {
            // Log.Warning("UserDefaultShapesConfig: Defs not loaded yet, deferring initialization");
        }
    }
}
