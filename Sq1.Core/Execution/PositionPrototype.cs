﻿using System;
using System.Collections.Generic;

namespace Sq1.Core.Execution {
	public class PositionPrototype {
		public readonly string Symbol;
		public readonly PositionLongShort LongShort;
		public double PriceEntry { get; protected set; }
		public double StopLossNegativeOffset { get; protected set; }
		public double StopLossActivationNegativeOffset { get; protected set; }
		public double TakeProfitPositiveOffset { get; protected set; }
		public double PriceStopLossActivation { get { return this.OffsetToPrice(this.StopLossActivationNegativeOffset); } }
		public double PriceStopLoss { get { return this.OffsetToPrice(this.StopLossNegativeOffset); } }
		public double PriceTakeProfit { get { return this.OffsetToPrice(this.TakeProfitPositiveOffset); } }

		public Alert StopLossAlertForAnnihilation;
		public Alert TakeProfitAlertForAnnihilation;
		private PositionLongShort positionLongShort;
		private double TP;
		private double SL;
		private double SLactivation;

		public PositionPrototype(string symbol, PositionLongShort positionLongShort, double priceEntry,
				double takeProfitPositiveOffset,
				double stopLossNegativeOffset, double stopLossActivationNegativeOffset = 0) {

			this.Symbol = symbol;
			this.LongShort = positionLongShort;
			this.PriceEntry = priceEntry;
			this.SetNewTakeProfitOffset(takeProfitPositiveOffset);
			this.SetNewStopLossOffsets(stopLossNegativeOffset, stopLossActivationNegativeOffset);
		}

		public void SetNewTakeProfitOffset(double newTakeProfitPositiveOffset) {
			this.checkTPOffsetThrowBeforeAbsorbing(newTakeProfitPositiveOffset);
			this.TakeProfitPositiveOffset = newTakeProfitPositiveOffset;
		}
		public void SetNewStopLossOffsets(double newStopLossNegativeOffset, double stopLossActivationNegativeOffset) {
			this.checkSLOffsetsThrowBeforeAbsorbing(newStopLossNegativeOffset, stopLossActivationNegativeOffset);
			this.StopLossActivationNegativeOffset = stopLossActivationNegativeOffset;
			this.StopLossNegativeOffset = newStopLossNegativeOffset;
		}
		public void checkTPOffsetThrowBeforeAbsorbing(double takeProfitPositiveOffset) {
			if (takeProfitPositiveOffset < 0) {
				throw new Exception("WRONG USAGE OF PositionPrototype.ctor()!"
					+ " PositionPrototype should contain positive offset for TakeProfit");
			}
		}
		public void checkSLOffsetsThrowBeforeAbsorbing(double stopLossNegativeOffset, double stopLossActivationNegativeOffset) {
			if (stopLossNegativeOffset > 0) {
				throw new Exception("WRONG USAGE OF PositionPrototype.ctor()!"
					+ " PositionPrototype should contain negative offset for StopLoss");
			}
			if (stopLossActivationNegativeOffset > 0) {
				throw new Exception("WRONG USAGE OF PositionPrototype.ctor()!"
					+ " PositionPrototype should contain negative offset for StopLossActivation");
			}
			if (stopLossActivationNegativeOffset == 0) return;
			if (stopLossActivationNegativeOffset <= stopLossNegativeOffset) {
				throw new Exception("USAGE: PositionPrototype(Long, Entry=100, TP=150, SL=-50, SLa=-40)"
					+ "; StopLossActivation[" + stopLossActivationNegativeOffset + "]"
					+ " should be >= StopLoss[" + stopLossNegativeOffset + "]");
			}
		}		//public void checkThrowAbsorbed() {
		//	this.checkSlOffsetsThrowBeforeAbsorbing(this.TakeProfitPositiveOffset, this.StopLossNegativeOffset, this.StopLossActivationNegativeOffset);
		//}
		//internal void StopLossNegativeSameActivationDistanceOffsetSafeUpdate(double newStopLossNegativeOffset) {
		//	double newActivationOffset = this.CalcActivationOffsetForNewClosing(newStopLossNegativeOffset);
		//	this.checkSlOffsetsThrowBeforeAbsorbing(this.TakeProfitPositiveOffset, newStopLossNegativeOffset, newActivationOffset);
		//	this.StopLossNegativeOffset = newStopLossNegativeOffset;
		//}

		public double CalcActivationOffsetForNewClosing(double newStopLossNegativeOffset) {
			// for a long position, activation price is above closing price
			double currentDistance = this.StopLossActivationNegativeOffset - this.StopLossNegativeOffset;
			return newStopLossNegativeOffset + currentDistance;
		}

		public double CalcStopLossDifference(double newStopLossNegativeOffset) {
			return newStopLossNegativeOffset - this.StopLossNegativeOffset;
		}

		public double OffsetToPrice(double newActivationOffset) {
			double priceFromOffset = 0;
			switch (this.LongShort) {
				case PositionLongShort.Long:
					priceFromOffset = this.PriceEntry + newActivationOffset;
					break;
				case PositionLongShort.Short:
					priceFromOffset = this.PriceEntry - newActivationOffset;
					break;
				default:
					string msg = "OffsetToPrice(): No PositionLongShort[" + this.LongShort + "] handler "
						+ "; must be one of those: Long/Short";
					throw new Exception(msg);
			}
			return priceFromOffset;
		}

		public double PriceToOffset(double newPrice) {
			return this.PriceEntry - newPrice;
		}
		public bool IsIdenticalTo(PositionPrototype proto) {
			return this.LongShort == proto.LongShort
				&& this.PriceEntry == proto.PriceEntry
				&& this.TakeProfitPositiveOffset == proto.TakeProfitPositiveOffset
				&& this.StopLossNegativeOffset == proto.StopLossNegativeOffset
				&& this.StopLossActivationNegativeOffset == proto.StopLossActivationNegativeOffset;
		}
		public override string ToString() {
			return this.LongShort + " Entry[" + this.PriceEntry + "]TP[" + this.TakeProfitPositiveOffset + "]SL["
				+ this.StopLossNegativeOffset + "]SLA[" + this.StopLossActivationNegativeOffset + "]";
		}
	}
}