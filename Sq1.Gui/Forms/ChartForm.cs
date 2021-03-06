﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Sq1.Core;
using Sq1.Core.DataTypes;
using Sq1.Core.Execution;
using Sq1.Core.StrategyBase;
using Sq1.Core.Streaming;
using Sq1.Gui.Singletons;
using Sq1.Widgets;
using Sq1.Widgets.LabeledTextBox;
using WeifenLuo.WinFormsUI.Docking;

namespace Sq1.Gui.Forms {
	public partial class ChartForm : DockContentImproved {
		public ChartFormManager ChartFormManager;

		List<string> GroupScaleLabeledTextboxes;
		List<string> GroupPositionSizeLabeledTextboxes;

		// SharpDevelop/VisualStudio Designer's constructor
		public ChartForm() {
			InitializeComponent();
			this.ChartControl.RangeBarCollapsed = !this.mniShowBarRange.Checked;
			
			// in case if Designer removes these from ChartForm.Designer.cs 
//			this.mnitlbYearly.UserTyped += new EventHandler<LabeledTextBoxUserTypedArgs>(this.mniltbAll_UserTyped);
//			this.mnitlbMonthly.UserTyped += new EventHandler<LabeledTextBoxUserTypedArgs>(this.mniltbAll_UserTyped);
//			this.mnitlbWeekly.UserTyped += new EventHandler<LabeledTextBoxUserTypedArgs>(this.mniltbAll_UserTyped);
//			this.mnitlbDaily.UserTyped += new EventHandler<LabeledTextBoxUserTypedArgs>(this.mniltbAll_UserTyped);
//			this.mnitlbHourly.UserTyped += new EventHandler<LabeledTextBoxUserTypedArgs>(this.mniltbAll_UserTyped);
//			this.mnitlbMinutes.UserTyped += new EventHandler<LabeledTextBoxUserTypedArgs>(this.mniltbAll_UserTyped);
//			this.mnitlbShowLastBars.UserTyped += new EventHandler<LabeledTextBoxUserTypedArgs>(this.mnitlbShowLastBars_UserTyped);

			this.GroupScaleLabeledTextboxes = new List<string>() {
				this.mnitlbMinutes.Name, this.mnitlbHourly.Name, this.mnitlbDaily.Name, this.mnitlbWeekly.Name, this.mnitlbMonthly.Name,
				//this.mnitlbQuarterly.Name,
				this.mnitlbYearly.Name};
			this.GroupPositionSizeLabeledTextboxes = new List<string>() {
				this.mnitlbPositionSizeDollarsEachTradeConstant.Name, this.mnitlbPositionSizeSharesConstantEachTrade.Name};
		}
		//programmer's constructor
		public ChartForm(ChartFormManager chartFormManager) : this() {
			this.ChartFormManager = chartFormManager;
			// right now this.ChartFormsManager.Executor IS NULL, will create and Chart.Initialize() upstack :((
			//this.Chart.Initialize(this.ChartFormsManager.Executor);
			// TOO_EARLY_NO_BARS_SET_WILL_BE_THROWN this.PopulateBtnStreamingText();
		}
		public void AttachEventsToChartFormsManager() {
			this.ChartControl.RangeBar.ValueMinChanged += this.ChartFormManager.EventManager.ChartRangeBar_AnyValueChanged;
			this.ChartControl.RangeBar.ValueMaxChanged += this.ChartFormManager.EventManager.ChartRangeBar_AnyValueChanged;
			this.ChartControl.RangeBar.ValuesMinAndMaxChanged += this.ChartFormManager.EventManager.ChartRangeBar_AnyValueChanged;
			
			this.OnStreamingButtonStateChanged += new EventHandler<EventArgs>(this.ChartFormManager.EventManager.ChartForm_StreamingButtonStateChanged);
			this.ChartControl.ChartSettingsChangedContainerShouldSerialize += new EventHandler<EventArgs>(ChartControl_ChartSettingsChangedContainerShouldSerialize);
			this.ChartControl.ContextScriptChangedContainerShouldSerialize += new EventHandler<EventArgs>(ChartControl_ContextScriptChangedContainerShouldSerialize);
		}

