import React from "react";
import "./statsBoxes.css";

export default ({ boxes, isLoadingData }) => {
  return (
    <ul className="statsBoxes">
      {boxes.map((box, index) => {
        return (
          <li key={index} className="pt-card pt-elevation-0 pt-interactive">
            <div className="box__icon">
              <span className={`pt-icon-standard ${box.icon}`} />
            </div>
            <div className={`box__title ${isLoadingData ? "pt-skeleton" : ""}`}>
              {box.value !== undefined ? box.value : "-"}
            </div>
            <div className="box__description">{box.title}</div>
            {box.progress ? (
              <div className="progress__wrapper">
                <div
                  style={{ width: box.progress + "%" }}
                  className="progress__line"
                />
              </div>
            ) : (
              ""
            )}
          </li>
        );
      })}
    </ul>
  );
};
