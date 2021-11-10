# Crypto Forecast Model Trainer CLI

Features:

- Specifying a trading pair using the `Pair` keyword arg, such as `Pair BTCUSD`. A csv file for the pair must exist
under the `Data/` directory such as `BTCUSD.csv` containing data in the format:

    ```csv
    Date,High,Low,Mid,Last,Bid,Ask,Volume
    2021-10-21,272.8,257.78,271.335,270.93,271.15,271.52,4373.01073066
    2021-10-20,265.86,253.3,261.04,261.16,261.0,261.08,3215.74497309
    ```
    Similarly for ETHUSD or XMRUSD.

- Running the notebook `graphs.ipynb` with the proper `currency` set in the first codeblock now generates a forecast graph. This must be run after a training execution with `dotnet run Action training Pair BTCUSD`.

- An optimization for the `windowSize` hyperparameter of the `Single Spectrum Analysis (SSA)`  model has been added. This is a critical tuning parameter which can be easily optimized by iteration. For each `windowSize` between 2 and 30, the model is fit to the training data and the mean absolute error metric is calculated.
The model that returns the lowest mean absolute error value is kept.
The test data used for evaluation is then included in training the model before it is saved, so that when predictions are made, the model has been trained on the entire dataset.

    To make a prediction run the CLI with:
`dotnet run Action predict Pair BTCUSD`.

TODO:

- Add auto fetching of data for new pairs using REST APIs.
- Add ability to plot forecasts into the future for models trained to the present day.
- Begin considering a SPA application that can display these graphs and offer a web-based GUI to train and generate predictions.