		void ChartControl_ContextScriptChangedContainerShouldSerialize(object sender, EventArgs e) {
			if (this.ChartFormManager.Strategy == null) {
				string msg = "I_INVOKED_YOU_FROM_REPORTER_NOT_POSSIBLE_STRATEGY_DISAPPEARED_NOW";
				Debugger.Break();
			}
			Assembler.InstanceInitialized.RepositoryDllJsonStrategy.StrategySave(this.ChartFormManager.Strategy);
		}
		void ChartControl_ChartSettingsChangedContainerShouldSerialize(object sender, EventArgs e) {
			this.ChartFormManager.DataSnapshotSerializer.Serialize();
		}
		// http://www.codeproject.com/Articles/525541/Decoupling-Content-From-Container-in-Weifen-Luos
		// using ":" since "=" leads to an exception in DockPanelPersistor.cs
		protected override string GetPersistString() {
			string ret = "Chart:" + this.GetType().FullName + ",ChartSerno:" + this.ChartFormManager.DataSnapshot.ChartSerno;
			if (this.ChartFormManager.Strategy != null) {
				ret += ",StrategyGuid:" + this.ChartFormManager.Strategy.Guid;
				if (this.ChartFormManager.Strategy.ScriptContextCurrent != null) {
					ret += ",StrategyScriptContextName:" + this.ChartFormManager.Strategy.ScriptContextCurrent.Name;
				}
			}
			return ret;
		}
		public void PrintQuoteTimestampsOnStreamingButtonBeforeExecution(Quote quote) {
			if (quote == null) return;
			if (InvokeRequired) {
				base.BeginInvoke((MethodInvoker)delegate { this.PrintQuoteTimestampsOnStreamingButtonBeforeExecution(quote); });
				return;
			}
			StringBuilder sb = new StringBuilder(
				"StreamingOn #" + quote.IntraBarSerno.ToString("000") + " "
				+ quote.ServerTime.ToString("HH:mm:ss.fff"));
			bool quoteTimesDifferMoreThanOneMicroSecond = quote.ServerTime.ToString("HH:mm:ss.f") != quote.LocalTimeCreatedMillis.ToString("HH:mm:ss.f");
			if (quoteTimesDifferMoreThanOneMicroSecond) {
				sb.Append(" :: " + quote.LocalTimeCreatedMillis.ToString("HH:mm:ss.fff"));
			}
			if (quote.HasParentBar) {
				TimeSpan timeLeft = (quote.ParentStreamingBar.DateTimeNextBarOpenUnconditional > quote.ServerTime)
					? quote.ParentStreamingBar.DateTimeNextBarOpenUnconditional.Subtract(quote.ServerTime)
					: quote.ServerTime.Subtract(quote.ParentStreamingBar.DateTimeNextBarOpenUnconditional);
				string format = ":ss";
				if (timeLeft.Minutes > 0) format = "mm:ss";
				if (timeLeft.Hours > 0) format = "HH:mm:ss";
				sb.Append(" " + new DateTime(timeLeft.Ticks).ToString(format));
			}
			this.btnStreaming.Text = sb.ToString();
		}
		public void PopulateBtnStreamingClickedAndText() {
			bool streamingNow = this.ChartFormManager.Executor.IsStreaming;
			if (streamingNow) {
				this.mniBacktestOnSelectorsChange.Enabled = false;
				this.mniBacktestNow.Enabled = false;
				this.btnAutoSubmit.Enabled = true;
			} else {
				this.mniBacktestOnSelectorsChange.Enabled = true;
				this.mniBacktestNow.Enabled = true;
				this.btnAutoSubmit.Enabled = false;
			}
			if (this.ChartFormManager.Executor.DataSource.StreamingProvider == null) {
				this.btnStreaming.Text = "DataSource: [" + StreamingProvider.NO_STREAMING_PROVIDER + "]";
				this.btnStreaming.Enabled = false;
			} else {
				this.btnStreaming.Text = "Streaming" + ((this.ChartFormManager.Executor.IsStreaming) ? "On" : "Off")
					+ " 00:00:00.000"; //+:: 00:00:00.000";
				this.btnStreaming.Enabled = true;
			}
		}
		public void PropagateContextChartOrScriptToLTB(ContextChart ctxChart, Bitmap streamingProviderIcon = null) {
			if (base.InvokeRequired) {
				base.BeginInvoke((MethodInvoker)delegate { this.PropagateContextChartOrScriptToLTB(ctxChart, streamingProviderIcon); });
				return;
			}
			
			if (streamingProviderIcon != null) {
				this.btnStreaming.Image = streamingProviderIcon;
			}
			// from btnStreaming_Click(); not related but visualises the last clicked state
			if (this.btnStreaming.Checked) {
				this.mniBacktestOnSelectorsChange.Enabled = false;
				this.mniBacktestNow.Enabled = false;
				this.btnAutoSubmit.Enabled = true;
			} else {
				this.mniBacktestOnSelectorsChange.Enabled = true;
				this.mniBacktestNow.Enabled = true;
				this.btnAutoSubmit.Enabled = false;
			}
			
			if (ctxChart.ShowRangeBar) {
				this.ChartControl.RangeBarCollapsed = false; 
				this.mniShowBarRange.Checked = true;
			} else {
				this.ChartControl.RangeBarCollapsed = true; 
				this.mniShowBarRange.Checked = false;
			}

			this.btnStreaming.Checked = ctxChart.ChartStreaming;
			ContextScript ctxScript = ctxChart as ContextScript;
			if (ctxScript == null) return;
			
			this.mniBacktestOnRestart.Checked = ctxScript.BacktestOnRestart;
			this.mniBacktestOnSelectorsChange.Checked = ctxScript.BacktestOnSelectorsChange;
			this.btnAutoSubmit.Checked = ctxScript.ChartAutoSubmitting;
			this.PropagateContextScriptToLTB(ctxScript);
		}
		
