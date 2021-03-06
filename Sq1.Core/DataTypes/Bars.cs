using System;
using System.Runtime.Serialization;

using Sq1.Core.DataFeed;
using Sq1.Core.Repositories;

namespace Sq1.Core.DataTypes {
	[DataContract]	// prevents serialization in JSON of the underlying bars
	public class Bars : BarsUnscaled {
		public static int InstanceAbsno = 0;

		public event EventHandler<BarEventArgs> BarStaticAdded;
		public event EventHandler<BarEventArgs> BarStreamingAdded;
		public event EventHandler<BarEventArgs> BarStreamingUpdatedMerged;
		
		public string SymbolHumanReadable;
		public BarScaleInterval ScaleInterval { get; private set; }

		public MarketInfo MarketInfo;
		public DataSource DataSource;

		public bool IsIntraday { get { return this.ScaleInterval.IsIntraday; } }
		public string SymbolIntervalScale { get { return "[" + this.Symbol + " " + this.ScaleInterval.ToString() + "]"; } }

		public Bar BarStreaming { get; private set; }
		public Bar BarStreamingCloneReadonly {
			get {
				//v1
//				Bar lastStatic = this.BarStaticLast;
//				DateTime lastStaticOrServerNow = (lastStatic != null)
//					? lastStatic.DateTimeNextBarOpenUnconditional
//					: this.MarketInfo.ServerTimeNow;
//				Bar ret = new Bar(this.Symbol, this.ScaleInterval, lastStaticOrServerNow);
//				ret.SetParentForBackwardUpdate(this, base.Count);
//				if (BarStreaming != null) {
//					ret.AbsorbOHLCVfrom(BarStreaming);
//				} else {
//					int a = 1;
//				}
//				return ret;
				
				//v2
				if (this.BarStreaming == null) return null;
				return this.BarStreaming.Clone();
			}
		}
		public Bar BarStaticFirst { get {
				Bar last = base.BarLast;
				if (last == null) return null; 
				if (last != this.BarStreaming) return last;
				return null;
				//throw new Exception("Bars.BarLast point to Bars.StreamingBar???");
			} }
		public Bar BarStaticLast { get {
				Bar last = base.BarLast;
				if (last == null) return null; 
				if (last != this.BarStreaming) return last;
				Bar preLast = base.BarPreLast;
				if (preLast == null) return null;
				if (preLast != this.BarStreaming) return preLast;
				//return null;
				throw new Exception("both Bars.BarLast and Bars.BarPreLast point to Bars.StreamingBar???");
			}
		}
		private Bars(string symbol, string reasonToExist = "NOREASON") : base(symbol, reasonToExist) {
			ScaleInterval = new BarScaleInterval(BarScale.Unknown, 0);
			SymbolHumanReadable = "";
			InstanceAbsno++;
		}
		public Bars(string symbol, BarScaleInterval scaleInterval, string reasonToExist) : this(symbol, reasonToExist) {
			this.ScaleInterval = scaleInterval;
			// it's a flashing tail but ALWAYS added into Bars for easy enumeration/charting/serialization;
			// ALWAYS ADDED, it is either still streaming (incomplete) OR it's complete (same instance becomes LastStaticBar);
			// while in streaming, you use AbsorbIntoStreaming(), when complete use CreateNewStreaming 
			//this.BarStreaming = new Bar(this.Symbol, this.ScaleInterval, DateTime.MinValue);
		}
		public Bars CloneNoBars(string reasonToExist = null, BarScaleInterval scaleIntervalConvertingTo = null) {
			if (scaleIntervalConvertingTo == null) scaleIntervalConvertingTo = this.ScaleInterval;
			if (string.IsNullOrEmpty(reasonToExist)) reasonToExist = "InitializedFrom(" + this.ReasonToExist + ")";
			Bars ret = new Bars(this.Symbol, scaleIntervalConvertingTo, reasonToExist);
			ret.SymbolHumanReadable = this.SymbolHumanReadable;
			ret.MarketInfo = this.MarketInfo;
			ret.SymbolInfo = this.SymbolInfo;
			ret.DataSource = this.DataSource;
			return ret;
		}
		public Bar CreateNewOrAbsorbIntoStreaming(Bar barToMergeToStreaming) {
			lock (base.LockBars) {
				bool shouldAppend = this.BarLast == null || barToMergeToStreaming.DateTimeOpen >= this.BarLast.DateTimeNextBarOpenUnconditional; 
				if (this.BarStreaming == null || shouldAppend) {	// if this.BarStreaming == null I'll have just one bar in Bars which will be streaming and no static 
					this.BarStreaming = this.barCreateAppendBindStreaming(barToMergeToStreaming);
					return this.BarStreaming;
				}
				if (this.BarStreaming == null) {
					throw new Exception("NO_STREAMING_BAR_TO_EXPAND: AbsorbIntoStreaming(" + barToMergeToStreaming + ")");
				}
				//base.BarAbsorbAppend(this.StreamingBar, open, high, low, close, volume);
				this.BarStreaming.MergeExpandHLCVwhileCompressingManyBarsToOne(barToMergeToStreaming);
				this.RaiseBarStreamingUpdated(barToMergeToStreaming);
				return this.BarStreaming;
			}
		}
		private Bar barCreateAppendBindStreaming(Bar bar) {
			return this.barCreateAppendBindStreaming(bar.DateTimeOpen, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume);
		}
		private Bar barCreateAppendBindStreaming(DateTime dateTime, double open, double high, double low, double close, double volume) {
			lock (base.LockBars) {
				Bar barAdding = new Bar(this.Symbol, this.ScaleInterval, dateTime);
				barAdding.SetOHLCV(open, high, low, close, volume);
				this.barAppendBindStreaming(barAdding);
				return barAdding;
			}
		}
		private void barAppendBindStreaming(Bar barAdding) {
			lock (base.LockBars) {
				this.BarAppendBind(barAdding);
				this.BarStreaming = barAdding;
				this.RaiseBarStreamingAdded(barAdding);
			}
		}
		public Bar BarCreateAppendBindStatic(DateTime dateTime, double open, double high, double low, double close, double volume) {
			lock (base.LockBars) {
				Bar barAdding = new Bar(this.Symbol, this.ScaleInterval, dateTime);
				barAdding.SetOHLCV(open, high, low, close, volume);
				this.BarAppendBindStatic(barAdding);
				return barAdding;
			}
		}
		public void BarAppendBindStatic(Bar barAdding) {
			lock (base.LockBars) {
				barAdding.CheckOHLCVthrow();
				this.BarStreaming = null;
				this.BarAppendBind(barAdding);
				this.RaiseBarStaticAdded(barAdding);
			}
		}
		protected override void CheckThrowDateIsNotLessThanScaleDictates(DateTime dateAdding) {
			if (this.Count == 0) return;
			if (dateAdding >= this.BarLast.DateTimeNextBarOpenUnconditional) return;
			throw new Exception("DATE_ADDING_IS_CLOSER_THAN_SCALEINTERVAL_DICTATES"
				+ ": dateAdding[" + dateAdding + "]<this.BarStaticLast.DateTimeNextBarOpenUnconditional["
				+ this.BarLast.DateTimeNextBarOpenUnconditional + "]");
		}
		protected void BarAppendBind(Bar barAdding) {
			lock (base.LockBars) {
				try {
					base.BarAppend(barAdding);
				} catch (Exception e) {
					string msg = "BARS_UNSCALED_IS_NOT_SATISFIED Bars.BarAppendBind[" + barAdding + "] to " + this;
					throw (new Exception(msg, e));
				}
				try {
					barAdding.SetParentForBackwardUpdate(this, base.Count - 1);
				} catch (Exception e) {
					string msg = "BACKWARD_UPDATE_FAILED adding bar[" + barAdding + "] to " + this;
					throw (new Exception(msg, e));
				}
			}
		}
		public void RaiseBarStaticAdded(Bar barAdding) {
			if (this.BarStaticAdded == null) return;
			try {
				this.BarStaticAdded(this, new BarEventArgs(barAdding));
			} catch (Exception ex) {
				string msg = "BarsBasic.BarStaticAdded(bar[" + barAdding + "])";
				Assembler.PopupException(msg, ex);
			}
		}
		public void RaiseBarStreamingAdded(Bar barAdding) {
			if (this.BarStreamingAdded == null) return;
			try {
				this.BarStreamingAdded(this, new BarEventArgs(barAdding));
			} catch (Exception ex) {
				string msg = "BarsBasic.BarStreamingAdded(bar[" + barAdding + "])";
				Assembler.PopupException(msg, ex);
			}
		}
		public void RaiseBarStreamingUpdated(Bar barUpdated) {
			if (this.BarStreamingUpdatedMerged == null) return;
			try {
				this.BarStreamingUpdatedMerged(this, new BarEventArgs(barUpdated));
			} catch (Exception ex) {
				string msg = "BarsBasic.BarStreamingUpdated(bar[" + barUpdated + "])";
				Assembler.PopupException(msg, ex);
			}
		}
		
