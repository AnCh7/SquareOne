using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Sq1.Core.Charting;
using Sq1.Core.DataTypes;
using Sq1.Core.DoubleBuffered;
using Sq1.Core.Execution;
using Sq1.Core.Indicators;

namespace Sq1.Core.Charting {
	public class ChartShadow : 
//COMMENTED_OUT_TO_MAKE_C#DEVELOPER_CLICK_THROUGH #if !NON_DOUBLE_BUFFERED_reverted_to_compulsory_UserControl_no_buffering
//	UserControlDoubleBuffered
//#else
	UserControl
//#endif
	{
		//REPLACED_BY_ScriptExecutorObjects public ScriptToChartCommunicator ScriptToChartCommunicator { get; protected set; }
		public event EventHandler<EventArgs> ChartSettingsChangedContainerShouldSerialize;
		public event EventHandler<EventArgs> ContextScriptChangedContainerShouldSerialize;

		public ChartShadow() : base() {
//			this.ScriptToChartCommunicator = new ScriptToChartCommunicator();
			this.Register();
		}
		public void Register(bool dontAccessAssemblerWhileInDesignMode = false) {
			if (base.DesignMode) return;
			if (dontAccessAssemblerWhileInDesignMode) return;
			if (Assembler.InstanceUninitialized.StatusReporter == null) return;
			Assembler.InstanceInitialized.AlertsForChart.Register(this);
		}
		
		public virtual void SelectPosition(Position position) {
			string msg = "ChartShadow::SelectPosition() TODO: implement HIGHLIGHTING for a position[" + position + "]; chart[" + this + "]";
			Assembler.PopupException(msg);
		}
		
		public virtual void SelectAlert(Alert alert) {
			string msg = "ChartShadow::SelectAlert() TODO: implement HIGHLIGHTING for a alert[" + alert + "]; chart[" + this + "]";
			Assembler.PopupException(msg);
		}

//#if !NON_DOUBLE_BUFFERED_reverted_to_compulsory_UserControl_no_buffering
//		protected override void OnPaintDoubleBuffered(PaintEventArgs paintEventArgs) {
//			if (base.DesignMode) return;
//			//throw new Exception("ChartShadow::OnPaintDoubleBuffered(): Implemented in Chart, make sure proper casting occurs");
//		}
//		protected override void OnPaintBackgroundDoubleBuffered(PaintEventArgs paintEventArgs) {
//			if (base.DesignMode) return;
//			//throw new Exception("ChartShadow::OnPaintBackgroundDoubleBuffered(): Implemented in Chart, make sure proper casting occurs");
//		}
//#else
//#endif
		public void RaiseChartSettingsChangedContainerShouldSerialize() {
			if (this.ChartSettingsChangedContainerShouldSerialize == null) return;
			try {
				this.ChartSettingsChangedContainerShouldSerialize(this, null);
			} catch (Exception ex) {
				Assembler.PopupException("RaiseChartSettingsChangedContainerShouldSerialize()", ex);
			}
		}
		public void RaiseContextScriptChangedContainerShouldSerialize() {
			if (this.ContextScriptChangedContainerShouldSerialize == null) return;
			try {
				this.ContextScriptChangedContainerShouldSerialize(this, null);
			} catch (Exception ex) {
				Assembler.PopupException("RaiseContextScriptChangedContainerShouldSerialize()", ex);
			}
		}
		
		
#region there is no graphics-related (PositionArrows / LinesDrawnOnChart) DataSnapshot in Core; ChartControl knows how to handle your wishes in terms of Core objects
		public virtual void PositionsClearBacktestStarting() {
			throw new NotImplementedException();
		}
		public virtual void PositionsBacktestAdd(List<Position> positionsMaster) {
			throw new NotImplementedException();
		}
		public virtual void PositionsRealtimeAdd(ReporterPokeUnit pokeUnit) {
			throw new NotImplementedException();
		}
		
		public virtual void PendingHistoryClearBacktestStarting() {
			throw new NotImplementedException();
		}
		public virtual void PendingHistoryBacktestAdd(Dictionary<int, List<Alert>> alertsPendingHistorySafeCopy) {
			throw new NotImplementedException();
		}
		public virtual void PendingRealtimeAdd(ReporterPokeUnit pokeUnit) {
			throw new NotImplementedException();
		}
		
		public virtual void LineDrawModify(string id, Color color, int width, Bar barStart, double priceStart, Bar barEnd, double priceEnd) {
			throw new NotImplementedException();
		}
#endregion
		
		public virtual HostPanelForIndicator GetHostPanelForIndicator(Indicator indicator) {
			throw new NotImplementedException();
		}
		public virtual void SetIndicators(Dictionary<string, Indicator> indicators) {
			throw new NotImplementedException();
		}
	}
}
