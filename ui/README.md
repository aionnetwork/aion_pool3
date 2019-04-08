## Building Requirements

In order to build the code on your machine, you need to have node `9.11` (or higher) and `yarn 1.7` (or higher) installed.
Note: you can also use npm instead of yarn.

## Installation

```bash
git clone from repo
cd into your folder
yarn
```

## Configuration

Ensure that the API URL from `config.json` is correct (note that depending on your setup,
you might encounter cross origin issues as well).

## Development build + server

```bash
yarn start
```

## Production Build

```bash
yarn build
```

## Production Deployment

Any web server should be able to host the app but since we're using html5 push state history API,
there might be some extra configuration to be done.
[More info here](https://github.com/facebook/create-react-app/blob/master/packages/react-scripts/template/README.md#serving-apps-with-client-side-routing)
