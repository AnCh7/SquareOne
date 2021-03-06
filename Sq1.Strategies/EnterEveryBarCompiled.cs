﻿using System;
using System.Diagnostics;
using Sq1.Core.DataTypes;
using Sq1.Core.Execution;
using Sq1.Core.Indicators;
using Sq1.Core.StrategyBase;
using Sq1.Core.Streaming;

namespace Sq1.Strategies.Demo {
	[ScriptParameterAttribute(Id=1, Name="test", ValueMin=0, ValueMax=10 )]
	[ScriptParameterAttribute(Id=2, Name="verbose", ValueMin=0, ValueMax=1, ValueCurrent=0, ValueIncrement=1, ReasonToExist="set to 0 if you don't want log() to spam your Exceptions window" )]
	public class EnterEveryBarCompiled : Script {
		
		[IndicatorParameterAttribute(Name="Period",
			ValueCurrent=55, ValueMin=11, ValueMax=88, ValueIncrement=11)]
		public IndicatorAverageMovingSimple MAslow { get; set; }

		[IndicatorParameterAttribute(Name = "Period",
			ValueCurrent = 15, ValueMin = 10, ValueMax = 20, ValueIncrement = 1)]
		public IndicatorAverageMovingSimple MAfast { get; set; }

		protected void log(string msg) {
			if (this.Parameters[2].ValueCurrent == 0.0) {
				return;
			}
			string whereIam = "\n\r\n\rEnterEveryBar.cs now=[" + DateTime.Now.ToString("ddd dd-MMM-yyyy HH:mm:ss.fff") + "]";
			//this.Executor.PopupException(msg + whereIam);
		}
		public override void InitializeBacktest() {
			//Debugger.Break();
			//this.PadBars(0);
			if (base.Strategy == null) {
				log("CANT_SET_EXCEPTIONS_LIMIT: base.Strategy == null");
				Debugger.Break();
				return;
			}
			base.Strategy.ExceptionsLimitToAbortBacktest = 10;
			//this.MAslow.NotOnChartSymbol = "SANDP-FUT";
			//this.MAslow.NotOnChartBarScaleInterval = new BarScaleInterval(BarScale.Hour, 1);
			//this.MAslow.NotOnChartBarScaleInterval = new BarScaleInterval(BarScale.Minute, 15);
			//this.MAslow.LineWidth = 2;
			this.MAslow.LineColor = System.Drawing.Color.LightCoral;
			this.MAfast.LineColor = System.Drawing.Color.LightSeaGreen;
		}
		public override void OnNewQuoteOfStreamingBarCallback(Quote quote) {
			//double slowStreaming = this.MAslow.BarClosesProxied.StreamingValue;
			double slowStatic = this.MAslow.ClosesProxyEffective.LastStaticValue;
			DateTime slowStaticDate = this.MAslow.ClosesProxyEffective.LastStaticDate;

			if (this.Executor.Backtester.IsBacktestingNow == false) {
				Bar bar = quote.ParentStreamingBar;
				int barNo = bar.ParentBarsIndex;
				if (barNo == -1) return;
				DateTime lastStaticBarDateTime = bar.ParentBars.BarStaticLast.DateTimeOpen;
				DateTime streamingBarDateTime = bar.DateTimeOpen;
				Bar barNormalizedDateTimes = new Bar(bar.Symbol, bar.ScaleInterval, quote.ServerTime);
				DateTime thisBarDateTimeOpen = barNormalizedDateTimes.DateTimeOpen;
				int a = 1;
			}
			//log("OnNewQuoteCallback(): [" + quote.ToString() + "]"); 
			string msg = "OnNewQuoteCallback(): [" + quote.ToString() + "]";
			log("EnterEveryBar.cs now=[" + DateTime.Now.ToString("ddd dd-MMM-yyyy HH:mm:ss.fff" + "]: " + msg));

			if (quote.IntraBarSerno == 0) {
				return;
			}
		}
		public override void OnBarStaticLastFormedWhileStreamingBarWithOneQuoteAlreadyAppendedCallback(Bar barStaticFormed) {
			Bar barStreaming = base.Bars.BarStreaming;
			if (this.Executor.Backtester.IsBacktestingNow == false) {
				Debugger.Break();
			}
			if (barStaticFormed.ParentBarsIndex <= 2) return;
			if (barStaticFormed.IsBarStreaming) {
				string msg = "SHOULD_NEVER_HAPPEN triggered@barStaticFormed.IsBarStreaming[" + barStaticFormed + "] while Streaming[" + barStreaming + "]";
				Debugger.Break();
			}

			Position lastPos = base.LastPosition;
			bool isLastPositionNotClosedYet = base.IsLastPositionNotClosedYet;
			if (isLastPositionNotClosedYet) {
				if (lastPos.EntryFilledBarIndex > barStaticFormed.ParentBarsIndex) {
					string msg1 = "prev bar you placed on streaming, now that streaming is static and positionEntry is still in the future but you have it filled?...";
					Debugger.Break();
				}

				if (lastPos.ExitAlert != null) {
					string msg1 = "you want to avoid POSITION_ALREADY_HAS_AN_EXIT_ALERT_REPLACE_INSTEAD_OF_ADDING_SECOND"
						+ " ExitAtMarket by throwing [can't have two closing alerts for one positionExit] Strategy[" + this.Strategy.ToString() + "]";
					Debugger.Break();
					return;
				}

				if (barStaticFormed.ParentBarsIndex == 163) {
					Debugger.Break();
					StreamingDataSnapshot streaming = this.Executor.DataSource.StreamingProvider.StreamingDataSnapshot;
					Quote lastQuote = streaming.LastQuoteGetForSymbol(barStaticFormed.Symbol);
					double priceForMarketOrder = streaming.LastQuoteGetPriceForMarketOrder(barStaticFormed.Symbol);
				}

				string msg = "ExitAtMarket@" + barStaticFormed.ParentBarsIdent;
				Alert exitPlaced = ExitAtMarket(barStreaming, lastPos, msg);
				log("Execute(): " + msg);
			}

			ExecutionDataSnapshot snap = base.Executor.ExecutionDataSnapshot;
			int alertsPendingCount = snap.AlertsPending.Count;
			int positionsOpenNowCount = snap.PositionsOpenNow.Count;

			bool hasAlertsPendingOrPositionsOpenNow = base.HasAlertsPendingOrPositionsOpenNow;
			if (hasAlertsPendingOrPositionsOpenNow) {
				if (alertsPendingCount > 0) {
					if (snap.AlertsPending[0] == lastPos.EntryAlert) {
						string msg = "EXPECTED: I don't have open positions but I have an unfilled alert from lastPosition.EntryAlert=alertsPending[0]";
					} else if (snap.AlertsPending[0] == lastPos.ExitAlert) {
						string msg = "EXPECTED: I have and open lastPosition with .ExitAlert=alertsPending[0]";
					} else {
						string msg = "UNEXPECTED: pending alert doesn't relate to lastPosition; who is here?";
					}
				}
				if (positionsOpenNowCount > 1) {
					string msg = "EXPECTED: I got multiple positions[" + positionsOpenNowCount + "]";
					if (snap.PositionsOpenNow[0] == lastPos) {
						msg += "50/50: positionsMaster.Last = positionsOpenNow.First";
					}
				}
				return;
			}

			if (barStaticFormed.ParentBarsIndex == 30) {
				Debugger.Break();
				StreamingDataSnapshot streaming = this.Executor.DataSource.StreamingProvider.StreamingDataSnapshot;
				Quote lastQuote = streaming.LastQuoteGetForSymbol(barStaticFormed.Symbol);
				double priceForMarketOrder = streaming.LastQuoteGetPriceForMarketOrder(barStaticFormed.Symbol);
			}


			if (barStaticFormed.Close > barStaticFormed.Open) {
				string msg = "BuyAtMarket@" + barStaticFormed.ParentBarsIdent;
				Position buyPlaced = BuyAtMarket(barStreaming, msg);
				//Debugger.Break();
			} else {
				string msg = "ShortAtMarket@" + barStaticFormed.ParentBarsIdent;
				Position shortPlaced = ShortAtMarket(barStreaming, msg);
				//Debugger.Break();
			}
			//base.Executor.ChartShadow.LineDrawModify(...);
		}
		public override void OnAlertFilledCallback(Alert alertFilled) {
			if (alertFilled.FilledBarIndex == 12) {
				//Debugger.Break();
			}
		}
		public override void OnAlertKilledCallback(Alert alertKilled) {
			Debugger.Break();
		}
		public override void OnAlertNotSubmittedCallback(Alert alertNotSubmitted, int barNotSubmittedRelno) {
			Debugger.Break();
		}
		public override void OnPositionOpenedCallback(Position positionOpened) {
			if (positionOpened.EntryFilledBarIndex == 37) {
				Debugger.Break();
			}
		}
		public override void OnPositionOpenedPrototypeSlTpPlacedCallback(Position positionOpenedByPrototype) {
			Debugger.Break();
		}
		public override void OnPositionClosedCallback(Position positionClosed) {
			if (positionClosed.EntryFilledBarIndex == 37) {
				Debugger.Break();
			}
		}
	}
}