		public void OverrideStreamingDOHLCVwith(Bar bar) {
			if (bar == null) {
				string msg = "I_DONT_ACCEPT_NULL_BARS_TO OverrideStreamingDOHLCVwith(" + bar + ")";
				throw new Exception(msg);
			}
			if (this.BarStreaming == null) {
				string msg = "CAN_ONLY_OVERRIDE_STREAMING_NOT_NULL_WHILE_NOW_IT_IS_NULL OverrideStreamingDOHLCVwith(" + bar + "): this.streamingBar == null";
				throw new Exception(msg);
			}
			//this.streamingBar.DateTimeOpen = bar.DateTimeOpen;
			this.BarStreaming.AbsorbOHLCVfrom(bar);
			this.RaiseBarStreamingUpdated(this.BarStreamingCloneReadonly);	// freeze changes in the clone so that subscribers get the same StreamingBar
		}

		public bool IsLastBarOfDay(int barNum) {
			if (this.IsIntraday == false) return false;
			if (barNum < base.Count - 1) {
				return base[barNum].DateTimeOpen.Date != base[barNum + 1].DateTimeOpen.Date;
			}
			DateTime dateTime = base[barNum].DateTimeOpen;
			for (int i = barNum - 1; i >= 0; i--) {
				if (base[i].DateTimeOpen.Date != base[i + 1].DateTimeOpen.Date) {
					DateTime dateTime2 = base[i].DateTimeOpen;
					return dateTime2.Hour == dateTime.Hour && dateTime2.Minute == dateTime.Minute;
				}
			}
			return false;
		}
		public override string ToString() {
			string ret = this.SymbolIntervalScale + base.Count + "bars";
			if (base.Count > 0) {
				try {
					Bar barLastStatic = this.BarStaticLast;
					ret += " LastStaticClose=[" + this.ValueFormatted(barLastStatic.Close) + "] @[" + barLastStatic.DateTimeOpen + "]";
				} catch (Exception e) {
					ret += " BARS_STATIC[" + (base.Count - 1) + "]_EXCEPTION";
				}
				try {
					Bar barStreaming = this.BarStreamingCloneReadonly;
					ret += " StreamingClose=[" + this.ValueFormatted(barStreaming.Close) + "] @[" + barStreaming.DateTimeOpen + "]";
				} catch (Exception e) {
					ret += " BARS_STREAMING[" + base.Count + "]_EXCEPTION";
				}
			}
			ret += " //Instance#" + InstanceAbsno;
			if (string.IsNullOrEmpty(ReasonToExist) == false) ret += ":" + ReasonToExist;

			return ret;
		}
		public string ValueFormatted(double ohlc) {
			return ohlc.ToString(this.SymbolInfo.PriceToStringFormat);
		}
		public override bool Equals(object another) {
			Bars bars = (Bars)another;
			string barsAsString = bars.ToString();
			string thisAsString = this.ToString();
			bool identicalContent = (
				bars.Symbol == this.Symbol
				&& bars.ScaleInterval == this.ScaleInterval
				&& bars.Count == base.Count
				&& bars.BarStaticFirst.ToString() == this.BarStaticFirst.ToString()
				&& bars.BarStaticLast.ToString() == this.BarStaticLast.ToString()
			);
			return identicalContent;
			//return (barsAsString == thisAsString);
		}

