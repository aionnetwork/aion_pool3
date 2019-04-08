import React from "react";
import ChartContext from "./chartContext";
import { Button } from "@blueprintjs/core";

const sortOptions = [
  {
    range: "hour",
    label: "1 hour"
  },
  {
    range: "week",
    label: "1 week"
  },
  {
    range: "month",
    label: "1 month"
  },
  {
    range: "all",
    label: "all"
  }
];

export default () => (
  <div className="chartFilters">
    <ul>
      {sortOptions.map((option, index) => (
        <ChartContext.Consumer key={index}>
          {context => (
            <li
              onClick={context.changeFilter}
              data-chartid={context.chartID}
              data-filter={option.range}
            >
              <Button
                loading={
                  context.activeFilter === option.range && context.loading
                }
                className={
                  context.activeFilter === option.range
                    ? "pt-button pt-icon-confirm pt-active"
                    : ""
                }
              >
                {option.label}
              </Button>
            </li>
          )}
        </ChartContext.Consumer>
      ))}
    </ul>
  </div>
);
