using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using DigitalRune.Windows.TextEditor;
using DigitalRune.Windows.TextEditor.Actions;
using DigitalRune.Windows.TextEditor.Completion;
using DigitalRune.Windows.TextEditor.Document;
using DigitalRune.Windows.TextEditor.Formatting;
using DigitalRune.Windows.TextEditor.Highlighting;
using DigitalRune.Windows.TextEditor.Insight;
using DigitalRune.Windows.TextEditor.Markers;
using DigitalRune.Windows.TextEditor.Selection;


namespace DigitalRune.Windows.SampleEditor
{
  public partial class SampleEditor : Form
  {
    private const string defaultContent =
      "// Syntax highlighting, automatic formatting and simple folding (\"outlining\")\n"
      + "// for C# are activated.\n"
      + "//\n"
      + "// Code Completion:\n"
      + "//   To show the code completion window press <Ctrl> + <Space>.\n"
      + "//\n"
      + "// Method Insight:\n"
      + "//   To show the method insight type \"MethodA(\". The method insight\n"
      + "//   should appear automatically.\n"
      + "//\n"
      + "// Text Templates: \n"
      + "//   To insert a template type \"for\" and press <Tab>.\n"
      + "//   To view all available templates press <Ctrl> + <T>.\n"
      + "//  \n"
      + "// Tool-Tips:\n"
      + "//   Hover with your mouse cursor over a word to show a tool-tip.\n"
      + "//\n"
      + "// Markers:\n"
      + "//   The button \"Add Markers\" adds random text markers (press multiple times).\n"
      + "//\n"
      + "using System;\n"
      + "using System.Collections.Generic;\n"
      + "using System.Text;\n\n"
      + "namespace ConsoleApplication1\n"
      + "{\n"
      + "  class Program\n"
      + "  {\n"
      + "    static void Main(string[] args)\n"
      + "    {\n"
      + "      Console.Out.WriteLine(\"Hello World...\");\n"
      + "    }\n"
      + "  }\n"
      + "}";

    private string fileName = String.Empty;


    public SampleEditor()
    {
      InitializeComponent();

      // Show the default text in the editor
      textEditorControl.Document.TextContent = defaultContent;
      
      // Set the syntax-highlighting for C#
      textEditorControl.Document.HighlightingStrategy = HighlightingManager.Manager.FindHighlighter("C#");

      // Set the formatting for C#
      textEditorControl.Document.FormattingStrategy = new CSharpFormattingStrategy();

      // Set a simple folding strategy that folds all "{ ... }" blocks
      textEditorControl.Document.FoldingManager.FoldingStrategy = new CodeFoldingStrategy();

      // ----- Use the following settings for XML content instead of C#
      //textEditorControl.Document.HighlightingStrategy = HighlightingManager.Manager.FindHighlighter("XML");
      //textEditorControl.Document.FormattingStrategy = new XmlFormattingStrategy();
      //textEditorControl.Document.FoldingManager.FoldingStrategy = new XmlFoldingStrategy();
      // -----

      // Try to set font "Consolas", because it's a lot prettier:
      Font consolasFont = new Font("Consolas", 9.75f);
      if (consolasFont.Name == "Consolas")        // Set font if it is available on this machine.
        textEditorControl.Font = consolasFont;

      // Add a context menu to the text editor
      textEditorControl.ContextMenuStrip = contextMenuStrip;
    }


    private void New(object sender, EventArgs e)
    {
      textEditorControl.Document.TextContent = "";
      textEditorControl.Refresh();
    }


    private void Open(object sender, EventArgs e)
    {
      DialogResult result = openFileDialog.ShowDialog();
      if (result == DialogResult.OK)
      {
        fileName = openFileDialog.FileName;
        textEditorControl.LoadFile(fileName);
      }
    }


    private void Save(object sender, EventArgs e)
    {
      if (String.IsNullOrEmpty(fileName))
        SaveAs(sender, e);
      else 
        textEditorControl.SaveFile(fileName);
    }


    private void SaveAs(object sender, EventArgs e)
    {
      if (!String.IsNullOrEmpty(fileName))
        saveFileDialog.FileName = fileName;

      DialogResult result = saveFileDialog.ShowDialog();
      if (result == DialogResult.OK)
      {
        fileName = saveFileDialog.FileName;
        textEditorControl.SaveFile(fileName);
      }
    }


    private void Print(object sender, EventArgs e)
    {
      PrintDocument printDocument = textEditorControl.PrintDocument;
      printDialog.Document = printDocument;
      DialogResult result = printDialog.ShowDialog();

      if (result == DialogResult.OK)
        printDocument.Print();
    }


    private void PrintPreview(object sender, EventArgs e)
    {
      printPreviewDialog.Document = textEditorControl.PrintDocument;
      printPreviewDialog.ShowDialog();
    }


    private void Exit(object sender, EventArgs e)
    {
      Close();
    }


    private void Undo(object sender, EventArgs e)
    {
      Undo undo = new Undo();
      undo.Execute(textEditorControl);
    }