		[Obsolete("Designer uses reflection which doesn't feel static methods; instead, use new BarsBasic().GenerateAppend()")]
		public static Bars GenerateRandom(BarScaleInterval scaleInt,  int howManyBars = 10,
			string symbol = "SAMPLE", string reasonToExist = "test-ChartControl-DesignMode") {
			Bars ret = new Bars(symbol, scaleInt, reasonToExist);
			ret.GenerateAppend(howManyBars);
			return ret;
		}
		public void GenerateAppend(int howManyBars = 10) {
			int lowest = 1000;
			int highest = 9999;
			int volumeMax = 1000;
			float closeAwayFromOpenPotentialRange = 0.1f;		// how big candle bodies are, max
			float shadowsLengthRelativelyToCandleBody = 0.3f;	// how big candle shadows are, max
			DateTime dateCurrent = new DateTime(2011, 7, 2, 13, 26, 0);	//three years from now
			Random rand = new Random();
			int open = rand.Next(lowest, highest);
			for (int i = 0; i < howManyBars; i++) {
				int closeLowest = open - (int)Math.Round(open * closeAwayFromOpenPotentialRange);
				int closeHighest = open + (int)Math.Round(open * closeAwayFromOpenPotentialRange);
				if (closeLowest < lowest)
					closeLowest = lowest;
				if (closeHighest > highest)
					closeHighest = highest;
				int close = rand.Next(closeLowest, closeHighest);
				int candleBodyLow = open;
				int candleBodyHigh = close;
				if (open > close) {
					candleBodyLow = close;
					candleBodyHigh = open;
				}
				int candleBody = Math.Abs(close - open);
				int shadowLimit = (int)Math.Round(candleBody * shadowsLengthRelativelyToCandleBody);
				int high = rand.Next(candleBodyHigh, candleBodyHigh + shadowLimit);
				int low = rand.Next(candleBodyLow - shadowLimit, candleBodyLow);
				int volume = rand.Next(volumeMax);
				this.BarCreateAppendBindStatic(dateCurrent, open, high, low, close, volume);
				dateCurrent = dateCurrent.AddSeconds(this.ScaleInterval.TimeSpanInSeconds);
				open = close;
			}
		}
		public Bars SelectRange(BarDataRange dataRangeRq) {
			DateTime startDate = DateTime.MinValue;
			DateTime endDate = DateTime.MaxValue;
			dataRangeRq.FillStartEndDate(out startDate, out endDate);
			if (startDate == DateTime.MinValue && endDate == DateTime.MaxValue && dataRangeRq.RecentBars == 0) return this;

			string start = (startDate == DateTime.MinValue) ? "MIN" : startDate.ToString("dd-MMM-yyyy");
			string end = (endDate == DateTime.MaxValue) ? "MAX" : endDate.ToString("dd-MMM-yyyy");
			Bars ret = this.CloneNoBars(this.ReasonToExist + " [" + dataRangeRq.ToString() + "]", this.ScaleInterval);
			for (int i=0; i<this.Count; i++) {
				if (dataRangeRq.RecentBars > 0 && i >= dataRangeRq.RecentBars) break; 
				Bar barAdding = this[i];
				bool skipThisBar = false;
				if (startDate > DateTime.MinValue && barAdding.DateTimeOpen < startDate) skipThisBar = true; 
				if (endDate < DateTime.MaxValue && barAdding.DateTimeOpen > endDate) skipThisBar = true;
				if (skipThisBar) continue;
				ret.BarAppendBindStatic(barAdding.CloneDetached());
			}
			return ret;
		}
		
