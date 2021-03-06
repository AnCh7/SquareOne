using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Forms;

//using Microsoft.VisualBasic.Logging;
using Newtonsoft.Json;
using Sq1.Core.Accounting;

namespace Sq1.Core.Execution {
	[DataContract]
	public class Order {
		//public string GUID = Guid.NewGuid().ToString();
		[DataMember]
		public DateTime TimeCreatedBroker;
		[DataMember]
		public double PriceRequested;
		[DataMember]
		public double PriceFill;
		[DataMember]
		public double QtyRequested;
		[DataMember]
		public double QtyFill;

		[DataMember]
		public string GUID;
		[DataMember]
		public OrderState State;
		[DataMember]
		public DateTime StateUpdateLastTimeLocal;
		[DataMember]
		public int SernoSession;
		[DataMember]
		public long SernoExchange;

		[DataMember]
		public bool IsReplacement;
		[DataMember]
		public string ReplacementForGUID;
		[DataMember]
		public string ReplacedByGUID;

		[DataMember]
		public bool IsEmergencyClose;
		[DataMember]
		public int EmergencyCloseAttemptSerno;
		[DataMember]
		public string EmergencyReplacementForGUID;
		[DataMember]
		public string EmergencyReplacedByGUID;

		[DataMember]
		public bool IsKiller;
		[DataMember]
		public string VictimGUID;
		[JsonIgnore]
		public Order VictimToBeKilled;
		[DataMember]
		public string KillerGUID;
		[JsonIgnore]
		public Order KillerOrder;

		[DataMember]
		public DateTime DateServerLastFillUpdate;
		[DataMember]
		public bool FromAutoTrading { get; private set; }
		[DataMember]
		public double SlippageFill;
		[DataMember]
		public int SlippageIndex;
		[DataMember]
		public double CurrentAsk;
		[DataMember]
		public double CurrentBid;		// json.deserialize will put NULL when { get; private set; }
		[DataMember]
		public OrderSpreadSide SpreadSide;
		[DataMember]
		public string PriceSpreadSideAsString {
			get {
				string ret = "";
				switch (this.SpreadSide) {
					case OrderSpreadSide.AskCrossed:
					case OrderSpreadSide.AskTidal:
						ret = CurrentAsk + " " + SpreadSide;
						break;
					case OrderSpreadSide.BidCrossed:
					case OrderSpreadSide.BidTidal:
						ret = CurrentBid + " " + SpreadSide;
						break;
					default:
						ret = SpreadSide + " bid[" + CurrentBid + "] ask[" + CurrentAsk + "]";
						break;
				}
				return ret;
			}
		}
		//[JsonIgnore]
		[DataMember]
		public Alert Alert;		// json.deserialize will put NULL when { get; private set; }

		
		[JsonIgnore]
		// why Concurrent: OrderProcessor adds while GUI reads (a copy); why Stack: ExecutionTree displays Messages RecentOnTop;
		// TODO: revert to List (with lock(privateLock) { messages.Add/Remove/Count}) when:
		//	1) ConcurrentQueue's Interlocked.CompareExchange<>() is slower than lock(privateLock),
		//	2) you'll need sorting by date/state BEFORE ExecutionTree (ExecutionTree simulates sorted lists by ),
		//	3) you'll prove that removing lock() won't cause "CollectionModifiedException"
		private ConcurrentQueue<OrderStateMessage> messages;
		
