﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using Sq1.Core;
using Sq1.Core.DataFeed;
using Sq1.Core.DataTypes;
using Sq1.Core.Execution;
using Sq1.Core.Serializers;
using Sq1.Core.StrategyBase;
using Sq1.Core.Support;
using Sq1.Gui.Forms;
using Sq1.Widgets.LabeledTextBox;
using WeifenLuo.WinFormsUI.Docking;

namespace Sq1.Gui.Singletons {
	public partial class MainForm : Form {
		public MainFormEventManager MainFormEventManager;
		public MainFormWorkspacesManager WorkspacesManager;
		public GuiDataSnapshot GuiDataSnapshot;
		public Sq1.Core.Serializers.Serializer<Sq1.Gui.GuiDataSnapshot> GuiDataSnapshotSerializer;
		public bool MainFormClosingSkipChartFormsRemoval;

		public ChartForm ChartFormActive {
			get {
				var ret = this.DockPanel.ActiveDocument as ChartForm;
				if (ret == null) {
					string msg = "MainForm.DockPanel.ActiveDocument is not a ChartForm; no charts open or drag your chart into DOCUMENT docking area";
					//throw new Exception(msg);
					return null;
				}
				foreach (ChartFormManager chartFormDataSnap in this.GuiDataSnapshot.ChartFormManagers.Values) {
					if (chartFormDataSnap.ChartForm == ret) return ret;
				}
				string msg2 = "MainForm.DockPanel.ActiveDocument is [" + ret.ToString() + "] but it's not found among MainForm.ChartFormsManagers registry;"
					+ "1) did you forget to add? 2) MainForm.ChartFormsManagers doesn't have DockContent-restored Forms added?";
				throw new Exception(msg2);
			}
		}


		public MainForm() {
			InitializeComponent();
			try {
				Assembler.InstanceUninitialized.Initialize(this as IStatusReporter);
				this.GuiDataSnapshotSerializer = new Serializer<GuiDataSnapshot>(this as IStatusReporter);
	
				DataSourceEditorForm.Instance.DataSourceEditorControl.InitializeContext(Assembler.InstanceInitialized);
				DataSourceEditorForm.Instance.DataSourceEditorControl.InitializeProviders(
					Assembler.InstanceInitialized.RepositoryDllStaticProvider.CloneableInstanceByClassName,
					Assembler.InstanceInitialized.RepositoryDllStreamingProvider.CloneableInstanceByClassName,
					Assembler.InstanceInitialized.RepositoryDllBrokerProvider.CloneableInstanceByClassName);
	
				DataSourcesForm.Instance.Initialize(Assembler.InstanceInitialized.RepositoryJsonDataSource, this as IStatusReporter, this.DockPanel);
				StrategiesForm.Instance.Initialize(Assembler.InstanceInitialized.RepositoryDllJsonStrategy, this as IStatusReporter, this.DockPanel);
				ExecutionForm.Instance.Initialize(Assembler.InstanceInitialized.OrderProcessor, this as IStatusReporter, this.DockPanel);
				CsvImporterForm.Instance.Initialize(Assembler.InstanceInitialized.RepositoryJsonDataSource, this as IStatusReporter, this.DockPanel);
			} catch (Exception ex) {
				this.PopupException(ex);
			}
		}

