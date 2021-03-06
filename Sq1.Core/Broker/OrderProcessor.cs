using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Sq1.Core.Accounting;
using Sq1.Core.DataTypes;
using Sq1.Core.Execution;
using Sq1.Core.StrategyBase;
using Sq1.Core.Support;

namespace Sq1.Core.Broker {
	public class OrderProcessor {
		public bool AlwaysExitAllSharesInPosition;
		public IStatusReporter StatusReporter { get; protected set; }
		public OrderProcessorDataSnapshot DataSnapshot { get; private set; }
		public OrderProcessorEventDistributor EventDistributor { get; private set; }
		public OrderPostProcessorEmergency OPPemergency { get; private set; }
		public OrderPostProcessorRejected OPPrejected { get; private set; }
		public OrderPostProcessorSequencerCloseThenOpen OPPsequencer { get; private set; }
		public OrderPostProcessorStateChangedTrigger OPPstatusCallbacks { get; private set; }

		public OrderProcessor() {
			this.OPPsequencer = new OrderPostProcessorSequencerCloseThenOpen(this);
			this.OPPemergency = new OrderPostProcessorEmergency(this, this.OPPsequencer);
			this.OPPrejected = new OrderPostProcessorRejected(this);
			this.OPPstatusCallbacks = new OrderPostProcessorStateChangedTrigger(this);
			this.DataSnapshot = new OrderProcessorDataSnapshot(this);
			this.EventDistributor = new OrderProcessorEventDistributor(this);
		}

		public void Initialize(string rootPath, IStatusReporter mainForm) {
			//if (rootPath.EndsWith(Path.DirectorySeparatorChar) == false) rootPath += Path.DirectorySeparatorChar;
			this.DataSnapshot.Initialize(rootPath);
			this.StatusReporter = mainForm;
		}

