﻿using System;
using System.Drawing;
using System.Windows.Forms;

using WeifenLuo.WinFormsUI.Docking;

namespace Sq1.Widgets {
	public class DockContentImproved : DockContent {
		// Docked Form with MinimumSize will take all MinimumSize area and overlap to neighbours
		// MinimumSize should be 0 to avoid overlap confusion and let myself resize to 0
		// using FloatWindowRecommendedSize as inDesigner-user-defined Size to set initial Floating Window size
		public Size FloatWindowRecommendedSize;
		
		public DockContentImproved() : base() {
		}
//		protected override void OnLoad(EventArgs e) {
//			if (base.ShowHint == DockState.Float) {
//				//base.Size = this.FloatWindowRecommendedSize;
//				this.Width = this.FloatWindowRecommendedSize.Width;
//				this.Height = this.FloatWindowRecommendedSize.Height;
//				base.FloatPane.ClientSize = this.FloatWindowRecommendedSize; 
//				Size a = base.FloatPane.PreferredSize; 
//				base.FloatPane.Size = this.FloatWindowRecommendedSize;
//			}
//		}
//		protected override void OnActivated(EventArgs e) {
//			if (base.ShowHint == DockState.Float) {
//				//base.Size = this.FloatWindowRecommendedSize;
//				this.Width = this.FloatWindowRecommendedSize.Width;
//				this.Height = this.FloatWindowRecommendedSize.Height;
//				base.FloatPane.ClientSize = this.FloatWindowRecommendedSize; 
//				Size a = base.FloatPane.PreferredSize; 
//				base.FloatPane.Size = this.FloatWindowRecommendedSize; 
//			}
//		}
//		public new void Show(DockPanel dp) {
//			if (base.ShowHint == DockState.Float) {
//				//base.Size = this.FloatWindowRecommendedSize;
//				this.Width = this.FloatWindowRecommendedSize.Width;
//				this.Height = this.FloatWindowRecommendedSize.Height;
//				//base.FloatPane.ClientSize = this.FloatWindowRecommendedSize; 
//				//Size a = base.FloatPane.PreferredSize; 
//				//base.FloatPane.Size = this.FloatWindowRecommendedSize; 
//			}
//			base.Show(dp);
//		}
		//http://msdn.microsoft.com/en-us/library/86faxx0d(v=vs.110).aspx
//			Application.ApplicationExit += new EventHandler(Application_ApplicationExit);
//		public bool ApplicationExitOccured = false;
//		void Application_ApplicationExit(object sender, EventArgs e) {
//			this.ApplicationExitOccured = true;
//		}
//		
//		public bool IsClosing = false;
//		protected override void OnClosing(System.ComponentModel.CancelEventArgs e) {
//			this.IsClosing = true;
//			base.OnClosing(e);
//		}		
//
//		// too late for ExceptionsControl.Splitter*.Distance to serialize
//		public bool IsFormClosing = false;
//		protected override void OnFormClosing(System.Windows.Forms.FormClosingEventArgs e) {
//			this.IsFormClosing = true;
//			base.OnFormClosing(e);
//		}
//		
//		public bool IsFormClosed = false;
//		protected override void OnFormClosed(System.Windows.Forms.FormClosedEventArgs e) {
//			this.IsFormClosed = true;
//			base.OnFormClosed(e);
//		}
		
		public void ShowAsDocumentTabNotPane(DockPanel dockPanel) {
			var docs = dockPanel.DocumentsToArray();
			if (docs.Length == 0) {
				this.Show(dockPanel, DockState.Document);
				return;
			}
			// add new tab, not a new pane besides existing one
			foreach (IDockContent doc in docs) {
				var hopefullyDockContent = doc as DockContent;
				if (hopefullyDockContent == null) continue; 
				this.Show(hopefullyDockContent.Pane, null);
				return;
			}
			this.Show(dockPanel, DockState.Document);
		}
	}
}
