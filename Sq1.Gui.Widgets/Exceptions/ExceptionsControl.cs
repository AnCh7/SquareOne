using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

using Sq1.Core;
using Sq1.Core.Serializers;
using Sq1.Support;

namespace Sq1.Widgets.Exceptions {
	public partial class ExceptionsControl {
		public ExceptionsControlDataSnapshot DataSnapshot;
		public Serializer<ExceptionsControlDataSnapshot> DataSnapshotSerializer;

		public List<Exception> Exceptions { get; protected set; }
		public Dictionary<Exception, DateTime> ExceptionTimes  { get; protected set; }
		public List<Exception> FirstExceptionAsList {
			get {
				var ret = new List<Exception>();
				if (this.Exceptions.Count > 0) ret.Add(this.Exceptions[0]);
				return ret;
			}
		}
		Exception exceptionSelectedInTree {
			get { return this.treeExceptions.SelectedObject as Exception; }
		}

		public ExceptionsControl() : base() {
			this.Exceptions = new List<Exception>();
			this.ExceptionTimes = new Dictionary<Exception, DateTime>();

			this.InitializeComponent();
			//WindowsFormsUtils.SetDoubleBuffered(this.tree);	//doesn't help, still flickers
			WindowsFormsUtils.SetDoubleBuffered(this.lvStackTrace);
			WindowsFormsUtils.SetDoubleBuffered(this);
			
			this.exceptionsTreeListViewCustomize();
			this.treeExceptions.SetObjects(this.Exceptions);
		}

