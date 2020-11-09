using Miningcore.Blockchain.Aion;
using Xunit;

namespace Miningcore.Tests.Blockchain.Aion
{
    public class RewardsCalculatorTest
    {
        AionRewardsCalculator ac = new AionRewardsCalculator();

                [Fact]
        public void TestCalculateRewardWithTimeSpanMinusOne()
        {
            decimal rewards = ac.calculateRewardWithTimeSpan(-1);
            Assert.Equal(0, rewards);
        }

        [Fact]
        public void TestCalculateRewardWithTimeSpan0()
        {
            decimal rewards = ac.calculateRewardWithTimeSpan(0);
            Assert.Equal(0, rewards);
        }

        [Fact]
        public void TestCalculateRewardWithTimeSpan1()
        {
            decimal rewards = ac.calculateRewardWithTimeSpan(1);
            Assert.Equal(2880000000000000000, rewards);
        }

        [Fact]
        public void TestCalculateRewardWithTimeSpan10()
        {
            decimal rewards = ac.calculateRewardWithTimeSpan(10);
            Assert.Equal(AionRewardsCalculator.blockReward, rewards);
        }

        [Fact]
        public void TestCalculateRewardWithTimeSpanCap()
        {
            decimal rewards = ac.calculateRewardWithTimeSpan(AionRewardsCalculator.capping);
            decimal expect = System.Decimal.Parse("25200000000000000000");
            Assert.Equal(expect, rewards);
        }

        [Fact]
        public void TestCalculateRewardWithTimeSpanCapPlus1()
        {
            decimal rewards = ac.calculateRewardWithTimeSpan(AionRewardsCalculator.capping + 1);
            decimal expect = System.Decimal.Parse("25200000000000000000");
            Assert.Equal(expect, rewards);
        }
    }
}