import React from "react";
import { HashRateChart } from "components/charts";
import get from "lodash.get";

export default ({ hashrate, hasFilter, loading, activeFilter }) =>
  get(hashrate, "[0].values.length", 0) > 0 ? (
    <HashRateChart
      title="Hashrate"
      loading={loading}
      data={hashrate}
      hasFilter={hasFilter}
      activeFilter={activeFilter}
    />
  ) : (
    ""
  );
