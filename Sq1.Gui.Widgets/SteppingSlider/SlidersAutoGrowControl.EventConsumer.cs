﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

using Sq1.Core;
using Sq1.Core.StrategyBase;
using Sq1.Widgets.SteppingSlider;

namespace Sq1.Widgets.SteppingSlider {
	public partial class SlidersAutoGrowControl {
		void slider_ValueCurrentChanged(object sender, EventArgs e) {
			try {
				SliderComboControl slider = sender as SliderComboControl;
				ScriptParameter scriptParameterChanged = slider.Tag as ScriptParameter;
				scriptParameterChanged.ValueCurrent = (double)slider.ValueCurrent;
				this.Strategy.DropChangedValueToScriptAndCurrentContextAndSerialize(scriptParameterChanged);
				this.RaiseOnSliderValueChanged(scriptParameterChanged);
			} catch (Exception ex) {
				Assembler.PopupException("slider_ValueCurrentChanged()", ex);
			}
		}
	}
}