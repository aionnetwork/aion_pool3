using System.Numerics;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Miningcore.Blockchain.Aion
{
    public class AionRewardsCalculator 
    {
        static decimal blockReward = 4500000000000000000;
        static decimal[] rewardsAdjustTable;
        static int capping = 70;
        int expectedBlockTime = 10;
        public AionRewardsCalculator() {
            rewardsAdjustTable = new decimal[capping];
            // we use rewardsSlope divided by the divisor to represent the floating point
            long rewardsSlope = 4;
            long divisor = 10;

            decimal baseline = blockReward * (divisor - rewardsSlope) / divisor;

            for (int i = 0 ; i< capping ; i++) {
                rewardsAdjustTable[i] = blockReward * (i + 1) * rewardsSlope / divisor / expectedBlockTime + baseline;
            }
        }

        public decimal calculateReward(long height) {
            var magnitude = 1000000000000000000;
            var rampUpLowerBound = 0;
            var rampUpUpperBound = 259200;
            var rampUpStartValue = 748994641621655092;
            var rampUpEndValue = blockReward;

            var delta = rampUpUpperBound - rampUpLowerBound;
            var m = (rampUpEndValue - rampUpStartValue) / delta;

            if (height <= rampUpUpperBound) {
                return ((decimal) (m * height) + rampUpStartValue) / magnitude;
            } else {
                return (decimal) blockReward / magnitude;
            }
        }

        public decimal calculateRewardWithTimeSpan(long timeSpan) {
            if (timeSpan == 0) {
                return 0;
            }

            return rewardsAdjustTable[timeSpan > capping ? capping - 1 : timeSpan - 1];
        }
    }
}