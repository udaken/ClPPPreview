namespace ClPPPreview.UI;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        label1 = new Label();
        label2 = new Label();
        tableLayoutPanel1 = new TableLayoutPanel();
        flowLayoutPanel2 = new FlowLayoutPanel();
        textBoxCommandLine = new TextBox();
        flowLayoutPanel1 = new FlowLayoutPanel();
        textBoxBuildToolPath = new TextBox();
        button1 = new Button();
        splitContainer1 = new SplitContainer();
        textBoxSourceCode = new RichTextBox();
        textBoxOutput = new TextBox();
        folderBrowserDialog1 = new FolderBrowserDialog();
        tableLayoutPanel1.SuspendLayout();
        flowLayoutPanel2.SuspendLayout();
        flowLayoutPanel1.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
        splitContainer1.Panel1.SuspendLayout();
        splitContainer1.Panel2.SuspendLayout();
        splitContainer1.SuspendLayout();
        SuspendLayout();
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(3, 0);
        label1.Name = "label1";
        label1.Size = new Size(128, 25);
        label1.TabIndex = 1;
        label1.Text = "&Build Tool Path";
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new Point(3, 0);
        label2.Name = "label2";
        label2.Size = new Size(132, 25);
        label2.TabIndex = 1;
        label2.Text = "&Command Line";
        // 
        // tableLayoutPanel1
        // 
        tableLayoutPanel1.ColumnCount = 1;
        tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tableLayoutPanel1.Controls.Add(flowLayoutPanel2, 0, 1);
        tableLayoutPanel1.Controls.Add(flowLayoutPanel1, 0, 0);
        tableLayoutPanel1.Controls.Add(splitContainer1, 0, 2);
        tableLayoutPanel1.Dock = DockStyle.Fill;
        tableLayoutPanel1.Location = new Point(0, 0);
        tableLayoutPanel1.Name = "tableLayoutPanel1";
        tableLayoutPanel1.RowCount = 3;
        tableLayoutPanel1.RowStyles.Add(new RowStyle());
        tableLayoutPanel1.RowStyles.Add(new RowStyle());
        tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        tableLayoutPanel1.Size = new Size(913, 936);
        tableLayoutPanel1.TabIndex = 0;
        // 
        // flowLayoutPanel2
        // 
        flowLayoutPanel2.Controls.Add(label2);
        flowLayoutPanel2.Controls.Add(textBoxCommandLine);
        flowLayoutPanel2.Dock = DockStyle.Fill;
        flowLayoutPanel2.Location = new Point(3, 57);
        flowLayoutPanel2.Name = "flowLayoutPanel2";
        flowLayoutPanel2.Size = new Size(907, 48);
        flowLayoutPanel2.TabIndex = 2;
        // 
        // textBoxCommandLine
        // 
        textBoxCommandLine.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        textBoxCommandLine.Location = new Point(141, 3);
        textBoxCommandLine.Multiline = true;
        textBoxCommandLine.Name = "textBoxCommandLine";
        textBoxCommandLine.Size = new Size(754, 45);
        textBoxCommandLine.TabIndex = 0;
        // 
        // flowLayoutPanel1
        // 
        flowLayoutPanel1.Controls.Add(label1);
        flowLayoutPanel1.Controls.Add(textBoxBuildToolPath);
        flowLayoutPanel1.Controls.Add(button1);
        flowLayoutPanel1.Dock = DockStyle.Fill;
        flowLayoutPanel1.Location = new Point(3, 3);
        flowLayoutPanel1.Name = "flowLayoutPanel1";
        flowLayoutPanel1.Size = new Size(907, 48);
        flowLayoutPanel1.TabIndex = 1;
        // 
        // textBoxBuildToolPath
        // 
        textBoxBuildToolPath.Location = new Point(137, 3);
        textBoxBuildToolPath.Name = "textBoxBuildToolPath";
        textBoxBuildToolPath.Size = new Size(633, 31);
        textBoxBuildToolPath.TabIndex = 0;
        // 
        // button1
        // 
        button1.Location = new Point(776, 3);
        button1.Name = "button1";
        button1.Size = new Size(112, 34);
        button1.TabIndex = 2;
        button1.Text = "Browse...";
        button1.UseVisualStyleBackColor = true;
        // 
        // splitContainer1
        // 
        splitContainer1.Dock = DockStyle.Fill;
        splitContainer1.Location = new Point(3, 111);
        splitContainer1.Name = "splitContainer1";
        // 
        // splitContainer1.Panel1
        // 
        splitContainer1.Panel1.Controls.Add(textBoxSourceCode);
        // 
        // splitContainer1.Panel2
        // 
        splitContainer1.Panel2.Controls.Add(textBoxOutput);
        splitContainer1.Size = new Size(907, 822);
        splitContainer1.SplitterDistance = 447;
        splitContainer1.TabIndex = 3;
        // 
        // textBoxSourceCode
        // 
        // textBoxSourceCode.AcceptsReturn = true; // Not available in RichTextBox
        textBoxSourceCode.AcceptsTab = true;
        textBoxSourceCode.Dock = DockStyle.Fill;
        textBoxSourceCode.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        textBoxSourceCode.Location = new Point(0, 0);
        textBoxSourceCode.Multiline = true;
        textBoxSourceCode.Name = "textBoxSourceCode";
        textBoxSourceCode.ScrollBars = RichTextBoxScrollBars.Vertical;
        textBoxSourceCode.Size = new Size(447, 822);
        textBoxSourceCode.TabIndex = 0;
        textBoxSourceCode.Text = "#include <stdio.h>\r\n\r\nint main()\r\n{\r\n    printf(\"Hello, World!\");\r\n}";
        // 
        // textBoxOutput
        // 
        textBoxOutput.Dock = DockStyle.Fill;
        textBoxOutput.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        textBoxOutput.Location = new Point(0, 0);
        textBoxOutput.Multiline = true;
        textBoxOutput.Name = "textBoxOutput";
        textBoxOutput.ReadOnly = true;
        textBoxOutput.ScrollBars = ScrollBars.Vertical;
        textBoxOutput.Size = new Size(456, 822);
        textBoxOutput.TabIndex = 0;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(10F, 25F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(913, 936);
        Controls.Add(tableLayoutPanel1);
        HelpButton = true;
        Name = "MainForm";
        Text = "MSVC PreProcessor Preview";
        tableLayoutPanel1.ResumeLayout(false);
        flowLayoutPanel2.ResumeLayout(false);
        flowLayoutPanel2.PerformLayout();
        flowLayoutPanel1.ResumeLayout(false);
        flowLayoutPanel1.PerformLayout();
        splitContainer1.Panel1.ResumeLayout(false);
        splitContainer1.Panel1.PerformLayout();
        splitContainer1.Panel2.ResumeLayout(false);
        splitContainer1.Panel2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
        splitContainer1.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel tableLayoutPanel1;
    private FlowLayoutPanel flowLayoutPanel2;
    private Label label2;
    private TextBox textBoxCommandLine;
    private FlowLayoutPanel flowLayoutPanel1;
    private Label label1;
    private TextBox textBoxBuildToolPath;
    private TextBox textBoxVsDevCmdPath;
    private SplitContainer splitContainer1;
    private Button button1;
    private Button buttonHelp;
    private RichTextBox textBoxSourceCode;
    private TextBox textBoxOutput;
    private FolderBrowserDialog folderBrowserDialog1;
}
