using System;
using Newtonsoft.Json;
//PERST_TOO_BULKY_TO_IMPLEMENT_FILES_TOO_BIG_FOR_NON_TICK using Perst;

namespace Sq1.Core.DataTypes {
	public class Bar {	//PERST_TOO_BULKY_TO_IMPLEMENT_FILES_TOO_BIG_FOR_NON_TICK : TimeSeriesTick
		public string Symbol { get; protected set; }
		//PERST_TOO_BULKY_TO_IMPLEMENT_FILES_TOO_BIG_FOR_NON_TICK [Transient]
		public BarScaleInterval ScaleInterval { get; protected set; }
		//PERST_TOO_BULKY_TO_IMPLEMENT_FILES_TOO_BIG_FOR_NON_TICK [Transient]
		public DateTime DateTimeOpen { get; protected set; }
		//[JsonIgnore]
		//public long Time { get { return this.DateTimeOpen.ToBinary(); } }
		//PERST_TOO_BULKY_TO_IMPLEMENT_FILES_TOO_BIG_FOR_NON_TICK [Transient]
		public DateTime DateTimeNextBarOpenUnconditional { get; protected set; }
		//[PERST_TOO_BULKY_TO_IMPLEMENT_FILES_TOO_BIG_FOR_NON_TICK Transient]
		public DateTime DateTimePreviousBarOpenUnconditional { get; protected set; }
		
		public double Open;
		public double High;
		public double Low;
		public double Close;
		public double Volume;

