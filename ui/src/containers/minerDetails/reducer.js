import { handleActions } from "redux-actions";
import {
  GET_MINER_DETAILS_START,
  GET_MINER_DETAILS_SUCCESS,
  GET_MINER_DETAILS_FAIL,
  GET_MINER_PAYMENTS_PAGE_START,
  GET_MINER_PAYMENTS_PAGE_SUCCESS,
  GET_MINER_PAYMENTS_PAGE_FAIL,
  GET_CHART_DETAILS_SUCCESS,
  GET_CHART_DETAILS_START
} from "./constants";

const initialState = {
  isLoadingMinerData: false,
  workers: [],
  hashrate: [],
  workerStats: [],
  payments: [],
  paymentsPage: 0,
  isLoadingPaymentsPage: false,
  isChartLoading: false
};

const getPerformanceData = performance => {
  const hashrate = {
    key: "solutions per second",
    values: []
  };

  const workerStats = {
    key: "workers",
    values: []
  };

  if (performance.length > 0) {
    performance.forEach(s => {
      const workerNames = Object.keys(s.workers);
      const totalHashrate = workerNames.reduce(
        (sum, n) => sum + s.workers[n].hashrate,
        0
      );
      const numberOfWorkers = workerNames.length;
      hashrate.values.push({ timestamp: s.created, value: totalHashrate });
      workerStats.values.push({ timestamp: s.created, value: numberOfWorkers });
    });
  } else {
    hashrate.values.push({ timestamp: new Date(), value: 0 });
    workerStats.values.push({ timestamp: new Date(), value: 0 });
  }

  return {
    hashrate,
    workerStats
  };
};

const storeNewStats = (state, { payload }) => {
  const { hashrate, workerStats } = getPerformanceData(
    payload.minerPerformance
  );

  return {
    ...state,
    hashrate: [hashrate],
    workerStats: [workerStats],
    isLoadingMinerData: false,
    isChartLoading: false
  };
};

const handleLoadMinerData = (state, { payload }) => {
  const { hashrate, workerStats } = getPerformanceData(
    payload.minerPerformance
  );

  const workers = payload.minerDetails.performance
    ? Object.keys(payload.minerDetails.performance.workers).map(name => ({
        name,
        ...payload.minerDetails.performance.workers[name]
      }))
    : [];

  return {
    ...state,
    workers,
    minerDetails: payload.minerDetails,
    payments: payload.payments.results,
    totalPayments: payload.payments.total,
    totalPaymentsPages: Math.ceil(
      payload.payments.total / payload.paymentsPageSize
    ),
    hashrate: [hashrate],
    workerStats: [workerStats],
    isLoadingMinerData: false
  };
};

export default handleActions(
  {
    [GET_MINER_DETAILS_START]: state => ({
      ...state,
      isLoadingMinerData: true,
      totalPayments: 0,
      totalPaymentsPages: 0
    }),
    [GET_MINER_DETAILS_SUCCESS]: handleLoadMinerData,
    [GET_MINER_DETAILS_FAIL]: (state, { payload }) => ({
      ...state,
      isLoadingMinerData: false,
      errorMessage: payload
    }),
    [GET_MINER_PAYMENTS_PAGE_START]: state => ({
      ...state,
      isLoadingPaymentsPage: true
    }),
    [GET_MINER_PAYMENTS_PAGE_SUCCESS]: (state, { payload }) => ({
      ...state,
      payments: payload.payments.results,
      paymentsPage: payload.paymentsPage,
      isLoadingPaymentsPage: false
    }),
    [GET_MINER_PAYMENTS_PAGE_FAIL]: (state, { payload }) => ({
      ...state,
      isLoadingPaymentsPage: false,
      errorMessage: payload
    }),
    [GET_CHART_DETAILS_START]: state => ({
      ...state,
      isChartLoading: true
    }),
    [GET_CHART_DETAILS_SUCCESS]: storeNewStats
  },
  initialState
);
