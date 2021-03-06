﻿using System;
using System.Collections.Generic;
using System.Reflection;

using Sq1.Core.DataTypes;
using Sq1.Core.Static;

namespace Sq1.Core.Streaming {
	public class DataDistributor {
		private StreamingProvider StreamingProvider { get; set; }
		public Dictionary<string, Dictionary<BarScaleInterval, SymbolScaleDistributionChannel>> DistributionChannels { get; protected set; }
		private Object lockConsumersBySymbol = new Object();

		public DataDistributor(StreamingProvider streamingProvider) {
			this.StreamingProvider = streamingProvider;
			DistributionChannels = new Dictionary<string, Dictionary<BarScaleInterval, SymbolScaleDistributionChannel>>();
		}

		public void ConsumerQuoteRegister(string symbol, BarScaleInterval scaleInterval, IStreamingConsumer consumer) {
			lock (lockConsumersBySymbol) {
				if (this.DistributionChannels.ContainsKey(symbol) == false) {
					SymbolScaleDistributionChannel newChannel = new SymbolScaleDistributionChannel(symbol, scaleInterval);
					newChannel.ConsumersQuoteAdd(consumer);
					Dictionary<BarScaleInterval, SymbolScaleDistributionChannel> newScaleChannels = new Dictionary<BarScaleInterval, SymbolScaleDistributionChannel>();
					newScaleChannels.Add(scaleInterval, newChannel);
					this.DistributionChannels.Add(symbol, newScaleChannels);
					if (this.StreamingProvider.UpstreamIsSubscribed(symbol) == false) {
						this.StreamingProvider.UpstreamSubscribe(symbol);
					}
					return;
				}
				Dictionary<BarScaleInterval, SymbolScaleDistributionChannel> channels = this.DistributionChannels[symbol];
				if (channels.ContainsKey(scaleInterval) == false) {
					SymbolScaleDistributionChannel newChannel = new SymbolScaleDistributionChannel(symbol, scaleInterval);
					newChannel.ConsumersQuoteAdd(consumer);
					channels.Add(scaleInterval, newChannel);
					return;
				}
				SymbolScaleDistributionChannel channel = channels[scaleInterval];
				if (channel.ConsumersQuoteContains(consumer) == false) {
					channel.ConsumersQuoteAdd(consumer);
					return;
				}
				Assembler.PopupException("QuoteConsumer [" + consumer + "] already registered for [" + channel + "]; returning");
			}
		}
		public void ConsumerQuoteUnRegister(string symbol, BarScaleInterval scaleInterval, IStreamingConsumer consumer) {
			lock (lockConsumersBySymbol) {
				if (this.DistributionChannels.ContainsKey(symbol) == false) {
					Assembler.PopupException("Can't unregister QuoteConsumer [" + consumer + "]: symbol[" + symbol + "] is not registered for any consumers; returning");
					return;
				}
				Dictionary<BarScaleInterval, SymbolScaleDistributionChannel> channels = this.DistributionChannels[symbol];
				if (channels.ContainsKey(scaleInterval) == false) {
					string symbolDistributorsAsString = "";
					foreach (SymbolScaleDistributionChannel d in channels.Values) symbolDistributorsAsString += d + ",";
					symbolDistributorsAsString.TrimEnd(',');
					Assembler.PopupException("Can't unregister QuoteConsumer [" + consumer + "]: scaleInterval[" + scaleInterval + "] not found among distributors [" + symbolDistributorsAsString + "]; returning");
					return;
				}
				SymbolScaleDistributionChannel channel = channels[scaleInterval];
				if (channel.ConsumersQuoteContains(consumer) == false) {
					Assembler.PopupException("Can't unregister QuoteConsumer [" + consumer + "]: consumer not found in [" + channel.ConsumersQuoteAsString + "]; returning");
					return;
				}
				channel.ConsumersQuoteRemove(consumer);
				if (channel.ConsumersBarCount == 0 && channel.ConsumersQuoteCount == 0) {
					//Assembler.PopupException("QuoteConsumer [" + consumer + "] was the last one using [" + symbol + "][" + scaleInterval + "]; removing QuoteBarDistributor[" + channel + "]");
					channels.Remove(scaleInterval);
					if (channels.Count == 0) {
						//Assembler.PopupException("QuoteConsumer [" + scaleInterval + "] was the last one listening for [" + symbol + "]");
						//Assembler.PopupException("...removing[" + symbol + "] from this.DistributionChannels[" + this.DistributionChannels + "]");
						this.DistributionChannels.Remove(symbol);
						//Assembler.PopupException("...UpstreamUnSubscribing [" + symbol + "]");
						this.StreamingProvider.UpstreamUnSubscribe(symbol);
					}
				}
			}

		}
		public bool ConsumerQuoteIsRegistered(string symbol, BarScaleInterval scaleInterval, IStreamingConsumer consumer) {
			bool ret = false;
			Dictionary<string, List<BarScaleInterval>> symbolsScaleIntervals = SymbolsScaleIntervalsQuoteConsumerRegistered(consumer);
			if (symbolsScaleIntervals == null) return ret;
			ret = symbolsScaleIntervals.ContainsKey(symbol);
			return ret;
		}

