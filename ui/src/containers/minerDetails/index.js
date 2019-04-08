import React, { Component } from "react";
import { Spinner } from "@blueprintjs/core";
import { connect } from "react-redux";
import { bindActionCreators } from "redux";
import get from "lodash.get";

import {
  getMinerData,
  getMinerPaymentsPage,
  statsWithFilters
} from "./actions";
import MinerStatsBoxes from "./components/minerStatsBoxes";
import PaymentsTable from "./components/paymentsTable";
import MinerHashRate from "./components/minerHashRate";
import { getAionDashboardAccountUrl, startDateEndDate } from "utils";
import NumberOfWorkersChart from "./components/numberOfWorkersChart";
import WorkersTable from "./components/workersTable";
import ChartsContext from "../../components/charts/chartContext";
import { USER_HASHRATE } from "./constants";
import "./styles.css";

class WorkerStats extends Component {
  constructor(props) {
    super(props);
    this.state = {
      activeFilter: {}
    };
  }
  componentWillMount() {
    const { match, getMinerData } = this.props;
    getMinerData(match.params.hash);
  }

  componentWillReceiveProps(newProps) {
    const { match, getMinerData } = this.props;
    if (match.params.hash !== newProps.match.params.hash) {
      getMinerData(newProps.match.params.hash);
    }
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

    const { match } = this.props;
    const { start, end } = startDateEndDate(filter);
    this.props.statsWithFilters(match.params.hash, start, end);
  };

  render() {
    const {
      match,
      isLoadingMinerData,
      minerDetails,
      workers,
      hashrate,
      payments,
      paymentsPage,
      getMinerPaymentsPage,
      isLoadingPaymentsPage,
      totalPaymentsPages,
      workerStats,
      isChartLoading,
      pool
    } = this.props;

    return (
      <div className="minerDetailsPage">
        <h1 className="page__title">Stats for</h1>
        <a
          className="page_subtitle"
          href={getAionDashboardAccountUrl(match.params.hash)}
          target="_blank"
        >
          <h2>{match.params.hash}</h2>
        </a>
        <MinerStatsBoxes
          minimumPayment={get(pool, "paymentProcessing.minimumPayment", "")}
          minerDetails={minerDetails}
          isLoadingMinerData={isLoadingMinerData}
        />

        {isLoadingMinerData ? (
          <div className="spinnerWrapper">
            <Spinner />
          </div>
        ) : (
          <React.Fragment>
            <PaymentsTable
              payments={payments}
              totalPaymentsPages={totalPaymentsPages}
              paymentsPage={paymentsPage}
              getMinerPaymentsPage={page =>
                getMinerPaymentsPage(match.params.hash, page)
              }
              isLoadingPaymentsPage={isLoadingPaymentsPage}
            />
            <WorkersTable workers={workers} />
            <ChartsContext.Provider
              value={{
                changeFilter: this.changeFilter,
                loading: isChartLoading,
                activeFilter: get(
                  this.state.activeFilter,
                  USER_HASHRATE,
                  "all"
                ),
                chartID: USER_HASHRATE
              }}
            >
              <MinerHashRate
                hashrate={hashrate}
                loading={isChartLoading}
                hasFilter
                activeFilter={get(
                  this.state.activeFilter,
                  USER_HASHRATE,
                  "all"
                )}
              />
            </ChartsContext.Provider>
            <NumberOfWorkersChart
              workerStats={workerStats}
              loading={isChartLoading}
              hasFilter={false}
              activeFilter={get(this.state.activeFilter, USER_HASHRATE, "all")}
            />
          </React.Fragment>
        )}
      </div>
    );
  }
}

const mapStateToProps = ({ minerDetails, pools }) => ({
  ...minerDetails,
  pool: pools.list[0]
});

const mapDispatchToProps = dispatch =>
  bindActionCreators(
    {
      getMinerData,
      getMinerPaymentsPage,
      statsWithFilters
    },
    dispatch
  );

export default connect(mapStateToProps, mapDispatchToProps)(WorkerStats);
