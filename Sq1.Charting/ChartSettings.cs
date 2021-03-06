﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.Serialization;

using Newtonsoft.Json;
using Sq1.Core.StrategyBase;

namespace Sq1.Charting {
	[DataContract]	// I don't want to JsonSerialize all member of base class Component
	// why ChartSettings inherits Component? F4 on ChartSettings will allow you to edit colors visually
	// REMOVE ": Component" when you're done with visual editing to stop Designer flooding ChartControl.Designer.cs
	public class ChartSettings {	//: Component {
		[DataMember] public Color	ChartColorBackground { get; set; }
		[DataMember] public int		BarWidthIncludingPadding { get; set; }
		[DataMember] public Font	PanelNameAndSymbolFont { get; set; }
		[DataMember] public Color	PriceColorBarUp { get; set; }
		[DataMember] public Color	PriceColorBarDown { get; set; }
		[DataMember] public Color	VolumeColorBarUp { get; set; }
		[DataMember] public Color	VolumeColorBarDown { get; set; }
		[DataMember] public Color	GutterRightColorBackground { get; set; }
		[DataMember] public Color	GutterRightColorForeground { get; set; }
		[DataMember] public int		GutterRightPadding { get; set; }
		[DataMember] public Font	GutterRightFont { get; set; }
		[DataMember] public Color	GutterBottomColorBackground { get; set; }
		[DataMember] public Color	GutterBottomColorForeground { get; set; }
		[DataMember] public Color	GutterBottomNewDateColorForeground { get; set; }
		[DataMember] public int		GutterBottomPadding { get; set; }
		[DataMember] public Font	GutterBottomFont { get; set; }
		[DataMember] public string	GutterBottomDateFormatDayOpener;
		[DataMember] public string	GutterBottomDateFormatIntraday;
		[DataMember] public string	GutterBottomDateFormatDaily;
		[DataMember] public string	GutterBottomDateFormatWeekly;
		[DataMember] public string	GutterBottomDateFormatYearly;
		[DataMember] public Color	GridlinesHorizontalColor { get; set; }
		[DataMember] public Color	GridlinesVerticalColor { get; set; }
		[DataMember] public Color	GridlinesVerticalNewDateColor { get; set; }
		[DataMember] public bool	GridlinesHorizontalShow { get; set; }
		[DataMember] public bool	GridlinesVerticalShow { get; set; }
		[DataMember] public int		ScrollSqueezeMouseDragSensitivityPx { get; set; }
		[DataMember] public int		ScrollNBarsPerOneDragMouseEvent { get; set; }
		[DataMember] public int		ScrollNBarsPerOneKeyPress { get; set; }
		[DataMember] public int		SqueezeVerticalPaddingPx { get; set; }
		[DataMember] public int		SqueezeVerticalPaddingStep { get; set; }
		[DataMember] public int		SqueezeHorizontalStep { get; set; }
		[DataMember] public int		SqueezeHorizontalMouse1pxDistanceReceivedToOneStep { get; set; }
		[DataMember] public int		SqueezeHorizontalKeyOnePressReceivedToOneStep { get; set; }
		[DataMember] public bool	TooltipPriceShow { get; set; }
		[DataMember] public bool	TooltipPriceShowOnlyWhenMouseTouchesCandle { get; set; }
		[DataMember] public bool	TooltipPositionShow { get; set; }
		[DataMember] public int		TooltipsPaddingFromBarLeftRightEdgesToAvoidMouseLeave { get; set; }
		[DataMember] public int		PositionArrowPaddingVertical { get; set; }
		[DataMember] public int		ScrollPositionAtBarIndex { get; set; }
		[DataMember] public int		TooltipBordersMarginToKeepBordersVisible { get; set; }
		[DataMember] public Color	PositionPlannedEllipseColor { get; set; }
		[DataMember] public int		PositionPlannedEllipseColorAlpha { get; set; }
		[DataMember] public int		PositionPlannedEllipseDiameter { get; set; }
		[DataMember] public Color	PositionFilledDotColor { get; set; }
		[DataMember] public int		PositionFilledDotColorAlpha { get; set; }
		[DataMember] public int		PositionFilledDotDiameter { get; set; }
		[DataMember] public int		PositionLineHighlightedWidth { get; set; }
		[DataMember] public int		PositionLineHighlightedAlpha { get; set; }
		[DataMember] public Color	PositionLineNoExitYetColor { get; set; }
		[DataMember] public int		PositionLineNoExitYetColorAlpha { get; set; }
		[DataMember] public Color	PositionLineProfitableColor { get; set; }
		[DataMember] public int		PositionLineProfitableColorAlpha { get; set; }
		[DataMember] public Color	PositionLineLossyColor { get; set; }
		[DataMember] public int		PositionLineLossyColorAlpha { get; set; }
		[DataMember] public int 	PriceVsVolumeSplitterDistance { get; set; }
		[DataMember] public Color	AlertPendingEllipseColor { get; set; }
		[DataMember] public int		AlertPendingEllipseColorAlpha { get; set; }
		[DataMember] public int		AlertPendingEllipseWidth { get; set; }
		[DataMember] public bool	MousePositionTrackOnGutters { get; set; }
		[DataMember] public Color	MousePositionTrackOnGuttersColor { get; set; }

