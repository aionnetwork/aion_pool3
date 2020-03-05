const webpack = require("webpack");
const { execSync } = require("child_process");
const currentVersion = execSync("git describe --always --dirty=-modified", {
  encoding: "utf-8"
});
module.exports = {
  plugins: [
    new webpack.DefinePlugin({
      CURRENTVERSION: JSON.stringify(currentVersion)
    })
  ]
};