		void initializeWorkspacesManagerTrampoline() {
			this.WorkspacesManager = new MainFormWorkspacesManager(this);
			//this.CtxWorkspaces.Items.Clear();
			this.CtxWorkspaces.Items.AddRange(this.WorkspacesManager.WorkspaceMenuItemsWithHandlers);
			this.mniWorkspaceDeleteCurrent.Click		+= new EventHandler(this.WorkspacesManager.WorkspaceDeleteCurrent_Click);
			this.mniltbWorklspaceCloneTo.UserTyped		+= new System.EventHandler<Sq1.Widgets.LabeledTextBox.LabeledTextBoxUserTypedArgs>(this.WorkspacesManager.WorkspaceCloneTo_UserTyped);
			this.mniltbWorklspaceRenameTo.UserTyped		+= new System.EventHandler<Sq1.Widgets.LabeledTextBox.LabeledTextBoxUserTypedArgs>(this.WorkspacesManager.WorkspaceRenameTo_UserTyped);
			this.mniltbWorklspaceNewBlank.UserTyped		+= new System.EventHandler<Sq1.Widgets.LabeledTextBox.LabeledTextBoxUserTypedArgs>(this.WorkspacesManager.WorkspaceNewBlank_UserTyped);
		}
		public void WorkspaceLoad(string workspaceToLoad) {
			try {
				if (Assembler.InstanceInitialized.AssemblerDataSnapshot.CurrentWorkspaceName != workspaceToLoad) {
					Assembler.InstanceInitialized.AssemblerDataSnapshot.CurrentWorkspaceName = workspaceToLoad;
					Assembler.InstanceInitialized.AssemblerDataSnapshotSerializer.Serialize();
				}
				bool createdNewFile = this.GuiDataSnapshotSerializer.Initialize(Assembler.InstanceInitialized.AppDataPath,
					"Sq1.Gui.GuiDataSnapshot.json", "Workspaces",
					Assembler.InstanceInitialized.AssemblerDataSnapshot.CurrentWorkspaceName);

				this.GuiDataSnapshot = this.GuiDataSnapshotSerializer.Deserialize();
				if (createdNewFile) {
					this.mainForm_LocationChanged(this, null);
					this.mainForm_ResizeEnd(this, null);
					this.GuiDataSnapshotSerializer.Serialize();
					
					// re-reading Workspaces\ since I just created one, and before it was empty; copy-paste from initializeWorkspacesManagerTrampoline()
					Assembler.InstanceInitialized.WorkspacesRepository.ScanFolders();
					this.WorkspacesManager = new MainFormWorkspacesManager(this);
					this.CtxWorkspaces.Items.AddRange(this.WorkspacesManager.WorkspaceMenuItemsWithHandlers);
				}
				//this.DataSnapshot.RebuildDeserializedChartFormsManagers(this);
				
				//foreach (Form each in this.OwnedForms) each.Close();
				foreach (IDockContent each in this.DockPanel.Documents) {
					var form = each as DockContent;
					form.Close();
				}
				foreach (FloatWindow each in this.DockPanel.FloatWindows) each.Close(); 
				foreach (DockWindow each in this.DockPanel.DockWindows) {
					//each.Close();
				}
	
				string file = this.LayoutXml;
				if (File.Exists(file) == false) file = this.LayoutXmlInitial;
				if (File.Exists(file)) {
					DeserializeDockContent deserializeDockContent = new DeserializeDockContent(this.PersistStringInstantiator);
					this.DockPanel.LoadFromXml(LayoutXml, deserializeDockContent);
				}
				Assembler.InstanceInitialized.MainFormDockFormsFullyDeserializedLayoutComplete = true;
	
//				this.mniExceptions.Checked = !ExceptionsForm.Instance.IsHidden;
//				this.mniSymbols.Checked = !DataSourcesForm.Instance.IsHidden;
//				this.mniSliders.Checked = !SlidersForm.Instance.IsHidden;
//				this.mniStrategies.Checked = !StrategiesForm.Instance.IsHidden;
//				this.mniSymbolManager.Checked = !SymbolManagerForm.Instance.IsHidden;
//				this.mniExecution.Checked = !ExecutionForm.Instance.IsHidden;
//				this.mniCsvImporter.Checked = !CsvImporterForm.Instance.IsHidden;
	
				this.initializeMainFromDeserializedDataSnapshot();
				this.mainFormEventManagerInitializeAfterDockingDeserialized();
	
				//this.PropagateSelectorsForCurrentChart();
				//WHY???this.MainFormEventManager.DockPanel_ActiveDocumentChanged(this, EventArgs.Empty);
	
				this.WorkspacesManager.SelectWorkspaceLoaded(workspaceToLoad);

				if (ExceptionsForm.Instance.ExceptionControl.Exceptions.Count > 0) {
					ExceptionsForm.Instance.Show(this.DockPanel);
					ExceptionsForm.Instance.ExceptionControl.PopulateDataSnapshotInitializeSplittersAfterDockContentDeserialized();
				}
			} catch (Exception ex) {
				#if DEBUG
				Debugger.Break();
				#endif
				this.PopupException(ex);
			}
		}
		void MainFormEventManagerInitializeWhenDockingIsNotNullAnymore() {
			DataSourcesForm.Instance.VisibleChanged += delegate { this.mniSymbols.Checked = DataSourcesForm.Instance.Visible; };
			ExceptionsForm.Instance.VisibleChanged += delegate { this.mniExceptions.Checked = ExceptionsForm.Instance.Visible; };
			SlidersForm.Instance.VisibleChanged += delegate { this.mniSliders.Checked = SlidersForm.Instance.Visible; };
			StrategiesForm.Instance.VisibleChanged += delegate { this.mniStrategies.Checked = StrategiesForm.Instance.Visible; };
			ExecutionForm.Instance.VisibleChanged += delegate { this.mniExecution.Checked = ExecutionForm.Instance.Visible; };
			CsvImporterForm.Instance.VisibleChanged += delegate { this.mniCsvImporter.Checked = CsvImporterForm.Instance.Visible; };

			this.MainFormEventManager = new MainFormEventManager(this);

			StrategiesForm.Instance.StrategiesTreeControl.OnStrategyOpenDefaultClicked += this.MainFormEventManager.StrategiesTree_OnStrategyOpenDefaultClicked;
			StrategiesForm.Instance.StrategiesTreeControl.OnStrategyOpenSavedClicked += this.MainFormEventManager.StrategiesTree_OnStrategyOpenNewChartClicked;
			StrategiesForm.Instance.StrategiesTreeControl.OnStrategyRenamed += this.MainFormEventManager.StrategiesTree_OnStrategyRenamed;
			StrategiesForm.Instance.StrategiesTreeControl.OnStrategySelected += new EventHandler<StrategyEventArgs>(this.MainFormEventManager.StrategiesTree_OnStrategySelected);

			DataSourcesForm.Instance.DataSourcesTreeControl.OnSymbolSelected += this.MainFormEventManager.DataSourcesTree_OnSymbolSelected;
			DataSourcesForm.Instance.DataSourcesTreeControl.OnDataSourceSelected += this.MainFormEventManager.DataSourcesTree_OnDataSourceSelected;
			DataSourcesForm.Instance.DataSourcesTreeControl.OnNewChartForSymbolClicked += this.MainFormEventManager.DataSourcesTree_OnNewChartForSymbolClicked;
			DataSourcesForm.Instance.DataSourcesTreeControl.OnOpenStrategyForSymbolClicked += this.MainFormEventManager.DataSourcesTree_OnOpenStrategyForSymbolClicked;
			DataSourcesForm.Instance.DataSourcesTreeControl.OnBarsAnalyzerClicked += this.MainFormEventManager.DataSourcesTree_OnBarsAnalyzerClicked;
			DataSourcesForm.Instance.DataSourcesTreeControl.OnDataSourceEditClicked += this.MainFormEventManager.DataSourcesTree_OnDataSourceEditClicked;
			//DataSourcesForm.Instance.DataSourcesTree.OnDataSourceDeleteClicked += this.MainFormEventManager.DataSourcesTree_OnDataSourceDeletedClicked;
			Assembler.InstanceInitialized.RepositoryJsonDataSource.OnItemCanBeRemoved += new EventHandler<NamedObjectJsonEventArgs<DataSource>>(this.MainFormEventManager.RepositoryJsonDataSource_OnDataSourceCanBeRemoved);
			Assembler.InstanceInitialized.RepositoryJsonDataSource.OnItemRemovedDone += new EventHandler<NamedObjectJsonEventArgs<DataSource>>(this.MainFormEventManager.RepositoryJsonDataSource_OnDataSourceRemoved);
			//DataSourcesForm.Instance.DataSourcesTreeControl.OnDataSourceNewClicked += this.MainFormEventManager.DataSourcesTree_OnDataSourceNewClicked;

			SlidersForm.Instance.SlidersAutoGrowControl.SliderValueChanged += this.MainFormEventManager.SlidersAutoGrow_SliderValueChanged;
			SlidersForm.Instance.SlidersAutoGrowControl.ScriptContextLoadRequestedSubscriberImplementsCurrentSwitch += this.MainFormEventManager.SlidersAutoGrow_OnScriptContextLoadClicked;
			SlidersForm.Instance.SlidersAutoGrowControl.ScriptContextRenamed += this.MainFormEventManager.SlidersAutoGrow_OnScriptContextRenamed;
		}
		void mainFormEventManagerInitializeAfterDockingDeserialized() {
			// too frequent
			//this.DockPanel.ActiveContentChanged += this.MainFormEventManager.DockPanel_ActiveContentChanged;
			// just as often as I needed!
			this.DockPanel.ActiveDocumentChanged += this.MainFormEventManager.DockPanel_ActiveDocumentChanged;
		}

		public void MainFormSerialize() {
			this.DockPanel.SaveAsXml(this.LayoutXml);
			this.GuiDataSnapshotSerializer.Serialize();
			// nope, I'm dumping when ReporterShortNamesUserInvoked.Add() & Remove()
			//foreach (var chart in this.DataSnapshot.ChartFormsManagers) {
			//	chart.DumpCurrentReportersForSerialization();
			//}
		}
	}
}
