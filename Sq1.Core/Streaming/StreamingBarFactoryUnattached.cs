using System;
//using log4net;
using Sq1.Core.DataTypes;
using System.Diagnostics;

namespace Sq1.Core.Streaming {
	public class StreamingBarFactoryUnattached {
		//private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public string Symbol { get; private set; }
		public BarScaleInterval ScaleInterval { get; set; }
		public int IntraBarSerno { get; private set; }
		public Bar StreamingBarUnattached { get; private set; }
		public Bar LastBarFormedUnattached { get; protected set; }

		public StreamingBarFactoryUnattached(string symbol, BarScaleInterval scaleInterval) {
			Symbol = symbol;
			ScaleInterval = scaleInterval;
			LastBarFormedUnattached = null;
			StreamingBarUnattached = new Bar(this.Symbol, this.ScaleInterval, DateTime.MinValue);
			IntraBarSerno = 0;
		}
		public virtual Quote EnrichQuoteWithSernoUpdateStreamingBarCreateNewBar(Quote quoteClone) {
			if (quoteClone.PriceLastDeal == 0) {
				string msg = "quote.PriceLastDeal[" + quoteClone.PriceLastDeal + "] == 0;"
					+ "what kind of quote is that?... (" + quoteClone + ")";
				throw new Exception(msg);
				//return;
			}

			if (this.StreamingBarUnattached.Symbol != quoteClone.Symbol) {
				string msg = "StreamingBar.Symbol=[" + this.StreamingBarUnattached.Symbol + "]!=quote.Symbol["
					+ quoteClone.Symbol + "] (" + quoteClone + ")";
				throw new Exception(msg);
				//return;
			}

			// included in if (quoteClone.ServerTime >= StreamingBar.DateTimeNextBarOpenUnconditional) !!!
			// on very first quote StreamingBar.DateTimeNextBarOpenUnconditional = DateTime.MinValue
			//SEE_BELOW if (StreamingBar.DateTimeOpen == DateTime.MinValue)
			//SEE_BELOW 	this.initStreamingBarResetIntraBarSerno(quoteClone.ServerTime, quoteClone.PriceLastDeal, quoteClone.Size);
			//SEE_BELOW }

			if (quoteClone.ServerTime >= this.StreamingBarUnattached.DateTimeNextBarOpenUnconditional) {
				this.LastBarFormedUnattached = this.StreamingBarUnattached.Clone();	//beware! on very first quote LastBarFormed.DateTimeOpen == DateTime.MinValue
				this.initStreamingBarResetIntraBarSerno(quoteClone.ServerTime, quoteClone.PriceLastDeal, quoteClone.Size);

				// quoteClone.IntraBarSerno doesn't feel new Bar; can contain 100004 for generatedQuotes;
				// I only want to reset to 0 when it's attributed to a new Bar; it's unlikely to face a new bar here for generatedQuotes;
				if (this.IntraBarSerno != 0) {
					string msg = "STREAMING_JUST_INITED_TO_ZERO WHY_NOW_IT_IS_NOT?";
					Debugger.Break();
				}
				if (quoteClone.IntraBarSerno != 0) {
					if (quoteClone.IntraBarSerno >= Quote.IntraBarSernoShiftForGeneratedTowardsPendingFill) {
						string msg = "GENERATED_QUOTES_ARENT_SUPPOSED_TO_GO_TO_NEXT_BAR";
						Debugger.Break();
					}
					quoteClone.IntraBarSerno = this.IntraBarSerno;
				}
				if (this.IntraBarSerno >= Quote.IntraBarSernoShiftForGeneratedTowardsPendingFill) {
					string msg = "BAR_FACTORY_INTRABAR_SERNO_NEVER_GOES_TO_SYNTHETIC_ZONE";
					Debugger.Break();
				}
			} else {
				if (Double.IsNaN(this.StreamingBarUnattached.Open) || this.StreamingBarUnattached.Open == 0.0) {
					throw new Exception("nonsense! we should've had StreamingBar already initialized with first quote of a bar");
					//log.Warn("Initializing OHL as quote.PriceLastDeal[" + quoteClone.PriceLastDeal + "];"
					//	+ " following previous InitWithStreamingBarInsteadOfEmpty message"
					//	+ " (if absent then never initialized)");
					//StreamingBar.Open = quoteClone.PriceLastDeal;
					//StreamingBar.High = quoteClone.PriceLastDeal;
					//StreamingBar.Low = quoteClone.PriceLastDeal;
				}
				this.StreamingBarUnattached.MergeExpandHLCVforStreamingBarUnattached(quoteClone);
				this.IntraBarSerno++;
			}
			if (quoteClone.ParentStreamingBar != null) {
				string msg = "QUOTE_ALREADY_ENRICHED_WITH_PARENT_STREAMING_BAR; I think it's a pre- bindStreamingBarForQueue() atavism";
				//Assembler.PopupException(msg);
			} else {
				quoteClone.SetParentBar(this.StreamingBarUnattached);
			}
			return quoteClone;
		}
		protected void initStreamingBarResetIntraBarSerno(DateTime quoteServerTime, double OHLC, double quoteSize) {
			this.StreamingBarUnattached = new Bar(this.Symbol, this.ScaleInterval, quoteServerTime);
			this.StreamingBarUnattached.Open = OHLC;
			this.StreamingBarUnattached.High = OHLC;
			this.StreamingBarUnattached.Low = OHLC;
			this.StreamingBarUnattached.Close = OHLC;
			this.StreamingBarUnattached.Volume = quoteSize;
			this.IntraBarSerno = 0;
		}
		public void InitWithStreamingBarInsteadOfEmpty(Bar StreamingBarInsteadOfEmpty) {
			string msg = "";
			if (StreamingBarInsteadOfEmpty.DateTimeOpen <= this.StreamingBarUnattached.DateTimeOpen) {
				msg += "StreamingBarInsteadOfEmpty.DateTimeOpen[" + StreamingBarInsteadOfEmpty.DateTimeOpen
					+ "] <= CurrentStreamingBar.Open[" + StreamingBarUnattached.Open + "]";
				//log.Warn(msg + " // " + this);
				return;
			}
			if (StreamingBarInsteadOfEmpty.DateTimeOpen == DateTime.MinValue) {
				msg += "StreamingBarInsteadOfEmpty.DateTimeOpen[" + StreamingBarInsteadOfEmpty.DateTimeOpen + "] == DateTime.MinValue ";
			}
			if (double.IsNaN(StreamingBarInsteadOfEmpty.Open)) {
				msg += "double.IsNaN(StreamingBarInsteadOfEmpty.Open[" + StreamingBarInsteadOfEmpty.Open + "]) ";
			}
			if (StreamingBarInsteadOfEmpty.Open == 0) {
				msg += "StreamingBarInsteadOfEmpty.Open[" + StreamingBarInsteadOfEmpty.Open + "] == 0 ";
			}
			this.StreamingBarUnattached = StreamingBarInsteadOfEmpty.Clone();
			if (string.IsNullOrEmpty(msg) == false) {
				//log.Warn("InitWithStreamingBarInsteadOfEmpty: " + msg + " // " + this);
			}
		}
		public override string ToString() {
			return Symbol + "_" + ScaleInterval + ":StreamingBar[" + StreamingBarUnattached + "]";
		}
	}
}