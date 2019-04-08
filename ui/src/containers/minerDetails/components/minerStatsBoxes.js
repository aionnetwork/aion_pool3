import React from "react";
import StatsBoxes from "components/statsBoxes";
import get from "lodash.get";

export default ({ isLoadingMinerData, minerDetails, minimumPayment }) => {
  const pendingBalance = get(minerDetails, "pendingBalance", 0);
  const paymentProgress = pendingBalance / minimumPayment * 100;

  const boxes = [
    {
      title: "VALID SHARES",
      icon: "pt-icon-confirm",
      value: get(minerDetails, "shares", 0)
    },
    {
      title: "INVALID SHARES",
      icon: "pt-icon-delete",
      value: get(minerDetails, "invalidShares", 0)
    },
    {
      title: "PENDING BALANCE",
      icon: "pt-icon-time",
      value: pendingBalance.toFixed(3),
      progress: paymentProgress
    },
    {
      title: "TOTAL PAID",
      icon: "pt-icon-bank-account",
      value: get(minerDetails, "totalPaid", 0).toFixed(3)
    }
  ];

  return <StatsBoxes boxes={boxes} isLoadingData={isLoadingMinerData} />;
};
