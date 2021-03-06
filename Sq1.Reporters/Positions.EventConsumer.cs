using System;
using System.Windows.Forms;
using Sq1.Core;
using Sq1.Core.Execution;

namespace Sq1.Reporters {
	public partial class Positions {
		void mniCopyToClipboard_Click(object sender, EventArgs e) {
			string text = this.generateTextScreenshot();
			Clipboard.SetText(text);
		}
		void mniSaveToFile_Click(object sender, EventArgs e) {
			this.SaveToFile();
		}
		void mniShowEntriesExits_Click(object sender, EventArgs e) {
			try {
				this.activateMniFilter(this.columnsByFilters, this.mniShowEntriesExits);
				this.snap.ShowEntriesExits = this.mniShowEntriesExits.Checked;
				base.RaiseContextScriptChangedContainerShouldSerialize();
			} catch (Exception ex) {
				Assembler.PopupException(ex.Message);
			}
		}
		void mniShowPercentage_Click(object sender, EventArgs e) {
			try {
				this.activateMniFilter(this.columnsByFilters, this.mniShowPercentage);
				this.snap.ShowPercentage = this.mniShowPercentage.Checked;
				base.RaiseContextScriptChangedContainerShouldSerialize();
			} catch (Exception ex) {
				Assembler.PopupException(ex.Message);
			}
		}
		void mniShowBarsHeld_Click(object sender, EventArgs e) {
			try {
				this.activateMniFilter(this.columnsByFilters, this.mniShowBarsHeld);
				this.snap.ShowBarsHeld = this.mniShowBarsHeld.Checked;
				base.RaiseContextScriptChangedContainerShouldSerialize();
			} catch (Exception ex) {
				Assembler.PopupException(ex.Message);
			}
		}
		void mniShowMaeMfe_Click(object sender, EventArgs e) {
			try {
				this.activateMniFilter(this.columnsByFilters, this.mniShowMaeMfe);
				this.snap.ShowMaeMfe = this.mniShowMaeMfe.Checked;
				base.RaiseContextScriptChangedContainerShouldSerialize();
			} catch (Exception ex) {
				Assembler.PopupException(ex.Message);
			}
		}
		void mniShowSignals_Click(object sender, EventArgs e) {
			try {
				this.activateMniFilter(this.columnsByFilters, this.mniShowSignals);
				this.snap.ShowSignals = this.mniShowSignals.Checked;
				base.RaiseContextScriptChangedContainerShouldSerialize();
			} catch (Exception ex) {
				Assembler.PopupException(ex.Message);
			}
		}
		void mniShowCommission_Click(object sender, EventArgs e) {
			try {
				this.activateMniFilter(this.columnsByFilters, this.mniShowCommission);
				this.snap.ShowCommission = this.mniShowCommission.Checked;
				base.RaiseContextScriptChangedContainerShouldSerialize();
			} catch (Exception ex) {
				Assembler.PopupException(ex.Message);
			}
		}
		void mniColorify_Click(object sender, EventArgs e) {
			try {
				this.snap.Colorify = this.mniColorify.Checked;
				this.objectListViewCustomizeColors();
				//this.olvPositions.Refresh();
				this.olvPositions.BuildList(true);
				base.RaiseContextScriptChangedContainerShouldSerialize();
			} catch (Exception ex) {
				Assembler.PopupException(ex.Message);
			}
		}
		void olvPositions_DoubleClick(object sender, EventArgs e) {
			try {
				if (this.olvPositions.SelectedItems.Count == 0) return;
				ListViewItem lvi = this.olvPositions.SelectedItems[0];
				Position pos = lvi.Tag as Position;
				if (pos == null) {
					string msg = "POSITION_WASNT_STORED_IN_TAG for lvi[" + lvi + "]";
					Assembler.PopupException(msg);
					return;
				}
				base.Chart.SelectPosition(pos);
			} catch (Exception ex) {
				Assembler.PopupException(ex.Message);
			}
		}
	}
}