		//!!!![JsonIgnore] is all down there because JSON.dll is .NET20 while [DataMember] is defined in .NET35's System.Runtime.Serialization

		// DONE_IN_RenderBarsPrice_KISS cache them all until user edits this.BarTotalWidthPx so they won't be calculated again with the same result for each bar
		[JsonIgnore] public int BarPaddingRight { get {
			if (this.BarWidthIncludingPadding <= 3) return 0;
			//int nominal = (int) (this.BarWidthIncludingPadding * 0.25F);
			int nominal = 1;
			// algo below allows you have this.BarTotalWidthPx both odd and even automatically
			int compensated = nominal;
			int keepWidthOdd = this.BarWidthIncludingPadding - compensated;
			if (keepWidthOdd % 2 == 0) compensated++;	// increase padding to have 1px shadows right in the middle of a bar
			return compensated;
		} }
		[JsonIgnore] public int BarWidthMinusRightPadding { get { return this.BarWidthIncludingPadding - this.BarPaddingRight; } }
		[JsonIgnore] public int BarShadowXoffset { get { return this.BarWidthMinusRightPadding / 2; } }
		
		[JsonIgnore] SolidBrush brushBackground;
		//[Browsable(false)]
		[JsonIgnore] public SolidBrush BrushBackground { get {
				if (this.brushBackground == null) this.brushBackground = new SolidBrush(this.ChartColorBackground);
				return this.brushBackground;
			} }

		[JsonIgnore] SolidBrush brushBackgroundReversed;
		//[Browsable(false)]
		[JsonIgnore] public SolidBrush BrushBackgroundReversed { get {
				if (this.brushBackgroundReversed == null) this.brushBackgroundReversed = new SolidBrush(ColorReverse(this.ChartColorBackground));
				return this.brushBackgroundReversed;
			} }

		[JsonIgnore] SolidBrush brushGutterRightBackground;
		//[Browsable(false)]
		[JsonIgnore] public SolidBrush BrushGutterRightBackground { get {
				if (this.brushGutterRightBackground == null) this.brushGutterRightBackground = new SolidBrush(this.GutterRightColorBackground);
				return this.brushGutterRightBackground;
			} }

		[JsonIgnore] SolidBrush brushGutterRightForeground;
		//[Browsable(false)]
		[JsonIgnore] public SolidBrush BrushGutterRightForeground { get {
				if (this.brushGutterRightForeground == null) this.brushGutterRightForeground = new SolidBrush(this.GutterRightColorForeground);
				return this.brushGutterRightForeground;
			} }

		[JsonIgnore] SolidBrush brushGutterBottomBackground;
		//[Browsable(false)]
		[JsonIgnore] public SolidBrush BrushGutterBottomBackground { get {
				if (this.brushGutterBottomBackground == null) this.brushGutterBottomBackground = new SolidBrush(this.GutterBottomColorBackground);
				return this.brushGutterBottomBackground;
			} }

