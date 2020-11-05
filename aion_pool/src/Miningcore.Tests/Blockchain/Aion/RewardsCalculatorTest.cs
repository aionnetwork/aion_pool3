using Miningcore.Blockchain.Aion;
using Xunit;

namespace Miningcore.Blockchain.Aion
{
    public class RewardsCalculatorTest
    {
        [Fact]
        public void TestCalculateRewardWithTimeSpan0()
        {
            AionRewardsCalculator ac = new AionRewardsCalculator();

            decimal rewards = ac.calculateRewardWithTimeSpan(0);

            Assert.Equal(0, rewards);
        }
    }
}