		public void ConsumerBarRegister(string symbol, BarScaleInterval scaleInterval, IStreamingConsumer consumer) {
			if (consumer is StaticProvider) {
				int a = 1;
			}
			lock (lockConsumersBySymbol) {
				if (this.DistributionChannels.ContainsKey(symbol) == false) {
					SymbolScaleDistributionChannel newChannel = new SymbolScaleDistributionChannel(symbol, scaleInterval);
					newChannel.ConsumersBarAdd(consumer);
					Dictionary<BarScaleInterval, SymbolScaleDistributionChannel> newScaleChannels = new Dictionary<BarScaleInterval, SymbolScaleDistributionChannel>();
					newScaleChannels.Add(scaleInterval, newChannel);
					this.DistributionChannels.Add(symbol, newScaleChannels);
					if (this.StreamingProvider.UpstreamIsSubscribed(symbol) == false) {
						this.StreamingProvider.UpstreamSubscribe(symbol);
					}
					return;
				}
				Dictionary<BarScaleInterval, SymbolScaleDistributionChannel> channels = this.DistributionChannels[symbol];
				if (channels.ContainsKey(scaleInterval) == false) {
					SymbolScaleDistributionChannel newChannel = new SymbolScaleDistributionChannel(symbol, scaleInterval);
					newChannel.ConsumersBarAdd(consumer);
					channels.Add(scaleInterval, newChannel);
					return;
				}
				SymbolScaleDistributionChannel channel = channels[scaleInterval];
				if (channel.ConsumersBarContains(consumer) == false) {
					channel.ConsumersBarAdd(consumer);
					return;
				}
				Assembler.PopupException("BarConsumer [" + consumer + "] already registered for [" + channel + "]; returning");
			}
		}
		public void ConsumerBarUnRegister(string symbol, BarScaleInterval scaleInterval, IStreamingConsumer consumer) {
			if (consumer is StaticProvider) {
				int a = 1;
			}
			lock (lockConsumersBySymbol) {
				if (this.DistributionChannels.ContainsKey(symbol) == false) {
					Assembler.PopupException("Can't unregister BarConsumer [" + consumer + "]: symbol[" + symbol + "] is not registered for any consumers; returning");
					return;
				}
				Dictionary<BarScaleInterval, SymbolScaleDistributionChannel> distributorsByScaleInterval = this.DistributionChannels[symbol];
				if (distributorsByScaleInterval.ContainsKey(scaleInterval) == false) {
					string symbolDistributorsAsString = "";
					foreach (SymbolScaleDistributionChannel d in distributorsByScaleInterval.Values) symbolDistributorsAsString += d + ",";
					symbolDistributorsAsString.TrimEnd(',');
					Assembler.PopupException("Can't unregister BarConsumer [" + consumer + "]: scaleInterval[" + scaleInterval + "] not found among distributors [" + symbolDistributorsAsString + "]; returning");
					return;
				}
				SymbolScaleDistributionChannel distributor = distributorsByScaleInterval[scaleInterval];
				if (distributor.ConsumersBarContains(consumer) == false) {
					Assembler.PopupException("Can't unregister BarConsumer [" + consumer + "]: consumer not found in [" + distributor.ConsumersBarAsString + "]; returning");
					return;
				}
				distributor.ConsumersBarRemove(consumer);
				if (distributor.ConsumersBarCount == 0 && distributor.ConsumersQuoteCount == 0) {
					//Assembler.PopupException("BarConsumer [" + consumer + "] was the last one using [" + symbol + "][" + scaleInterval + "]; removing QuoteBarDistributor[" + distributor + "]");
					distributorsByScaleInterval.Remove(scaleInterval);
					if (distributorsByScaleInterval.Count == 0) {
						//Assembler.PopupException("BarConsumer [" + scaleInterval + "] was the last one listening for [" + symbol + "]");
						//Assembler.PopupException("...removing[" + symbol + "] from this.DistributionChannels[" + this.DistributionChannels + "]");
						this.DistributionChannels.Remove(symbol);
						//Assembler.PopupException("...UpstreamUnSubscribing [" + symbol + "]");
						this.StreamingProvider.UpstreamUnSubscribe(symbol);
					}
				}
			}
		}
		public bool ConsumerBarIsRegistered(string symbol, BarScaleInterval scaleInterval, IStreamingConsumer consumer) {
			bool ret = false;
			Dictionary<string, List<BarScaleInterval>> symbolsScaleIntervals = SymbolsScaleIntervalsBarConsumerRegistered(consumer);
			if (symbolsScaleIntervals == null) return ret;
			ret = symbolsScaleIntervals.ContainsKey(symbol);
			return ret;
		}

