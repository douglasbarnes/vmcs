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
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.generalregs = new System.Windows.Forms.ListBox();
            this.button1 = new System.Windows.Forms.Button();
            this.specialregs = new System.Windows.Forms.ListBox();
            this.IsDec = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.memviewer = new System.Windows.Forms.ListBox();
            this.button2 = new System.Windows.Forms.Button();
            this.gotoMemSrc = new System.Windows.Forms.TextBox();
            this.gotoaddr = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(92, 37);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(646, 381);
            this.listBox1.TabIndex = 0;
            // 
            // generalregs
            // 
            this.generalregs.FormattingEnabled = true;
            this.generalregs.Location = new System.Drawing.Point(777, 199);
            this.generalregs.Name = "generalregs";
            this.generalregs.Size = new System.Drawing.Size(341, 82);
            this.generalregs.TabIndex = 1;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(777, 356);
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
            // memviewer
            // 
            this.memviewer.Font = new System.Drawing.Font("Consolas", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.memviewer.FormattingEnabled = true;
            this.memviewer.ItemHeight = 18;
            this.memviewer.Location = new System.Drawing.Point(92, 446);
            this.memviewer.Name = "memviewer";
            this.memviewer.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.memviewer.Size = new System.Drawing.Size(1026, 220);
            this.memviewer.TabIndex = 7;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(858, 356);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 9;
            this.button2.Text = "Run";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.Button2_Click);
            // 
            // gotoMemSrc
            // 
            this.gotoMemSrc.Location = new System.Drawing.Point(173, 674);
            this.gotoMemSrc.Name = "gotoMemSrc";
            this.gotoMemSrc.Size = new System.Drawing.Size(100, 20);
            this.gotoMemSrc.TabIndex = 10;
            this.gotoMemSrc.TextChanged += new System.EventHandler(this.SetMemviewPos);
            // 
            // gotoaddr
            // 
            this.gotoaddr.Location = new System.Drawing.Point(92, 672);
            this.gotoaddr.Name = "gotoaddr";
            this.gotoaddr.Size = new System.Drawing.Size(75, 23);
            this.gotoaddr.TabIndex = 13;
            this.gotoaddr.Text = "Goto";
            this.gotoaddr.UseVisualStyleBackColor = true;
            this.gotoaddr.Click += new System.EventHandler(this.SetMemviewPos);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1685, 867);
            this.Controls.Add(this.gotoaddr);
            this.Controls.Add(this.gotoMemSrc);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.memviewer);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.specialregs);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.generalregs);
            this.Controls.Add(this.listBox1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.ListBox generalregs;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ListBox specialregs;
        private System.Windows.Forms.RadioButton IsDec;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ListBox memviewer;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox gotoMemSrc;
        private System.Windows.Forms.Button gotoaddr;
    }
}

