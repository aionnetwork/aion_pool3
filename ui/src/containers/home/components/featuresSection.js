import React from "react";

import Features from "./featuresList";
import LastMinedBlocks from "./lastMinedBlocksList";

export default ({
  lastMinedBlocks,
  poolFeePercent,
  isLoadingStats,
  minimumConfirmations,
  minimumPayment,
  paymentFactor
}) => {
  const features = [
    "AION Pool",
    "Pay Per Last N Shares (PPLNS)",
    "Accurate hashrate reporting",
    "Full stratum support",
    "Efficient mining engine, low orphan rate",
    `${poolFeePercent}% fee`,
    `Minimum Confirmations: ${minimumConfirmations}`,
    `Minimum Payment: ${minimumPayment}`,
    `Payment Factor: ${paymentFactor}`
  ];

  return (
    <div className="pool-features">
      <Features features={features} />
      <LastMinedBlocks
        lastMinedBlocks={lastMinedBlocks}
        isLoadingStats={isLoadingStats}
      />
    </div>
  );
};