		bool isExitOrderConsistentLogInconsistency(Order order) {
			bool exitOrderHasNoErrors = true;
			string errormsg = "";
			Position positionShouldBeFilled = order.Alert.PositionAffected;
			if (positionShouldBeFilled == null) {
				errormsg += "positionShouldBeFilled[" + positionShouldBeFilled + "]=null, ERROR filling order.Alert.PositionAffected !!! ";
				order.State = OrderState.IRefuseToCloseNonStreamingPosition;
			}
			if (positionShouldBeFilled.Shares <= 0) {
				errormsg += "Shares<=0 for positionShouldBeFilled[" + positionShouldBeFilled + "]; skipping PositionClose ";
				order.State = OrderState.IRefuseToCloseUnfilledEntry;
			}
			if (positionShouldBeFilled.EntryFilledPrice <= 0) {
				errormsg += "EntryPrice<=0 for positionShouldBeFilled[" + positionShouldBeFilled + "]; skipping PositionClose ";
				order.State = OrderState.IRefuseToCloseUnfilledEntry;
			}
			if (positionShouldBeFilled.EntryAlert == null) {
				errormsg += "EntryAlert=null for positionShouldBeFilled[" + positionShouldBeFilled + "]; won't close position opened in backtest closing while in streaming ";
				order.State = OrderState.IRefuseToCloseUnfilledEntry;
			}
			if (errormsg == "" && positionShouldBeFilled.EntryAlert.OrderFollowed == null) {
				errormsg += "EntryAlert.OrderFollowed=null for positionShouldBeFilled[" + positionShouldBeFilled + "]; won't close position opened in backtest closing while in streaming ";
				order.State = OrderState.IRefuseToCloseUnfilledEntry;
			}
			if (errormsg == "") {
				//if (positionShouldBeFilled.EntryAlert.OrderFollowed.StateFilledOrPartially == false) {
				//	errormsg += "EntryAlert.OrderFollowed.State[" + positionShouldBeFilled.EntryAlert.OrderFollowed.State + "]"
				//		+ " must be [Filled] or [Partially]; skipping PositionClose"
				//		//+ " for positionShouldBeFilled[" + positionShouldBeFilled + "]"
				//		;
				//	order.State = OrderState.IRefuseToCloseUnfilledEntry;
				//}
				if (positionShouldBeFilled.EntryAlert.OrderFollowed.QtyFill != positionShouldBeFilled.EntryAlert.Qty) {
					errormsg += "EntryAlert.OrderFollowed.QtyFill[" + positionShouldBeFilled.EntryAlert.OrderFollowed.QtyFill + "]"
							+ " EntryAlert.Qty[" + positionShouldBeFilled.EntryAlert.Qty + "]"
							+ "; skipping PositionClose"
							//+ " for positionShouldBeFilled[" + positionShouldBeFilled + "]"
							;
					//order.State = OrderState.IRefuseToCloseUnfilledEntry;
				}
			}
			if (errormsg != "") {
				order.AppendMessage(errormsg);
				exitOrderHasNoErrors = false;
			}
			return exitOrderHasNoErrors;
		}
		public Order CreatePropagateOrderFromAlert(Alert alert, bool setStatusSubmitting, bool fromAutoTrading) {
			if (alert.MarketLimitStop == MarketLimitStop.AtClose) {
				string msg = "NYI: alert.OrderType= OrderType.AtClose [" + alert + "]";
				throw new Exception(msg);
			}
			Order newborn = new Order(alert, fromAutoTrading, false);
			try {
				newborn.Alert.DataSource.BrokerProvider.ModifyOrderTypeAccordingToMarketOrderAs(newborn);
			} catch (Exception e) {
				string msg = "hoping that MarketOrderAs.MarketMinMax influenced order.Alert.MarketLimitStop["
					+ newborn.Alert.MarketLimitStop + "]=MarketLimitStop.Limit for further match; PREV=" + newborn.LastMessage;
				this.AppendOrderMessageAndPropagateCheckThrowOrderNull(newborn, msg);
			}
			if (alert.IsExitAlert) {
				if (this.isExitOrderConsistentLogInconsistency(newborn) == false) {
					this.DataSnapshot.OrderAddSynchronizedAndPropagate(newborn);
					string reason = newborn.LastMessage;
					return null;
				}
				//adjustExitOrderQtyRequestedToMatchEntry(order);
				alert.PositionAffected.EntryAlert.OrderFollowed.DerivedOrdersAdd(newborn);
			}

			OrderState newbornOrderState = OrderState.AutoSubmitNotEnabled;
			string newbornMessage = "alert[" + alert + "]";

			if (setStatusSubmitting == true) {
				if (newborn.hasBrokerProvider("CreatePropagateOrderFromAlert(): ") == false) {
					string msg = "CRAZY #61";
					Assembler.PopupException(msg);
					return null;
				}
				newbornOrderState = this.IsOrderEatable(newborn) ? OrderState.Submitting : OrderState.ErrorSubmittingNotEatable;
				//string isPastDue = newborn.Alert.IsAlertCreatedOnPreviousBar;
				//if (fromAutoTrading && String.IsNullOrEmpty(isPastDue) == false) {
				//	newbornMessage += "; " + isPastDue;
				//	newbornOrderState = OrderState.AlertCreatedOnPreviousBarNotAutoSubmitted;
				//}
			}
			this.UpdateOrderStateNoPostProcess(newborn, new OrderStateMessage(newborn, newbornOrderState, newbornMessage));
			this.DataSnapshot.OrderAddSynchronizedAndPropagate(newborn);
			this.DataSnapshot.SerializerLogrotateOrders.HasChangesToSave = true;
			return newborn;
		}
		public void CreateOrdersSubmitToBrokerProviderInNewThreadGroups(List<Alert> alertsBatch, bool setStatusSubmitting, bool fromAutoTrading) {
			if (alertsBatch.Count == 0) {
				string msg = "no alerts to Add; why did you call me? make sure you invoke using a synchronized Queue";
				Assembler.PopupException(msg);
				//return;
			}

			bool ordersClosingAllSameDirection = true;
			PositionLongShort ordersClosingPositionLongShort = PositionLongShort.Unknown;
			bool ordersOpeningAllSameDirection = true;
			PositionLongShort ordersOpeningPositionLongShort = PositionLongShort.Unknown;

			List<Order> ordersAgnostic = new List<Order>();
			List<Order> ordersClosing = new List<Order>();
			List<Order> ordersOpening = new List<Order>();
			BrokerProvider broker = null;
			foreach (Alert alert in alertsBatch) {
				// I only needed alert.OrderFollowed=newOrder... mb even CreatePropagateOrderFromAlert() should be reduced for backtest
				if (alert.Strategy.Script.Executor.Backtester.IsBacktestingNow) {
					continue;
				}

				Order newOrder;
				try {
					newOrder = CreatePropagateOrderFromAlert(alert, setStatusSubmitting, fromAutoTrading);
				} catch (Exception e) {
					this.PopupException(e);
					continue;
				}
				if (newOrder == null) {
					string msg = "CreatePropagateOrderFromAlert=null => nothing sent to BrokerProver"
						+ " and I should've removed alert[" + alert + "] from all pending collections";
					this.PopupException(new Exception(msg));
					continue;
				}
				if (newOrder.State != OrderState.Submitting) {
					if (newOrder.State == OrderState.AlertCreatedOnPreviousBarNotAutoSubmitted) {
						alert.Strategy.Script.Executor.CreatedOrderWontBePlacedPastDueInvokeScript(alert, alert.Bars.Count);
						continue;
					}
					if (newOrder.State == OrderState.AutoSubmitNotEnabled) continue;
					//if (newOrder.Alert.Strategy.Script.Executor.IsAutoSubmitting == true
					//&& newOrder.Alert.Strategy.Script.Executor.IsStreaming == true
					//&& newOrder.FromAutoTrading == true
					string msg = "Unexpected newOrder.State[" + newOrder.State + "] from CreatePropagateOrderFromAlert()";
					this.PopupException(new Exception(msg));
					continue;
				}
				if (broker == null) {
					broker = alert.DataSource.BrokerProvider;
				} else {
					if (broker != alert.DataSource.BrokerProvider) {
						throw new Exception("alertsBatch MUST contain alerts for the same broker"
							+ "; prevAlert.Broker[" + broker + "] while thisAlert.DataSource.BrokerProvider[" + alert.DataSource.BrokerProvider + "]");
					}
				}
				if (alert.Bars.SymbolInfo.SameBarPolarCloseThenOpen) {
					if (alert.IsExitAlert) {
						ordersClosing.Add(newOrder);
						if (ordersClosingPositionLongShort == PositionLongShort.Unknown) {
							ordersClosingPositionLongShort = newOrder.Alert.PositionLongShortFromDirection;
						} else {
							if (ordersClosingPositionLongShort != newOrder.Alert.PositionLongShortFromDirection) ordersClosingAllSameDirection = false;
						}
					} else {
						ordersOpening.Add(newOrder);
						if (ordersOpeningPositionLongShort == PositionLongShort.Unknown) {
							ordersOpeningPositionLongShort = newOrder.Alert.PositionLongShortFromDirection;
						} else {
							if (ordersOpeningPositionLongShort != newOrder.Alert.PositionLongShortFromDirection) ordersOpeningAllSameDirection = false;
						}
					}
				} else {
					ordersAgnostic.Add(newOrder);
				}
			}
			if (ordersAgnostic.Count == 0 && (ordersClosing.Count + ordersOpening.Count) == 0) {
				string msg = "NO_ORDERS_TO_SUBMIT (did you turn Submit=Off?...)"
					+ " newBornOrdersToSubmit.Count=0 while alertsBatch.Count[" + alertsBatch.Count + "]>0 "
					+ ": ordersAgnostic.Count[" + ordersAgnostic.Count + "]"
					+ " && ordersClosing.Count[" + ordersClosing.Count + "]"
					+   "  ordersOpening.Count[" + ordersOpening.Count + "]";
				throw new Exception(msg);
			}

			if (ordersAgnostic.Count > 0 && (ordersClosing.Count > 0 || ordersOpening.Count > 0)) {
				string msg = "got mix of orderAware/Agnostic securities in AlertsBatch"
					+ "ordersAgnostic[" + ordersAgnostic.Count + "] :: ordersClosing[" + ordersClosing.Count
					+ "] ordersOpening[" + ordersOpening.Count+ "]";
				throw new Exception(msg);
			}
			if (ordersAgnostic.Count > 0) {
				string msg = "Scheduling SubmitOrdersThreadEntry ordersAgnostic[" + ordersAgnostic.Count + "] through [" + broker + "]";
				//this.PopupException(new Exception(msg));
				ThreadPool.QueueUserWorkItem(new WaitCallback(broker.SubmitOrdersThreadEntry), new object[] { ordersAgnostic });
				return;
			}
			if (ordersClosing.Count > 0 && ordersOpening.Count == 0) {
				string msg = "Scheduling SubmitOrdersThreadEntry ordersClosing[" + ordersClosing.Count + "] through [" + broker + "]";
				//this.PopupException(new Exception(msg));
				ThreadPool.QueueUserWorkItem(new WaitCallback(broker.SubmitOrdersThreadEntry), new object[] { ordersClosing });
				return;
			}
			if (ordersClosing.Count == 0 && ordersOpening.Count > 0) {
				string msg = "Scheduling SubmitOrdersThreadEntry ordersOpening[" + ordersOpening.Count + "] through [" + broker + "]";
				//this.PopupException(new Exception(msg));
				ThreadPool.QueueUserWorkItem(new WaitCallback(broker.SubmitOrdersThreadEntry), new object[] { ordersOpening });
				return;
			}

			if (ordersClosingAllSameDirection == true && ordersOpeningAllSameDirection == true) {
				if (ordersClosingPositionLongShort != ordersOpeningPositionLongShort) {
					this.OPPsequencer.InitializeSequence(ordersClosing, ordersOpening);
					string msg = "Scheduling SubmitOrdersThreadEntry ordersClosing[" + ordersClosing.Count
						+ "] through [" + broker + "], then  ordersOpening[" + ordersOpening.Count + "]";
					//this.PopupException(new Exception(msg));
					ThreadPool.QueueUserWorkItem(new WaitCallback(broker.SubmitOrdersThreadEntry), new object[] { ordersClosing });
					return;
				} else {
					List<Order> ordersMerged = new List<Order>(ordersClosing);
					ordersMerged.AddRange(ordersOpening);
					string msg = "Scheduling SubmitOrdersThreadEntry ordersMerged[" + ordersMerged.Count + "] through [" + broker + "]";
					//this.PopupException(new Exception(msg));
					ThreadPool.QueueUserWorkItem(new WaitCallback(broker.SubmitOrdersThreadEntry), new object[] { ordersMerged });
					return;
				}
			} else {
				string msg = "DANGEROUS MIX, NOT OPTIMIZED: Scheduling SubmitOrdersThreadEntry ordersAgnostic[" + ordersAgnostic.Count + "] through [" + broker + "]"
					+ " (ordersClosingAllSameLongOrShort=[" + ordersClosingAllSameDirection + "]"
					+ " && ordersOpeningAllSameLongOrShort=[" + ordersClosingAllSameDirection + "]"
					+ " && ordersClosingPositionLongShort[" + ordersClosingPositionLongShort + "]"
					+ " != ordersOpeningPositionLongShort[" + ordersOpeningPositionLongShort + "]) == FALSE";
				Assembler.PopupException(msg);
			}
		}
		public void SubmitEatableOrdersFromGui(List<Order> orders) {
			List<Order> ordersEatable = new List<Order>();
			foreach (Order order in orders) {
				if (this.IsOrderEatable(order) == false) continue;
				ordersEatable.Add(order);
				string msg = "Submitting Eatable Order From Gui";
				OrderStateMessage newOrderState = new OrderStateMessage(order, OrderState.Submitting, msg);
				this.UpdateOrderStateAndPostProcess(order, newOrderState);
			}
			if (ordersEatable.Count > 0) {
				BrokerProvider broker = extractSameBrokerProviderThrowIfDifferent(ordersEatable, "SubmitEatableOrders(): ");
				broker.SubmitOrders(ordersEatable);
			}
			this.DataSnapshot.SerializerLogrotateOrders.HasChangesToSave = true;
			this.DataSnapshot.UpdateActiveOrdersCountEvent();
		}
		public BrokerProvider extractSameBrokerProviderThrowIfDifferent(List<Order> orders, string callerMethod) {
			BrokerProvider broker = null;
			foreach (Order order in orders) {
				if (order.hasBrokerProvider(callerMethod) == false) {
					string msg = "CRAZY #64";
					Assembler.PopupException(msg);
					continue;
				}
				if (broker == null) broker = order.Alert.DataSource.BrokerProvider;
				if (broker != order.Alert.DataSource.BrokerProvider) {
					throw new Exception(callerMethod + "NIY: orderProcessor can not handle orders for several brokers"
						+ "; prevOrder.Broker[" + broker + "] while someOrderBroker[" + order.Alert.DataSource.BrokerProvider + "]");
				}
			}
			return broker;
		}
		bool IsOrderEatable(Order order) {
			if (order.Alert.Strategy == null) return true;
			if (order.IsKiller) return true;
			if (order.Alert.Direction == Direction.Sell || order.Alert.Direction == Direction.Cover) {
				return true;
			}
			Account account = null;
			if (account == null) return true;
			if (account.CashAvailable <= 0) {
				string msg = "ACCOUNT_CASH_ZERO";
				OrderStateMessage newOrderState = new OrderStateMessage(order, OrderState.ErrorOrderInconsistent, msg);
				this.UpdateOrderStateAndPostProcess(order, newOrderState);
				return false;
			}
			return true;
		}