		[JsonIgnore] SolidBrush brushGutterBottomForeground;
		//[Browsable(false)]
		[JsonIgnore] public SolidBrush BrushGutterBottomForeground { get {
				if (this.brushGutterBottomForeground == null) this.brushGutterBottomForeground = new SolidBrush(this.GutterBottomColorForeground);
				return this.brushGutterBottomForeground;
			} }

		[JsonIgnore] SolidBrush brushGutterBottomNewDateForeground;
		//[Browsable(false)]
		[JsonIgnore] public SolidBrush BrushGutterBottomNewDateForeground { get {
				if (this.brushGutterBottomNewDateForeground == null) this.brushGutterBottomNewDateForeground = new SolidBrush(this.GutterBottomNewDateColorForeground);
				return this.brushGutterBottomNewDateForeground;
			} }

		[JsonIgnore] SolidBrush brushPriceBarUp;
		//[Browsable(false)]
		[JsonIgnore] public SolidBrush BrushPriceBarUp { get {
				if (this.brushPriceBarUp == null) this.brushPriceBarUp = 
					new SolidBrush(this.PriceColorBarUp);
				return this.brushPriceBarUp;
			} }

		[JsonIgnore] Pen penPriceBarUp;
		//[Browsable(false)]
		[JsonIgnore] public Pen PenPriceBarUp { get {
				if (this.penPriceBarUp == null) this.penPriceBarUp = 
					new Pen(this.PriceColorBarUp);
				return this.penPriceBarUp;
			} }

		[JsonIgnore] SolidBrush brushPriceBarDown;
		//[Browsable(false)]
		[JsonIgnore] public SolidBrush BrushPriceBarDown { get {
				if (this.brushPriceBarDown == null) this.brushPriceBarDown = 
					new SolidBrush(this.PriceColorBarDown);
				return this.brushPriceBarDown;
			} }

		[JsonIgnore] Pen penPriceBarDown;
		//[Browsable(false)]
		[JsonIgnore] public Pen PenPriceBarDown { get {
				if (this.penPriceBarDown == null) this.penPriceBarDown = 
					new Pen(this.PriceColorBarDown);
				return this.penPriceBarDown;
			} }

		[JsonIgnore] SolidBrush brushVolumeBarUp;
		//[Browsable(false)]
		[JsonIgnore] public SolidBrush BrushVolumeBarUp { get {
				if (this.brushVolumeBarUp == null) this.brushVolumeBarUp = 
					new SolidBrush(this.VolumeColorBarUp);
				return this.brushVolumeBarUp;
			} }

		[JsonIgnore] SolidBrush brushVolumeBarDown;
		//[Browsable(false)]
		[JsonIgnore] public SolidBrush BrushVolumeBarDown { get {
				if (this.brushVolumeBarDown == null) this.brushVolumeBarDown = 
					new SolidBrush(this.VolumeColorBarDown);
				return this.brushVolumeBarDown;
			} }

		[JsonIgnore] Pen penGridlinesHorizontal;
		//[Browsable(false)]
		[JsonIgnore] public Pen PenGridlinesHorizontal { get {
				if (this.penGridlinesHorizontal == null) this.penGridlinesHorizontal = 
					new Pen(this.GridlinesHorizontalColor);
				return this.penGridlinesHorizontal;
			} }

		[JsonIgnore] Pen penGridlinesVertical;
		//[Browsable(false)]
		[JsonIgnore] public Pen PenGridlinesVertical { get {
				if (this.penGridlinesVertical == null) this.penGridlinesVertical = 
					new Pen(this.GridlinesVerticalColor);
				return this.penGridlinesVertical;
			} }

