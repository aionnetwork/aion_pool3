import React from "react";
import SimpleTable from "components/simpleTable";
import { getReadableHashRateString } from "utils";
import { Link } from "react-router-dom";

const columns = [
  {
    title: "Miner Address",
    property: "miner",
    width: 510
  },
  {
    title: "Hashrate",
    property: "hashrate",
    width: 175
  },
  {
    title: "Efficiency",
    property: "efficiency",
    width: 175
  },
  {
    title: "Total Aions Received",
    property: "totalPaid",
    width: 180
  }
];

const renderCellContent = (rowIndex, columnIndex, cellData) => {
  switch (columnIndex) {
    case 0:
      return <Link to={`/miner/${cellData}`}>{cellData}</Link>;
    case 1:
      return getReadableHashRateString(cellData);
    case 2:
      return `${cellData * 100}%`;
    case 3:
      return Number(cellData).toFixed(3);
    default:
      return cellData;
  }
};

export default ({
  miners,
  minersPage,
  getMinersPage,
  isLoadingMinersPage,
  totalMinersPages
}) => (
  <SimpleTable
    hasPaging
    totalPages={totalMinersPages}
    isLoadingPage={isLoadingMinersPage}
    currentPage={minersPage}
    getPage={getMinersPage}
    columns={columns}
    data={miners}
    renderCellContent={renderCellContent}
  />
);
