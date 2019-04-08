import React from "react";
import { connect } from "react-redux";
import { bindActionCreators } from "redux";
import { getPoolStats, statsWithFilters } from "./actions";
import get from "lodash.get";

import { startDateEndDate } from "utils";

import {
  HashRateChart,
  IntegerChart,
  PercentageChart,
  LineChart
} from "components/charts";

import {
  MINERS,
  POOLHASHRATE,
  HASHRATE_PERCENTAGE,
  DEFAULT_ACTIVE_FILTER,
  PAYMENTS,
  NETWORK_DIFFICULTY
} from "./constants";

import ChartContext from "../../components/charts/chartContext";

class Stats extends React.Component {
  constructor(props) {
    super(props);
    this.state = {
      activeFilter: {}
    };
  }

  componentWillMount() {
    this.props.getPoolStats();
  }

  changeFilter = ev => {
    const chart = ev.currentTarget.dataset.chartid;
    const filter = ev.currentTarget.dataset.filter;

    this.setState({
      activeFilter: {
        ...this.state.activeFilter,
        [chart]: filter
      }
    });

    const { start, end } = startDateEndDate(filter);
    this.props.statsWithFilters(chart, start, end);
  };

  render() {
    const {
      poolHashrate,
      miners,
      payments,
      hashRatePercentage,
      networkDifficulty,
      poolHashrateChartLoading,
      hashRatePercentageChartLoading,
      networkDifficultyChartLoading,
      paymentsChartLoading,
      minersChartLoading
    } = this.props;

    return (
      <React.Fragment>
        <h1 className="page__title">Recent Statistics</h1>
        <React.Fragment>
          <ChartContext.Provider
            value={{
              changeFilter: this.changeFilter,
              loading: minersChartLoading,
              activeFilter: get(
                this.state.activeFilter,
                MINERS,
                DEFAULT_ACTIVE_FILTER
              ),
              chartID: MINERS
            }}
          >
            <IntegerChart
              title="Active Miners (shares submitted within last 24 hours)"
              data={[miners]}
              loading={minersChartLoading}
              hasFilter
              activeFilter={get(
                this.state.activeFilter,
                MINERS,
                DEFAULT_ACTIVE_FILTER
              )}
            />
          </ChartContext.Provider>
          <ChartContext.Provider
            value={{
              changeFilter: this.changeFilter,
              loading: poolHashrateChartLoading,
              activeFilter: get(
                this.state.activeFilter,
                POOLHASHRATE,
                DEFAULT_ACTIVE_FILTER
              ),
              chartID: POOLHASHRATE
            }}
          >
            <HashRateChart
              title="Pool Hashrate"
              data={[poolHashrate]}
              loading={poolHashrateChartLoading}
              hasFilter
              activeFilter={get(
                this.state.activeFilter,
                POOLHASHRATE,
                DEFAULT_ACTIVE_FILTER
              )}
            />
          </ChartContext.Provider>
          <ChartContext.Provider
            value={{
              changeFilter: this.changeFilter,
              loading: hashRatePercentageChartLoading,
              activeFilter: get(
                this.state.activeFilter,
                HASHRATE_PERCENTAGE,
                DEFAULT_ACTIVE_FILTER
              ),
              chartID: HASHRATE_PERCENTAGE
            }}
          >
            <PercentageChart
              title="Hashrate Percentage of Total Network"
              data={[hashRatePercentage]}
              loading={hashRatePercentageChartLoading}
              hasFilter
              activeFilter={get(
                this.state.activeFilter,
                HASHRATE_PERCENTAGE,
                DEFAULT_ACTIVE_FILTER
              )}
            />
          </ChartContext.Provider>
          <ChartContext.Provider
            value={{
              changeFilter: this.changeFilter,
              loading: networkDifficultyChartLoading,
              activeFilter: get(
                this.state.activeFilter,
                NETWORK_DIFFICULTY,
                DEFAULT_ACTIVE_FILTER
              ),
              chartID: NETWORK_DIFFICULTY
            }}
          >
            <LineChart
              title="Network Difficulty"
              data={[networkDifficulty]}
              loading={networkDifficultyChartLoading}
              hasFilter
              activeFilter={get(
                this.state.activeFilter,
                NETWORK_DIFFICULTY,
                DEFAULT_ACTIVE_FILTER
              )}
            />
          </ChartContext.Provider>
          <ChartContext.Provider
            value={{
              changeFilter: this.changeFilter,
              loading: paymentsChartLoading,
              activeFilter: get(
                this.state.activeFilter,
                PAYMENTS,
                DEFAULT_ACTIVE_FILTER
              ),
              chartID: PAYMENTS
            }}
          >
            <IntegerChart
              title="Total aions paid"
              data={[payments]}
              loading={paymentsChartLoading}
              hasFilter
              activeFilter={get(
                this.state.activeFilter,
                PAYMENTS,
                DEFAULT_ACTIVE_FILTER
              )}
            />
          </ChartContext.Provider>
        </React.Fragment>
      </React.Fragment>
    );
  }
}

const mapStateToProps = ({ poolStats }) => poolStats;

const mapDispatchToProps = dispatch =>
  bindActionCreators(
    {
      getPoolStats,
      statsWithFilters
    },
    dispatch
  );

export default connect(mapStateToProps, mapDispatchToProps)(Stats);