		//[JsonIgnore]
		[DataMember]
		// List because Json.Net doesn't serialize ConcurrentQueue as []; I wanted deserialization compability
		public List<OrderStateMessage> MessagesSerializationProxy {
			get {
				// don't return {new List(empty)} as the next line; if JsonConvert.DeserializeObject gets NULL it'll SET a deserialized list  
				if (this.messages.Count == 0) return null; 
				return new List<OrderStateMessage>(this.messages);
				//string msg = "JsonConvert.DeserializeObject gets the deserialized Messages exactly once and it should get NULL"
				//	+ "; never access MessagesSafeCopy.set manually"
				//	+ "; it's a [DataMember] used by LogrotateSerializer<Order>.Deserialize() and .Serialize()";
				//throw new Exception(msg);
			}
			set {
				if (this.messages.Count > 0) {
					string msg = "JsonConvert.DeserializeObject sets the deserialized Messages exactly once"
						+ "; never access MessagesSafeCopy.set manually"
						+ "; it's a [DataMember] used by LogrotateSerializer<Order>.Deserialize() and .Serialize()";
					throw new Exception(msg);
					//return;
				}
				this.messages = new ConcurrentQueue<OrderStateMessage>(value);
			}
		}
		public ConcurrentQueue<OrderStateMessage> MessagesSafeCopy {
			get { return new ConcurrentQueue<OrderStateMessage>(this.messages); }
		}

		//public Position PositionFollowed;
		//public AccountPosition AccountPositionFollowed;

		// no search among lvOrders.Items[] is required to populate the order update
		public ListViewItem ListViewItemInExecutionForm;
		public int StateImageIndex;
		
		public List<Order> DerivedOrders;
		
		[DataMember]
		public List<string> DerivedOrdersGuids;/* {
			get {
				if (this.derivedOrdersGuids == null) return null;
				var ret = new List<string>();
				foreach (Order order in this.DerivedOrders) {
					ret.Add(order.GUID);
				}
				return ret;
			}
			set {
				if (this.derivedOrdersGuids.Count > 0) return;	//Json restored while we've had non-empty
				this.derivedOrdersGuids = value;
			}
		}
		private List<string> derivedOrdersGuids;*/
		
		public Order DerivedFrom;

		public bool RebuildDerivedOrdersGuids() {
			List<string> backup = this.DerivedOrdersGuids;
			this.DerivedOrdersGuids = new List<string>();
			foreach (Order order in this.DerivedOrders) {
				this.DerivedOrdersGuids.Add(order.GUID);
			}
			return backup.Count != this.DerivedOrders.Count;
		}
		
		public Order FindOrderGuidAmongDerivedsRecursively(string Guid) {
			Order ret = null;
			foreach (Order derived in this.DerivedOrders) {
				if (derived.GUID != Guid) continue;
				ret = derived;
				break;
			}
			
			Order foundAmongChildrenOfDerived = null;
			if (ret == null) {
				foreach (Order derived in this.DerivedOrders) {
					foundAmongChildrenOfDerived = derived.FindOrderGuidAmongDerivedsRecursively(Guid);
					if (foundAmongChildrenOfDerived == null) continue;
					break;
				}
				if (foundAmongChildrenOfDerived != null) ret = foundAmongChildrenOfDerived; 
			}
			return ret;
		}
		
		// TODO_OrderStateCollections: REFACTOR
		// states to be better named/handled in OrderStateCollections.cs
		public bool CanBeReplacedLimitStop {
			get {
				return this.State == OrderState.Active
					|| this.State == OrderState.Submitted
					|| this.State == OrderState.FilledPartially;
			}
		}
		public bool ExpectingCallbackFromBroker {
			get {
				return this.State == OrderState.Active
					|| this.State == OrderState.Submitting
					|| this.State == OrderState.Submitted
					|| this.State == OrderState.KillPending;
			}
		}
		public bool StateChangeableToSubmitted {
			get {
				if (this.State == OrderState.PreSubmit
					|| this.State == OrderState.AlertCreatedOnPreviousBarNotAutoSubmitted
					|| this.State == OrderState.AutoSubmitNotEnabled
					//&& this._IsLoggedInOrPaperAccount(current3)
					) {
					return true;
				}
				return false;
			}
		}
		public bool InEmergencyState {
			get {
				if (this.State == OrderState.EmergencyCloseSheduledForRejected
						|| this.State == OrderState.EmergencyCloseSheduledForRejectedLimitReached
						|| this.State == OrderState.EmergencyCloseSheduledForErrorSubmittingBroker) {
					return true;
				}
				return false;
			}
		}
		public OrderState ComplementaryEmergencyStateForError {
			get {
				OrderState newState = OrderState.Error;
				if (this.State == OrderState.Rejected) newState = OrderState.EmergencyCloseSheduledForRejected;
				if (this.State == OrderState.RejectedLimitReached) newState = OrderState.EmergencyCloseSheduledForRejectedLimitReached;
				if (this.State == OrderState.ErrorSubmittingBroker) newState = OrderState.EmergencyCloseSheduledForErrorSubmittingBroker;
				return newState;
			}
		}
		public bool stateChangeableToEmergency {
			get {
				return (ComplementaryEmergencyStateForError != OrderState.Error);
			}
		}
		// /TODO_OrderStateCollections: REFACTOR



