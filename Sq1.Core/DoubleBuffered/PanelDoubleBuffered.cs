﻿using System;
using System.Drawing;
using System.Windows.Forms;

//http://www.codeproject.com/Articles/12870/Don-t-Flicker-Double-Buffer
namespace Sq1.Core.DoubleBuffered {
	public abstract class PanelDoubleBuffered : Panel {
		protected BufferedGraphicsContext graphicManager;
		protected BufferedGraphics bufferedGraphics;
		
		protected abstract void OnPaintDoubleBuffered(PaintEventArgs pe);
		protected virtual void OnPaintBackgroundDoubleBuffered(PaintEventArgs pe) {
			pe.Graphics.SetClip(base.ClientRectangle);	// always repaint whole Panel; by default, only extended area is "Clipped"
			pe.Graphics.Clear(base.BackColor);
		}
		
		// what sort of TRANSPARENT does it allow?... ommitting will require "override OnResize()"
		//ABSOLUTELY_FLICKERS protected override CreateParams CreateParams { get {
		//	CreateParams cp = base.CreateParams;
		//	cp.ExStyle |= 0x00000020; //WS_EX_TRANSPARENT
		//	return cp;
		//} }

		public PanelDoubleBuffered() : base() {
			Application.ApplicationExit += new EventHandler(DisposeAndNullifyToRecreateInPaint);
			base.SetStyle( ControlStyles.AllPaintingInWmPaint
						 | ControlStyles.OptimizedDoubleBuffer
					//	 | ControlStyles.UserPaint
					//	 | ControlStyles.ResizeRedraw
					, true);
			this.graphicManager = BufferedGraphicsManager.Current;
		}
		private void InitializeBuffer() {
			this.graphicManager.MaximumBuffer =  new Size(base.Width + 1, base.Height + 1);
			this.bufferedGraphics = this.graphicManager.Allocate(this.CreateGraphics(),  base.ClientRectangle);
		}
		private void DisposeAndNullifyToRecreateInPaint(object sender, EventArgs e) {
			if (this.bufferedGraphics == null) return;
			this.bufferedGraphics.Dispose();
			this.bufferedGraphics = null;
		}
		protected override void OnPaint(PaintEventArgs pe) {
			try {
				// overhead here since we need to call this.InitializeBuffer() in ctor() after
				// UserControlChild.InitializeComponents() where UserControl.Width and UserControl.Height are set
				if (this.bufferedGraphics == null) this.InitializeBuffer();
	
				// let the child draw on BufferedGraphics
				PaintEventArgs peSubstituted = new PaintEventArgs(bufferedGraphics.Graphics, pe.ClipRectangle);
				// REVERTED_TO_KEEP_WINFORMS_COMPATIBLE: child has to clip after resize
				//PaintEventArgs peSubstituted = new PaintEventArgs(bufferedGraphics.Graphics, base.ClientRectangle);
				//pe.Graphics.SetClip(base.ClientRectangle);	// always repaint whole Panel Surface; by default, only extended area is "Clipped"
				
				//NO_MANUAL_BG_FOLLOW_WINFORMS_MODEL this.OnPaintBackgroundDoubleBuffered(peSubstituted);
				this.OnPaintDoubleBuffered(peSubstituted);
				//OVERHEAD_REMOVED base.OnPaint(peSubstituted);
	
				// now we spit BufferedGraphics into the screen
				this.bufferedGraphics.Render(pe.Graphics);
			} catch (Exception ex) {
				System.Diagnostics.Debugger.Break();
			}
		}
		protected override void OnPaintBackground(PaintEventArgs pe) {
			// overhead here since we need to call this.InitializeBuffer() in ctor() after
			// UserControlChild.InitializeComponents() where UserControl.Width and UserControl.Height are set
			if (this.bufferedGraphics == null) this.InitializeBuffer();
			PaintEventArgs peSubstituted = new PaintEventArgs(bufferedGraphics.Graphics, pe.ClipRectangle);
			this.OnPaintBackgroundDoubleBuffered(peSubstituted);
		}
		protected override void OnResize(EventArgs e) {
			this.DisposeAndNullifyToRecreateInPaint(this, e);
			this.Invalidate();
		}
	}
}