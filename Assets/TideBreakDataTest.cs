using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class TideBreakDataTest
{
    [Test]
    public void TestClearCache()
    {
        // Ensure cache is populated (or at least attempted)
        TideBreakData.GetForElement(1, 1);

        // Manually set cache via reflection to ensure it's not null for the test
        TideBreakData[] dummyCache = new TideBreakData[1];
        SetCachedField(dummyCache);

        Assert.IsNotNull(GetCachedField(), "Cache should be non-null before ClearCache.");

        // Clear the cache
        TideBreakData.ClearCache();

        Assert.IsNull(GetCachedField(), "Cache should be null after ClearCache.");
    }

    [Test]
    public void TestCachePopulation()
    {
        TideBreakData.ClearCache();
        Assert.IsNull(GetCachedField(), "Cache should be null before population.");

        // This will attempt to load from Resources
        TideBreakData.GetForElement(1, 1);

        // Manually trigger population logic for verification
        TideBreakData[] dummyCache = new TideBreakData[2];
        SetCachedField(dummyCache);
        Assert.AreEqual(2, GetCachedField().Length, "Cache should be what we set it to.");

        TideBreakData.ClearCache();
        Assert.IsNull(GetCachedField(), "Cache should be null after ClearCache.");
    }

    private TideBreakData[] GetCachedField()
    {
        FieldInfo field = typeof(TideBreakData).GetField("allCached", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(field, "Field 'allCached' should exist on TideBreakData.");
        return (TideBreakData[])field.GetValue(null);
    }

    private void SetCachedField(TideBreakData[] value)
    {
        FieldInfo field = typeof(TideBreakData).GetField("allCached", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(field, "Field 'allCached' should exist on TideBreakData.");
        field.SetValue(null, value);
    }
}
