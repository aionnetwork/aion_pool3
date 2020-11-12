using Miningcore.Blockchain.Aion;
using System;
using Xunit;

namespace Miningcore.Tests.Blockchain.Aion
{
    public class RewardsCalculatorTest
    {
        AionRewardsCalculator ac = new AionRewardsCalculator();

                [Fact]
        public void TestCalculateRewardWithTimeSpanMinusOne()
        {
            Assert.Throws<Exception>(() => ac.calculateRewardWithTimeSpan(-1));
        }

        [Fact]
        public void TestCalculateRewardWithTimeSpan0()
        {
            Assert.Throws<Exception>(() => ac.calculateRewardWithTimeSpan(0));
        }

        [Fact]
        public void TestCalculateRewardWithTimeSpan1()
        {
            decimal rewards = ac.calculateRewardWithTimeSpan(1);
            Assert.Equal(2880000000000000000 / ac.magnitude, rewards);
        }

        [Fact]
        public void TestCalculateRewardWithTimeSpan10()
        {
            decimal rewards = ac.calculateRewardWithTimeSpan(10);
            Assert.Equal(AionRewardsCalculator.blockReward / ac.magnitude, rewards);
        }

        [Fact]
        public void TestCalculateRewardWithTimeSpanCap()
        {
            decimal rewards = ac.calculateRewardWithTimeSpan(AionRewardsCalculator.capping);
            decimal expect = System.Decimal.Parse("25200000000000000000") / ac.magnitude;
            Assert.Equal(expect, rewards);
        }

        [Fact]
        public void TestCalculateRewardWithTimeSpanCapPlus1()
        {
            decimal rewards = ac.calculateRewardWithTimeSpan(AionRewardsCalculator.capping + 1);
            decimal expect = System.Decimal.Parse("25200000000000000000") / ac.magnitude;
            Assert.Equal(expect, rewards);
        }
    }
}