		[JsonIgnore] Pen penGridlinesVerticalNewDate;
		//[Browsable(false)]
		[JsonIgnore] public Pen PenGridlinesVerticalNewDate { get {
				if (this.penGridlinesVerticalNewDate == null) this.penGridlinesVerticalNewDate = 
					new Pen(this.GridlinesVerticalNewDateColor);
				return this.penGridlinesVerticalNewDate;
			} }

		
		[JsonIgnore] Pen penPositionPlannedEllipse;
		//[Browsable(false)]
		[JsonIgnore] public Pen PenPositionPlannedEllipse { get {
				if (this.penPositionPlannedEllipse == null) this.penPositionPlannedEllipse = 
					new Pen(ColorMakeTransparent(this.PositionPlannedEllipseColor, this.PositionPlannedEllipseColorAlpha), this.PositionPlannedEllipseDiameter);
				return this.penPositionPlannedEllipse;
			} }

		[JsonIgnore] Pen penPositionFilledDot;
		//[Browsable(false)]
		[JsonIgnore] public Pen PenPositionFilledDot { get {
				if (this.penPositionFilledDot == null) this.penPositionFilledDot = 
					new Pen(ColorMakeTransparent(this.PositionFilledDotColor, this.PositionFilledDotColorAlpha));
				return this.penPositionFilledDot;
			} }

		[JsonIgnore] Brush brushPositionFilledDot;
		//[Browsable(false)]
		[JsonIgnore] public Brush BrushPositionFilledDot { get {
				if (this.brushPositionFilledDot == null) this.brushPositionFilledDot = 
					new SolidBrush(ColorMakeTransparent(this.PositionFilledDotColor, this.PositionFilledDotColorAlpha));
				return this.brushPositionFilledDot;
			} }

		[JsonIgnore] Pen penPositionLineEntryExitConnectedUnknown;
		//[Browsable(false)]
		[JsonIgnore] public Pen PenPositionLineEntryExitConnectedUnknown { get {
				if (this.penPositionLineEntryExitConnectedUnknown == null) this.penPositionLineEntryExitConnectedUnknown =
					new Pen(ColorMakeTransparent(this.PositionLineNoExitYetColor, this.PositionLineNoExitYetColorAlpha));
				return this.penPositionLineEntryExitConnectedUnknown;
			} }
		[JsonIgnore] Pen penPositionLineEntryExitConnectedProfit;
		//[Browsable(false)]
		[JsonIgnore] public Pen PenPositionLineEntryExitConnectedProfit { get {
				if (this.penPositionLineEntryExitConnectedProfit == null) this.penPositionLineEntryExitConnectedProfit =
					new Pen(ColorMakeTransparent(this.PositionLineProfitableColor, this.PositionLineProfitableColorAlpha));
				return this.penPositionLineEntryExitConnectedProfit;
			} }


		[JsonIgnore] Pen penPositionLineEntryExitConnectedLoss;
		//[Browsable(false)]
		[JsonIgnore] public Pen PenPositionLineEntryExitConnectedLoss { get {
				if (this.penPositionLineEntryExitConnectedLoss == null) this.penPositionLineEntryExitConnectedLoss =
					new Pen(ColorMakeTransparent(this.PositionLineLossyColor, this.PositionLineLossyColorAlpha));
				return this.penPositionLineEntryExitConnectedLoss;
			} }

		[JsonIgnore] Pen penAlertPendingEllipse;
		//[Browsable(false)]
		[JsonIgnore] public Pen PenAlertPendingEllipse { get {
				if (this.penAlertPendingEllipse == null) this.penAlertPendingEllipse =
					new Pen(ColorMakeTransparent(this.AlertPendingEllipseColor, this.AlertPendingEllipseColorAlpha), this.AlertPendingEllipseWidth);
				return this.penAlertPendingEllipse;
			} }