		public void PopupException(Exception exception) {
			if (this.StatusReporter == null) return;
			this.StatusReporter.PopupException(exception);
		}

		public Order CreateKillerOrder(Order victimOrder) {
			string msig = "CreateKillerOrder(): ";
			if (victimOrder == null) {
				string msg = "victimOrder == null why did you call me?";
				Assembler.PopupException(msg);
				return null;
			}
			if (victimOrder.hasBrokerProvider(msig) == false) {
				string msg = "CRAZY #62";
				Assembler.PopupException(msg);
				return null;
			}
			//this.RemovePendingAlertsForVictimOrderMustBePostKill(victimOrder, msig);

			Order killerOrder = victimOrder.DeriveKillerOrder();
			DateTime serverTimeNow = victimOrder.Alert.Bars.MarketInfo.ConvertLocalTimeToServer(DateTime.Now);
			killerOrder.TimeCreatedBroker = serverTimeNow;
			this.DataSnapshot.OrderAddSynchronizedAndPropagate(killerOrder);
			this.EventDistributor.RaiseOrderReplacementOrKillerCreatedForVictim(victimOrder);
			this.DataSnapshot.SerializerLogrotateOrders.HasChangesToSave = true;
			this.DataSnapshot.UpdateActiveOrdersCountEvent();
			return killerOrder;
		}
		public void KillAll() {
			//List<Order> allOrdersClone = new List<Order>();
			//this.KillSelectedOrders(allOrdersClone);
			Assembler.PopupException("DOESNT_MAKE_SENSE_NOT_IMPLEMETED KillAll()");
		}
		public void KillSelectedOrders(List<Order> orders) {
			if (orders.Count == 0) return;
			List<Order> ordersToCancel = new List<Order>();
			for (int i = orders.Count - 1; i >= 0; i--) {
				Order victimOrder = orders[i];
				this.KillOrderUsingKillerOrder(victimOrder);
			}
			this.DataSnapshot.SerializerLogrotateOrders.HasChangesToSave = true;
			this.DataSnapshot.UpdateActiveOrdersCountEvent();
		}
		public void KillOrderUsingKillerOrder(Order victimOrder) {
			Order killerOrder = this.CreateKillerOrder(victimOrder);
			//killerOrder.FromAutoTrading = false;
			if (killerOrder.hasBrokerProvider("KillOrder():") == false) {
				string msg = "CRAZY #63";
				Assembler.PopupException(msg);
				return;
			};
			string msgVictim = "expecting callback on successful KILLER completion [" + killerOrder + "]";
			OrderStateMessage newOrderStateVictim = new OrderStateMessage(victimOrder, OrderState.KillPending, msgVictim);
			this.UpdateOrderStateAndPostProcess(victimOrder, newOrderStateVictim);

			killerOrder.Alert.DataSource.BrokerProvider.OrderKillSubmitUsingKillerOrder(killerOrder);
		}
		public void CancelStrategyOrders(string account, Strategy strategy, string symbol, BarScaleInterval dataScale) {
			try {
				Exception e = new Exception("just for call stack trace");
				throw e;
			} catch (Exception ex) {
				string msg = "I won't cancel any orders; reconsider application architecture";
				Assembler.PopupException(msg, ex);
			}
		}
		public void CancelAllPending() {
			List<Order> ordersToCancel = new List<Order>();
			//TODO: refactor!!!
			//lock (this.OrdersLock) {
			//	foreach (Order order in this.Orders) {
			//		switch (order.State) {
			//			case OrderState.Submitted:
			//			case OrderState.Active:
			//			case OrderState.FilledPartially:
			//				//if (order.State == OrderState.Submitted) {
			//				//	order.FromAutoTrading = false;
			//				//	log.Error("I didn't set order[" + order.ToString() + "].FromAutoTrading=false because order.Status=Submitted during CancelAll()");
			//				//}
			//				OrderStateMessage newOrderState = new OrderStateMessage(order, OrderState.KillPending,
			//					"CancelAll(Active|FilledPartially): expecting callback on successful KILLER completion");
			//				this.UpdateOrderStateAndPostProcess(order, newOrderState, order.PriceRequested, order.QtyFill);
			//				//if (this.OrderStateChangedByExecution != null) {
			//				//	this.OrderStateChangedByExecution(this, new OrderEventArgs(order));
			//				//}
			//				ordersToCancel.Add(order);
			//				break;

			//			default:
			//				log.Fatal("CancelAllPending(): doing nothing with order[" + order + "] because order.State[" + order.State + "] is not within {Submitted,Active,FilledPartially}");
			//				break;
			//		}
			//	}
			//}
			if (ordersToCancel.Count == 0) {
				return;
			}
			BrokerProvider broker = extractSameBrokerProviderThrowIfDifferent(ordersToCancel, "CancelAll(): ");
			//broker.KillSelectedOrders(ordersToCancel);
			//this.DataSnapshot.UpdateActiveOrdersCountEvent();
			this.KillSelectedOrders(ordersToCancel);
		}
		public void CancelReplaceOrder(Order orderToReplace, Order orderReplacement) {
			string msgVictim = "expecting callback on successful REPLACEMENT completion [" + orderReplacement + "]";
			OrderStateMessage newOrderStateVictim = new OrderStateMessage(orderToReplace, OrderState.KillPending, msgVictim);
			this.UpdateOrderStateAndPostProcess(orderToReplace, newOrderStateVictim);

			orderReplacement.State = OrderState.Submitted;
			orderReplacement.IsReplacement = true;
			this.DataSnapshot.OrderAddSynchronizedAndPropagate(orderReplacement);

			if (orderToReplace.hasBrokerProvider("CancelReplaceOrder(): ") == false) {
				string msg = "CRAZY #65";
				Assembler.PopupException(msg);
				return;
			}
			orderToReplace.Alert.DataSource.BrokerProvider.CancelReplace(orderToReplace, orderReplacement);

			this.DataSnapshot.SerializerLogrotateOrders.HasChangesToSave = true;
			this.DataSnapshot.UpdateActiveOrdersCountEvent();
		}

