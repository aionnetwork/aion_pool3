import React from "react";
import SimpleTable from "components/simpleTable";
import { getReadableHashRateString } from "utils";

const columns = [
  {
    title: "Name",
    property: "name",
    width: 300
  },
  {
    title: "Hashrate",
    property: "hashrate",
    width: 300
  },
  {
    title: "Shares per Second",
    property: "sharesPerSecond",
    width: 365
  }
];

const renderCellContent = (rowIndex, columnIndex, cellData) => {
  switch (columnIndex) {
    case 1:
      return getReadableHashRateString(cellData);
    case 2:
      return Number(cellData).toFixed(2);
    default:
      return cellData;
  }
};

export default ({ workers }) =>
  workers.length > 1 ? (
    <React.Fragment>
      <h2 className="table_section_title">Workers</h2>
      <div className="workersTable">
        <SimpleTable
          columns={columns}
          data={workers}
          renderCellContent={renderCellContent}
        />
      </div>
    </React.Fragment>
  ) : (
    ""
  );
