# Crypto Price Forecaster

Cryptocurrency price analysis and forecasting desktop app, built with C#,
ML.NET, LiveCharts v2 and Avalonia.

## Historical Data Exploration

![HistoricalDataView](./images/HistoricalData.gif)

## Model Training and Predictions

![ModelTraining](./images/ModelTraining.gif)

## Running the application locally

1. Download the latest stable release from the releases section of the
   repository.
2. Add your `data.nasdaq.com` API key as an environment variable `API_KEY`. This
   is needed for historical data queries.
3. Extract the zip and run `ForecasterGUI.exe`.

Current price queries powered by _CoinGecko_.