		public Order UpdateOrderStateByGuidNoPostProcess(string orderGUID, OrderState orderState, string message) {
			Order orderFound = this.DataSnapshot.OrdersSubmitting.FindByGUID(orderGUID);
			if (orderFound == null) {
				 orderFound = this.DataSnapshot.OrdersAll.FindByGUID(orderGUID);
			}
			if (orderFound == null) {
				string msg = "order[" + orderGUID + "] wasn't found; OrderProcessorDataSnapshot.OrderCount=[" + this.DataSnapshot.OrderCountThreadSafe + "]";
				throw new Exception(msg);
				//log.Fatal(msg, new Exception(msg));
				//return;
			}
			OrderState orderStateAbsorbed = (orderState == OrderState.LeaveTheSame) ? orderFound.State : orderState;
			if (orderStateAbsorbed != orderFound.State) {
				OrderStateMessage osm = new OrderStateMessage(orderFound, orderStateAbsorbed, message);
				UpdateOrderStateNoPostProcess(orderFound, osm);
			} else {
				this.AppendOrderMessageAndPropagateCheckThrowOrderNull(orderFound, message);
			}
			return orderFound;
		}
		void appendOrderMessageAndPropagate(Order order, OrderStateMessage omsg) {
			//log.Debug(omsg.Message);
			if (string.IsNullOrEmpty(omsg.Message)) {
				int a = 1;
			}
			omsg.Order.AppendMessageSynchronized(omsg);
			this.EventDistributor.RaiseOrderMessageAddedExecutionFormNotification(this, omsg);
		}
		public void AppendOrderMessageAndPropagateCheckThrowOrderNull(Order order, string msg) {
			if (order == null) {
				throw new Exception("order=NULL! you don't want to get NullPointerException and debug it");
			}
			OrderStateMessage omsg = new OrderStateMessage(order, order.State, msg);
			this.appendOrderMessageAndPropagate(order, omsg);
		}

