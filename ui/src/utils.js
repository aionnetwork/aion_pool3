import moment from "moment";

const sols = "Sol/s";
export const aionDashboardUrl = "https://mainnet.aion.network";
export const units = [" ", " K", " M", " G", " T", " P"];

export const getReadableHashRateString = (number, precision = 2) => {
  return getReadableUnitString(number, precision, sols);
};

export const getReadableUnitString = (number, precision = 2, unit = "") => {
  // what tier? (determines prefix)
  const tier = (Math.log10(number) / 3) | 0;

  // get prefix and determine scale
  const exactUnit = units[tier];
  const scale = Math.pow(10, tier * 3);

  // scale the number
  const scaled = number / scale;

  // format number and add prefix as suffix
  return scaled.toFixed(precision) + exactUnit + unit;
};

const customTooltipFormat = (color, key, info, date) => {
  return `<table>
      <thead>
        <tr>
          <td colspan="3"><strong class="x-value">${date}</strong></td>
        </tr>
      </thead>
      <tbody> 
        <tr>
          <td class="legend-color-guide"><div style="background-color: ${color}"></div></td>
          <td class="key">${key}</td>
          <td class="value">${info}</td>
        </tr>
      </tbody>
    </table>`;
};

export const formatTooltip = (number, precision, activeFilter) => {
  return customTooltipFormat(
    number.series[0].color,
    number.series[0].key,
    getReadableHashRateString(number.series[0].value, precision),
    formatDate(number.value, activeFilter)
  );
};

export const formatTooltipDiff = (number, precision, activeFilter) => {
  return customTooltipFormat(
    number.series[0].color,
    number.series[0].key,
    getReadableUnitString(number.series[0].value, precision, ""),
    formatDate(number.value, activeFilter)
  );
};

export const getMinMaxValues = (number, precision, data) => {
  const getHashes = data[0].values.map(item => item.value);
  const min = Math.min(...getHashes);
  const max = Math.max(...getHashes);

  if (number > min && number < max) {
    return "";
  }

  return getReadableHashRateString(number, precision);
};

export const formatDate = (timestamp, dateFormat) => {
  if (
    timestamp &&
    typeof timestamp === "string" &&
    timestamp.indexOf("T") > -1
  ) {
    return new Date(timestamp).toLocaleDateString();
  }

  switch (dateFormat) {
    case "hour":
      dateFormat = "LT";
      break;
    default:
      dateFormat = "l";
      break;
  }
  const dStr = moment.unix(timestamp).format(dateFormat);
  return dStr.indexOf("0") === 0 ? dStr.slice(1) : dStr;
};

export const fullDateFormat = unparsedDate => formatDate(unparsedDate, "l");

export const getAionDashboardAccountUrl = address =>
  `${aionDashboardUrl}/#/account/${address}`;

export const getAionTransactionUrl = hash =>
  `${aionDashboardUrl}/#/transaction/${hash}`;

export const convertToTimestamp = unparsedDate =>
  new Date(unparsedDate).valueOf();

const getTimeRange = (noOfDays, timeframe) => {
  const start = moment()
    .subtract(noOfDays, timeframe)
    .unix(); // how many days to go back
  const end = moment().unix(); // today

  return {
    start,
    end
  };
};

export const startDateEndDate = filter => {
  switch (filter) {
    case "hour":
      return getTimeRange(1, "hours");
    case "week":
      return getTimeRange(7, "days");
    case "month":
      return getTimeRange(1, "months");
    default:
      return getTimeRange(3650, "days");
  }
};

export const parseArrayForCharts = (values, object) => ({
  ...object,
  values
});
