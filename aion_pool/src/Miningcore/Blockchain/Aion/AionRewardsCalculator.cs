using System.Numerics;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Miningcore.Blockchain.Aion
{
    class AionRewardsCalculator 
    {
        private int COMPOUND_YEAR_MAX = 128;
        private int ANNUM = 3110400;
        private int INTEREST_BASE_POINT = 100;
        private int CURRENT_TERM;
        private BigInteger CURRENT_REWARD;
        private BigInteger TOTAL_SUPPLY;
        private long FORK_BLOCK;
        private decimal MAGNITUDE = 1000000000000000000;
        private Dictionary<int, BigInteger> COMPOUND_TABLE = new Dictionary<int, BigInteger>();

        public AionRewardsCalculator(long forkBlock, BigInteger initialSupply) {
            FORK_BLOCK = forkBlock;
            calculateTotalSupply(initialSupply, forkBlock);
            populateCompound(TOTAL_SUPPLY);
        }

        public decimal calculateReward(long height) {
            if(height < FORK_BLOCK) {
                return (decimal) calculateOldReward(height) / MAGNITUDE;
            }

            return (decimal) calculateCurrentReward(height, FORK_BLOCK, TOTAL_SUPPLY) / MAGNITUDE;
        }

        public BigInteger calculateOldReward(long height) {
            BigInteger blockReward = 1497989283243310185;
            var rampUpLowerBound = 0;
            var rampUpUpperBound = 259200;
            var rampUpStartValue = 748994641621655092;
            var rampUpEndValue = blockReward;

            var delta = rampUpUpperBound - rampUpLowerBound;
            var m = (rampUpEndValue - rampUpStartValue) / delta;

            if (height <= rampUpUpperBound) {
                return (m * height) + rampUpStartValue;
            } else {
                return blockReward;
            }
        }

        public BigInteger calculateCurrentReward(long height, decimal forkBlock, BigInteger initialSupply) {
            var startingBlockNum = forkBlock;
            var term = (int) ((height - startingBlockNum - 1) / ANNUM + 1);        
            if(term != CURRENT_TERM) {
                CURRENT_REWARD = COMPOUND_TABLE[term];
                if(CURRENT_REWARD == null) {
                    CURRENT_REWARD = COMPOUND_TABLE[0];
                }
                CURRENT_TERM = term;
            }    

            return CURRENT_REWARD;
        }

        private void populateCompound(BigInteger supply) {
            for(int i = 0; i< COMPOUND_YEAR_MAX; i++) {
                COMPOUND_TABLE[i] = calculateCompound(i, supply);
            }
        }

        private void calculateTotalSupply(BigInteger initialSupply, long forkBlock) {
            var ts = initialSupply;
            for(var i = 1; i <= forkBlock; i++) {
                ts += calculateRewardInternal(i, forkBlock);
            }

            TOTAL_SUPPLY = ts;
        }

        private BigInteger calculateCompound(long term, BigInteger supply) {
            BigInteger divider = 10000;
            var inflationRate = 10000 + INTEREST_BASE_POINT;

            BigInteger compound = BigInteger.Multiply(divider, supply);
            BigInteger preCompound = compound;

            for (long i = 0; i < term; i++) {
                preCompound = compound;
                compound = preCompound * inflationRate / divider;
            }

            compound = compound - preCompound;

            return BigInteger.Divide(BigInteger.Divide(compound, ANNUM), divider);
        }

        private BigInteger calculateRewardInternal(long height, decimal forkBlock = 3346000) {
            if(height < forkBlock) {
                return calculateOldReward(height);
            }

            return calculateCurrentReward(height, forkBlock, TOTAL_SUPPLY);
        }
    }
}