using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace depthComposite
{
	public partial class ViewForm : Form
	{
		private bool is_mouse_down_ = false;
		private System.Drawing.Point origin_;

		public ViewForm()
		{
			InitializeComponent();
		}

		private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == System.Windows.Forms.MouseButtons.Left)
			{
				origin_ = pictureBox1.Parent.PointToScreen(e.Location);
				Cursor.Current = Cursors.Cross; // マウスカーソルの見た目を変更
				is_mouse_down_ = true;
			}
			else
			{
				;
			}
		}

		private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
		{
			if (is_mouse_down_)
			{
				var current = pictureBox1.PointToScreen(e.Location);
				int x = current.X - origin_.X;
				int y = current.Y - origin_.Y;
				pictureBox1.Location = new System.Drawing.Point(x, y);
			}
		}

		private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
		{
			is_mouse_down_ = false;
			Cursor.Current = Cursors.Default;
			pictureBox1.Refresh();
		}
	}
}
