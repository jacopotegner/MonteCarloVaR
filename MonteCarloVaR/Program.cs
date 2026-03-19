using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;

namespace MonteCarloVaR
{

    public class MarketDataFetcher
    {

        private readonly HttpClient _httpClient;

        public MarketDataFetcher()
        {
            _httpClient = new HttpClient();

            /*
             * Yahoo Finance will block requests that don't look human, this is a way to mask them. 
             * Alternativly to get the data we could use YahooFinanceAPI or a professional API like Polygon.io.
             * If the running the main gives this error: 429 (Too Many Requests) try one of the method above
             */
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
        }

        //Async method to download files
        public async Task<(double Drift, double Volatility, double LastPrice)> GetAssetParametersAsync(string ticker, string range = "1y")
        {
           
            string url = $"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}?interval=1d&range={range}";

            try
            {
    
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                
                string jsonResponse = await response.Content.ReadAsStringAsync();

                //Estracts the closing prices from the JSON
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                var result = doc.RootElement.GetProperty("chart").GetProperty("result")[0];
                var closePricesElement = result.GetProperty("indicators").GetProperty("quote")[0].GetProperty("close");

                List<double> closePrices = new List<double>();
                foreach (var price in closePricesElement.EnumerateArray())
                {
                    //Yahoo Finance sometimes reports null value, we try to hande il this way
                    if (price.ValueKind != JsonValueKind.Null)
                    {
                        closePrices.Add(price.GetDouble());
                    }
                }

                //Calculates the return
                List<double> dailyReturns = new List<double>();
                for (int i = 1; i < closePrices.Count; i++)
                {
                    
                    double logReturn = Math.Log(closePrices[i] / closePrices[i - 1]);
                    dailyReturns.Add(logReturn);
                }

                //Calculate Drift and Volatility
                double dailyMean = dailyReturns.Average();
                double dailyVariance = dailyReturns.Select(r => Math.Pow(r - dailyMean, 2)).Sum() / dailyReturns.Count;
                double dailyVolatility = Math.Sqrt(dailyVariance);

                double annualizedDrift = dailyMean * 252;
                double annualizedVolatility = dailyVolatility * Math.Sqrt(252);
                double lastPrice = closePrices.Last();

                return (annualizedDrift, annualizedVolatility, lastPrice);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during the data fetch for {ticker}: {ex.Message}");
                throw;
            }
        }
    }
    
//Random number generator through the Box-Muller method to obtain a Normal distribution
public static class NormalRandomGenerator
    {
        private static readonly Random random = new Random();

        public static double GenerateStandardNormal()
        {
            double u1 = 1.0 - random.NextDouble();
            double u2 = 1.0 - random.NextDouble();
            //Box-Muller transformation
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }
    }

    public class MonteCarloSimulator
    {
        public double[] SimulatePrices(double initialPrice, double drift, double volatility, int days, int numSimulations)
        {
            double[] finalPrices = new double[numSimulations];
            double dt = 1.0 / 252.0; //This represent (roughly) the number of days in which the markets are open in a year

            //Parallel lets us use all the core of the CPU
            Parallel.For(0, numSimulations, i =>
            {
                double currentPrice = initialPrice;

                for (int day = 0; day < days; day++)
                {
                    double z = NormalRandomGenerator.GenerateStandardNormal();

                    //Using Geometric Brownian Motion, a common model to calculate the prices in the future
                    double exponent = (drift - (Math.Pow(volatility, 2) / 2)) * dt + volatility * Math.Sqrt(dt) * z;
                    currentPrice = currentPrice * Math.Exp(exponent);
                }

                finalPrices[i] = currentPrice;
            });

            return finalPrices;
        }
    }

    public class VaRCalculator
    {
        public double CalculateVaR(double[] finalPrices, double initialCapital, double confidenceLevel)
        {
            //Calculate profit and losses (pnl) for EACH simulation
            double[] pnl = finalPrices.Select(price => price - initialCapital).ToArray();

            Array.Sort(pnl);

            int index = (int)Math.Floor(pnl.Length * (1.0 - confidenceLevel));

            //VaR is the max loss at the confidence level
            return pnl[index];
        }
    }

  
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("--- Monte Carlo VaR with Live API Data ---");

            string ticker = "AAPL"; //Thicker can be MODIFIED
            Console.WriteLine($"Downloading hystoric data of {ticker} from Yahoo Finance...");

            var dataFetcher = new MarketDataFetcher();

            var (drift, volatility, lastPrice) = await dataFetcher.GetAssetParametersAsync(ticker, "1y");

            Console.WriteLine($"Success:");
            Console.WriteLine($"- Price: ${lastPrice:F2}");
            Console.WriteLine($"- Annualized drift: {drift:P2}");
            Console.WriteLine($"- Annualized volatility: {volatility:P2}");

            
            int daysToSimulate = 30;            //days to simulates can be MODIFIED
            int numSimulations = 100000;        //number of simulations can be MODIFIED
            double confidenceLevel = 0.95;      //confidence level can be MODIFIED

            var simulator = new MonteCarloSimulator();
            double[] simulatedPrices = simulator.SimulatePrices(lastPrice, drift, volatility, daysToSimulate, numSimulations);

            var varCalc = new VaRCalculator();
            double varValue = varCalc.CalculateVaR(simulatedPrices, lastPrice, confidenceLevel);

            Console.WriteLine($"\n--- Value at Risk Results ---");
            Console.WriteLine($"VaR at {daysToSimulate} days at {confidenceLevel * 100}%: ${Math.Abs(varValue):F2}");
        }
    }
}
