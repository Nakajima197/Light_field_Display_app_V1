namespace depthComposite
{
	partial class batch
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
			this.fglist = new System.Windows.Forms.ListBox();
			this.bglist = new System.Windows.Forms.ListBox();
			this.SortCheckBox = new System.Windows.Forms.CheckBox();
			this.ClearButton = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.rotcheck = new System.Windows.Forms.CheckBox();
			this.htCngCheck = new System.Windows.Forms.CheckBox();
			this.htgroup = new System.Windows.Forms.GroupBox();
			this.dnbox = new System.Windows.Forms.TextBox();
			this.upbox = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.hgCnt = new System.Windows.Forms.NumericUpDown();
			this.label3 = new System.Windows.Forms.Label();
			this.saveJpg = new System.Windows.Forms.CheckBox();
			this.htgroup.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.hgCnt)).BeginInit();
			this.SuspendLayout();
			// 
			// fglist
			// 
			this.fglist.AllowDrop = true;
			this.fglist.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.fglist.FormattingEnabled = true;
			this.fglist.ItemHeight = 12;
			this.fglist.Location = new System.Drawing.Point(1, 30);
			this.fglist.Name = "fglist";
			this.fglist.Size = new System.Drawing.Size(312, 232);
			this.fglist.TabIndex = 4;
			this.fglist.DragDrop += new System.Windows.Forms.DragEventHandler(this.fglist_DragDrop);
			this.fglist.DragEnter += new System.Windows.Forms.DragEventHandler(this.fglist_DragEnter);
			this.fglist.MouseDown += new System.Windows.Forms.MouseEventHandler(this.fglist_MouseDown);
			this.fglist.MouseUp += new System.Windows.Forms.MouseEventHandler(this.fglist_MouseUp);
			// 
			// bglist
			// 
			this.bglist.AllowDrop = true;
			this.bglist.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.bglist.FormattingEnabled = true;
			this.bglist.ItemHeight = 12;
			this.bglist.Location = new System.Drawing.Point(317, 30);
			this.bglist.Name = "bglist";
			this.bglist.Size = new System.Drawing.Size(331, 232);
			this.bglist.TabIndex = 5;
			this.bglist.DragDrop += new System.Windows.Forms.DragEventHandler(this.bglist_DragDrop);
			this.bglist.DragEnter += new System.Windows.Forms.DragEventHandler(this.bglist_DragEnter);
			this.bglist.MouseDown += new System.Windows.Forms.MouseEventHandler(this.bglist_MouseDown);
			this.bglist.MouseUp += new System.Windows.Forms.MouseEventHandler(this.bglist_MouseUp);
			// 
			// SortCheckBox
			// 
			this.SortCheckBox.AutoSize = true;
			this.SortCheckBox.Checked = true;
			this.SortCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.SortCheckBox.Location = new System.Drawing.Point(363, 9);
			this.SortCheckBox.Name = "SortCheckBox";
			this.SortCheckBox.Size = new System.Drawing.Size(68, 16);
			this.SortCheckBox.TabIndex = 19;
			this.SortCheckBox.Text = "List Sort";
			this.SortCheckBox.UseVisualStyleBackColor = true;
			// 
			// ClearButton
			// 
			this.ClearButton.Location = new System.Drawing.Point(246, 5);
			this.ClearButton.Name = "ClearButton";
			this.ClearButton.Size = new System.Drawing.Size(65, 23);
			this.ClearButton.TabIndex = 20;
			this.ClearButton.Text = "List Clear";
			this.ClearButton.UseVisualStyleBackColor = true;
			this.ClearButton.Click += new System.EventHandler(this.ClearButton_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(8, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(48, 12);
			this.label1.TabIndex = 21;
			this.label1.Text = "テクスチャ";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(317, 10);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(37, 12);
			this.label2.TabIndex = 22;
			this.label2.Text = "Zマップ";
			// 
			// rotcheck
			// 
			this.rotcheck.AutoSize = true;
			this.rotcheck.Location = new System.Drawing.Point(432, 9);
			this.rotcheck.Name = "rotcheck";
			this.rotcheck.Size = new System.Drawing.Size(48, 16);
			this.rotcheck.TabIndex = 23;
			this.rotcheck.Text = "回転";
			this.rotcheck.UseVisualStyleBackColor = true;
			this.rotcheck.CheckedChanged += new System.EventHandler(this.rotcheck_CheckedChanged);
			// 
			// htCngCheck
			// 
			this.htCngCheck.AutoSize = true;
			this.htCngCheck.Location = new System.Drawing.Point(481, 9);
			this.htCngCheck.Name = "htCngCheck";
			this.htCngCheck.Size = new System.Drawing.Size(68, 16);
			this.htCngCheck.TabIndex = 24;
			this.htCngCheck.Text = "高さ変化";
			this.htCngCheck.UseVisualStyleBackColor = true;
			this.htCngCheck.CheckedChanged += new System.EventHandler(this.htCngCheck_CheckedChanged);
			// 
			// htgroup
			// 
			this.htgroup.Controls.Add(this.dnbox);
			this.htgroup.Controls.Add(this.upbox);
			this.htgroup.Controls.Add(this.label4);
			this.htgroup.Controls.Add(this.label5);
			this.htgroup.Controls.Add(this.hgCnt);
			this.htgroup.Controls.Add(this.label3);
			this.htgroup.Location = new System.Drawing.Point(317, 33);
			this.htgroup.Name = "htgroup";
			this.htgroup.Size = new System.Drawing.Size(331, 229);
			this.htgroup.TabIndex = 25;
			this.htgroup.TabStop = false;
			this.htgroup.Text = "高さパラメータ";
			this.htgroup.Visible = false;
			// 
			// dnbox
			// 
			this.dnbox.Location = new System.Drawing.Point(87, 56);
			this.dnbox.Name = "dnbox";
			this.dnbox.Size = new System.Drawing.Size(47, 19);
			this.dnbox.TabIndex = 21;
			this.dnbox.Text = "100";
			// 
			// upbox
			// 
			this.upbox.Location = new System.Drawing.Point(87, 34);
			this.upbox.Name = "upbox";
			this.upbox.Size = new System.Drawing.Size(47, 19);
			this.upbox.TabIndex = 20;
			this.upbox.Text = "15";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(6, 60);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(79, 12);
			this.label4.TabIndex = 19;
			this.label4.Text = "最大距離[mm]";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(6, 38);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(79, 12);
			this.label5.TabIndex = 18;
			this.label5.Text = "最小距離[mm]";
			// 
			// hgCnt
			// 
			this.hgCnt.Location = new System.Drawing.Point(87, 15);
			this.hgCnt.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
			this.hgCnt.Minimum = new decimal(new int[] {
            2,
            0,
            0,
            0});
			this.hgCnt.Name = "hgCnt";
			this.hgCnt.Size = new System.Drawing.Size(47, 19);
			this.hgCnt.TabIndex = 1;
			this.hgCnt.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(6, 17);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(41, 12);
			this.label3.TabIndex = 0;
			this.label3.Text = "分割数";
			// 
			// saveJpg
			// 
			this.saveJpg.AutoSize = true;
			this.saveJpg.Location = new System.Drawing.Point(549, 8);
			this.saveJpg.Name = "saveJpg";
			this.saveJpg.Size = new System.Drawing.Size(39, 16);
			this.saveJpg.TabIndex = 26;
			this.saveJpg.Text = "jpg";
			this.saveJpg.UseVisualStyleBackColor = true;
			// 
			// batch
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(647, 263);
			this.Controls.Add(this.saveJpg);
			this.Controls.Add(this.htgroup);
			this.Controls.Add(this.htCngCheck);
			this.Controls.Add(this.rotcheck);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.ClearButton);
			this.Controls.Add(this.SortCheckBox);
			this.Controls.Add(this.bglist);
			this.Controls.Add(this.fglist);
			this.Name = "batch";
			this.Text = "batch";
			this.htgroup.ResumeLayout(false);
			this.htgroup.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.hgCnt)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox SortCheckBox;
		private System.Windows.Forms.Button ClearButton;
		public System.Windows.Forms.ListBox fglist;
		public System.Windows.Forms.ListBox bglist;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.CheckBox rotcheck;
		private System.Windows.Forms.GroupBox htgroup;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		public System.Windows.Forms.CheckBox htCngCheck;
		public System.Windows.Forms.NumericUpDown hgCnt;
		public System.Windows.Forms.TextBox dnbox;
		public System.Windows.Forms.TextBox upbox;
		public System.Windows.Forms.CheckBox saveJpg;

	}
}