    private void Redo(object sender, EventArgs e)
    {
      Redo redo = new Redo();
      redo.Execute(textEditorControl);
    }


    private void Cut(object sender, EventArgs e)
    {
      Cut cut = new Cut();
      cut.Execute(textEditorControl);
    }


    private void Copy(object sender, EventArgs e)
    {
      Copy copy = new Copy();
      copy.Execute(textEditorControl);
    }


    private void Paste(object sender, EventArgs e)
    {
      Paste paste = new Paste();
      paste.Execute(textEditorControl);
    }


    private void SelectAll(object sender, EventArgs e)
    {
      SelectWholeDocument selectAll = new SelectWholeDocument();
      selectAll.Execute(textEditorControl);
    }


    private void Options(object sender, EventArgs e)
    {
      OptionsDialog optionsDialog = new OptionsDialog(textEditorControl);
      optionsDialog.ShowDialog(this);
    }


    private void About(object sender, EventArgs e)
    {
      AboutDialog aboutDialog = new AboutDialog();
      aboutDialog.ShowDialog(this);
    }


    private void UpdateFoldings(object sender, EventArgs e)
    {
      // The foldings needs to be manually updated:
      // In this example a timer updates the foldings every 2 seconds.
      // You should manually update the foldings when
      // - a new document is loaded
      // - content is added (paste)
      // - the parse-info is updated
      // - etc.
      textEditorControl.Document.FoldingManager.UpdateFolds(null, null);
    }
    

    private void CompletionRequest(object sender, CompletionEventArgs e)
    {
      if (textEditorControl.CompletionWindowVisible)
        return;

      // e.Key contains the key that the user wants to insert and which triggered
      // the CompletionRequest.
      // e.Key == '\0' means that the user triggered the CompletionRequest by pressing <Ctrl> + <Space>.
      
      if (e.Key == '\0')
      {
        // The user has requested the completion window by pressing <Ctrl> + <Space>.
        textEditorControl.ShowCompletionWindow(new CodeCompletionDataProvider(), e.Key, false);
      }
      else if (char.IsLetter(e.Key))
      {
        // The user is typing normally. 
        // -> Show the completion to provide suggestions. Automatically close the window if the 
        // word the user is typing does not match the completion data. (Last argument.)
        textEditorControl.ShowCompletionWindow(new CodeCompletionDataProvider(), e.Key, true);
      }
    }


    private void InsightRequest(object sender, InsightEventArgs e)
    {
      textEditorControl.ShowInsightWindow(new MethodInsightDataProvider());
    }


    private void ToolTipRequest(object sender, ToolTipRequestEventArgs e)
    {
      if (!e.InDocument || e.ToolTipShown)
        return;

      // Get word under cursor
      TextLocation position = e.LogicalPosition;
      LineSegment line = textEditorControl.Document.GetLineSegment(position.Y);
      if (line != null)
      {
        TextWord word = line.GetWord(position.X);
        if (word != null && !String.IsNullOrEmpty(word.Word))
          e.ShowToolTip("Current word: \"" + word.Word + "\"\n" + "\nRow: " + (position.Y + 1) + " Column: " + (position.X + 1));
      }
    }


    private void Mark(object sender, EventArgs e)
    {
      if (!textEditorControl.ActiveTextAreaControl.TextArea.SelectionManager.HasSomethingSelected)
        return;

      foreach (ISelection selection in textEditorControl.ActiveTextAreaControl.TextArea.SelectionManager.Selections)
      {
        Marker marker = new Marker(selection.Offset, selection.Length, MarkerType.SolidBlock, Color.DarkRed, Color.White);
        textEditorControl.Document.MarkerStrategy.AddMarker(marker);
      }
      textEditorControl.Refresh();
    }


    private void Underline(object sender, EventArgs e)
    {
      if (!textEditorControl.ActiveTextAreaControl.TextArea.SelectionManager.HasSomethingSelected)
        return;

      foreach (ISelection selection in textEditorControl.ActiveTextAreaControl.TextArea.SelectionManager.Selections)
      {
        Marker marker = new Marker(selection.Offset, selection.Length, MarkerType.Underlined, Color.Blue);
        textEditorControl.Document.MarkerStrategy.AddMarker(marker);
      }
      textEditorControl.Refresh();
    }


    private void Zigzag(object sender, EventArgs e)
    {
      if (!textEditorControl.ActiveTextAreaControl.TextArea.SelectionManager.HasSomethingSelected)
        return;

      foreach (ISelection selection in textEditorControl.ActiveTextAreaControl.TextArea.SelectionManager.Selections)
      {
        Marker marker = new Marker(selection.Offset, selection.Length, MarkerType.WaveLine, Color.Red);
        textEditorControl.Document.MarkerStrategy.AddMarker(marker);
      }
      textEditorControl.Refresh();
    }


    private void ClearMarkers(object sender, EventArgs e)
    {
      textEditorControl.Document.MarkerStrategy.Clear();
      textEditorControl.Refresh();
    }
  }
}