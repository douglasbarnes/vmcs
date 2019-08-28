using System;
using System.Drawing;
using System.Windows.Forms;
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
        /// the contents of this method with the code editor. <-- lol!
        /// </summary>
        /// 

        private void InitializeComponent()
        {
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1070, 867);
            FormBorderStyle = FormBorderStyle.None;
            Name = "MainForm";
            Text = "Disassembler";
            Load += new EventHandler(Form1_Load);
            
        }

        #endregion
        
        
    }
}