		[JsonIgnore]
		public Bars ParentBars { get; protected set; }
		[JsonIgnore]
		public int ParentBarsIndex { get; protected set; }
		[JsonIgnore]
		public bool HasParentBars { get { return this.ParentBars != null; } }
		public string ParentBarsIdent {
			get {
				if (this.HasParentBars == false) return "NO_PARENT_BARS";
				string ret = "StaticBar";
				//if (this.ParentBarsIndex <  this.ParentBars.Count - 1) ret = this.ParentBarsIndex.ToString();
				//if (this.ParentBarsIndex == this.ParentBars.Count - 1) ret = "LastStaticBar";// +this.ParentBarsIndex;
				//if (this.ParentBarsIndex == this.ParentBars.Count) ret = "StreamingBar";// +this.ParentBarsIndex;
				//if (this.ParentBarsIndex >  this.ParentBars.Count) ret = "ScaryGhostBar:" + this.ParentBarsIndex;
				if (this.IsBarStreaming) ret = "BarStreaming";// +this.ParentBarsIndex;
				if (this.IsBarStaticLast) ret = "StaticBarLast";// +this.ParentBarsIndex;
				if (this.IsBarStaticFirst) ret = "StaticBarFist";// +this.ParentBarsIndex;
				ret += "#" + this.ParentBarsIndex + "/" + (this.ParentBars.Count-1);
				return ret;
			}
		}
		[JsonIgnore]
		public bool IsBarStreaming {
			get {
				if (this.HasParentBars == false) {
					throw new Exception("PROPERTY_VALID_ONLY_WHEN_THIS_BAR_IS_ADDED_INTO_BARS: IsStreamingBar: Bar[" + this + "].HasParentBars=false");
				}
				return this == this.ParentBars.BarStreaming;
			}
		}
		[JsonIgnore]
		public bool IsBarStaticLast {
			get {
				if (this.HasParentBars == false) {
					throw new Exception("PROPERTY_VALID_ONLY_WHEN_THIS_BAR_IS_ADDED_INTO_BARS: IsLastStaticBar: Bar[" + this + "].HasParentBars=false");
				}
				return this == this.ParentBars.BarStaticLast;
			}
		}
		[JsonIgnore]
		public bool IsBarStaticFirst {
			get {
				if (this.HasParentBars == false) {
					throw new Exception("PROPERTY_VALID_ONLY_WHEN_THIS_BAR_IS_ADDED_INTO_BARS: IsFirstStaticBar: Bar[" + this + "].HasParentBars=false");
				}
				return this == this.ParentBars.BarStaticFirst;
			}
		}
		// Perst deserializer invokes default ctor()
		public Bar() {
			// ChartRenderer would update its max/min if NaN
			this.ParentBarsIndex = -1;
			this.Open = Double.NaN;
			this.High = Double.NaN;
			this.Low = Double.NaN;
			this.Close = Double.NaN;
			this.Volume = Double.NaN;
		}
		public Bar(string symbol, BarScaleInterval scaleInterval, DateTime dateTimeOpen) : this() {
			this.Symbol = symbol;
			this.ScaleInterval = scaleInterval;
			if (dateTimeOpen == DateTime.MinValue) {
				this.DateTimeOpen = DateTime.MinValue;
				this.DateTimeNextBarOpenUnconditional = DateTime.MinValue;
				this.DateTimePreviousBarOpenUnconditional = DateTime.MinValue;
			} else {
				this.RoundDateDownInitTwoAuxDates(dateTimeOpen);
			}
		}
		public void SetOHLCV(double open, double high, double low, double close, double volume) {
			this.Open = open;
			this.High = high;
			this.Low = low;
			this.Close = close;
			this.Volume = volume;
		}
		public void AbsorbOHLCVfrom(Bar bar) {
			this.Open = bar.Open;
			this.High = bar.High;
			this.Low = bar.Low;
			this.Close = bar.Close;
			this.Volume = bar.Volume;
		}
		public void SetParentForBackwardUpdate(Bars parentBars, int parentBarsIndex) {
			if (this.ParentBars == parentBars) {
				string msg = "TYRING_AVOID_BUGS: same ParentBars as I have already;"
					+ "  this.ParentBars==parentBars[" + parentBars + "]";
				throw new Exception(msg);
			}
			if (this.ParentBarsIndex == parentBarsIndex) {
				string msg = "TYRING_AVOID_BUGS: same ParentBarsIndex[" + this.ParentBarsIndex + "] as I have already;"
					+ "  this.ParentBars==parentBars[" + parentBars + "]";
				throw new Exception(msg);
			}
			if (this.ParentBars != null) {
				if (this.ParentBars.Symbol != parentBars.Symbol) {
					string msg1 = "here is the problem for a streaming bar to carry another symbol!";
					throw new Exception(msg1);
				}
				string msg = "this.ParentBars!=null => this Bar is already assigned to Bars;"
					+ " use Bar.CloneDetached() if you add this Bar to another BarSeries"
					+ " otherwise reciprocity will be uneven"
					+ " and strategies relying on quote.ParentBar.ParentBarsIndex will be messed up";
				throw new Exception(msg);
			}
			if (this.ParentBarsIndex != -1) {
				string msg = "this.ParentBarsIndex!=-1 => this Bar is already assigned to Bars;"
					+ " use Bar.CloneDetached() if you add this Bar to another BarSeries"
					+ " otherwise reciprocity will be uneven"
					+ " and strategies relying on quote.ParentBar.ParentBarsIndex will be messed up";
				throw new Exception(msg);
			}
			this.ParentBars = parentBars;
			this.ParentBarsIndex = parentBarsIndex;
		}
		public void RoundDateDownInitTwoAuxDates(DateTime dateTimeOpen) {
			this.DateTimeOpen = roundDateDownToMyInterval(dateTimeOpen);
			///if (this.DateTimeOpen.CompareTo(dateTimeOpen) == 0) {
			//	int a = 1;
			//}
			this.DateTimeNextBarOpenUnconditional = this.addIntervalsToDate(this.DateTimeOpen, 1);
			this.DateTimePreviousBarOpenUnconditional = this.addIntervalsToDate(this.DateTimeOpen, -1);
		}
		public Bar Clone() {
			return (Bar)this.MemberwiseClone();
		}
		public Bar CloneDetached() {
			Bar detached = this.Clone();
			detached.ParentBars = null;
			detached.ParentBarsIndex = -1;
			return detached;
		}
		public override bool Equals(object other) {
			if (other is Bar == false) {
				return base.Equals(other);
			}
			Bar bar = (Bar)other;
			string barAsString = bar.ToString();
			string thisAsString = this.ToString();
			return (barAsString == thisAsString);
		}
		public void CheckOHLCVthrow() {
			string msg = "";
			if (this.Open <= 0) msg = "Open[" + this.Open + "] <= 0";
			if (this.High <= 0) msg = "High[" + this.High + "] <= 0";
			if (this.Low <= 0) msg = "Low[" + this.Low + "] <= 0";
			if (this.Close <= 0) msg = "Close[" + this.Close + "] <= 0";
			//if (this.Volume <= 0) msg = "Volume[" + this.Volume + "] <= 0";
			if (string.IsNullOrEmpty(msg)) return;
			throw new Exception(msg);
		}
		public bool HasSameDOHLCVas(Bar bar, string barIdent, string thisIdent, out string msg) {
			if (this.Symbol != bar.Symbol) {
				msg = thisIdent + ".Symbol[" + this.Symbol + "] != " + barIdent + ".Symbol[" + bar.Symbol + "]";
				return false;
			}

			if (this.ScaleInterval != bar.ScaleInterval) {
				msg = thisIdent + ".ScaleInterval[" + this.ScaleInterval + "] != "
					+ barIdent + ".ScaleInterval[" + bar.ScaleInterval + "]";
				return false;
			}

			if (this.DateTimeOpen != bar.DateTimeOpen) {
				msg = thisIdent + ".DateTimeOpen[" + this.DateTimeOpen + "] != " 
					+ barIdent + ".DateTimeOpen[" + bar.DateTimeOpen + "]";
				return false;
			}

			bool sameOHLCV = (this.Open == bar.Open && this.High == bar.High
				&& this.Low == bar.Low && this.Close == bar.Close && this.Volume == bar.Volume);
			if (sameOHLCV == false) {
				msg = "OHLCV are different while DateTimeOpen is the same: "
					+ thisIdent + "[" + this + "] != " + barIdent + "[" + bar + "]";
				return false;
			}

			bool sameParent = (this.ParentBars == bar.ParentBars && this.ParentBarsIndex == bar.ParentBarsIndex);
			if (sameParent == false) {
				msg = "CAN_SKIP_PARENT_DIFFERENT:"
					+ " " + thisIdent + ".ParentBars[" + this.ParentBarsIdent + "]"
					+ " != " + bar + ".ParentBarsIndex[" + bar.ParentBarsIdent + "]"
					+ " while lastStaticBar.DOHLCV=barAdding.DOHLCV";
				return true;
			}

			msg = "lastStaticBar.DOHLCV=barAdding.DOHLCV";
			return true;
		}
		private DateTime addIntervalsToDate(DateTime dateTime1, int intervalMultiplier) {
			if (this.DateTimeOpen == DateTime.MinValue) return DateTime.MinValue;
			DateTime dateTime = roundDateDownToMyInterval(dateTime1);
			int addTimeIntervals = this.ScaleInterval.Interval * intervalMultiplier;
			switch (this.ScaleInterval.Scale) {
				case BarScale.Tick:
					throw new ArgumentException("Tick scale is not supported");
				case BarScale.Second:
					dateTime = dateTime.AddSeconds((double)addTimeIntervals);
					break;
				case BarScale.Minute:
					dateTime = dateTime.AddMinutes((double)addTimeIntervals);
					break;
				case BarScale.Hour:
					dateTime = dateTime.AddHours((double)addTimeIntervals);
					break;
				case BarScale.Daily:
					dateTime = dateTime.Date.AddDays((double)addTimeIntervals);
					break;
				case BarScale.Weekly:
					dateTime = dateTime.Date.AddDays(addTimeIntervals * 7);
					break;
				case BarScale.Monthly:
					dateTime = dateTime.Date.AddMonths(addTimeIntervals);
					break;
				case BarScale.Quarterly:
					dateTime = dateTime.Date.AddMonths(addTimeIntervals * 3);
					break;
				case BarScale.Yearly:
					dateTime = dateTime.Date.AddYears(addTimeIntervals);
					break;
				default:
					throw new Exception("this.ScaleInterval.Scale[" + this.ScaleInterval.Scale
						+ "] is not supported");
			}
			return dateTime;
		}
		private DateTime roundDateDownToMyInterval(DateTime dateTime1) {
			if (this.ScaleInterval == null) throw new Exception("ScaleInterval=null in roundDateDownToInterval(" + dateTime1 + ")");
			DateTime dateTime = new DateTime(dateTime1.Ticks);
			switch (this.ScaleInterval.Scale) {
				case BarScale.Tick:
					throw new ArgumentException("Tick scale is not supported");
				case BarScale.Second:
					int secondsRoundedDown = ((int)Math.Floor((double)dateTime.Second / this.ScaleInterval.Interval)) * this.ScaleInterval.Interval;
					dateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, secondsRoundedDown);
					break;
				case BarScale.Minute:
					int minutesRoundedDown = ((int)Math.Floor((double)dateTime.Minute / this.ScaleInterval.Interval)) * this.ScaleInterval.Interval;
					dateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, minutesRoundedDown, 0);
					break;
				case BarScale.Hour:
					int hoursRoundedDown = ((int)Math.Floor((double)dateTime.Hour / this.ScaleInterval.Interval)) * this.ScaleInterval.Interval;
					dateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, hoursRoundedDown, 0, 0);
					break;
				case BarScale.Daily:
					dateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0);
					break;
				case BarScale.Weekly:
					while (dateTime.DayOfWeek != DayOfWeek.Monday) dateTime.AddDays(-1);
					dateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0);
					break;
				case BarScale.Monthly:
					dateTime = new DateTime(dateTime.Year, dateTime.Month, 1, 0, 0, 0);
					break;
				case BarScale.Quarterly:
					int monthBeginningOfQuarter = ((int)Math.Floor((double)dateTime.Month / 3)) * 3;
					dateTime = new DateTime(dateTime.Year, monthBeginningOfQuarter, 1, 0, 0, 0);
					break;
				case BarScale.Yearly:
					dateTime = new DateTime(dateTime.Year, 1, 1, 0, 0, 0);
					break;
				default:
					throw new Exception("this.ScaleInterval.Scale[" + this.ScaleInterval.Scale + "] is not supported");
			}
			return dateTime;
		}
		public void MergeExpandHLCVwhileCompressingManyBarsToOne(Bar bar) {
			if (bar.High > this.High) this.High = bar.High;
			if (bar.Low < this.Low) this.Low = bar.Low;
			this.Close = bar.Close;
			this.Volume += bar.Volume;
		}
		public void MergeExpandHLCVforStreamingBarUnattached(Quote quoteClone) {
			if (quoteClone.PriceLastDeal > this.High) this.High = quoteClone.PriceLastDeal;
			if (quoteClone.PriceLastDeal < this.Low) this.Low = quoteClone.PriceLastDeal;
			this.Close = quoteClone.PriceLastDeal;
			this.Volume += quoteClone.Size;
		}
		public override string ToString() {
			return this.ParentBarsIdent + ":"
				+ Symbol + "(" + ScaleInterval + ") "
				+ "T[" + DateTimeOpen + "]"
				+ "O[" + Math.Round(Open, 3) + "]"
				+ "H[" + Math.Round(High, 3) + "]"
				+ "L[" + Math.Round(Low, 3) + "]"
				+ "C[" + Math.Round(Close, 3) + "]"
				+ "V[" + Math.Round(Volume, 3) + "]"
				;
		}
		
		public bool ContainsPrice(double entryFillPrice) {
			if (entryFillPrice < this.Low) return false; 
			if (entryFillPrice > this.High) return false;
			return true;			
		}
	}
}