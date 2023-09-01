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
	public partial class batch : Form
	{

		int lSelect1, lSelect2;

		public batch()
		{
			InitializeComponent();
		}

		private void fglist_MouseDown(object sender, MouseEventArgs e)
		{
			lSelect1 = fglist.SelectedIndex;
		}

		private void bglist_MouseDown(object sender, MouseEventArgs e)
		{
			lSelect2 = bglist.SelectedIndex;
		}

		private void fglist_MouseUp(object sender, MouseEventArgs e)
		{
			try
			{
				//移動先のインデックスを取得
				int listChangeNo = fglist.SelectedIndex;

				if (lSelect1 != listChangeNo) // 変更された場合のみ実行
				{
					object tmpData;

					//移動元のデータを取得
					tmpData = fglist.Items[lSelect1];

					//移動元のデータを削除
					fglist.Items.RemoveAt(lSelect1);

					//移動先にデータを追加
					fglist.Items.Insert(listChangeNo, tmpData);

					//選択先のインデックスを指定
					fglist.SelectedIndex = listChangeNo;
				}
			}
			catch
			{
				;
			}
		}

		private void bglist_MouseUp(object sender, MouseEventArgs e)
		{
			try
			{
				//移動先のインデックスを取得
				int listChangeNo = bglist.SelectedIndex;

				if (lSelect2 != listChangeNo) // 変更された場合のみ実行
				{
					object tmpData;

					//移動元のデータを取得
					tmpData = bglist.Items[lSelect2];

					//移動元のデータを削除
					bglist.Items.RemoveAt(lSelect2);

					//移動先にデータを追加
					bglist.Items.Insert(listChangeNo, tmpData);

					//選択先のインデックスを指定
					bglist.SelectedIndex = listChangeNo;
				}
			}
			catch
			{
				;
			}
		}

		private void fglist_DragDrop(object sender, DragEventArgs e)
		{
			//ドロップされたすべてのファイル名を取得する
			string[] fileName =
					(string[])e.Data.GetData(DataFormats.FileDrop, false);
			fglist.Items.AddRange(fileName);
			if ( SortCheckBox.Checked )
				fglist.Sorted = true;
      else
				fglist.Sorted = false;
		}

		private void fglist_DragEnter(object sender, DragEventArgs e)
		{
			//コントロール内にドラッグされたとき実行される
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
				//ドラッグされたデータ形式を調べ、ファイルのときはコピーとする
				e.Effect = DragDropEffects.Copy;
			else
				//ファイル以外は受け付けない
				e.Effect = DragDropEffects.None;
		}

		private void bglist_DragDrop(object sender, DragEventArgs e)
		{
			//ドロップされたすべてのファイル名を取得する
			string[] fileName =
					(string[])e.Data.GetData(DataFormats.FileDrop, false);
			bglist.Items.AddRange(fileName);
			if (SortCheckBox.Checked)
				bglist.Sorted = true;
			else
				bglist.Sorted = false;
		}

		private void bglist_DragEnter(object sender, DragEventArgs e)
		{
			//コントロール内にドラッグされたとき実行される
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
				//ドラッグされたデータ形式を調べ、ファイルのときはコピーとする
				e.Effect = DragDropEffects.Copy;
			else
				//ファイル以外は受け付けない
				e.Effect = DragDropEffects.None;
		}

		private void ClearButton_Click(object sender, EventArgs e)
		{
			bglist.Items.Clear();
			fglist.Items.Clear();
		}

		private bool nexck = true;
		private void htCngCheck_CheckedChanged(object sender, EventArgs e)
		{
			if (nexck)
				if (htCngCheck.Checked)
				{
					nexck = false;
					rotcheck.Checked = false;
					htgroup.Visible = true;
					bglist.Visible = false;
					nexck = true;
				}
				else 
				{
					nexck = false;
					//rotcheck.Checked = true;
					htgroup.Visible = false;
					bglist.Visible = true;
					nexck = true;
				}
		}

		private void rotcheck_CheckedChanged(object sender, EventArgs e)
		{
			if (nexck)
				if (rotcheck.Checked)
				{
					nexck = false;
					htCngCheck.Checked = false;
					htgroup.Visible = true;
					bglist.Visible = false;
					nexck = true;
				}
				else
				{
					nexck = false;
					//rotcheck.Checked = true;
					htgroup.Visible = false;
					bglist.Visible = true;
					nexck = true;
				}
		}
	}
}
