import {
  getMinerStats,
  getHashrateStats,
  getPercentageStats,
  getNetworkDifficulty,
  getPaymentsStats
} from "data-access";
import {
  GET_POOL_STATS_START,
  GET_POOL_STATS_SUCCESS,
  GET_POOL_STATS_FAIL,
  GET_POOL_STATS_WITH_FILTER_SUCCESS,
  GET_POOL_STATS_WITH_FILTER_START,
  MINERS,
  POOLHASHRATE,
  HASHRATE_PERCENTAGE,
  NETWORK_DIFFICULTY,
  PAYMENTS
} from "./constants";
import { getPoolId } from "../pools/actions";

export const getPoolStats = () => {
  return (dispatch, getState) => {
    dispatch({ type: GET_POOL_STATS_START });

    getPoolId(dispatch).then(poolId => {
      Promise.all([
        getMinerStats(poolId),
        getHashrateStats(poolId),
        getPercentageStats(poolId),
        getNetworkDifficulty(poolId),
        getPaymentsStats(poolId)
      ])
        .then(([miners, hashrate, percentage, difficulty, payments]) => {
          dispatch({
            type: GET_POOL_STATS_SUCCESS,
            payload: {
              miners,
              hashrate,
              percentage,
              difficulty,
              payments
            }
          });
        })
        .catch(() => dispatch({ type: GET_POOL_STATS_FAIL }));
    });
  };
};

const storeStats = (response, chartId, dispatch) => {
  dispatch({
    type: GET_POOL_STATS_WITH_FILTER_SUCCESS,
    payload: {
      key: chartId,
      response,
      loading: `${chartId}ChartLoading`
    }
  });
};

export const statsWithFilters = (chartId, start, end) => {
  return (dispatch, getState) => {
    dispatch({
      type: GET_POOL_STATS_WITH_FILTER_START,
      payload: `${chartId}ChartLoading`
    });

    getPoolId(dispatch)
      .then(poolId => {
        switch (chartId) {
          case MINERS:
            return getMinerStats(poolId, start, end).then(miners =>
              storeStats(miners, chartId, dispatch)
            );
          case POOLHASHRATE:
            return getHashrateStats(poolId, start, end).then(hashrate =>
              storeStats(hashrate, chartId, dispatch)
            );
          case HASHRATE_PERCENTAGE:
            return getPercentageStats(poolId, start, end).then(percentage =>
              storeStats(percentage, chartId, dispatch)
            );
          case PAYMENTS:
            return getPaymentsStats(poolId, start, end).then(percentage =>
              storeStats(percentage, chartId, dispatch)
            );
          case NETWORK_DIFFICULTY:
            return getNetworkDifficulty(poolId, start, end).then(difficulty => {
              storeStats(difficulty, chartId, dispatch);
            });
          default:
            return;
        }
      })
      .catch(() => dispatch({ type: GET_POOL_STATS_FAIL }));
  };
};
