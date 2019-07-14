namespace debugger
{
    partial class Form1
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
        private void InitializeComponent()
        {
            this.generalregs = new System.Windows.Forms.ListBox();
            this.button1 = new System.Windows.Forms.Button();
            this.specialregs = new System.Windows.Forms.ListBox();
            this.IsDec = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.button2 = new System.Windows.Forms.Button();
            this.gotoMemSrc = new System.Windows.Forms.TextBox();
            this.memviewer = new System.Windows.Forms.ListView();
            this.Address = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Bytes = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.label1 = new System.Windows.Forms.Label();
            this.eflags = new System.Windows.Forms.ListBox();
            this.disassembly = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // generalregs
            // 
            this.generalregs.FormattingEnabled = true;
            this.generalregs.Location = new System.Drawing.Point(777, 199);
            this.generalregs.Name = "generalregs";
            this.generalregs.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.generalregs.Size = new System.Drawing.Size(341, 82);
            this.generalregs.TabIndex = 1;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(777, 395);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 2;
            this.button1.Text = "Step";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.Step_Click);
            // 
            // specialregs
            // 
            this.specialregs.FormattingEnabled = true;
            this.specialregs.Location = new System.Drawing.Point(777, 86);
            this.specialregs.Name = "specialregs";
            this.specialregs.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.specialregs.Size = new System.Drawing.Size(341, 82);
            this.specialregs.TabIndex = 3;
            // 
            // IsDec
            // 
            this.IsDec.AutoSize = true;
            this.IsDec.Location = new System.Drawing.Point(84, 20);
            this.IsDec.Name = "IsDec";
            this.IsDec.Size = new System.Drawing.Size(45, 17);
            this.IsDec.TabIndex = 4;
            this.IsDec.Text = "Dec";
            this.IsDec.UseVisualStyleBackColor = true;
            this.IsDec.CheckedChanged += new System.EventHandler(this._refresh);
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Checked = true;
            this.radioButton2.Location = new System.Drawing.Point(20, 20);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(44, 17);
            this.radioButton2.TabIndex = 5;
            this.radioButton2.TabStop = true;
            this.radioButton2.Text = "Hex";
            this.radioButton2.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.IsDec);
            this.groupBox1.Controls.Add(this.radioButton2);
            this.groupBox1.Location = new System.Drawing.Point(777, 37);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(262, 43);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Format";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(861, 395);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 9;
            this.button2.Text = "Run";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.Button2_Click);
            // 
            // gotoMemSrc
            // 
            this.gotoMemSrc.Location = new System.Drawing.Point(131, 464);
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
            this.memviewer.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Address,
            this.Bytes});
            this.memviewer.Font = new System.Drawing.Font("Consolas", 11.25F);
            this.memviewer.HideSelection = false;
            this.memviewer.Location = new System.Drawing.Point(92, 490);
            this.memviewer.Name = "memviewer";
            this.memviewer.Size = new System.Drawing.Size(586, 365);
            this.memviewer.TabIndex = 14;
            this.memviewer.UseCompatibleStateImageBehavior = false;
            this.memviewer.View = System.Windows.Forms.View.Details;
            // 
            // Address
            // 
            this.Address.Text = "Address";
            this.Address.Width = 170;
            // 
            // Bytes
            // 
            this.Bytes.Text = "0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F ";
            this.Bytes.Width = 400;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(92, 467);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(33, 13);
            this.label1.TabIndex = 15;
            this.label1.Text = "Goto:";
            // 
            // eflags
            // 
            this.eflags.FormattingEnabled = true;
            this.eflags.Location = new System.Drawing.Point(777, 307);
            this.eflags.Name = "eflags";
            this.eflags.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.eflags.Size = new System.Drawing.Size(341, 82);
            this.eflags.TabIndex = 16;
            // 
            // disassembly
            // 
            this.disassembly.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.disassembly.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.disassembly.Location = new System.Drawing.Point(82, 37);
            this.disassembly.Name = "disassembly";
            this.disassembly.Size = new System.Drawing.Size(596, 411);
            this.disassembly.TabIndex = 17;
            this.disassembly.UseCompatibleStateImageBehavior = false;
            this.disassembly.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Disassembly";
            this.columnHeader1.Width = 550;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1685, 867);
            this.Controls.Add(this.disassembly);
            this.Controls.Add(this.eflags);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.memviewer);
            this.Controls.Add(this.gotoMemSrc);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.specialregs);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.generalregs);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ListBox generalregs;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ListBox specialregs;
        private System.Windows.Forms.RadioButton IsDec;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox gotoMemSrc;
        private System.Windows.Forms.ListView memviewer;
        private System.Windows.Forms.ColumnHeader Address;
        private System.Windows.Forms.ColumnHeader Bytes;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox eflags;
        private System.Windows.Forms.ListView disassembly;
        private System.Windows.Forms.ColumnHeader columnHeader1;
    }
}

