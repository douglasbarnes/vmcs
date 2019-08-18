using static debugger.FormSettings;
namespace debugger
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        /// 

        private void InitializeComponent()
        {
            this.gotoMemSrc = new System.Windows.Forms.TextBox();
            this.memviewer = new debugger.CustomControls.MemoryListView();
            this.label1 = new System.Windows.Forms.Label();
            this.PanelRegisters = new debugger.CustomControls.RegisterPanel();
            this.RDXLABEL = new debugger.CustomControls.RegisterLabel();
            this.RCXLABEL = new debugger.CustomControls.RegisterLabel();
            this.RBXLABEL = new debugger.CustomControls.RegisterLabel();
            this.RAXLABEL = new debugger.CustomControls.RegisterLabel();
            this.RDILABEL = new debugger.CustomControls.RegisterLabel();
            this.RSILABEL = new debugger.CustomControls.RegisterLabel();
            this.RBPLABEL = new debugger.CustomControls.RegisterLabel();
            this.RSPLABEL = new debugger.CustomControls.RegisterLabel();
            this.RIPLABEL = new debugger.CustomControls.RegisterLabel();
            this.PanelFlags = new debugger.CustomControls.RegisterPanel();
            this.LabelAuxiliary = new debugger.CustomControls.FlagLabel();
            this.LabelParity = new debugger.CustomControls.FlagLabel();
            this.LabelSign = new debugger.CustomControls.FlagLabel();
            this.LabelZero = new debugger.CustomControls.FlagLabel();
            this.LabelOverflow = new debugger.CustomControls.FlagLabel();
            this.LabelCarry = new debugger.CustomControls.FlagLabel();
            this.ButtonStep = new debugger.CustomControls.StepButton();
            this.ButtonRun = new debugger.CustomControls.StepButton();
            this.ButtonReset = new debugger.CustomControls.StepButton();
            this.ListViewDisassembly = new debugger.CustomControls.DisassemblyListView();
            this.DisassemblyPadding = new System.Windows.Forms.Panel();
            this.DisassemblyBorder = new debugger.CustomControls.BorderedPanel();
            this.PanelMemory = new debugger.CustomControls.BorderedPanel();
            this.PanelRegisters.SuspendLayout();
            this.PanelFlags.SuspendLayout();
            this.DisassemblyPadding.SuspendLayout();
            this.DisassemblyBorder.SuspendLayout();
            this.PanelMemory.SuspendLayout();
            this.SuspendLayout();
            // 
            // gotoMemSrc
            // 
            this.gotoMemSrc.Location = new System.Drawing.Point(755, 485);
            this.gotoMemSrc.MaxLength = 18;
            this.gotoMemSrc.Name = "gotoMemSrc";
            this.gotoMemSrc.Size = new System.Drawing.Size(140, 20);
            this.gotoMemSrc.TabIndex = 10;
            this.gotoMemSrc.WordWrap = false;
            this.gotoMemSrc.TextChanged += new System.EventHandler(this.SetMemviewPos);
            this.gotoMemSrc.MouseEnter += new System.EventHandler(this.GotoMemSrc_MouseEnter);
            // 
            // memviewer
            // 
            this.memviewer.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(18)))), ((int)(((byte)(18)))));
            this.memviewer.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.memviewer.DrawingLayer = debugger.FormSettings.Layer.Imminent;
            this.memviewer.Font = new System.Drawing.Font("Consolas", 11.25F);
            this.memviewer.ForeColor = System.Drawing.SystemColors.ControlLight;
            this.memviewer.HideSelection = false;
            this.memviewer.Location = new System.Drawing.Point(3, 16);
            this.memviewer.Name = "memviewer";
            this.memviewer.OwnerDraw = true;
            this.memviewer.Size = new System.Drawing.Size(391, 362);
            this.memviewer.TabIndex = 14;
            this.memviewer.TextEmphasis = debugger.FormSettings.Emphasis.High;
            this.memviewer.UseCompatibleStateImageBehavior = false;
            this.memviewer.View = System.Windows.Forms.View.Details;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(671, 488);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(33, 13);
            this.label1.TabIndex = 15;
            this.label1.Text = "Goto:";
            // 
            // PanelRegisters
            // 
            this.PanelRegisters.Controls.Add(this.RDXLABEL);
            this.PanelRegisters.Controls.Add(this.RCXLABEL);
            this.PanelRegisters.Controls.Add(this.RBXLABEL);
            this.PanelRegisters.Controls.Add(this.RAXLABEL);
            this.PanelRegisters.Controls.Add(this.RDILABEL);
            this.PanelRegisters.Controls.Add(this.RSILABEL);
            this.PanelRegisters.Controls.Add(this.RBPLABEL);
            this.PanelRegisters.Controls.Add(this.RSPLABEL);
            this.PanelRegisters.Controls.Add(this.RIPLABEL);
            this.PanelRegisters.DrawingLayer = debugger.FormSettings.Layer.Imminent;
            this.PanelRegisters.Location = new System.Drawing.Point(674, 43);
            this.PanelRegisters.Name = "PanelRegisters";
            this.PanelRegisters.Size = new System.Drawing.Size(368, 267);
            this.PanelRegisters.TabIndex = 25;
            this.PanelRegisters.Tag = "Registers";
            this.PanelRegisters.TextEmphasis = debugger.FormSettings.Emphasis.High;
            // 
            // RDXLABEL
            // 
            this.RDXLABEL.AutoSize = true;
            this.RDXLABEL.DrawingLayer = debugger.FormSettings.Layer.Surface;
            this.RDXLABEL.Location = new System.Drawing.Point(4, 190);
            this.RDXLABEL.Name = "RDXLABEL";
            this.RDXLABEL.Size = new System.Drawing.Size(146, 13);
            this.RDXLABEL.TabIndex = 8;
            this.RDXLABEL.Text = "REG : 0x0000000000000000";
            this.RDXLABEL.TextEmphasis = debugger.FormSettings.Emphasis.Medium;
            // 
            // RCXLABEL
            // 
            this.RCXLABEL.AutoSize = true;
            this.RCXLABEL.DrawingLayer = debugger.FormSettings.Layer.Surface;
            this.RCXLABEL.Location = new System.Drawing.Point(4, 170);
            this.RCXLABEL.Name = "RCXLABEL";
            this.RCXLABEL.Size = new System.Drawing.Size(146, 13);
            this.RCXLABEL.TabIndex = 7;
            this.RCXLABEL.Text = "REG : 0x0000000000000000";
            this.RCXLABEL.TextEmphasis = debugger.FormSettings.Emphasis.Medium;
            // 
            // RBXLABEL
            // 
            this.RBXLABEL.AutoSize = true;
            this.RBXLABEL.DrawingLayer = debugger.FormSettings.Layer.Surface;
            this.RBXLABEL.Location = new System.Drawing.Point(4, 150);
            this.RBXLABEL.Name = "RBXLABEL";
            this.RBXLABEL.Size = new System.Drawing.Size(146, 13);
            this.RBXLABEL.TabIndex = 6;
            this.RBXLABEL.Text = "REG : 0x0000000000000000";
            this.RBXLABEL.TextEmphasis = debugger.FormSettings.Emphasis.Medium;
            // 
            // RAXLABEL
            // 
            this.RAXLABEL.AutoSize = true;
            this.RAXLABEL.DrawingLayer = debugger.FormSettings.Layer.Surface;
            this.RAXLABEL.Location = new System.Drawing.Point(4, 130);
            this.RAXLABEL.Name = "RAXLABEL";
            this.RAXLABEL.Size = new System.Drawing.Size(146, 13);
            this.RAXLABEL.TabIndex = 5;
            this.RAXLABEL.Text = "REG : 0x0000000000000000";
            this.RAXLABEL.TextEmphasis = debugger.FormSettings.Emphasis.Medium;
            // 
            // RDILABEL
            // 
            this.RDILABEL.AutoSize = true;
            this.RDILABEL.DrawingLayer = debugger.FormSettings.Layer.Surface;
            this.RDILABEL.Location = new System.Drawing.Point(4, 110);
            this.RDILABEL.Name = "RDILABEL";
            this.RDILABEL.Size = new System.Drawing.Size(146, 13);
            this.RDILABEL.TabIndex = 4;
            this.RDILABEL.Text = "REG : 0x0000000000000000";
            this.RDILABEL.TextEmphasis = debugger.FormSettings.Emphasis.Medium;
            // 
            // RSILABEL
            // 
            this.RSILABEL.AutoSize = true;
            this.RSILABEL.DrawingLayer = debugger.FormSettings.Layer.Surface;
            this.RSILABEL.Location = new System.Drawing.Point(4, 90);
            this.RSILABEL.Name = "RSILABEL";
            this.RSILABEL.Size = new System.Drawing.Size(146, 13);
            this.RSILABEL.TabIndex = 3;
            this.RSILABEL.Text = "REG : 0x0000000000000000";
            this.RSILABEL.TextEmphasis = debugger.FormSettings.Emphasis.Medium;
            // 
            // RBPLABEL
            // 
            this.RBPLABEL.AutoSize = true;
            this.RBPLABEL.DrawingLayer = debugger.FormSettings.Layer.Surface;
            this.RBPLABEL.Location = new System.Drawing.Point(4, 70);
            this.RBPLABEL.Name = "RBPLABEL";
            this.RBPLABEL.Size = new System.Drawing.Size(146, 13);
            this.RBPLABEL.TabIndex = 2;
            this.RBPLABEL.Text = "REG : 0x0000000000000000";
            this.RBPLABEL.TextEmphasis = debugger.FormSettings.Emphasis.Medium;
            // 
            // RSPLABEL
            // 
            this.RSPLABEL.AutoSize = true;
            this.RSPLABEL.DrawingLayer = debugger.FormSettings.Layer.Surface;
            this.RSPLABEL.Location = new System.Drawing.Point(4, 50);
            this.RSPLABEL.Name = "RSPLABEL";
            this.RSPLABEL.Size = new System.Drawing.Size(146, 13);
            this.RSPLABEL.TabIndex = 1;
            this.RSPLABEL.Text = "REG : 0x0000000000000000";
            this.RSPLABEL.TextEmphasis = debugger.FormSettings.Emphasis.Medium;
            // 
            // RIPLABEL
            // 
            this.RIPLABEL.AutoSize = true;
            this.RIPLABEL.DrawingLayer = debugger.FormSettings.Layer.Surface;
            this.RIPLABEL.Location = new System.Drawing.Point(4, 30);
            this.RIPLABEL.Name = "RIPLABEL";
            this.RIPLABEL.Size = new System.Drawing.Size(146, 13);
            this.RIPLABEL.TabIndex = 0;
            this.RIPLABEL.Text = "REG : 0x0000000000000000";
            this.RIPLABEL.TextEmphasis = debugger.FormSettings.Emphasis.Medium;
            // 
            // PanelFlags
            // 
            this.PanelFlags.Controls.Add(this.LabelAuxiliary);
            this.PanelFlags.Controls.Add(this.LabelParity);
            this.PanelFlags.Controls.Add(this.LabelSign);
            this.PanelFlags.Controls.Add(this.LabelZero);
            this.PanelFlags.Controls.Add(this.LabelOverflow);
            this.PanelFlags.Controls.Add(this.LabelCarry);
            this.PanelFlags.DrawingLayer = debugger.FormSettings.Layer.Imminent;
            this.PanelFlags.Location = new System.Drawing.Point(674, 327);
            this.PanelFlags.Name = "PanelFlags";
            this.PanelFlags.Size = new System.Drawing.Size(368, 98);
            this.PanelFlags.TabIndex = 26;
            this.PanelFlags.Tag = "Flags";
            this.PanelFlags.TextEmphasis = debugger.FormSettings.Emphasis.High;
            // 
            // LabelAuxiliary
            // 
            this.LabelAuxiliary.AutoSize = true;
            this.LabelAuxiliary.DrawingLayer = debugger.FormSettings.Layer.Surface;
            this.LabelAuxiliary.Location = new System.Drawing.Point(171, 65);
            this.LabelAuxiliary.Name = "LabelAuxiliary";
            this.LabelAuxiliary.Size = new System.Drawing.Size(35, 13);
            this.LabelAuxiliary.TabIndex = 5;
            this.LabelAuxiliary.Tag = "Auxiliary";
            this.LabelAuxiliary.Text = "label2";
            this.LabelAuxiliary.TextEmphasis = debugger.FormSettings.Emphasis.Medium;
            // 
            // LabelParity
            // 
            this.LabelParity.AutoSize = true;
            this.LabelParity.DrawingLayer = debugger.FormSettings.Layer.Surface;
            this.LabelParity.Location = new System.Drawing.Point(171, 45);
            this.LabelParity.Name = "LabelParity";
            this.LabelParity.Size = new System.Drawing.Size(35, 13);
            this.LabelParity.TabIndex = 4;
            this.LabelParity.Tag = "Parity";
            this.LabelParity.Text = "label2";
            this.LabelParity.TextEmphasis = debugger.FormSettings.Emphasis.Medium;
            // 
            // LabelSign
            // 
            this.LabelSign.AutoSize = true;
            this.LabelSign.DrawingLayer = debugger.FormSettings.Layer.Surface;
            this.LabelSign.Location = new System.Drawing.Point(4, 65);
            this.LabelSign.Name = "LabelSign";
            this.LabelSign.Size = new System.Drawing.Size(35, 13);
            this.LabelSign.TabIndex = 3;
            this.LabelSign.Tag = "Sign";
            this.LabelSign.Text = "label2";
            this.LabelSign.TextEmphasis = debugger.FormSettings.Emphasis.Medium;
            // 
            // LabelZero
            // 
            this.LabelZero.AutoSize = true;
            this.LabelZero.DrawingLayer = debugger.FormSettings.Layer.Surface;
            this.LabelZero.Location = new System.Drawing.Point(171, 25);
            this.LabelZero.Name = "LabelZero";
            this.LabelZero.Size = new System.Drawing.Size(35, 13);
            this.LabelZero.TabIndex = 2;
            this.LabelZero.Tag = "Zero";
            this.LabelZero.Text = "label2";
            this.LabelZero.TextEmphasis = debugger.FormSettings.Emphasis.Medium;
            // 
            // LabelOverflow
            // 
            this.LabelOverflow.AutoSize = true;
            this.LabelOverflow.DrawingLayer = debugger.FormSettings.Layer.Surface;
            this.LabelOverflow.Location = new System.Drawing.Point(4, 45);
            this.LabelOverflow.Name = "LabelOverflow";
            this.LabelOverflow.Size = new System.Drawing.Size(35, 13);
            this.LabelOverflow.TabIndex = 1;
            this.LabelOverflow.Tag = "Overflow";
            this.LabelOverflow.Text = "label2";
            this.LabelOverflow.TextEmphasis = debugger.FormSettings.Emphasis.Medium;
            // 
            // LabelCarry
            // 
            this.LabelCarry.AutoSize = true;
            this.LabelCarry.DrawingLayer = debugger.FormSettings.Layer.Surface;
            this.LabelCarry.Location = new System.Drawing.Point(4, 25);
            this.LabelCarry.Name = "LabelCarry";
            this.LabelCarry.Size = new System.Drawing.Size(35, 13);
            this.LabelCarry.TabIndex = 0;
            this.LabelCarry.Tag = "Carry";
            this.LabelCarry.Text = "label2";
            this.LabelCarry.TextEmphasis = debugger.FormSettings.Emphasis.Medium;
            // 
            // ButtonStep
            // 
            this.ButtonStep.DrawingLayer = debugger.FormSettings.Layer.Surface;
            this.ButtonStep.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ButtonStep.Location = new System.Drawing.Point(674, 431);
            this.ButtonStep.Name = "ButtonStep";
            this.ButtonStep.Size = new System.Drawing.Size(75, 23);
            this.ButtonStep.TabIndex = 2;
            this.ButtonStep.Text = "Step";
            this.ButtonStep.TextEmphasis = debugger.FormSettings.Emphasis.Medium;
            this.ButtonStep.UseVisualStyleBackColor = true;
            this.ButtonStep.Click += new System.EventHandler(this.VMContinue_ButtonEvent);
            // 
            // ButtonRun
            // 
            this.ButtonRun.DrawingLayer = debugger.FormSettings.Layer.Surface;
            this.ButtonRun.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ButtonRun.Location = new System.Drawing.Point(755, 431);
            this.ButtonRun.Name = "ButtonRun";
            this.ButtonRun.Size = new System.Drawing.Size(75, 23);
            this.ButtonRun.TabIndex = 9;
            this.ButtonRun.Text = "Run";
            this.ButtonRun.TextEmphasis = debugger.FormSettings.Emphasis.Medium;
            this.ButtonRun.UseVisualStyleBackColor = true;
            this.ButtonRun.Click += new System.EventHandler(this.VMContinue_ButtonEvent);
            // 
            // ButtonReset
            // 
            this.ButtonReset.DrawingLayer = debugger.FormSettings.Layer.Surface;
            this.ButtonReset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ButtonReset.Location = new System.Drawing.Point(836, 431);
            this.ButtonReset.Name = "ButtonReset";
            this.ButtonReset.Size = new System.Drawing.Size(75, 23);
            this.ButtonReset.TabIndex = 20;
            this.ButtonReset.Text = "Reset";
            this.ButtonReset.TextEmphasis = debugger.FormSettings.Emphasis.Medium;
            this.ButtonReset.UseVisualStyleBackColor = true;
            this.ButtonReset.Click += new System.EventHandler(this.Reset_Click);
            // 
            // ListViewDisassembly
            // 
            this.ListViewDisassembly.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(18)))), ((int)(((byte)(18)))));
            this.ListViewDisassembly.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ListViewDisassembly.DrawingLayer = debugger.FormSettings.Layer.Imminent;
            this.ListViewDisassembly.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ListViewDisassembly.ForeColor = System.Drawing.SystemColors.ControlDark;
            this.ListViewDisassembly.FullRowSelect = true;
            this.ListViewDisassembly.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.ListViewDisassembly.HideSelection = false;
            this.ListViewDisassembly.Location = new System.Drawing.Point(0, 0);
            this.ListViewDisassembly.MultiSelect = false;
            this.ListViewDisassembly.Name = "ListViewDisassembly";
            this.ListViewDisassembly.OwnerDraw = true;
            this.ListViewDisassembly.Size = new System.Drawing.Size(627, 394);
            this.ListViewDisassembly.TabIndex = 17;
            this.ListViewDisassembly.TextEmphasis = debugger.FormSettings.Emphasis.High;
            this.ListViewDisassembly.UseCompatibleStateImageBehavior = false;
            this.ListViewDisassembly.View = System.Windows.Forms.View.Details;
            // 
            // DisassemblyPadding
            // 
            this.DisassemblyPadding.Controls.Add(this.ListViewDisassembly);
            this.DisassemblyPadding.Location = new System.Drawing.Point(3, 14);
            this.DisassemblyPadding.Name = "DisassemblyPadding";
            this.DisassemblyPadding.Size = new System.Drawing.Size(593, 394);
            this.DisassemblyPadding.TabIndex = 18;
            // 
            // DisassemblyBorder
            // 
            this.DisassemblyBorder.Controls.Add(this.DisassemblyPadding);
            this.DisassemblyBorder.DrawingLayer = debugger.FormSettings.Layer.Imminent;
            this.DisassemblyBorder.Location = new System.Drawing.Point(38, 43);
            this.DisassemblyBorder.Name = "DisassemblyBorder";
            this.DisassemblyBorder.Size = new System.Drawing.Size(599, 411);
            this.DisassemblyBorder.TabIndex = 27;
            this.DisassemblyBorder.Tag = "Disassembly";
            this.DisassemblyBorder.TextEmphasis = debugger.FormSettings.Emphasis.High;
            // 
            // PanelMemory
            // 
            this.PanelMemory.Controls.Add(this.memviewer);
            this.PanelMemory.DrawingLayer = debugger.FormSettings.Layer.Imminent;
            this.PanelMemory.Location = new System.Drawing.Point(38, 472);
            this.PanelMemory.Name = "PanelMemory";
            this.PanelMemory.Size = new System.Drawing.Size(399, 383);
            this.PanelMemory.TabIndex = 28;
            this.PanelMemory.Tag = "Memory";
            this.PanelMemory.TextEmphasis = debugger.FormSettings.Emphasis.High;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1070, 867);
            this.Controls.Add(this.PanelMemory);
            this.Controls.Add(this.PanelFlags);
            this.Controls.Add(this.PanelRegisters);
            this.Controls.Add(this.DisassemblyBorder);
            this.Controls.Add(this.ButtonReset);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.gotoMemSrc);
            this.Controls.Add(this.ButtonRun);
            this.Controls.Add(this.ButtonStep);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "MainForm";
            this.Text = "Disassembler";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.PanelRegisters.ResumeLayout(false);
            this.PanelRegisters.PerformLayout();
            this.PanelFlags.ResumeLayout(false);
            this.PanelFlags.PerformLayout();
            this.DisassemblyPadding.ResumeLayout(false);
            this.DisassemblyBorder.ResumeLayout(false);
            this.PanelMemory.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox gotoMemSrc;
        private CustomControls.MemoryListView memviewer;
        private System.Windows.Forms.Label label1;
        private CustomControls.RegisterPanel PanelRegisters;
        private CustomControls.FlagLabel LabelOverflow;
        private CustomControls.FlagLabel LabelCarry;
        private CustomControls.FlagLabel LabelAuxiliary;
        private CustomControls.FlagLabel LabelParity;
        private CustomControls.FlagLabel LabelSign;
        private CustomControls.FlagLabel LabelZero;
        private CustomControls.RegisterLabel RDXLABEL;
        private CustomControls.RegisterLabel RCXLABEL;
        private CustomControls.RegisterLabel RBXLABEL;
        private CustomControls.RegisterLabel RAXLABEL;
        private CustomControls.RegisterLabel RDILABEL;
        private CustomControls.RegisterLabel RSILABEL;
        private CustomControls.RegisterLabel RBPLABEL;
        private CustomControls.RegisterLabel RSPLABEL;
        private CustomControls.RegisterLabel RIPLABEL;
        private CustomControls.StepButton ButtonStep;
        private CustomControls.StepButton ButtonRun;
        private CustomControls.StepButton ButtonReset;
        private CustomControls.DisassemblyListView ListViewDisassembly;
        private CustomControls.BorderedPanel DisassemblyBorder;
        private System.Windows.Forms.Panel DisassemblyPadding;
        private CustomControls.BorderedPanel PanelMemory;
        private CustomControls.RegisterPanel PanelFlags;
    }
}

