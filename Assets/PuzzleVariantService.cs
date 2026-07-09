public static class PuzzleVariantService
{
    public static bool IsGreedConsumptionEnabled(PuzzleData data)
    {
        return data != null && data.enableConsumption;
    }

    public static bool IsGreedEconomyEnabled(PuzzleData data)
    {
        return data != null && data.enableGreedEconomy;
    }

    public static int GetConsumptionAmount(PuzzleData data)
    {
        if (data == null)
        {
            return 0;
        }
        return data.consumptionAmount;
    }

    public static int GetCoinTileYield(PuzzleData data)
    {
        if (data == null)
        {
            return 0;
        }
        return data.coinTileYield;
    }

    public static string GetVariantLabel(PuzzleData data)
    {
        if (data == null)
        {
            return "default";
        }

        if (data.enableConsumption)
        {
            return "greed";
        }

        if (data.enableGreedEconomy)
        {
            return "greed";
        }

        return "default";
    }
}
