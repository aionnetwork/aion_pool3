import React from "react";
import NVD3Chart from "react-nvd3";
import d3 from "d3";
import {
  getMinMaxValues,
  formatDate,
  formatTooltip,
  formatTooltipDiff
} from "utils";
import ChartsFilter from "./chartsFilter";
import { Spinner } from "@blueprintjs/core";

import "./charts.css";

const chartDefaults = {
  useInteractiveGuideline: false,
  clipEdge: false,
  x: d => d.timestamp,
  y: d => d.value
};

const ChartWrapping = ({ children, title, hasFilter, loading }) => (
  <React.Fragment>
    <div className="chartLabel">{title}</div>
    {hasFilter && <ChartsFilter />}
    <div className="chartWrapper">
      {loading ? <Spinner className="chartSpinner" /> : children}
    </div>
  </React.Fragment>
);

export const HashRateChart = ({
  data,
  title,
  hasFilter,
  activeFilter,
  loading
}) => (
  <ChartWrapping title={title} hasFilter={hasFilter} loading={loading}>
    <NVD3Chart
      type="lineChart"
      xAxis={{ tickFormat: d => formatDate(d, activeFilter) }}
      yAxis={{ tickFormat: d => getMinMaxValues(d, 0, data) }}
      tooltip={{ contentGenerator: d => formatTooltip(d, 2, activeFilter) }}
      datum={data}
      margin={{ left: 70, right: 40 }}
      {...chartDefaults}
    />
  </ChartWrapping>
);

export const IntegerChart = ({
  data,
  title,
  hasFilter,
  activeFilter,
  loading
}) => (
  <ChartWrapping title={title} hasFilter={hasFilter} loading={loading}>
    <NVD3Chart
      type="stackedAreaChart"
      xAxis={{ tickFormat: d => formatDate(d, activeFilter) }}
      yAxis={{ tickFormat: d => d.toFixed(0) }}
      showControls={false}
      datum={data}
      margin={{ left: 40, right: 40 }}
      {...chartDefaults}
    />
  </ChartWrapping>
);

export const PercentageChart = ({
  data,
  title,
  hasFilter,
  activeFilter,
  loading
}) => (
  <ChartWrapping title={title} hasFilter={hasFilter} loading={loading}>
    <NVD3Chart
      type="lineChart"
      xAxis={{ tickFormat: d => formatDate(d, activeFilter) }}
      yAxis={{ tickFormat: d3.format(".0%") }}
      datum={data}
      margin={{ left: 40, right: 40 }}
      {...chartDefaults}
    />
  </ChartWrapping>
);

export const LineChart = ({
  data,
  title,
  hasFilter,
  activeFilter,
  loading
}) => (
  <ChartWrapping title={title} hasFilter={hasFilter} loading={loading}>
    <NVD3Chart
      type="lineChart"
      xAxis={{ tickFormat: d => formatDate(d, activeFilter) }}
      yAxis={{ tickFormat: d3.format("d") }}
      tooltip={{ contentGenerator: d => formatTooltipDiff(d, 2, activeFilter) }}
      datum={data}
      margin={{ left: 70, right: 40 }}
      {...chartDefaults}
    />
  </ChartWrapping>
);