		void PostProcessVictimOrder(Order victimOrder, OrderStateMessage newStateOmsg) {
			this.UpdateOrderStateNoPostProcess(victimOrder, newStateOmsg);
			switch (victimOrder.State) {
				case OrderState.KillPending:
				case OrderState.SLAnnihilated:
				case OrderState.TPAnnihilated:
					break;
				case OrderState.Submitting:
				case OrderState.Active:
				case OrderState.Filled:
					break;
				case OrderState.Killed:
					if (victimOrder.FindStateInOrderMessages(OrderState.SLAnnihilated)) {
						this.UpdateOrderStateNoPostProcess(victimOrder,
							new OrderStateMessage(victimOrder, OrderState.SLAnnihilated,
								"Setting State to the reason why it was killed"));
					}
					if (victimOrder.FindStateInOrderMessages(OrderState.TPAnnihilated)) {
						this.UpdateOrderStateNoPostProcess(victimOrder,
							new OrderStateMessage(victimOrder, OrderState.TPAnnihilated,
								"Setting State to the reason why it was killed"));
					}

					Order killerOrder = victimOrder.KillerOrder;
					this.UpdateOrderStateNoPostProcess(killerOrder,
						new OrderStateMessage(killerOrder, OrderState.KillerDone,
							"Victim.Killed => Killer.KillerDone"));
					break;
				default:
					string msg = "no handler for victimOrder[" + victimOrder + "]'s state[" + victimOrder.State + "]"
						+ "your BrokerProvider should call for Victim.States:{"
						//+ OrderState.KillSubmitting + ","
						+ OrderState.KillPending + ","
						//+ OrderState.Killed + ","
						//+ OrderState.SLAnnihilated + ","
						//+ OrderState.TPAnnihilated + "}";
						;
					throw new Exception(msg);
					break;
			}
		}
		void PostProcessKillerOrder(Order killerOrder, OrderStateMessage newStateOmsg) {
			switch (killerOrder.State) {
				case OrderState.JustCreated:
				case OrderState.KillerPreSubmit:
				case OrderState.KillerSubmitting:
				case OrderState.KillerBulletFlying:
				case OrderState.KillerDone:
					this.UpdateOrderStateNoPostProcess(killerOrder, newStateOmsg);
					break;
				default:
					string msg = "no handler for killerOrder[" + killerOrder + "]'s state[" + killerOrder.State + "]"
						+ "your BrokerProvider should call for Killer.States:{"
						+ OrderState.KillerBulletFlying + ","
						+ OrderState.KillerDone + "}";
					break;
					//throw new Exception(msg);
			}
		}
		public void UpdateOrderStateNoPostProcess(Order order, OrderStateMessage newStateOmsg) {
			if (newStateOmsg.Order != order) {
				string msg = "sorry for athavism, but OrderStateMessage.Order should be equal to order here";
				throw new Exception(msg);
			}
			if (order == null) {
				string msg = "how come ORDER=NULL?";
			}
			if (order.Alert == null) {
				string msg = "how come ORDER.AlertNULL?";
			}
			if (order.Alert.OrderFollowed != order) {
				string msg = "order.Alert.OrderFollowed[" + order.Alert.OrderFollowed + "] != order[" + order + "]";
				//throw new Exception(msg);
			}
			if (order.State == newStateOmsg.State) {
				string msg = "Replace with AppendOrderMessage()! UpdateOrderStateNoPostProcess(): got the same OrderState[" + order.State + "]?";
				throw new Exception(msg);
			}

			if (newStateOmsg.State == OrderState.Active) {
				bool signalled = order.MreActiveCanCome.WaitOne(-1);
			}

			OrderState orderStatePriorToUpdate = order.State;
			order.State = newStateOmsg.State;
			order.StateUpdateLastTimeLocal = newStateOmsg.DateTime;
			this.DataSnapshot.SwitchLanesForOrderPostStatusUpdate(order, orderStatePriorToUpdate);

			this.EventDistributor.RaiseOrderStateChanged(this, order);
			this.appendOrderMessageAndPropagate(order, newStateOmsg);

			if (order.State == OrderState.Submitted) {
				order.MreActiveCanCome.Set();
			}
		}
		public void UpdateOrderStateAndPostProcess(Order order, OrderStateMessage newStateOmsg, double priceFill = 0, double qtyFill = 0) {
			string msig = "UpdateOrderStateAndPostProcess(): ";

			try {
				if (order.State == OrderState.Killed) {
					int a = 1;
					// crawl in debugger if I can find VictimOrder.Alert.Executor.Script.OnAlertKilledCallback
					//this.RemovePendingAlertsForVictimOrderMustBePostKill(order, msig);
				}

				if (order.VictimToBeKilled != null) {
					this.PostProcessKillerOrder(order, newStateOmsg);
					return;
				}
				if (order.KillerOrder != null) {
					this.PostProcessVictimOrder(order, newStateOmsg);
					return;
				}

				if (order.hasBrokerProvider("handleOrderStatusUpdate():") == false) {
					string msg = "most likely QuikTerminal.CallbackOrderStatus got something wrong...";
					Assembler.PopupException(msg);
					return;
				}


				if (newStateOmsg.State == OrderState.Rejected && order.State == OrderState.EmergencyCloseLimitReached) {
					string prePostErrorMsg = "BrokerProvider CALLBACK DUPE: Status[" + newStateOmsg.State + "] delivered for EmergencyCloseLimitReached "
						//+ "; skipping PostProcess for [" + order + "]"
						;
					this.AppendOrderMessageAndPropagateCheckThrowOrderNull(order, prePostErrorMsg);
					return;
				}
				if (newStateOmsg.State == OrderState.Rejected && order.InEmergencyState) {
					string prePostErrorMsg = "BrokerProvider CALLBACK DUPE: Status[" + newStateOmsg.State + "] delivered for"
						+ " order.inEmergencyState[" + order.State + "] "
						//+ "; skipping PostProcess for [" + order + "]"
						;
					this.AppendOrderMessageAndPropagateCheckThrowOrderNull(order, prePostErrorMsg);
					return;
				}

				OrderCallbackDupesChecker dupesChecker = order.Alert.DataSource.BrokerProvider.OrderCallbackDupesChecker;
				if (dupesChecker != null) {
					string why = dupesChecker.OrderCallbackDupeResonWhy(order, newStateOmsg, priceFill, qtyFill);
					if (string.IsNullOrEmpty(why) == false) {
						string msgChecker = "OrderCallbackDupeResonWhy[" + why + "]; skipping PostProcess for [" + order + "]";
						this.AppendOrderMessageAndPropagateCheckThrowOrderNull(order, msgChecker);
						return;
					}
				}

				if (newStateOmsg.State == OrderState.Filled) {
					int a = 1;
				}

				/*if (qtyFill != 0) {
					if (order.QtyFill == 0 || order.QtyFill == -999) {
						order.QtyFill = qtyFill;
					} else {
						if (order.QtyFill != qtyFill && order.QtyFill != -qtyFill) {
							string msg = "got qtyFill[" + qtyFill + "] while order.QtyFill=[" + order.QtyFill
								+ "]; skipping update; trying to figure out why Filled orders get ZERO price from QUIK";
							order.AppendMessage(msg);
						}
					}
				}*/
				if (priceFill != 0) {
					if (order.PriceFill == 0 || order.PriceFill == -999.99) {
						order.PriceFill = priceFill;
						order.QtyFill = qtyFill;
					} else {
						bool marketWasSubstituted = order.Alert.MarketLimitStop == MarketLimitStop.Limit
								&& order.Alert.Bars.SymbolInfo.MarketOrderAs == MarketOrderAs.MarketMinMaxSentToBroker;
						if (order.PriceFill != priceFill && marketWasSubstituted == false) {
							string msg = "got priceFill[" + priceFill + "] while order.PriceFill=[" + order.PriceFill + "]"
								+ "; weird for Order.Alert.MarketLimitStop=[" + order.Alert.MarketLimitStop + "]";
							order.AppendMessage(msg);
						}
					}
				}
				this.UpdateOrderStateNoPostProcess(order, newStateOmsg);
				this.PostProcessOrderState(order, priceFill, qtyFill);
			} catch (Exception e) {
				string msg = "trying to figure out why SL is not getting placed - we didn't reach PostProcess??";
				this.PopupException(new Exception(msg, e));
			}
		}
		void PostProcessAccounting(Order order, double qtyFill) {
			if (order.Alert.Direction == Direction.Unknown) {
				string msg = "Direction.Unknown can't be here; Unknown is default for Deserialization errors!";
				Assembler.PopupException(msg);
			}
			//moved to Order.FillPositionAffected() to make MarketSim to fill without orderProcessor
			//if (order.Alert.PositionAffected != null) { 	// alert.PositionAffected = null when order created by chart-click-mni
			//	if (order.Alert.isEntryAlert) {
			//		order.Alert.PositionAffected.EntryFilledWith(order.PriceFill, order.SlippageFill, 0);
			//	} else {
			//		order.Alert.PositionAffected.ExitFilledWith(order.PriceFill, order.SlippageFill, 0);
			//	}
			//} else {
			//	log.Fatal("NO POSITION AFFECTED; order[" + order + "] alert[" + order.Alert + "]");
			//}
			// FIXME: UNCOMMENT AND FIX DataSource == null here...
			/*
			Account account = this.DataSnapshot.FindAccountByNumber(order.Alert.AccountNumber);
			if (account == null) {
				string msg = "Account not found for order[" + order.ToString() + "]";
				log.Fatal(msg);
				//throw new Exception(msg);
			} else {
				AccountPosition positionAlready = this.DataSnapshot.DataSnapshot.FindAccountPositionForOrder(order);
				if (positionAlready != null) {
					double _SharesFilledDiff = qtyFill - order.QtyFill;
					if (order.Alert.PositionLongShortFromDirection == PositionLongShort.Short) _SharesFilledDiff = -_SharesFilledDiff;
					log.Warn("Adding Shares[" + _SharesFilledDiff + "] to existing Position[" + positionAlready + "]");
					positionAlready.QtyFill += _SharesFilledDiff;
					// FIXME: UNCOMMENT AND FIX DataSource == null here...
					order.Alert.DataSource.BrokerProvider.AccountPositionModified(account);
					if (this.AccountPositionChanged != null) {
						this.AccountPositionChanged(this, new AccountPositionEventArgs(positionAlready));
					}
				} else {
					AccountPosition positionNew = new AccountPosition(order);
					log.Info("Adding new Position[" + positionNew + "]");
					positionNew.Account = account;
					account.Positions.Add(positionNew);
					// FIXME: UNCOMMENT AND FIX DataSource == null here...
					order.Alert.DataSource.BrokerProvider.AccountPositionAdded(account);
					if (this.AccountPositionAdded != null) {
						this.AccountPositionAdded(this, new AccountPositionEventArgs(positionNew));
					}
				}
			}*/
		}
		public void PostProcessOrderState(Order order, double priceFill, double qtyFill) {
			string msig = "PostProcessOrderState(): ";
			string msgException = order.State + " " + order.LastMessage;
			//if (order.Alert.isExitAlert || order.IsEmergencyClose) {
			//	order.State = OrderState.Rejected;
			//}
			//if (order.Alert.isEntryAlert && order.State == OrderState.Rejected) {
			//	order.State = OrderState.Filled;
			//}
			switch (order.State) {
				case OrderState.Filled:
				case OrderState.FilledPartially:
					order.FilledWith(priceFill, qtyFill);
					this.PostProcessAccounting(order, qtyFill);

					if (order.IsEmergencyClose) {
						this.OPPemergency.RemoveEmergencyLockFilled(order);
					}
					this.OPPsequencer.OrderFilledUnlockSequenceSubmitOpening(order);
					try {
						// of course here we are getting streaming=on... hope it's not only an assumption :)
						int barRelnoFill = order.Alert.Bars.Count;
						order.Alert.Strategy.Script.Executor.CallbackAlertFilledMoveAroundInvokeScript(order.Alert, null, barRelnoFill,
							order.PriceFill, order.QtyFill, order.SlippageFill, order.CommissionFill);
					} catch (Exception e) {
						string msg = "PostProcessOrderState caught from CallbackAlertFilledMoveAroundInvokeScript() " + msgException;
						this.PopupException(new Exception(msg, e));
					}
					break;

				case OrderState.ErrorCancelReplace:
					this.DataSnapshot.OrdersRemove(new List<Order>() { order });
					this.EventDistributor.RaiseOrderRemovedExecutionFormNotification(this, order);
					Assembler.PopupException(msgException);
					break;

				case OrderState.Error:
				case OrderState.ErrorMarketPriceZero:
				case OrderState.ErrorSubmitOrder:
				case OrderState.ErrorSlippageCalc:
					Assembler.PopupException("PostProcess(): order.PriceFill=0 " + msgException);
					order.PriceFill = 0;
					//NEVER order.PricePaid = 0;
					break;

				case OrderState.ErrorSubmittingBroker:
				case OrderState.Rejected:
				case OrderState.RejectedLimitReached:
					if (order.State == OrderState.Rejected) {
						bool a = order.IsEmergencyClose;
						bool b = string.IsNullOrEmpty(order.EmergencyReplacedByGUID) == false;
						bool c = string.IsNullOrEmpty(order.ReplacedByGUID) == false;
						if (b || c) {
							string msg = "";
							if (b) msg += " BrokerProvider CALLBACK DUPE: Rejected was already replaced by"
								+ " EmergencyReplacedByGUID[" + order.EmergencyReplacedByGUID + "]"
								//+ "; skipping PostProcess for [" + order + "]"
								;
							if (c) msg += " BrokerProvider CALLBACK DUPE: Rejected was already replaced by"
								+ " ReplacedByGUID[" + order.ReplacedByGUID + "]"
								//+ "; skipping PostProcess for [" + order + "]"
								;
							this.AppendOrderMessageAndPropagateCheckThrowOrderNull(order, msg);
							this.EventDistributor.RaiseOrderPropertiesUpdatedByExecutionButSameState(order);
							return;
						}
					}
			
					Assembler.PopupException("PostProcess(): order.PriceFill=0 " + msgException);
					order.PriceFill = 0;
					//NEVER order.PricePaid = 0;

					if (order.IsEmergencyClose) {
						this.OPPemergency.CreateEmergencyReplacementAndResubmitFor(order);
					} else {
						if (order.Alert.IsExitAlert) {
							this.OPPemergency.AddLockAndCreateEmergencyReplacementAndResubmitFor(order);
						} else {
							this.OPPrejected.HandleReplaceRejected(order);
						}
					}
					break;

				case OrderState.Submitting:
					string msg2 = "all Orders.State!=Submitting aren't sent to BrokerProvider;"
						+ " we shouldn't be here in a broker-originated State change handler...";
					break;
				case OrderState.SubmittingSequenced:
				case OrderState.Submitted:
				case OrderState.SubmittedNoFeedback:
				case OrderState.Active:
					break;

				case OrderState.IRefuseOpenTillEmergencyCloses:
				case OrderState.IRefuseToCloseNonStreamingPosition:
				case OrderState.IRefuseToCloseUnfilledEntry:
					break;

				case OrderState.EmergencyCloseSheduledForErrorSubmittingBroker:
				case OrderState.EmergencyCloseSheduledForNoReason:
				case OrderState.EmergencyCloseSheduledForRejected:
				case OrderState.EmergencyCloseSheduledForRejectedLimitReached:
				case OrderState.EmergencyCloseComplete:
				case OrderState.EmergencyCloseLimitReached:
				case OrderState.EmergencyCloseUserInterrupted:
					break;

				case OrderState.PreSubmit:
					break;

				case OrderState.JustCreated:
					break;

				default:
					Assembler.PopupException("No handler for order.State[" + order.State + "] message[" + msgException + "]");
					break;
			}
		}

