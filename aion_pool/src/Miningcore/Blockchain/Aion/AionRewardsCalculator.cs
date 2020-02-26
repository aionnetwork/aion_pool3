using System.Numerics;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Miningcore.Blockchain.Aion
{
    class AionRewardsCalculator 
    {
        public AionRewardsCalculator() {}

        public decimal calculateReward(long height) {
            var blockReward = 4500000000000000000;
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
    }
}