		public string ExtendedOrderType;
		public int AddedToOrdersListCounter;
		public string OrderExtendedOrMarketLimitStopAsString {
			get {
				if (this.ExtendedOrderType != "") {
					return this.ExtendedOrderType;
				}
				return this.Alert.MarketLimitStop.ToString();
			}
		}
		public string LastMessage {
			get {
				int count = this.messages.Count; 
				if (count == 0) return "";
				OrderStateMessage lastOmsg;
				bool success = this.messages.TryPeek(out lastOmsg);
				if (!success) {
					throw new Exception("messages.TryPeek() failed while messages.Count[" + count + "/" + messages.Count + "]");
				}
				return lastOmsg.Message; 
			}
		}
		public bool hasSlippagesDefined {
			get {
				int slippageIndexMax = this.Alert.Bars.SymbolInfo.getSlippageIndexMax(this.Alert.Direction);
				return (slippageIndexMax == -1) ? false : true;
			}
		}
		public bool noMoreSlippagesAvailable {
			get {
				string msg = "slippagesNotDefinedOr?";
				int slippageIndexMax = this.Alert.Bars.SymbolInfo.getSlippageIndexMax(this.Alert.Direction);
				if (slippageIndexMax == -1) return false;
				return (this.SlippageIndex > slippageIndexMax) ? true : false;
			}
		}
		public bool EmergencyCloseAttemptSernoExceedLimit {
			get {
				if (this.Alert.Bars.SymbolInfo.EmergencyCloseAttemptsMax <= 0) return false;
				if (this.EmergencyCloseAttemptSerno > this.Alert.Bars.SymbolInfo.EmergencyCloseAttemptsMax) {
					return true;
				}
				return false;
			}
		}

		private static int absno = 0;
		public double CommissionFill;
		public static string newGUID() {
			string ret = DateTime.Now.ToString("Hmmssfff");
			try {
				int noLeadingZero = Int32.Parse(ret);
				noLeadingZero += ++absno;
				ret = noLeadingZero.ToString();
			} catch (Exception e) {
				int a = 1;
			}
			return ret;
		}

		public ManualResetEvent MreActiveCanCome { get; protected set; }

