﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Sq1.Core.Execution {
	public class PositionSizeEventArgs : EventArgs {
		public PositionSize PositionSize {get; private set; }
		public PositionSizeEventArgs(PositionSize positionSize) {
			this.PositionSize = positionSize.Clone();
		}
	}
}