		[JsonIgnore] Pen penMousePositionTrackOnGutters;
		//[Browsable(false)]
		[JsonIgnore] public Pen PenMousePositionTrackOnGutters { get {
				if (this.penMousePositionTrackOnGutters == null) this.penMousePositionTrackOnGutters = 
					new Pen(this.MousePositionTrackOnGuttersColor);
				return this.penMousePositionTrackOnGutters;
			} }

		
		public ChartSettings()	{
			ChartColorBackground = Color.White;
			BarWidthIncludingPadding = 8;
			PanelNameAndSymbolFont = new Font("Microsoft Sans Serif", 8.25f);
			PriceColorBarUp = Color.RoyalBlue;
			PriceColorBarDown = Color.IndianRed;
			VolumeColorBarUp = Color.CadetBlue;
			VolumeColorBarDown = Color.CadetBlue;
			GutterRightColorBackground = Color.Gainsboro;
			GutterRightColorForeground = Color.Black;
			GutterRightFont = new Font("Consolas", 8f);
			GutterRightPadding = 5;
			GutterBottomColorBackground = Color.Gainsboro;
			GutterBottomColorForeground = Color.Black;
			GutterBottomNewDateColorForeground = Color.Green;
			GutterBottomFont = new Font("Consolas", 8f);
			GutterBottomPadding = 2;
			GutterBottomDateFormatDayOpener = "ddd dd-MMM-yyyy";
			GutterBottomDateFormatIntraday = "HH:mm";
			GutterBottomDateFormatDaily = "ddd dd-MMM";
			GutterBottomDateFormatWeekly = "MMM-yy";
			GutterBottomDateFormatYearly = "yyyy";
			GridlinesHorizontalShow = true;
			GridlinesHorizontalColor = Color.WhiteSmoke;
			GridlinesVerticalShow = true;
			GridlinesVerticalColor = Color.WhiteSmoke;
			GridlinesVerticalNewDateColor = Color.Lime;
			ScrollSqueezeMouseDragSensitivityPx = 1;
			ScrollNBarsPerOneDragMouseEvent = 3;
			ScrollNBarsPerOneKeyPress = 1;
			SqueezeVerticalPaddingPx = 0;
			SqueezeVerticalPaddingStep = 1;		// in VerticalPixels (converted to Price differently for different symbols: priceRange/PanelHeight != const)
			SqueezeHorizontalStep = 1;			// in BarWidthPixels
			SqueezeHorizontalMouse1pxDistanceReceivedToOneStep = 5;
			SqueezeHorizontalKeyOnePressReceivedToOneStep = 1;
			TooltipPriceShow = true;
			TooltipPositionShow = true;
			TooltipPriceShowOnlyWhenMouseTouchesCandle = true;
			TooltipsPaddingFromBarLeftRightEdgesToAvoidMouseLeave = 3;		// MouseX will never go over tooltip => PanelNamedFolding.OnMouseLeave() never invoked
			PositionArrowPaddingVertical = 2;
			TooltipBordersMarginToKeepBordersVisible = 2;
			PositionPlannedEllipseColor = Color.Aqua;
			PositionPlannedEllipseColorAlpha = 90;
			PositionPlannedEllipseDiameter = 6;
			PositionFilledDotColor = Color.Olive;
			PositionFilledDotColorAlpha = 200;
			PositionFilledDotDiameter = 4;
			PositionLineHighlightedWidth = 2;
			PositionLineHighlightedAlpha = 230;
			PositionLineNoExitYetColor = Color.Gray;
			PositionLineNoExitYetColorAlpha = 100;
			PositionLineProfitableColor = Color.Green;
			PositionLineProfitableColorAlpha = 100;
			PositionLineLossyColor = Color.Salmon;
			PositionLineLossyColorAlpha = 100;
			PriceVsVolumeSplitterDistance = 0;
			AlertPendingEllipseColor = Color.Maroon;
			AlertPendingEllipseColorAlpha = 160;
			AlertPendingEllipseWidth = 1;
			MousePositionTrackOnGutters = true;
			MousePositionTrackOnGuttersColor = Color.LightGray;
		}
		
		public static Color ColorReverse(Color color) {
			int red = 255 - color.R;
			int green = 255 - color.G;
			int blue = 255 - color.B;
			return Color.FromArgb((int)red, (int)green, (int)blue);
		}
		public static Color ColorMakeTransparent(Color color, int alpha=255) {
			return Color.FromArgb(alpha, color.R, color.G, color.B);
		}
	}
}