 		public Order() {	// called by Json.Deserialize(); what if I'll make it protected?
			this.GUID = newGUID();
			this.messages = new ConcurrentQueue<OrderStateMessage>();
			this.ExtendedOrderType = "";
			this.PriceRequested = 0;
			this.PriceFill = 0;
			this.QtyRequested = 0;
			this.QtyFill = 0;
			//this.PositionFollowed = null;
			//this.AccountPositionFollowed = null;

			this.State = OrderState.Unknown;
			this.SernoSession = 0;		//QUIK
			this.SernoExchange = 0;		//QUIK

			this.IsReplacement = false;
			this.ReplacementForGUID = "";
			this.ReplacedByGUID = "";

			this.IsKiller = false;
			this.VictimGUID = "";
			this.KillerGUID = "";

			this.StateImageIndex = 0;
			this.StateUpdateLastTimeLocal = DateTime.MinValue;
			this.FromAutoTrading = false;
			this.SlippageFill = 0;
			this.SlippageIndex = 0;
			this.CurrentAsk = 0;
			this.CurrentBid = 0;
			this.SpreadSide = OrderSpreadSide.Unknown;
			this.MreActiveCanCome = new ManualResetEvent(false);
			this.DerivedOrders = new List<Order>();
			this.DerivedOrdersGuids = new List<string>();
		}
		public Order(Alert alert, bool fromAutoTrading,
				bool forceOverwriteAlertOrderFollowedToNewlyCreatedOrder = false) : this() {
			if (alert == null) {
				string msg = "Order(): alert=null (serializer will get upset) for " + this.ToString();
				throw new Exception(msg);
			}
			if (alert.OrderFollowed != null && forceOverwriteAlertOrderFollowedToNewlyCreatedOrder == false) {
				string msg = "I refuse to create one more order for an alert.OrderFollowed!=null; alert[" + alert + "] alert.OrderFollowed[" + alert.OrderFollowed + "]";
				alert.OrderFollowed.AppendMessage(msg);
				throw new Exception(msg);
			}
			this.PriceRequested = alert.PriceScript;
			this.QtyRequested = alert.Qty;
			this.FromAutoTrading = fromAutoTrading;
			//this.TimeCreatedServer = alert.TimeCreatedLocal;
			// due to serverTime lagging, replacements orders are born before the original order...
			this.TimeCreatedBroker = alert.Bars.MarketInfo.ConvertLocalTimeToServer(DateTime.Now);
			//this.PositionFollowed = new Position() when order.State becomes OrderState.Filled;
			this.SpreadSide = alert.OrderSpreadSide;
			//this.ExtendedOrderType = alert.ExtendedOrderType;

			// Bid/Ask Dictionaries are synchronized => no exceptions
			if (alert.DataSource != null && alert.DataSource.StreamingProvider != null) {
				this.CurrentBid = alert.DataSource.StreamingProvider.StreamingDataSnapshot.BestBidGetForMarketOrder(alert.Symbol);
				this.CurrentAsk = alert.DataSource.StreamingProvider.StreamingDataSnapshot.BestAskGetForMarketOrder(alert.Symbol);
			}

			this.Alert = alert;
			alert.OrderFollowed = this;
			
//MOVED_TO OrderProcessor::CreatePropagateOrderFromAlert() for {if (alert.IsExitAlert) alert.PositionAffected.EntryAlert.OrderFollowed.DerivedOrdersAdd(newborn);}
//			if (alert.PositionAffected != null && alert.PositionAffected.EntryAlert != null && alert.PositionAffected.EntryAlert.OrderFollowed != null) {
//				alert.PositionAffected.EntryAlert.OrderFollowed.DerivedOrdersAdd(this);
//				// TODO will also have to notify ExecutionForm on this Order, which will close a Position 
//			}
			alert.MreOrderFollowedIsNotNull.Set();	// Order is fully constructed, all properties assigned, go read them
		}
		public Order DeriveKillerOrder() {
			if (this.Alert == null) {
				string msg = "DeriveKillerOrder(): Alert=null (serializer will get upset) for " + this.ToString();
				throw new Exception(msg);
			}
			Order killer = new Order(this.Alert, this.FromAutoTrading, true);
			killer.State = OrderState.JustCreated;
			killer.PriceRequested = 0;
			killer.PriceFill = 0;
			killer.QtyRequested = 0;
			killer.QtyFill = 0;

			this.KillerOrder = killer;
			this.KillerGUID = killer.GUID;
			killer.VictimToBeKilled = this;
			killer.VictimGUID = this.GUID;
			killer.Alert.SignalName = "Killer4: " + this.Alert.SignalName;
			killer.IsKiller = true;
			
			this.DerivedOrdersAdd(killer);
			
			return killer;
		}
		public Order DeriveReplacementOrder() {
			if (this.Alert == null) {
				string msg = "DeriveReplacementOrder(): Alert=null (serializer will get upset) for " + this.ToString();
				throw new Exception(msg);
			}
			Order replacement = new Order(this.Alert, this.FromAutoTrading, true);
			replacement.State = OrderState.JustCreated;
			replacement.SlippageIndex = this.SlippageIndex;
			replacement.ReplacementForGUID = this.GUID;
			this.ReplacedByGUID = replacement.GUID;
			
			this.DerivedOrdersAdd(replacement);

			return replacement;
		}
		public void DerivedOrdersAdd(Order killerReplacementPositionclose) {
			if (this.DerivedOrdersGuids.Contains(killerReplacementPositionclose.GUID)) {
				string msg = "ALREADY_ADDED DerivedOrder.GUID[" + killerReplacementPositionclose.GUID + "]";
				Assembler.PopupException(msg);
				return;
			}
			this.DerivedOrdersGuids.Add(killerReplacementPositionclose.GUID);
			this.DerivedOrders.Add(killerReplacementPositionclose);
			killerReplacementPositionclose.DerivedFrom = this;
		}

