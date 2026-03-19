
# Monte Carlo Value at Risk (VaR) Engine

An end-to-end C# application that calculates the **Value at Risk (VaR)** of financial assets using **Monte Carlo simulations** and live market data. 

This project demonstrates the practical application of quantitative finance models, asynchronous programming, and high-performance computing in C#.

## 🚀 Key Features

* **Live Market Data Integration:** Asynchronously fetches historical stock data from Yahoo Finance via HTTP requests and parses the JSON response to calculate annualized drift and volatility using logarithmic returns.
* **High-Performance Computing:** Utilizes C#'s `Parallel.For` to run hundreds of thousands of Monte Carlo simulations across multiple CPU cores, drastically reducing execution time.
* **Advanced Statistical Modeling:** Implements the **Box-Muller transform** to generate normally distributed random variables from uniform random numbers.
* **Geometric Brownian Motion (GBM):** Projects future asset prices using the standard continuous-time stochastic process used in quantitative finance.

## 🧮 Mathematical Background

This engine relies on several core quantitative finance concepts:

### 1. Geometric Brownian Motion (GBM)
Future prices are simulated using the GBM formula, which assumes that the logarithmic returns of an asset follow a normal distribution:

$$S_{t+1} = S_t \cdot \exp\left( \left(\mu - \frac{\sigma^2}{2}\right) \Delta t + \sigma \sqrt{\Delta t} \cdot Z \right)$$

Where:
* $S_t$ = Current stock price
* $\mu$ = Annualized drift (expected return)
* $\sigma$ = Annualized volatility
* $\Delta t$ = Time step (1/252 for daily trading days)
* $Z$ = Standard normal random variable 

### 2. Box-Muller Transform
Since standard computer random generators produce uniform distributions, the Box-Muller transform is used to convert them into a standard normal distribution ($N(0,1)$), which is required for the $Z$ variable in the GBM equation.

### 3. Value at Risk (VaR)
VaR measures the maximum potential loss of an investment over a given time horizon at a specific confidence level (e.g., 95%). The engine calculates this by ordering the simulated P&L (Profit & Loss) outcomes and finding the threshold at the chosen percentile.

## 🛠️ Configuration & Usage

You can easily modify the parameters directly in the `Program.cs` file to test different scenarios:

```csharp
string ticker = "AAPL";           // The stock ticker (e.g., TSLA, MSFT, SPY)
int daysToSimulate = 30;          // Time horizon for the VaR calculation
int numSimulations = 100000;      // Number of Monte Carlo paths
double confidenceLevel = 0.95;    // Confidence level (e.g., 95% or 99%)
```

### Note on Data Fetching & API Limitations
The project uses the public Yahoo Finance API endpoint. To prevent `429 (Too Many Requests)` errors, the `HttpClient` is configured with a browser-like `User-Agent`. 
*If you encounter rate-limiting issues or wish to deploy this in a production environment, consider replacing the data fetcher with a professional API like [Polygon.io](https://polygon.io/) or Alpha Vantage.*

## 💻 Tech Stack
* **Language:** C# (.NET)
* **Architecture:** Object-Oriented Programming (OOP)
* **Libraries:** `System.Net.Http`, `System.Text.Json`, `System.Threading.Tasks`