		public void PropagateContextScriptToLTB(ContextScript ctxScript) {
			if (ctxScript.ScaleInterval.Scale == BarScale.Unknown) {
				string msg = "TODO: figure out why deserialized / userSelected strategyClicked[" + this.ChartFormManager.Executor.Strategy
					+ "].ScriptContextCurrent.ScaleInterval[" + ctxScript.ScaleInterval + "] has BarScale.Unknown #4";
				Assembler.PopupException(msg);
			} else {
				MenuItemLabeledTextBox mnitlbForScale = null;
				switch (ctxScript.ScaleInterval.Scale) {
					case BarScale.Minute:		mnitlbForScale = this.mnitlbMinutes; break; 
					case BarScale.Hour:			mnitlbForScale = this.mnitlbDaily; break; 
					case BarScale.Daily:		mnitlbForScale = this.mnitlbHourly; break; 
					case BarScale.Weekly:		mnitlbForScale = this.mnitlbWeekly; break; 
					case BarScale.Monthly:		mnitlbForScale = this.mnitlbMonthly; break; 
					//case BarScale.Quarterly		mnitlbForScale = this.mnitlbQuarterly; break; 
					case BarScale.Yearly:		mnitlbForScale = this.mnitlbYearly; break;
					default:
						string msg = "SCALE_UNHANDLED_NO_TEXTBOX_TO_POPULATE " + ctxScript.ScaleInterval.Scale;
						Assembler.PopupException(msg);
						break;
				}
				
				if (mnitlbForScale != null) {
					mnitlbForScale.InputFieldValue = ctxScript.ScaleInterval.Interval.ToString();
					mnitlbForScale.BackColor = Color.Gainsboro;
				}
			}

			this.mniShowBarRange.Checked = ctxScript.ShowRangeBar;
			switch (ctxScript.DataRange.Range) {
				case BarRange.AllData:
					this.mnitlbShowLastBars.InputFieldValue = "";
					this.mnitlbShowLastBars.BackColor = Color.White;
					break;
				case BarRange.DateRange:
					this.ChartControl.RangeBar.ValueMin = ctxScript.DataRange.StartDate; 
					this.ChartControl.RangeBar.ValueMax = ctxScript.DataRange.EndDate; 
					this.mnitlbShowLastBars.InputFieldValue = "";
					this.mnitlbShowLastBars.BackColor = Color.White;
					break;
				case BarRange.RecentBars:
					this.mnitlbShowLastBars.InputFieldValue = ctxScript.DataRange.RecentBars.ToString();
					this.mnitlbShowLastBars.BackColor = Color.Gainsboro;
					//this.mniShowBarRange.Checked = false;
					break;
				default:
					string msg = "DATE_RANGE_UNHANDLED_RECENT_TIMEUNITS_NYI " + ctxScript.DataRange;
					Assembler.PopupException(msg);
					break;
			}
			this.ChartControl.RangeBarCollapsed = !this.mniShowBarRange.Checked; 

			switch (ctxScript.PositionSize.Mode) {
				case PositionSizeMode.SharesConstantEachTrade:
					this.mnitlbPositionSizeSharesConstantEachTrade.InputFieldValue = ctxScript.PositionSize.SharesConstantEachTrade.ToString();
					this.mnitlbPositionSizeSharesConstantEachTrade.BackColor = Color.Gainsboro;
					this.mnitlbPositionSizeDollarsEachTradeConstant.BackColor = Color.White;
					break;
				case PositionSizeMode.DollarsConstantForEachTrade:
					this.mnitlbPositionSizeDollarsEachTradeConstant.InputFieldValue = ctxScript.PositionSize.DollarsConstantEachTrade.ToString();
					this.mnitlbPositionSizeDollarsEachTradeConstant.BackColor = Color.Gainsboro;
					this.mnitlbPositionSizeSharesConstantEachTrade.BackColor = Color.White;
					break;
				default:
					string msg = "POSITION_SIZE_UNHANDLED_NYI " + ctxScript.PositionSize.Mode;
					Assembler.PopupException(msg);
					break;
			}

			DataSourcesForm.Instance.DataSourcesTreeControl.SelectSymbol(ctxScript.DataSourceName, ctxScript.Symbol);
			StrategiesForm.Instance.StrategiesTreeControl.SelectStrategy(this.ChartFormManager.Executor.Strategy);
			this.PropagateSelectorsDisabledIfStreamingForCurrentChart();
		}
		void TsiProgressBarETAClick(object sender, EventArgs e) {
			this.ChartFormManager.Executor.Backtester.AbortRunningBacktestWaitAborted("Backtest Aborted by clicking on progress bar");
		}
		public void PropagateSelectorsDisabledIfStreamingForCurrentChart() {
			Strategy strategyClicked = this.ChartFormManager.Strategy;
			if (strategyClicked == null) return;

			//do not disturb a streaming chart with selector's changes (disable selectors if streaming; for script-free charts strategy=null)
			bool enableForNonStreaming = !strategyClicked.ScriptContextCurrent.ChartStreaming;
			
			//DataSourcesForm.Instance.DataSourcesTree.Enabled = enableForNonStreaming;
			
			this.mnitlbMinutes.Enabled = enableForNonStreaming;
			this.mnitlbDaily.Enabled = enableForNonStreaming;
			this.mnitlbHourly.Enabled = enableForNonStreaming;
			this.mnitlbWeekly.Enabled = enableForNonStreaming;
			this.mnitlbMonthly.Enabled = enableForNonStreaming;
			//this.mnitlbQuarterly.Enabled = enableForNonStreaming;
			this.mnitlbYearly.Enabled = enableForNonStreaming;
			this.mnitlbShowLastBars.Enabled = enableForNonStreaming;
			this.mnitlbPositionSizeDollarsEachTradeConstant.Enabled = enableForNonStreaming;
			this.mnitlbPositionSizeSharesConstantEachTrade.Enabled = enableForNonStreaming;
		}		
	}
}