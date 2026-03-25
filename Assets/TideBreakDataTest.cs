using System.Reflection;
using NUnit.Framework;

public class TideBreakDataTest
{
    [Test]
    public void TestClearCache()
    {
        TideBreakData.ClearCache();
        TideBreakData.GetForElement(1, 1);

        Assert.IsNotNull(GetCachedField(), "Cache should be non-null before ClearCache.");

        TideBreakData.ClearCache();

        Assert.IsNull(GetCachedField(), "Cache should be null after ClearCache.");
    }

    [Test]
    public void TestCachePopulation()
    {
        TideBreakData.ClearCache();
        Assert.IsNull(GetCachedField(), "Cache should be null before population.");

        var fireTideBreaks = TideBreakData.GetForElement(1, 1);

        Assert.Greater(fireTideBreaks.Count, 0, "Fire tide breaks should load from Resources/TideBreakData.");
        Assert.IsNotNull(GetCachedField(), "Cache should be populated after calling GetForElement.");
        for (int i = 0; i < fireTideBreaks.Count; i++)
        {
            Assert.IsNotNull(fireTideBreaks[i], "Loaded tide break entry should not be null.");
            Assert.AreEqual(1, fireTideBreaks[i].element, "Loaded tide break should match requested element.");
            Assert.LessOrEqual(fireTideBreaks[i].unlockLevel, 1, "Loaded tide break should satisfy level filter.");
        }

        TideBreakData.ClearCache();
        Assert.IsNull(GetCachedField(), "Cache should be null after ClearCache.");
    }

    private TideBreakData[] GetCachedField()
    {
        FieldInfo field = typeof(TideBreakData).GetField("allCached", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(field, "Field 'allCached' should exist on TideBreakData.");
        return (TideBreakData[])field.GetValue(null);
    }

}
