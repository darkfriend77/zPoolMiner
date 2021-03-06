﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using zPoolMiner.Configs;

namespace zPoolMiner
{
    internal class ExchangeRateAPI
    {
        public class Result
        {
            public Object Algorithms { get; set; }
            public Object Servers { get; set; }
            public Object Idealratios { get; set; }
            public List<Dictionary<string, string>> Exchanges { get; set; }
            public Dictionary<string, double> Exchanges_fiat { get; set; }
        }

        public class ExchangeRateJSON
        {
            public Result Result { get; set; }
            public string Method { get; set; }
        }

        private const string apiUrl = "https://api.nicehash.com/api?method=nicehash.service.info";

        private static Dictionary<string, double> exchanges_fiat = null;
        private static double USD_BTC_rate = -1;
        public static string ActiveDisplayCurrency = "USD";

        private static bool ConverterActive
        {
            get { return ConfigManager.GeneralConfig.DisplayCurrency != "USD"; }
        }

        public static double ConvertToActiveCurrency(double amount)
        {
            if (!ConverterActive)
            {
                return amount;
            }

            // if we are still null after an update something went wrong. just use USD hopefully itll update next tick
            if (exchanges_fiat == null || ActiveDisplayCurrency == "USD")
            {
                // Moved logging to update for berevity
                return amount;
            }

            //Helpers.ConsolePrint("CurrencyConverter", "Current Currency: " + ConfigManager.Instance.GeneralConfig.DisplayCurrency);
            if (exchanges_fiat.TryGetValue(ActiveDisplayCurrency, out double usdExchangeRate))
                return amount * usdExchangeRate;
            else
            {
                Helpers.ConsolePrint("CurrencyConverter", "Unknown Currency Tag: " + ActiveDisplayCurrency + " falling back to USD rates");
                ActiveDisplayCurrency = "USD";
                return amount;
            }
        }

        public static double GetUSDExchangeRate()
        {
            if (USD_BTC_rate > 0)
            {
                return USD_BTC_rate;
            }
            return 0.0;
        }

        public static void UpdateAPI(string worker)
        {
            var WR = (HttpWebRequest)WebRequest.Create("https://min-api.cryptocompare.com/data/pricemulti?fsyms=BTC&tsyms=AUD,BRL,CAD,CHF,CLP,CNY,DKK,EUR,GBP,HKD,INR,ISK,JPY,KRW,NZD,PLN,RUB,SEK,SGD,THB,TWD,ZAR,USD");
            var Response = WR.GetResponse();
            var SS = Response.GetResponseStream();
            SS.ReadTimeout = 20 * 1000;
            var Reader = new StreamReader(SS);
            var ResponseFromServer = Reader.ReadToEnd();
            if (ResponseFromServer.Length == 0 || ResponseFromServer[0] != '{')
                throw new Exception("Not JSON!");
            Reader.Close();
            Response.Close();

            dynamic fiat_rates = JObject.Parse(ResponseFromServer);
            try
            {
                //USD_BTC_rate = Helpers.ParseDouble((string)fiat_rates[ConfigManager.GeneralConfig.DisplayCurrency]["last"]);
                USD_BTC_rate = Helpers.ParseDouble((string)fiat_rates["BTC"]["USD"]);

                exchanges_fiat = new Dictionary<string, double>();
                foreach (var c in _supportedCurrencies)
                    //exchanges_fiat.Add(c, Helpers.ParseDouble((string)fiat_rates[c]["7d"]) / USD_BTC_rate);
                    exchanges_fiat.Add(c, Helpers.ParseDouble((string)fiat_rates["BTC"][c]) / USD_BTC_rate);
            }
            catch
            {
            }
        }

        private static readonly string[] _supportedCurrencies = {
            "AUD",
            "BRL",
            "CAD",
            "CHF",
            "CLP",
            "CNY", //
            "DKK",
            "EUR",
            "GBP",
            "HKD",
            "INR",
            "ISK", //
            "JPY",
            "KRW",
            "NZD",
            "PLN",
            "RUB",
            "SEK",
            "SGD",
            "THB",
            "TWD",//
            "ZAR",
            "USD"
        };
    }
}