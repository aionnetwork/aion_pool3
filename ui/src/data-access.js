import moment from "moment";

import config from "./config.json";

const START_DATE = moment()
  .subtract(3560, "days")
  .unix(); // how many days to go back
const END_DATE = moment().unix(); // today

export const getPools = () => makeRequest(`${config.apiUrl}/pools`);

export const getMiners = (poolId, page, pageSize) =>
  makeRequest(
    `${config.apiUrl}/pools/${poolId}/miners?page=${page}&pageSize=${pageSize}`
  );

export const getLastBlocks = poolId =>
  makeRequest(
    `${
      config.apiUrl
    }/pools/${poolId}/blocks?page=0&pageSize=10&status=confirmed`
  );

export const getMinerDetails = (poolId, minerAddress) =>
  makeRequest(`${config.apiUrl}/pools/${poolId}/miners/${minerAddress}`);

export const getMinerPerformance = (
  poolId,
  minerAddress,
  start = START_DATE,
  end = END_DATE
) =>
  makeRequest(
    `${
      config.apiUrl
    }/pools/${poolId}/miners/${minerAddress}/performance?start=${start}&end=${end}`
  );

export const getMinerPayments = (poolId, minerAddress, page, pageSize) =>
  makeRequest(
    `${
      config.apiUrl
    }/pools/${poolId}/miners/${minerAddress}/payments?page=${page}&pageSize=${pageSize}`
  );

export const getPoolStats = poolId =>
  makeRequest(`${config.apiUrl}/pools/${poolId}/stats`);

export const getCoinPrice = coinName =>
  makeRequest(`${config.apiUrl}/coin/${coinName}`);

export const getPayments = (poolId, page, pageSize) =>
  makeRequest(
    `${
      config.apiUrl
    }/pools/${poolId}/payments?page=${page}&pageSize=${pageSize}`
  );

export const getMinerStats = (poolId, start = START_DATE, end = END_DATE) =>
  makeRequest(
    `${config.apiUrl}/pools/${poolId}/stats/miners?start=${start}&end=${end}`
  );

export const getPaymentsStats = (poolId, start = START_DATE, end = END_DATE) =>
  makeRequest(
    `${config.apiUrl}/pools/${poolId}/stats/payments?start=${start}&end=${end}`
  );

export const getHashrateStats = (poolId, start = START_DATE, end = END_DATE) =>
  makeRequest(
    `${config.apiUrl}/pools/${poolId}/stats/hashrate?start=${start}&end=${end}`
  );

export const getPercentageStats = (
  poolId,
  start = START_DATE,
  end = END_DATE
) =>
  makeRequest(
    `${
      config.apiUrl
    }/pools/${poolId}/stats/percentage?start=${start}&end=${end}`
  );

export const getNetworkDifficulty = (
  poolId,
  start = START_DATE,
  end = END_DATE
) =>
  makeRequest(
    `${
      config.apiUrl
    }/pools/${poolId}/networkDifficulty?start=${start}&end=${end}`
  );

const makeRequest = url => {
  return new Promise((resolve, reject) => {
    fetch(url)
      .then(response => {
        if (response.ok) {
          return response.json().catch(() => "Error deserializing JSON data");
        }
      })
      .then(response => resolve(response))
      .catch(error => {
        reject(error);
      });
  });
};
