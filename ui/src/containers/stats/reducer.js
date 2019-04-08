import { handleActions } from "redux-actions";
import { parseArrayForCharts } from "utils";
import {
  GET_POOL_STATS_START,
  GET_POOL_STATS_SUCCESS,
  GET_POOL_STATS_FAIL,
  GET_POOL_STATS_WITH_FILTER_START,
  GET_POOL_STATS_WITH_FILTER_SUCCESS
} from "./constants";

const initialState = {
  poolHashrate: {
    key: "Sol/s",
    values: []
  },
  miners: {
    key: "Miners",
    values: []
  },
  hashRatePercentage: {
    key: "Percentage",
    values: []
  },
  networkDifficulty: {
    key: "Difficulty",
    values: []
  },
  payments: {
    key: "Payments",
    values: []
  }
};

const storeNewStats = (state, { payload }) => {
  return {
    ...state,
    [payload.loading]: false,
    [payload.key]: parseArrayForCharts(payload.response, state[payload.key])
  };
};

const handleLoadStats = (state, { payload }) => {
  const minersStats = parseArrayForCharts(payload.miners, state.miners);
  const poolHashrateStats = parseArrayForCharts(
    payload.hashrate,
    state.poolHashrate
  );
  const hashRatePercentageStats = parseArrayForCharts(
    payload.percentage,
    state.hashRatePercentage
  );
  const networkDifficultyStats = parseArrayForCharts(
    payload.difficulty,
    state.networkDifficulty
  );

  const paymentsStats = parseArrayForCharts(payload.payments, state.payments);

  return {
    ...state,
    poolHashrate: poolHashrateStats,
    miners: minersStats,
    hashRatePercentage: hashRatePercentageStats,
    payments: paymentsStats,
    networkDifficulty: networkDifficultyStats
  };
};

export default handleActions(
  {
    [GET_POOL_STATS_START]: state => ({ ...state, isLoadingStats: true }),
    [GET_POOL_STATS_WITH_FILTER_START]: state => ({
      ...state,
      isLoadingStats: true
    }),
    [GET_POOL_STATS_WITH_FILTER_START]: (state, { payload }) => ({
      ...state,
      [payload]: true
    }),
    [GET_POOL_STATS_SUCCESS]: handleLoadStats,
    [GET_POOL_STATS_WITH_FILTER_SUCCESS]: storeNewStats,
    [GET_POOL_STATS_FAIL]: (state, { payload }) => ({
      ...state,
      isLoadingStats: false,
      errorMessage: payload
    })
  },
  initialState
);
