using System.ComponentModel;
using System.Windows.Forms;

namespace Sq1.Widgets.StrategiesTree {
	public partial class StrategiesTreeControl : UserControl {
		private IContainer components;
		private ImageList imageList;
		public ContextMenuStrip ctxStrategy;
		private ToolStripMenuItem mniStrategyDelete;
		private ToolStripSeparator sepStrategy;
		private ToolStripMenuItem mniStrategyEdit;
		private ToolStripMenuItem mniStrategyMoveToAnotherFolder;
		public ContextMenuStrip ctxFolder;
		public ToolStripMenuItem mniFolderCreate;
		private ToolStripMenuItem mniFolderDelete;

		protected override void Dispose(bool disposing) {
			if (disposing && this.components != null) {
				this.components.Dispose();
			}
			base.Dispose(disposing);
		}
		private void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StrategiesTreeControl));
			this.imageList = new System.Windows.Forms.ImageList(this.components);
			this.ctxStrategy = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.mniStrategyOpen = new System.Windows.Forms.ToolStripMenuItem();
			this.mniStrategyOpenWith = new System.Windows.Forms.ToolStripMenuItem();
			this.mniScriptContext1 = new System.Windows.Forms.ToolStripMenuItem();
			this.mniScriptContext2 = new System.Windows.Forms.ToolStripMenuItem();
			this.mniScriptContext3 = new System.Windows.Forms.ToolStripMenuItem();
			this.mniStrategyEdit = new System.Windows.Forms.ToolStripMenuItem();
			this.mniStrategyDuplicate = new System.Windows.Forms.ToolStripMenuItem();
			this.mniStrategyMoveToAnotherFolder = new System.Windows.Forms.ToolStripMenuItem();
			this.mniltbStrategyDuplicateTo = new Sq1.Widgets.LabeledTextBox.MenuItemLabeledTextBox();
			this.sepStrategy = new System.Windows.Forms.ToolStripSeparator();
			this.mniStrategyRename = new System.Windows.Forms.ToolStripMenuItem();
			this.mniltbStrategyRenameTo = new Sq1.Widgets.LabeledTextBox.MenuItemLabeledTextBox();
			this.mniStrategyDelete = new System.Windows.Forms.ToolStripMenuItem();
			this.ctxFolder = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.mniFolderCreate = new System.Windows.Forms.ToolStripMenuItem();
			this.mniltbFolderCreate = new Sq1.Widgets.LabeledTextBox.MenuItemLabeledTextBox();
			this.mniFolderRename = new System.Windows.Forms.ToolStripMenuItem();
			this.mniltbFolderRename = new Sq1.Widgets.LabeledTextBox.MenuItemLabeledTextBox();
			this.mniFolderDelete = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.mniFolderCreateStrategy = new System.Windows.Forms.ToolStripMenuItem();
			this.mniltbStrategyCreate = new Sq1.Widgets.LabeledTextBox.MenuItemLabeledTextBox();
			this.textBoxFilterTree = new System.Windows.Forms.TextBox();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.tree = new BrightIdeasSoftware.TreeListView();
			this.olvColumnName = new BrightIdeasSoftware.OLVColumn();
			this.olvColumnSize = new BrightIdeasSoftware.OLVColumn();
			this.olvColumnModified = new BrightIdeasSoftware.OLVColumn();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.btnClear = new System.Windows.Forms.Button();
			this.ctxStrategy.SuspendLayout();
			this.ctxFolder.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.tree)).BeginInit();
			this.tableLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// imageList
			// 
			this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
			this.imageList.TransparentColor = System.Drawing.Color.Fuchsia;
			this.imageList.Images.SetKeyName(0, "folder-closed.png");
			this.imageList.Images.SetKeyName(1, "folder-opened.png");
			this.imageList.Images.SetKeyName(2, "dll-closed.png");
			this.imageList.Images.SetKeyName(3, "dll-opened.png");
			// 
			// ctxStrategy
			// 
			this.ctxStrategy.ImageScalingSize = new System.Drawing.Size(18, 18);
			this.ctxStrategy.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.mniStrategyOpen,
									this.mniStrategyOpenWith,
									this.mniStrategyEdit,
									this.mniStrategyDuplicate,
									this.mniltbStrategyDuplicateTo,
									this.sepStrategy,
									this.mniStrategyMoveToAnotherFolder,
									this.mniStrategyRename,
									this.mniltbStrategyRenameTo,
									this.mniStrategyDelete});
			this.ctxStrategy.Name = "popupStrategy";
			this.ctxStrategy.Size = new System.Drawing.Size(226, 190);
			this.ctxStrategy.Opening += new System.ComponentModel.CancelEventHandler(this.ctxStrategy_Opening);
			// 
			// mniStrategyOpen
			// 
			this.mniStrategyOpen.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
			this.mniStrategyOpen.Name = "mniStrategyOpen";
			this.mniStrategyOpen.Size = new System.Drawing.Size(225, 22);
			this.mniStrategyOpen.Text = "Open";
			this.mniStrategyOpen.Click += new System.EventHandler(this.mniStrategyOpen_Click);
			// 
			// mniStrategyOpenWith
			// 
			this.mniStrategyOpenWith.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.mniScriptContext1,
									this.mniScriptContext2,
									this.mniScriptContext3});
			this.mniStrategyOpenWith.Name = "mniStrategyOpenWith";
			this.mniStrategyOpenWith.Size = new System.Drawing.Size(225, 22);
			this.mniStrategyOpenWith.Text = "Open Saved...";
			// 
			// mniScriptContext1
			// 
			this.mniScriptContext1.Name = "mniScriptContext1";
			this.mniScriptContext1.Size = new System.Drawing.Size(151, 22);
			this.mniScriptContext1.Text = "ScriptContext1";
			this.mniScriptContext1.Click += new System.EventHandler(this.mniStrategyOpenWithScriptContext_Click);
			// 
			// mniScriptContext2
			// 
			this.mniScriptContext2.Name = "mniScriptContext2";
			this.mniScriptContext2.Size = new System.Drawing.Size(151, 22);
			this.mniScriptContext2.Text = "ScriptContext2";
			// 
			// mniScriptContext3
			// 
			this.mniScriptContext3.Name = "mniScriptContext3";
			this.mniScriptContext3.Size = new System.Drawing.Size(151, 22);
			this.mniScriptContext3.Text = "ScriptContext3";
			// 
			// mniStrategyEdit
			// 
			this.mniStrategyEdit.ImageTransparentColor = System.Drawing.Color.Fuchsia;
			this.mniStrategyEdit.Name = "mniStrategyEdit";
			this.mniStrategyEdit.Size = new System.Drawing.Size(225, 22);
			this.mniStrategyEdit.Text = "Edit";
			this.mniStrategyEdit.Click += new System.EventHandler(this.mniStrategyEdit_Click);
			// 
			// mniStrategyDuplicate
			// 
			this.mniStrategyDuplicate.Name = "mniStrategyDuplicate";
			this.mniStrategyDuplicate.Size = new System.Drawing.Size(225, 22);
			this.mniStrategyDuplicate.Text = "Duplicate";
			this.mniStrategyDuplicate.Click += new System.EventHandler(this.mniStrategyDuplicate_Click);
			this.mniStrategyDuplicate.Visible = false;
			// 
			// mniStrategyEdit
			// 
			this.mniStrategyMoveToAnotherFolder.ImageTransparentColor = System.Drawing.Color.Fuchsia;
			this.mniStrategyMoveToAnotherFolder.Name = "mniStrategyMoveToAnotherFolder";
			this.mniStrategyMoveToAnotherFolder.Size = new System.Drawing.Size(225, 22);
			this.mniStrategyMoveToAnotherFolder.Text = "Move To...";
			// 
			// mniltbStrategyDuplicateTo
			// 
			this.mniltbStrategyDuplicateTo.BackColor = System.Drawing.Color.Transparent;
			this.mniltbStrategyDuplicateTo.InputFieldOffsetX = 80;
			this.mniltbStrategyDuplicateTo.InputFieldValue = "";
			this.mniltbStrategyDuplicateTo.InputFieldWidth = 85;
			this.mniltbStrategyDuplicateTo.Name = "mniltbStrategyDuplicateTo";
			this.mniltbStrategyDuplicateTo.Size = new System.Drawing.Size(165, 21);
			this.mniltbStrategyDuplicateTo.Text = "Duplicate To:";
			this.mniltbStrategyDuplicateTo.TextRed = false;
			this.mniltbStrategyDuplicateTo.UserTyped += new System.EventHandler<LabeledTextBox.LabeledTextBoxUserTypedArgs>(mniltbStrategyDuplicateTo_UserTyped);
			// 
			// sepStrategy
			// 
			this.sepStrategy.Name = "sepStrategy";
			this.sepStrategy.Size = new System.Drawing.Size(222, 6);
			// 
			// mniStrategyRename
			// 
			this.mniStrategyRename.Name = "mniStrategyRename";
			this.mniStrategyRename.ShortcutKeys = System.Windows.Forms.Keys.F2;
			this.mniStrategyRename.Size = new System.Drawing.Size(225, 22);
			this.mniStrategyRename.Text = "Rename";
			this.mniStrategyRename.Click += new System.EventHandler(this.mniStrategyRename_Click);
			this.mniStrategyRename.Visible = false;
			// 
			// mniltbStrategyRenameTo
			// 
			this.mniltbStrategyRenameTo.BackColor = System.Drawing.Color.Transparent;
			this.mniltbStrategyRenameTo.InputFieldOffsetX = 80;
			this.mniltbStrategyRenameTo.InputFieldValue = "";
			this.mniltbStrategyRenameTo.InputFieldWidth = 85;
			this.mniltbStrategyRenameTo.Name = "mniltbStrategyRenameTo";
			this.mniltbStrategyRenameTo.Size = new System.Drawing.Size(165, 21);
			this.mniltbStrategyRenameTo.Text = "Rename To:";
			this.mniltbStrategyRenameTo.TextRed = false;
			this.mniltbStrategyRenameTo.UserTyped += new System.EventHandler<LabeledTextBox.LabeledTextBoxUserTypedArgs>(mniltbStrategyRenameTo_UserTyped);
			// 
			// mniStrategyDelete
			// 
			this.mniStrategyDelete.ImageTransparentColor = System.Drawing.Color.Fuchsia;
			//this.mniStrategyDelete.ShortcutKeys = System.Windows.Forms.Keys.Delete;
			this.mniStrategyDelete.Name = "mniStrategyDelete";
			this.mniStrategyDelete.Size = new System.Drawing.Size(225, 22);
			this.mniStrategyDelete.Text = "Delete Strategy";
			this.mniStrategyDelete.Click += new System.EventHandler(this.mniStrategyDelete_Click);
			// 
			// ctxFolder
			// 
			this.ctxFolder.ImageScalingSize = new System.Drawing.Size(18, 18);
			this.ctxFolder.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.mniFolderCreate,
									this.mniltbFolderCreate,
									this.mniFolderRename,
									this.mniltbFolderRename,
									this.mniFolderDelete,
									this.toolStripSeparator1,
									this.mniFolderCreateStrategy,
									this.mniltbStrategyCreate});
			this.ctxFolder.Name = "popupSymbol";
			this.ctxFolder.Size = new System.Drawing.Size(226, 170);
			// 
			// mniFolderCreate
			// 
			this.mniFolderCreate.ImageTransparentColor = System.Drawing.Color.Fuchsia;
			this.mniFolderCreate.Name = "mniFolderCreate";
			this.mniFolderCreate.Size = new System.Drawing.Size(225, 22);
			this.mniFolderCreate.Text = "Create New Folder";
			this.mniFolderCreate.Visible = false;
			this.mniFolderCreate.Click += new System.EventHandler(this.mniFolderCreate_Click);
			// 
			// mniltbFolderCreate
			// 
			this.mniltbFolderCreate.BackColor = System.Drawing.Color.Transparent;
			this.mniltbFolderCreate.InputFieldOffsetX = 80;
			this.mniltbFolderCreate.InputFieldValue = "";
			this.mniltbFolderCreate.InputFieldWidth = 85;
			this.mniltbFolderCreate.Name = "mniltbFolderCreate";
			this.mniltbFolderCreate.Size = new System.Drawing.Size(165, 21);
			this.mniltbFolderCreate.Text = "Create Folder";
			this.mniltbFolderCreate.TextRed = false;
			this.mniltbFolderCreate.UserTyped += new System.EventHandler<LabeledTextBox.LabeledTextBoxUserTypedArgs>(mniltbFolderCreate_UserTyped);
			// 
			// mniFolderRename
			// 
			this.mniFolderRename.Name = "mniFolderRename";
			this.mniFolderRename.ShortcutKeys = System.Windows.Forms.Keys.F2;
			this.mniFolderRename.Size = new System.Drawing.Size(225, 22);
			this.mniFolderRename.Text = "Rename";
			this.mniFolderRename.Click += new System.EventHandler(this.mniFolderRename_Click);
			this.mniFolderRename.Visible = false;
			// 
			// mniltbFolderRename
			// 
			this.mniltbFolderRename.BackColor = System.Drawing.Color.Transparent;
			this.mniltbFolderRename.InputFieldOffsetX = 80;
			this.mniltbFolderRename.InputFieldValue = "";
			this.mniltbFolderRename.InputFieldWidth = 85;
			this.mniltbFolderRename.Name = "mniltbFolderRename";
			this.mniltbFolderRename.Size = new System.Drawing.Size(165, 21);
			this.mniltbFolderRename.Text = "Rename To";
			this.mniltbFolderRename.TextRed = false;
			this.mniltbFolderRename.UserTyped += new System.EventHandler<LabeledTextBox.LabeledTextBoxUserTypedArgs>(mniltbFolderRename_UserTyped);
			// 
			// mniFolderDelete
			// 
			this.mniFolderDelete.ImageTransparentColor = System.Drawing.Color.Fuchsia;
			//this.mniFolderDelete.ShortcutKeys = System.Windows.Forms.Keys.Delete;
			this.mniFolderDelete.Name = "mniFolderDelete";
			this.mniFolderDelete.Size = new System.Drawing.Size(225, 22);
			this.mniFolderDelete.Text = "Delete Folder";
			this.mniFolderDelete.Click += new System.EventHandler(this.mniFolderDelete_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(222, 6);
			// 
			// mniFolderCreateStrategy
			// 
			this.mniFolderCreateStrategy.Name = "mniFolderCreateStrategy";
			this.mniFolderCreateStrategy.Size = new System.Drawing.Size(225, 22);
			this.mniFolderCreateStrategy.Text = "Create New Strategy";
			this.mniFolderCreateStrategy.Click += new System.EventHandler(this.mniFolderCreateStrategy_Click);
			this.mniFolderCreateStrategy.Visible = false;
			// 
			// mniltbStrategyCreate
			// 
			this.mniltbStrategyCreate.BackColor = System.Drawing.Color.Transparent;
			this.mniltbStrategyCreate.InputFieldOffsetX = 80;
			this.mniltbStrategyCreate.InputFieldValue = "";
			this.mniltbStrategyCreate.InputFieldWidth = 85;
			this.mniltbStrategyCreate.Name = "mniltbStrategyCreate";
			this.mniltbStrategyCreate.Size = new System.Drawing.Size(165, 21);
			this.mniltbStrategyCreate.Text = "New Strategy:";
			this.mniltbStrategyCreate.TextRed = false;
			this.mniltbStrategyCreate.UserTyped += new System.EventHandler<LabeledTextBox.LabeledTextBoxUserTypedArgs>(mniltbStrategyCreate_UserTyped);
			// 
			// textBoxFilterTree
			// 
			this.textBoxFilterTree.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBoxFilterTree.Location = new System.Drawing.Point(3, 3);
			this.textBoxFilterTree.Name = "textBoxFilterTree";
			this.textBoxFilterTree.Size = new System.Drawing.Size(71, 20);
			this.textBoxFilterTree.TabIndex = 3;
			this.textBoxFilterTree.TextChanged += new System.EventHandler(this.txtFilterSymbol_TextChanged);
			// 
			// tree
			// 
			this.tree.Activation = System.Windows.Forms.ItemActivation.OneClick;
			this.tree.AllColumns.Add(this.olvColumnName);
			this.tree.AllColumns.Add(this.olvColumnSize);
			this.tree.AllColumns.Add(this.olvColumnModified);
			this.tree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
									| System.Windows.Forms.AnchorStyles.Left) 
									| System.Windows.Forms.AnchorStyles.Right)));
			this.tree.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.tree.CellEditActivation = BrightIdeasSoftware.ObjectListView.CellEditActivateMode.F2Only;
			this.tree.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
									this.olvColumnName});
			//this.tree.ContextMenuStrip = this.ctxFolder;
			this.tree.Cursor = System.Windows.Forms.Cursors.Arrow;
			this.tree.EmptyListMsg = "Right Click To Create";
			this.tree.FullRowSelect = true;
			this.tree.HeaderUsesThemes = false;
			this.tree.HideSelection = false;
			this.tree.Location = new System.Drawing.Point(0, 0);
			this.tree.Name = "StrategiesTree";
			this.tree.OwnerDraw = true;
			this.tree.ShowCommandMenuOnRightClick = true;
			this.tree.ShowGroups = false;
			this.tree.Size = new System.Drawing.Size(102, 135);
			this.tree.SmallImageList = this.imageList;
			this.tree.TabIndex = 2;
			this.tree.TintSortColumn = true;
			this.tree.UnfocusedHighlightBackgroundColor = System.Drawing.SystemColors.GradientActiveCaption;
			this.tree.UseCompatibleStateImageBehavior = false;
			this.tree.UseFilterIndicator = true;
			this.tree.UseFiltering = true;
			this.tree.UseHotItem = true;
			this.tree.UseTranslucentHotItem = true;
			this.tree.View = System.Windows.Forms.View.Details;
			this.tree.VirtualMode = true;
			this.tree.CellEditValidating += new BrightIdeasSoftware.CellEditEventHandler(this.treeListView_CellEditValidating);
			this.tree.CellClick += new System.EventHandler<BrightIdeasSoftware.CellClickEventArgs>(this.treeListView_CellClick);
			this.tree.CellRightClick += new System.EventHandler<BrightIdeasSoftware.CellRightClickEventArgs>(this.treeListView_CellRightClick);
			this.tree.ModelCanDrop += new System.EventHandler<BrightIdeasSoftware.ModelDropEventArgs>(this.treeListView_ModelCanDrop);
			this.tree.ModelDropped += new System.EventHandler<BrightIdeasSoftware.ModelDropEventArgs>(this.treeListView_ModelDropped);
			this.tree.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.treeListView_ItemSelectionChanged);
			this.tree.KeyDown += new System.Windows.Forms.KeyEventHandler(this.treeListView_KeyDown);
			this.tree.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.treeListView_MouseDoubleClick);
			// 
			// olvColumnName
			// 
			this.olvColumnName.CellPadding = null;
			this.olvColumnName.FillsFreeSpace = true;
			this.olvColumnName.Hideable = false;
			this.olvColumnName.Text = "Name";
			// 
			// olvColumnSize
			// 
			this.olvColumnSize.CellPadding = null;
			this.olvColumnSize.DisplayIndex = 1;
			this.olvColumnSize.IsVisible = false;
			this.olvColumnSize.Text = "Size";
			this.olvColumnSize.Width = 30;
			// 
			// olvColumnModified
			// 
			this.olvColumnModified.CellPadding = null;
			this.olvColumnModified.DisplayIndex = 2;
			this.olvColumnModified.IsVisible = false;
			this.olvColumnModified.Text = "Modified";
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.BackColor = System.Drawing.SystemColors.Control;
			this.tableLayoutPanel1.ColumnCount = 2;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.tableLayoutPanel1.Controls.Add(this.textBoxFilterTree, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.btnClear, 1, 0);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 135);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 1;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(102, 27);
			this.tableLayoutPanel1.TabIndex = 4;
			// 
			// btnClear
			// 
			this.btnClear.Dock = System.Windows.Forms.DockStyle.Fill;
			this.btnClear.Enabled = false;
			this.btnClear.Location = new System.Drawing.Point(80, 3);
			this.btnClear.Name = "btnClear";
			this.btnClear.Size = new System.Drawing.Size(19, 21);
			this.btnClear.TabIndex = 4;
			this.btnClear.Text = "X";
			this.btnClear.UseVisualStyleBackColor = true;
			this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
			// 
			// StrategiesTreeControl
			// 
			this.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this.Controls.Add(this.tableLayoutPanel1);
			this.Controls.Add(this.tree);
			this.Name = "StrategiesTreeControl";
			this.Size = new System.Drawing.Size(102, 162);
			this.ctxStrategy.ResumeLayout(false);
			this.ctxFolder.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.tree)).EndInit();
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);
		}

		private Sq1.Widgets.LabeledTextBox.MenuItemLabeledTextBox mniltbStrategyCreate;
		private Sq1.Widgets.LabeledTextBox.MenuItemLabeledTextBox mniltbFolderRename;
		private Sq1.Widgets.LabeledTextBox.MenuItemLabeledTextBox mniltbFolderCreate;
		private Sq1.Widgets.LabeledTextBox.MenuItemLabeledTextBox mniltbStrategyRenameTo;
		private Sq1.Widgets.LabeledTextBox.MenuItemLabeledTextBox mniltbStrategyDuplicateTo;

		public BrightIdeasSoftware.TreeListView tree;
		private TextBox textBoxFilterTree;
		private ToolTip toolTip1;
		private ToolStripMenuItem mniStrategyOpen;
		private ToolStripMenuItem mniStrategyOpenWith;
		private ToolStripMenuItem mniStrategyRename;
		private ToolStripMenuItem mniFolderRename;
		private ToolStripMenuItem mniScriptContext1;
		private ToolStripMenuItem mniScriptContext2;
		private ToolStripMenuItem mniScriptContext3;
		private ToolStripMenuItem mniStrategyDuplicate;
		private ToolStripMenuItem mniFolderCreateStrategy;
		private ToolStripSeparator toolStripSeparator1;
		private BrightIdeasSoftware.OLVColumn olvColumnName;
		private BrightIdeasSoftware.OLVColumn olvColumnSize;
		private BrightIdeasSoftware.OLVColumn olvColumnModified;
		private TableLayoutPanel tableLayoutPanel1;
		private Button btnClear;
	}
}