		public void ConsumerQuoteUnregisterDying(IStreamingConsumer dyingConsumer) {
			lock (lockConsumersBySymbol) {
				Dictionary<string, List<BarScaleInterval>> symbolsScaleIntervals = this.SymbolsScaleIntervalsQuoteConsumerRegistered(dyingConsumer);
				if (symbolsScaleIntervals == null) {
					Assembler.PopupException("QuoteConsumer [" + dyingConsumer + "] was not registered to any symbols + ScaleIntervals; returning");
					return;
				}
				foreach (string symbol in symbolsScaleIntervals.Keys) {
					foreach (BarScaleInterval scaleInterval in symbolsScaleIntervals[symbol]) {
						this.ConsumerQuoteUnRegister(symbol, scaleInterval, dyingConsumer);
					}
				}
			}
		}
		public void ConsumerBarUnregisterDying(IStreamingConsumer dyingConsumer) {
			lock (lockConsumersBySymbol) {
				Dictionary<string, List<BarScaleInterval>> symbolsScaleIntervals = this.SymbolsScaleIntervalsBarConsumerRegistered(dyingConsumer);
				if (symbolsScaleIntervals == null) {
					Assembler.PopupException("BarConsumer [" + dyingConsumer + "] was not registered to any symbols + ScaleIntervals; returning");
					return;
				}
				foreach (string symbol in symbolsScaleIntervals.Keys) {
					foreach (BarScaleInterval scaleInterval in symbolsScaleIntervals[symbol]) {
						this.ConsumerBarUnRegister(symbol, scaleInterval, dyingConsumer);
					}
				}
			}
		}

		public Dictionary<string, List<BarScaleInterval>> SymbolsScaleIntervalsQuoteConsumerRegistered(IStreamingConsumer consumer) {
			Dictionary<string, List<BarScaleInterval>> ret = null;
			foreach (string symbol in this.DistributionChannels.Keys) {
				Dictionary<BarScaleInterval, SymbolScaleDistributionChannel> consumersByScaleInterval = DistributionChannels[symbol];
				foreach (BarScaleInterval scaleInterval in consumersByScaleInterval.Keys) {
					SymbolScaleDistributionChannel consumers = consumersByScaleInterval[scaleInterval];
					if (consumers.ConsumersQuoteContains(consumer)) {
						if (ret == null) ret = new Dictionary<string, List<BarScaleInterval>>();
						if (ret.ContainsKey(symbol) == false) ret.Add(symbol, new List<BarScaleInterval>());
						ret[symbol].Add(scaleInterval);
					}
				}
			}
			return ret;
		}
		public Dictionary<string, List<BarScaleInterval>> SymbolsScaleIntervalsBarConsumerRegistered(IStreamingConsumer consumer) {
			Dictionary<string, List<BarScaleInterval>> ret = null;
			foreach (string symbol in this.DistributionChannels.Keys) {
				Dictionary<BarScaleInterval, SymbolScaleDistributionChannel> consumersByScaleInterval = DistributionChannels[symbol];
				foreach (BarScaleInterval scaleInterval in consumersByScaleInterval.Keys) {
					SymbolScaleDistributionChannel consumers = consumersByScaleInterval[scaleInterval];
					if (consumers.ConsumersBarContains(consumer)) {
						if (ret == null) ret = new Dictionary<string, List<BarScaleInterval>>();
						if (ret.ContainsKey(symbol) == false) ret.Add(symbol, new List<BarScaleInterval>());
						ret[symbol].Add(scaleInterval);
					}
				}
			}
			return ret;
		}