		public bool CanConvertTo(BarScaleInterval scaleIntervalTo) {
			// for proper comparison, make sure Sq1.Core.DataTypes.BarScale enum has scales growing from Tick to Yearly
			if (this.ScaleInterval.Scale > scaleIntervalTo.Scale) return false;	//can't convert from 1hr to 5min
			if (this.ScaleInterval.Scale < scaleIntervalTo.Scale) return true;
			// here we are if (this.ScaleInterval.Scale == scaleIntervalTo.Scale)
			if (this.ScaleInterval.Interval <= scaleIntervalTo.Interval) return true;
			return false;
		}
		void checkThrowCanConvert(BarScaleInterval scaleIntervalTo) {
			string msig = "checkThrowCanConvert(" + this.ScaleInterval + "=>" + scaleIntervalTo + ") for " + this + " datasource[" + this.DataSource + "]";
			string msg = "";
			bool canConvert = this.CanConvertTo(scaleIntervalTo);
			if (canConvert == false) msg += "CANNOT_CONVERT_TO_LARGER_SCALE_INTERVAL";
			if (this.Count == 0) msg += " EMPTY_BARS_FROM";
			//if (barsFrom.ScaleInterval.Scale == BarScale.Tick) msg += " TICKS_CAN_NOT_BE_CONVERTED_TO_ANYTHING";
			if (string.IsNullOrEmpty(msg)) return;
			throw new Exception(msg + msig);
		}
		public Bars ToLargerScaleInterval(BarScaleInterval scaleIntervalTo) {
			if (this.ScaleInterval == scaleIntervalTo) return this;
			this.checkThrowCanConvert(scaleIntervalTo);

			Bars barsConverted = this.CloneNoBars(this.ReasonToExist + "=>[" + scaleIntervalTo + "]", scaleIntervalTo);
			if (this.Count == 0) return barsConverted;
			
			Bar barFromFirst = this[0];
			Bar barCompressing = new Bar(this.Symbol, scaleIntervalTo, barFromFirst.DateTimeOpen);	// I'm happy with RoundDateDownInitTwoAuxDates()
			barCompressing.AbsorbOHLCVfrom(barFromFirst);

			for (int i = 1; i < this.Count; i++) {
				Bar barEach = this[i];
				if (barEach.DateTimeOpen >= barCompressing.DateTimeNextBarOpenUnconditional) {
					barsConverted.BarAppendBindStatic(barCompressing);
					barCompressing = new Bar(this.Symbol, scaleIntervalTo, barEach.DateTimeOpen);
					barCompressing.AbsorbOHLCVfrom(barEach);
				} else {
					barCompressing.MergeExpandHLCVwhileCompressingManyBarsToOne(barEach);
				}
			}
			return barsConverted;
		}
	}
}
