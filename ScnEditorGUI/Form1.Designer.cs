namespace ScnEditorGUI
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
            this.components = new System.ComponentModel.Container();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.openFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.huffmanToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.decompressImageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.compressImageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tryRecoveryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // listBox1
            // 
            this.listBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listBox1.FormattingEnabled = true;
            this.listBox1.ItemHeight = 16;
            this.listBox1.Location = new System.Drawing.Point(16, 15);
            this.listBox1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(579, 340);
            this.listBox1.TabIndex = 0;
            this.listBox1.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
            // 
            // textBox1
            // 
            this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox1.Location = new System.Drawing.Point(16, 357);
            this.textBox1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(579, 22);
            this.textBox1.TabIndex = 1;
            this.textBox1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBox1_KeyPress);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openFileToolStripMenuItem,
            this.saveFileToolStripMenuItem,
            this.huffmanToolStripMenuItem,
            this.tryRecoveryToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(182, 136);
            // 
            // openFileToolStripMenuItem
            // 
            this.openFileToolStripMenuItem.Name = "openFileToolStripMenuItem";
            this.openFileToolStripMenuItem.Size = new System.Drawing.Size(181, 26);
            this.openFileToolStripMenuItem.Text = "Open File";
            this.openFileToolStripMenuItem.Click += new System.EventHandler(this.openFileToolStripMenuItem_Click);
            // 
            // saveFileToolStripMenuItem
            // 
            this.saveFileToolStripMenuItem.Name = "saveFileToolStripMenuItem";
            this.saveFileToolStripMenuItem.Size = new System.Drawing.Size(181, 26);
            this.saveFileToolStripMenuItem.Text = "Save File";
            this.saveFileToolStripMenuItem.Click += new System.EventHandler(this.saveFileToolStripMenuItem_Click);
            // 
            // huffmanToolStripMenuItem
            // 
            this.huffmanToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.decompressImageToolStripMenuItem,
            this.compressImageToolStripMenuItem});
            this.huffmanToolStripMenuItem.Name = "huffmanToolStripMenuItem";
            this.huffmanToolStripMenuItem.Size = new System.Drawing.Size(181, 26);
            this.huffmanToolStripMenuItem.Text = "Huffman";
            // 
            // decompressImageToolStripMenuItem
            // 
            this.decompressImageToolStripMenuItem.Name = "decompressImageToolStripMenuItem";
            this.decompressImageToolStripMenuItem.Size = new System.Drawing.Size(212, 26);
            this.decompressImageToolStripMenuItem.Text = "Decompress Image";
            this.decompressImageToolStripMenuItem.Click += new System.EventHandler(this.decompressImageToolStripMenuItem_Click);
            // 
            // compressImageToolStripMenuItem
            // 
            this.compressImageToolStripMenuItem.Name = "compressImageToolStripMenuItem";
            this.compressImageToolStripMenuItem.Size = new System.Drawing.Size(212, 26);
            this.compressImageToolStripMenuItem.Text = "Compress Image";
            this.compressImageToolStripMenuItem.Click += new System.EventHandler(this.compressImageToolStripMenuItem_Click);
            // 
            // tryRecoveryToolStripMenuItem
            // 
            this.tryRecoveryToolStripMenuItem.Name = "tryRecoveryToolStripMenuItem";
            this.tryRecoveryToolStripMenuItem.Size = new System.Drawing.Size(181, 26);
            this.tryRecoveryToolStripMenuItem.Text = "Try Recovery";
            this.tryRecoveryToolStripMenuItem.Click += new System.EventHandler(this.tryRecoveryToolStripMenuItem_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(612, 396);
            this.ContextMenuStrip = this.contextMenuStrip1;
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.listBox1);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "Form1";
            this.Text = "Scn Editor";
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem openFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem huffmanToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem decompressImageToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem compressImageToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tryRecoveryToolStripMenuItem;
    }
}