		public void PushQuoteToChannel(Quote quote) {
			if (String.IsNullOrEmpty(quote.Symbol)) {
				Assembler.PopupException("quote[" + quote + "]'se Symbol is null or empty, returning");
				return;
			}
			Quote lastQuote = this.StreamingProvider.StreamingDataSnapshot.LastQuoteGetForSymbol(quote.Symbol);
			List<SymbolScaleDistributionChannel> channelsForSymbol = GetDistributionChannelsFor(quote.Symbol);
			foreach (SymbolScaleDistributionChannel channel in channelsForSymbol) {
				// late quote should be within current StreamingBar, otherwize don't deliver for channel
				if (lastQuote != null && quote.ServerTime < lastQuote.ServerTime) {
					Bar streamingBar = channel.StreamingBarFactoryUnattached.StreamingBarUnattached;
					if (quote.ServerTime <= streamingBar.DateTimeOpen) {
						string msg = "skipping old quote for quote.ServerTime[" + quote.ServerTime + "], can only accept for current"
							+ " StreamingBar (" + streamingBar.DateTimeOpen + " .. " + streamingBar.DateTimeNextBarOpenUnconditional + "];"
							+ " quote=[" + quote + "]";
						Assembler.PopupException(msg);
						this.StreamingProvider.StatusReporter.PopupException(new Exception(msg));
						continue;
					}
				}
				// don't clone quote here!! enrich inside each channel => IntraBarSerno++,
				// then clone quote for every consumer lateBind to ParentBars, variable on the history length
				channel.PushQuoteToConsumers(quote);
			}
		}
		public List<SymbolScaleDistributionChannel> GetDistributionChannelsFor(string symbol) {
			List<SymbolScaleDistributionChannel> distributors = new List<SymbolScaleDistributionChannel>();
			lock (lockConsumersBySymbol) {
				distributors = new List<SymbolScaleDistributionChannel>(this.DistributionChannels[symbol].Values);
			}
			return distributors;
		}

		public SymbolScaleDistributionChannel GetDistributionChannelFor(string symbol, BarScaleInterval barScaleInterval) {

			if (this.DistributionChannels.ContainsKey(symbol) == false) {
				string msg = "NO_SYMBOL_SUBSCRIBER DataDistributor[" + this + "].DistributionChannels.ContainsKey(" + symbol + ")=false";
				Assembler.PopupException(msg);
				throw new Exception(msg);
			}
			Dictionary<BarScaleInterval, SymbolScaleDistributionChannel> distributionChannels = this.DistributionChannels[symbol];
			if (distributionChannels.ContainsKey(barScaleInterval) == false) {
				string msg = "NO_SCALEINTERVAL_SUBSCRIBER DataDistributor[" + this
					+ "].DistributionChannels[" + symbol + "].ContainsKey(" + barScaleInterval + ")=false";
				Assembler.PopupException(msg);
				//this.StreamingProvider.StatusReporter.PopupException(new Exception(msg));
				throw new Exception(msg);
			}
			return distributionChannels[barScaleInterval];
		}
		public override string ToString() {
			string ret = "";
			foreach (string symbol in DistributionChannels.Keys) {
				string consumers = "";
				Dictionary<BarScaleInterval, SymbolScaleDistributionChannel> distributionChannel = DistributionChannels[symbol];
				foreach (BarScaleInterval scaleInterval in distributionChannel.Keys) {
					if (consumers != "") consumers += ",";
					consumers += distributionChannel[scaleInterval];
				}
				ret += symbol + "{" + consumers + "}";
			}
			return ret;
		}
	}
}