		public void Initialize() {
			this.DataSnapshotSerializer = new Serializer<ExceptionsControlDataSnapshot>(Assembler.InstanceInitialized.StatusReporter);
			bool createdNewFile = this.DataSnapshotSerializer.Initialize(Assembler.InstanceInitialized.AppDataPath,
				"Sq1.Widgets.ExceptionsControlDataSnapshot.json", "Workspaces",
				Assembler.InstanceInitialized.AssemblerDataSnapshot.CurrentWorkspaceName);
			this.DataSnapshot = this.DataSnapshotSerializer.Deserialize();
			if (createdNewFile) {
				this.DataSnapshot.SplitDistanceVertical = this.splitContainerVertical.SplitterDistance;
				this.DataSnapshot.SplitDistanceHorizontal = this.splitContainerHorizontal.SplitterDistance;
				this.DataSnapshotSerializer.Serialize();
			}
		}
		public void PopulateDataSnapshotInitializeSplittersAfterDockContentDeserialized() {
			if (this.Width == 0) {
				string msg = "CANT_SET_SPLITTER_DISTANCE_FOR_UNSHOWN_CONTROL ExceptionsControl.Visible[" + this.Visible + "]; can't set SplitDistanceVertical, SplitDistanceHorizontal";
				Assembler.PopupException(msg);
			} else {
				if (this.DataSnapshot.SplitDistanceVertical > 0 && this.splitContainerVertical.SplitterDistance != this.DataSnapshot.SplitDistanceVertical) {
					this.splitContainerVertical.SplitterDistance = this.DataSnapshot.SplitDistanceVertical;
				}
				if (this.DataSnapshot.SplitDistanceHorizontal > 0 && this.splitContainerHorizontal.SplitterDistance != this.DataSnapshot.SplitDistanceHorizontal) {
					this.splitContainerHorizontal.SplitterDistance = this.DataSnapshot.SplitDistanceHorizontal;
				}
			}
			//late binding prevents SplitterMoved() induced by DockContent layouting LoadAsXml()ed docked forms 
			this.splitContainerVertical.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.SplitContainerVertical_SplitterMoved);
			this.splitContainerHorizontal.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.SplitContainerHorizontal_SplitterMoved);
			this.mniRecentAlwaysSelected.Checked = this.DataSnapshot.RecentAlwaysSelected;
			this.mniltbDelay.InputFieldValue = this.DataSnapshot.TreeRefreshDelayMsec.ToString();
			this.mniTreeShowsTimesInsteadOfMessages.Checked = this.DataSnapshot.TreeShowsTimesInsteadOfMessages;
			this.olvTime.Text = this.DataSnapshot.TreeShowsTimesInsteadOfMessages ? "Time" : "Message";
		}
		public void InsertException(Exception exception) {
			this.ExceptionTimes.Add(exception, DateTime.Now);
			this.Exceptions.Insert(0, exception);
			
			//flushToTree will visualize whole the control AFTER IT WILL BE SHOWN 
//			this.treeExceptions.SetObjects(this.Exceptions);
//			//this.tree.RefreshObject(exception);
//			this.treeExceptions.RebuildAll(true);
//			//v2 http://stackoverflow.com/questions/7949887/how-to-add-a-new-item-into-objectlistview
//			//"When the ListView is in virtual mode, you cannot add items to the ListView items collection. Use  the VirtualListSize property instead to change the size of the ListView items collection."
//			//this.tree.InsertObjects(0, new List<Exception>() { exception });
//			this.treeExceptions.Expand(exception);
//			// MAKES StrategiesTreeControl.CellClick invoke handlers 100 times!!! nonsense I know Application.DoEvents();	// TsiProgressBarETAClick doesn't get control when every quote there is an exception and user can't interrupt the backtest
			this.flushListToTree();
		}
		void selectMostRecentException() {
			if (this.treeExceptions.GetItemCount() == 0) return;
			this.treeExceptions.SelectedIndex = 0;
			this.treeExceptions.EnsureVisible(0);
		}
		void displayStackTrace() {
			try {
				this.lvStackTrace.BeginUpdate();
				this.lvStackTrace.Items.Clear();
				StackTrace stackTrace = new StackTrace(this.exceptionSelectedInTree);
				for (int i = 0; i < stackTrace.FrameCount - 1; i++) {
					StackFrame stackFrame = stackTrace.GetFrame(i);
					if (stackFrame == null) continue;
					string declaringType = stackFrame.GetMethod().DeclaringType.Name;
					string methodName = stackFrame.GetMethod().Name;	//.ToString();
					//if (methodName == null) continue;
					string fileName = stackFrame.GetFileName();
					string lineNumber = stackFrame.GetFileLineNumber().ToString();
					ListViewItem item = new ListViewItem();
					item.Text = declaringType;
					item.SubItems.Add(methodName);
					item.SubItems.Add(lineNumber);
					item.SubItems.Add(fileName);
					this.lvStackTrace.Items.Add(item);
				}
			} finally {
				this.lvStackTrace.EndUpdate();
			}
		}
		void flushListToTree() {
			if (this.Visible == false) return;
			this.treeExceptions.SetObjects(this.Exceptions);
			this.treeExceptions.RebuildAll(true);
			if (this.Exceptions.Count == 0) {
				this.txtExceptionMessage.Text = "";
				this.lvStackTrace.Items.Clear();
				return;
			}
			this.treeExceptions.ExpandAll();
			this.selectMostRecentException();
		}
		public override string ToString() {
			StringBuilder formattedException = new StringBuilder();
			if (this.exceptionSelectedInTree != null) {
				formattedException.Append("EXCEPTION INFORMATION").Append(Environment.NewLine)
					.Append(Environment.NewLine)
					.Append("Date/Time: ").Append(DateTime.Now.ToString("F", CultureInfo.CurrentCulture))
					.Append(Environment.NewLine)
					.Append("Type: ").Append(this.exceptionSelectedInTree.GetType().FullName).Append(Environment.NewLine)
					.Append("Message: ").Append(this.exceptionSelectedInTree.Message).Append(Environment.NewLine)
					.Append("Source: ").Append(this.exceptionSelectedInTree.Source).Append(Environment.NewLine)
					.Append("Target Method: ");
				if (this.exceptionSelectedInTree.TargetSite != null) {
					formattedException.Append(this.exceptionSelectedInTree.TargetSite.ToString());
				}
				formattedException.Append(Environment.NewLine).Append(Environment.NewLine)
					.Append("Call Stack:").Append(Environment.NewLine);

				StackTrace exceptionStack = new StackTrace(this.exceptionSelectedInTree);

				for (int i = 0; i < exceptionStack.FrameCount; i++) {
					StackFrame exceptionFrame = exceptionStack.GetFrame(i);

					formattedException.Append("\t").Append("Method Name: ").Append(exceptionFrame.GetMethod().ToString()).Append(Environment.NewLine)
								.Append("\t").Append("\t").Append("File Name: ").Append(exceptionFrame.GetFileName()).Append(Environment.NewLine)
								.Append("\t").Append("\t").Append("Column: ").Append(exceptionFrame.GetFileColumnNumber()).Append(Environment.NewLine)
								.Append("\t").Append("\t").Append("Line: ").Append(exceptionFrame.GetFileLineNumber()).Append(Environment.NewLine)
								.Append("\t").Append("\t").Append("CIL Offset: ").Append(exceptionFrame.GetILOffset()).Append(Environment.NewLine)
								.Append("\t").Append("\t").Append("Native Offset: ").Append(exceptionFrame.GetNativeOffset()).Append(Environment.NewLine)
						.Append(Environment.NewLine);
				}

				formattedException.Append("Inner Exception(s)").Append(Environment.NewLine);

				Exception innerException = this.exceptionSelectedInTree.InnerException;

				while (innerException != null) {
					formattedException.Append("\t").Append("Exception: ")
						.Append(innerException.GetType().FullName).Append(Environment.NewLine);
					innerException = innerException.InnerException;
				}

				formattedException.Append(Environment.NewLine).Append("Custom Properties")
					.Append(Environment.NewLine);

				Type exceptionType = typeof(Exception);

				foreach (PropertyInfo propertyInfo in this.exceptionSelectedInTree.GetType().GetProperties()) {
					if (exceptionType.GetProperty(propertyInfo.Name) == null) {
						formattedException.Append("\t").Append(propertyInfo.Name).Append(": ")
							.Append(propertyInfo.GetValue(this.exceptionSelectedInTree, null))
							.Append(Environment.NewLine);
					}
				}
			}

			return formattedException.ToString();
		}
		public void CopyExceptionDataToClipboard() {
			Clipboard.SetDataObject(this.ToString(), true);
		}
		void mniRecentAlwaysSelected_Click(object sender, EventArgs e) {
			this.DataSnapshot.RecentAlwaysSelected = this.mniRecentAlwaysSelected.Checked;
			this.DataSnapshotSerializer.Serialize();
			this.selectMostRecentException();
		}				
	}
}