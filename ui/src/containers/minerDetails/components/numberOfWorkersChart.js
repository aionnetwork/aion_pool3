import React from "react";
import { IntegerChart } from "components/charts";
import get from "lodash.get";

export default ({ workerStats, hasFilter, loading, activeFilter }) => {
  const workersDataset = get(workerStats, "[0].values", []);
  const isContainingMultipleWorkers = workersDataset.some(d => d.value > 1);

  return isContainingMultipleWorkers ? (
    <IntegerChart
      title="Number of Workers"
      data={workerStats}
      loading={loading}
      hasFilter={hasFilter}
      activeFilter={activeFilter}
    />
  ) : (
    ""
  );
};
