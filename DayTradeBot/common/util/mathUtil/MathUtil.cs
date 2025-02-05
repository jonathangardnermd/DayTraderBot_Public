namespace DayTradeBot.common.util.mathUtil;

public static class MathUtil
{
    public static int[] DistributeIntegers(float[] percentages, int qtyToDistribute)
    {
        int[] roundedQtys = percentages.Select(pct => (int)Math.Round(pct * qtyToDistribute)).ToArray();
        int sum = roundedQtys.Sum();

        int diff = qtyToDistribute - sum;

        if (diff < 0)
        {
            // sum is too large, need to shave off a few
            int idx = 0;
            int lastIdx = roundedQtys.Length - 1;
            while (idx < Math.Abs(diff))
            {
                roundedQtys[lastIdx - idx] = roundedQtys[lastIdx - idx] - 1;
                idx++;
            }
        }
        else if (diff > 0)
        {
            // sum not large enough, need to add a few
            int idx = 0;
            while (idx < diff)
            {
                roundedQtys[idx] = roundedQtys[idx] + 1;
                idx++;
            }
        }

        if (roundedQtys.Count() != percentages.Count())
        {
            throw new Exception("Length of output array is not equal to length of input array");
        }
        if (roundedQtys.Sum() != qtyToDistribute)
        {
            throw new Exception("Sum of roundedQtys not equal to the qtyToDistribute");
        }
        return roundedQtys;
    }

}