		public void MoveStopLoss(PositionPrototype proto, double newActivationOffset, double newStopLossNegativeOffset) {
			if (proto.StopLossAlertForAnnihilation == null) {
				string msg = "I refuse to move StopLoss order because proto.StopLossAlertForAnnihilation=null";
				throw new Exception(msg);
			}
			if (proto.StopLossAlertForAnnihilation.OrderFollowed == null) {
				string msg = "I refuse to move StopLoss order because proto.StopLossAlertForAnnihilation.OrderFollowed=null";
				throw new Exception(msg);
			}

			Order order2killAndReplace = proto.StopLossAlertForAnnihilation.OrderFollowed;

			OrderState stateBeforeActiveAssummingSubmitting = order2killAndReplace.State;
			OrderState stateBeforeKilledAssumingActive = OrderState.Unknown;

			string msig = "MoveStopLoss(" + proto.StopLossActivationNegativeOffset + "/" + proto.StopLossNegativeOffset
				+ "=>" + newActivationOffset + "/" + newStopLossNegativeOffset + "): ";

			// 1. hook onKilled=>submitNew
			OrderPostProcessorStateHook stopLossGotKilledHook = new OrderPostProcessorStateHook("StopLossGotKilledHook",
				order2killAndReplace, OrderState.Killed,
				delegate(Order stopLossKilled, ReporterPokeUnit pokeUnit) {
					string msg = msig + "StopLossGotKilledHook(): invoking OnStopLossKilledCreateNewStopLossAndAddToPokeUnit() "
						+ " [" + stateBeforeKilledAssumingActive + "] => "
						+ "[" + stopLossKilled.State + "]";
					stopLossKilled.AppendMessage(msg);
					this.OnStopLossKilledCreateNewStopLossAndAddToPokeUnit(stopLossKilled, newActivationOffset, newStopLossNegativeOffset, pokeUnit);
				}
			);

			// 2. hook onActive=>kill
			OrderPostProcessorStateHook stopLossReceivedActiveCallback = new OrderPostProcessorStateHook("StopLossReceivedActiveCallback",
				order2killAndReplace, OrderState.Active,
				delegate(Order stopLossToBeKilled, ReporterPokeUnit pokeUnit) {
					string msg = msig + "StopLossReceivedActiveCallback(): invoking KillOrderUsingKillerOrder() "
						+ " [" + stateBeforeActiveAssummingSubmitting + "] => "
						+ "[" + stopLossToBeKilled.State + "]";
					stopLossToBeKilled.AppendMessage(msg);
					stateBeforeKilledAssumingActive = stopLossToBeKilled.State;
					this.KillOrderUsingKillerOrder(order2killAndReplace);
				}
			);

			this.OPPstatusCallbacks.AddStateChangedHook(stopLossReceivedActiveCallback);
			this.OPPstatusCallbacks.AddStateChangedHook(stopLossGotKilledHook);

			this.AppendOrderMessageAndPropagateCheckThrowOrderNull(proto.StopLossAlertForAnnihilation.OrderFollowed, msig + "hooked stopLossReceivedActiveCallback() and stopLossGotKilledHook()");
		}
		public void OnStopLossKilledCreateNewStopLossAndAddToPokeUnit(Order killedStopLoss, double newActivationOffset, double newStopLossNegativeOffset, ReporterPokeUnit pokeUnit) {
			string msig = "OnStopLossKilledCreateNewStopLossAndAddToPokeUnit(): ";
			ScriptExecutor executor = killedStopLoss.Alert.Strategy.Script.Executor;
			Position position = killedStopLoss.Alert.PositionAffected;
			// resetting proto.SL to NULL is a legal permission to set new StopLossAlert for SellOrCoverRegisterAlerts()
			position.Prototype.StopLossAlertForAnnihilation = null;
			// resetting position.ExitAlert is a legal permission to for SimulateRealtimeOrderFill() to not to throw "I refuse to tryFill an ExitOrder"
			position.ExitAlert = null;
			// set new SL+SLa as new targets for Activator
			string msg = position.Prototype.ToString();
			position.Prototype.SetNewStopLossOffsets(newStopLossNegativeOffset, newActivationOffset);
			msg += " => " + position.Prototype.ToString();
			Alert replacement = executor.PositionPrototypeActivator.CreateStopLossFromPositionPrototype(position);
			// dont CreateAndSubmit, pokeUnit will be submitted with oneNewAlertPerState in InvokeHooksAndSubmitNewAlertsBackToBrokerProvider();
			//this.CreateOrdersSubmitToBrokerProviderInNewThreadGroups(new List<Alert>() {replacement}, true, true);
			pokeUnit.AlertsNew.Add(replacement);
			msg += " newAlert[" + replacement + "]";
			killedStopLoss.AppendMessage(msig + msg);
		}
		public void MoveTakeProfit(PositionPrototype proto, double newTakeProfitPositiveOffset) {
			if (proto.TakeProfitAlertForAnnihilation == null) {
				string msg = "I refuse to move TakeProfit order because proto.TakeProfitAlertForAnnihilation=null";
				throw new Exception(msg);
			}
			if (proto.TakeProfitAlertForAnnihilation.OrderFollowed == null) {
				string msg = "I refuse to move TakeProfit order because proto.TakeProfitAlertForAnnihilation.OrderFollowed=null";
				throw new Exception(msg);
			}

			Order order2killAndReplace = proto.TakeProfitAlertForAnnihilation.OrderFollowed;

			OrderState stateBeforeActiveAssummingSubmitting = order2killAndReplace.State;
			OrderState stateBeforeKilledAssumingActive = OrderState.Unknown;

			string msig = "MoveTakeProfit(" + proto.TakeProfitPositiveOffset + "=>" + newTakeProfitPositiveOffset + "): ";

			// 1. hook onKilled=>submitNew
			OrderPostProcessorStateHook takeProfitGotKilledHook = new OrderPostProcessorStateHook("TakeProfitGotKilledHook",
				order2killAndReplace, OrderState.Killed,
				delegate(Order takeProfitKilled, ReporterPokeUnit pokeUnit) {
					string msg = msig + "takeProfitGotKilledHook(): invoking OnTakeProfitKilledCreateNewTakeProfitAndAddToPokeUnit() "
						+ " [" + stateBeforeKilledAssumingActive + "] => "
						+ "[" + takeProfitKilled.State + "]";
					takeProfitKilled.AppendMessage(msg);
					this.OnTakeProfitKilledCreateNewTakeProfitAndAddToPokeUnit(takeProfitKilled, newTakeProfitPositiveOffset, pokeUnit);
				}
			);

			// 2. hook onActive=>kill
			OrderPostProcessorStateHook takeProfitReceivedActiveCallback = new OrderPostProcessorStateHook("TakeProfitReceivedActiveCallback",
				order2killAndReplace, OrderState.Active,
				delegate(Order takeProfitToBeKilled, ReporterPokeUnit pokeUnit) {
					string msg = msig + "takeProfitReceivedActiveCallback(): invoking KillOrderUsingKillerOrder() "
						+ " [" + stateBeforeActiveAssummingSubmitting + "] => "
						+ "[" + takeProfitToBeKilled.State + "]";
					takeProfitToBeKilled.AppendMessage(msg);
					stateBeforeKilledAssumingActive = takeProfitToBeKilled.State;
					this.KillOrderUsingKillerOrder(order2killAndReplace);
				}
			);

			this.OPPstatusCallbacks.AddStateChangedHook(takeProfitReceivedActiveCallback);
			this.OPPstatusCallbacks.AddStateChangedHook(takeProfitGotKilledHook);

			this.AppendOrderMessageAndPropagateCheckThrowOrderNull(proto.TakeProfitAlertForAnnihilation.OrderFollowed, msig + ": hooked takeProfitReceivedActiveCallback() and takeProfitGotKilledHook()");
		}
		public void OnTakeProfitKilledCreateNewTakeProfitAndAddToPokeUnit(Order killedTakeProfit, double newTakeProfitPositiveOffset, ReporterPokeUnit pokeUnit) {
			string msig = "OnTakeProfitKilledCreateNewTakeProfitAndAddToPokeUnit(): ";
			ScriptExecutor executor = killedTakeProfit.Alert.Strategy.Script.Executor;
			Position position = killedTakeProfit.Alert.PositionAffected;
			// resetting proto.SL to NULL is a legal permission to set new TakeProfitAlert for SellOrCoverRegisterAlerts()
			position.Prototype.TakeProfitAlertForAnnihilation = null;
			// resetting position.ExitAlert is a legal permission to for SimulateRealtimeOrderFill() to not to throw "I refuse to tryFill an ExitOrder"
			position.ExitAlert = null;
			// set new SL+SLa as new targets for Activator
			string msg = position.Prototype.ToString();
			position.Prototype.SetNewTakeProfitOffset(newTakeProfitPositiveOffset);
			msg += " => " + position.Prototype.ToString();
			Alert replacement = executor.PositionPrototypeActivator.CreateTakeProfitFromPositionPrototype(position);
			// dont CreateAndSubmit, pokeUnit will be submitted with oneNewAlertPerState in InvokeHooksAndSubmitNewAlertsBackToBrokerProvider();
			//this.CreateOrdersSubmitToBrokerProviderInNewThreadGroups(new List<Alert>() { replacement }, true, true);
			pokeUnit.AlertsNew.Add(replacement);
			msg += " newAlert[" + replacement + "]";
			killedTakeProfit.AppendMessage(msig + msg);
		}
		public void InvokeHooksAndSubmitNewAlertsBackToBrokerProvider(Order orderWithNewState) {
			ScriptExecutor executor = orderWithNewState.Alert.Strategy.Script.Executor;
			ReporterPokeUnit afterHooksInvokedPokeUnit = new ReporterPokeUnit(null);
			int hooksInvoked = this.OPPstatusCallbacks.InvokeOnceHooksForOrderStateAndDelete(orderWithNewState, afterHooksInvokedPokeUnit);
			if (executor.Backtester.IsBacktestingNow) return;

			List<Alert> alertsCreatedByHooks = afterHooksInvokedPokeUnit.AlertsNew;
			if (alertsCreatedByHooks.Count == 0) {
				string msg = "NOT_AN_ERROR: ZERO alerts from [" + hooksInvoked + "] hooks invoked; order[" + orderWithNewState + "]";
				//this.PopupException(new Exception(msg));
				return;
			}
			bool setStatusSubmitting = executor.IsStreaming && executor.IsAutoSubmitting;
			this.CreateOrdersSubmitToBrokerProviderInNewThreadGroups(alertsCreatedByHooks, setStatusSubmitting, true);
			orderWithNewState.Alert.Strategy.Script.Executor.PushPositionsOpenedClosedToReportersAsyncUnsafe(afterHooksInvokedPokeUnit);
		}
		public void RemovePendingAlertsForVictimOrderMustBePostKill(Order orderKilled, string msig) {
			string msg = "";
			//if (OrderStateCollections.Cemeteries.Contains(orderKilled.State) == false) return;

			if (orderKilled.State != OrderState.Killed) {
				if (orderKilled.State == OrderState.KillerDone) {
					if (orderKilled.VictimToBeKilled != null) {
						msg = "unhealthy killer; VictimToBeKilled=null";
					} else {
						string msg1 = "healthy killer; we can use killer.Victim.Alert for one rabied victim having no Alert";
					}
				} else {
					msg = "not a killer";
					if (orderKilled.IsKiller == true) {
						msg = "not a killer but claims to be a killer";
					}
				}
				orderKilled.AppendMessage(msig + msg + " " + orderKilled);
				return;
			}
			Alert alertForOrder = orderKilled.Alert;
			if (alertForOrder == null) {
				msg = "orderKilled.Alert=null; dunno what to remove from PendingAlerts";
				orderKilled.AppendMessage(msig + msg + " " + orderKilled);
				return;
			}
			ScriptExecutor executor = alertForOrder.Strategy.Script.Executor;
			try {
				executor.CallbackAlertKilledInvokeScript(alertForOrder);
				msg = orderKilled.State + " => AlertsPendingRemove.Remove(orderExecuted.Alert)'d";
				orderKilled.AppendMessage(msig + msg);
			} catch (Exception e) {
				msg = orderKilled.State + " is a Cemetery but [" + e.Message + "]"
					+ "; comment the State out; alert[" + alertForOrder + "]";
				orderKilled.AppendMessage(msig + msg);
			}
		}

//		public OrderProcessor(IContainer container) : this() {
//			container.Add(this);
//		}
//		protected override void Dispose(bool disposing) {
//			this.DataSnapshot.SerializerLogRotate.OrdersBuffered.Serialize();
//			this.DataSnapshot.SerializerLogRotate.HistoricalTradesBuffered.Serialize();
//
//			if (disposing && this.components != null) {
//				this.components.Dispose();
//			}
//			base.Dispose(disposing);
//		}
	}
}