		public void AppendMessage(string msg) {
			this.AppendMessageSynchronized(new OrderStateMessage(this, msg));
		}
		public void AppendMessageSynchronized(OrderStateMessage omsg) {
			//this.messages.Push(omsg);
			this.messages.Enqueue(omsg);
		}
		public override string ToString() {
			string ret = "";
			ret += this.GUID + " ";
			if (this.IsKiller) ret += "KILLER_FOR ";
			if (this.Alert != null) {
				ret += this.Alert.ToStringForOrder();
			} else {
				ret += " this.Alert=null_CONSTRUCTOR_NOT_COMPLETE";
			}
			ret += " @ " + this.PriceRequested;
			ret += " " + this.State;
			if (this.SernoSession != 0) ret += " SernoSession[" + this.SernoSession + "]";
			//if (GUID != "") ret += " GUID[" + GUID + "]";
			//if (SernoExchange != 0) ret += " SernoExchange[" + SernoExchange + "]";
			if (this.QtyFill != 0.0) ret += " FillQty[" + this.QtyFill + "]";
			if (this.PriceFill != 0.0) ret += " PriceFilled[" + this.PriceFill + "]";
			//if (this.Alert.PriceDeposited != 0) ret += " PricePaid[" + this.Alert.PriceDeposited + "]";
			if (this.FromAutoTrading) ret += " FromAutoTrading";
			return ret;
		}
		public bool hasBrokerProvider(string callerMethod) {
			bool ret = true;
			string errormsg = "";
			if (this.Alert.DataSource == null) {
				errormsg += "order.Alert[" + this.Alert + "].DataSource property must be set ";
			}
			if (this.Alert.DataSource.BrokerProvider == null) {
				errormsg += "order.Alert.DataSource[" + this.Alert.DataSource + "].BrokerProvider property must be set ";
			}
			if (errormsg != "") {
				this.AppendMessage(callerMethod + errormsg);
				ret = false;
			}
			return ret;
		}
		public void FilledWith(double priceFill, double qtyFill, double slippageFill = 0, double commissionFill = 0) {
			this.PriceFill = priceFill;
			this.QtyFill = qtyFill;
			if (this.SlippageFill == 0 && slippageFill != 0) {
				this.SlippageFill = slippageFill;
			}
			if (this.CommissionFill == 0 && commissionFill != 0) {
				this.CommissionFill = commissionFill;
			}
		}
		public bool FindStateInOrderMessages(OrderState orderState, int occurenciesLooking = 1) {
			lock (this.messages) {
				int occurenciesFound = 0;
				foreach (OrderStateMessage osm in messages) {
					if (osm.State != orderState) continue;
					occurenciesFound++;
				}
				return (occurenciesFound >= occurenciesLooking);
			}
		}
	}
}