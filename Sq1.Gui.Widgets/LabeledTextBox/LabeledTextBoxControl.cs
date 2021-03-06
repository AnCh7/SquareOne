﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using Sq1.Core.Execution;

namespace Sq1.Widgets.LabeledTextBox {
	public partial class LabeledTextBoxControl : UserControl {
		public LabeledTextBoxControl() {
			InitializeComponent();
			//base.Size = new Size(base.Width, base.Height - 3);
		}
//		[Browsable(true)]
//		public new Size Size {
//			get { return new Size(base.Size.Width, base.Size.Height - 2); }
//			set { base.Size = new Size(value.Width, value.Height + 2); }
//		}
		[Browsable(true)]
		public TextBox InternalTextBox {
			get { return this.TextBox; }
			set { this.TextBox = value; }
		}
//		[Browsable(true)]
//		public Label InternalLabel {
//			get { return this.Label; }
//			set { this.Label = value; }
//		}
		[Browsable(true)]
		public new string Text {
			get { return this.Label.Text; }
			set { this.Label.Text = value; }
		}
		[Browsable(true)]
		public bool TextRed {
			get { return this.Label.ForeColor == Color.Red; }
			set { this.Label.ForeColor = value ? Color.Red : Color.Black; }
		}
		[Browsable(true)]
		public string InputFieldValue {
			get { return this.TextBox.Text; }
			set { this.TextBox.Text = value; }
		}
		[DefaultValueAttribute(typeof(TextBox), null), Browsable(true)]
		public int InputFieldOffsetX {
			get { return this.TextBox.Location.X; }
			set { this.TextBox.Location = new Point(value, this.TextBox.Location.Y); }
		}
		[DefaultValueAttribute(typeof(TextBox), null), Browsable(true)]
		public int InputFieldWidth {
			get { return this.TextBox.Width; }
			set { this.TextBox.Width = value; }
		}
		public ToolStripControlHost ParentToolStripControlHost;
	}
}
