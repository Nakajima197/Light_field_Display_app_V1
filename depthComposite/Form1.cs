using AForge.Imaging.Filters;
using FreeImageAPI;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Numerics;

namespace depthComposite
{
	public partial class Form1 : Form
	{
		#region	パラメータ

		const int BYTEL = 4;
		bool org_dpt;  // 選択中の絵は 絵本体: true / depth MAP: false
		private Bitmap BMP1;			// 変換された絵の入れ物
		private double FOCUS = 7.0;		// 焦点距離
		private double PIXEL = 0.1245;	// 1 pixel [mm]
		private int DATANUM = 168;		// MLA 1個に含まれる画素数
		private uint[] d3;
		private double fv;
		private double brt = 1.0; //倍率
		private int saveMD = 0;

		private double[] celx ={
									   0, 1,
								-2,-1, 0, 1, 2, 3,
						     -3,-2,-1, 0, 1, 2, 3, 4,
					   -5,-4,-3,-2,-1, 0, 1, 2, 3, 4, 5, 6,
					-6,-5,-4,-3,-2,-1, 0, 1, 2, 3, 4, 5, 6, 7,
					-6,-5,-4,-3,-2,-1, 0, 1, 2, 3, 4, 5, 6, 7,
					-6,-5,-4,-3,-2,-1, 0, 1, 2, 3, 4, 5, 6, 7,
					-6,-5,-4,-3,-2,-1, 0, 1, 2, 3, 4, 5, 6, 7,
					-6,-5,-4,-3,-2,-1, 0, 1, 2, 3, 4, 5, 6, 7,
					-6,-5,-4,-3,-2,-1, 0, 1, 2, 3, 4, 5, 6, 7,
					-6,-5,-4,-3,-2,-1, 0, 1, 2, 3, 4, 5, 6, 7,
					-6,-5,-4,-3,-2,-1, 0, 1, 2, 3, 4, 5, 6, 7,
					   -5,-4,-3,-2,-1, 0, 1, 2, 3, 4, 5, 6,
						     -3,-2,-1, 0, 1, 2, 3, 4,
								-2,-1, 0, 1, 2, 3,
									   0, 1
                       };

		private int[] cely ={
									  -7,-7,
								-6,-6,-6,-6,-6,-6,
						     -5,-5,-5,-5,-5,-5,-5,-5,
					   -4,-4,-4,-4,-4,-4,-4,-4,-4,-4,-4,-4,
					-3,-3,-3,-3,-3,-3,-3,-3,-3,-3,-3,-3,-3,-3,
					-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,
					-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
					 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
					 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
					 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
					 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
					 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
						5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
						      6, 6, 6, 6, 6, 6, 6, 6,
								 7, 7, 7, 7, 7, 7,
									   8, 8
                       };

		[StructLayout(LayoutKind.Sequential)]
		struct whichMla
		{
			public int[] dpx;
			public int[] dpy;
		}

		//whichMla Mla;

		bool rendo = false;			// XY連動中

		private struct cel
		{
			public int[] celDx; //最大絞りの配列
			public int[] celDy;
			//public byte[] celFp; //強度
			//public int ponum;
			//public int[] poDx;  //瞳孔径に含まれる部分の配列
			//public int[] poDy;
		}

		/// <summary>
		/// パータン
		/// </summary>
		public struct cel1
		{
			/// <summary>
			/// X方向
			/// </summary>
			public int[] celDx;
			/// <summary>
			/// Y方向
			/// </summary>
			public int[] celDy;
			/// <summary>
			/// 強度
			/// </summary>
			public byte[] celFp;
			/// <summary>
			/// 強度の合算
			/// </summary>
			public uint all;
		}
		private cel PG;

		private Bitmap GUR1;

		#endregion

		#region フォーム処理

		ArrayList dispList = new ArrayList();
		ViewForm m_vForm;
		batch m_batch;

		public Form1()
		{
			InitializeComponent();

			OrgPictureBox.AllowDrop = true;
			DepthPictureBox.AllowDrop = true;
			//Application.ScreenUpdating = true;

			calculationB(); //パターン描画

			// サブ View をフルスクリーン表示にする
			m_vForm = new ViewForm();
			m_vForm.Show();
			m_vForm.StartPosition = FormStartPosition.Manual;
			m_vForm.Location = new Point(0, 0);
			m_vForm.ControlBox = false;
			m_vForm.Text = "";

			// 複数ディスプレイか調べて ComboBox に入れる
			foreach (Screen s in Screen.AllScreens)
			{
				this.screenCombo.Items.Add(s.DeviceName);
				dispList.Add(s);

				this.screenCombo.SelectedIndex = dispList.Count - 1;
				this.m_vForm.SetDesktopLocation(s.Bounds.X, s.Bounds.Y);
			}

			// 何もなければ先頭選択
			if (this.screenCombo.SelectedIndex == -1)
			{
				this.screenCombo.SelectedIndex = 0;
			}
		}

		private void screenCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			// 一旦 Normal に戻さないと移動できない
			this.m_vForm.WindowState = FormWindowState.Normal;
			Screen s = (Screen)dispList[screenCombo.SelectedIndex];
			this.m_vForm.SetDesktopLocation(s.Bounds.X, s.Bounds.Y);
			this.m_vForm.WindowState = FormWindowState.Maximized;

			this.Activate();
		}

		private void exitButton_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		#endregion

		#region ファイル処理
		private void OrgPictureLoad_Click(object sender, EventArgs e)
		{
			org_dpt = true;

			PictureOpenFileDialog.FileName = "*.png";
			PictureOpenFileDialog.DefaultExt = "png";
			PictureOpenFileDialog.Title = "Load";
			PictureOpenFileDialog.Filter = "Pict File(*.png)|*.png|Pict File(*.bmp)|*.bmp|ALL|*.*";

			PictureOpenFileDialog.ShowDialog();
		}

		private void OrgPictureOpenFileDialog_FileOk(object sender, CancelEventArgs e)
		{
			if (org_dpt)
			{
				OrgPictureBox.LoadAsync(PictureOpenFileDialog.FileName);
				FNametextBox.Text = PictureOpenFileDialog.FileName;
			}
			else
			{
				DepthPictureBox.LoadAsync(PictureOpenFileDialog.FileName);
				ZNametextBox.Text = PictureOpenFileDialog.FileName;
			}
		}

		/// <summary>
		/// テスト画像
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void testPict_Click(object sender, EventArgs e)
		{
			FNametextBox.Text = "..\\..\\..\\pict2\\FG1.png";
			ZNametextBox.Text = "..\\..\\..\\pict2\\BG1.png";

			OrgPictureBox.Load(FNametextBox.Text);
			DepthPictureBox.Load(ZNametextBox.Text);
			// カレントディレクトリを取得する
			//string stCurrentDir = System.IO.Directory.GetCurrentDirectory();

			// カレントディレクトリを表示する
			//MessageBox.Show(stCurrentDir);
			TestText.Text = OrgPictureBox.Image.Width.ToString() + " " + OrgPictureBox.Image.Height.ToString();
		}

		private void testPict2_Click(object sender, EventArgs e)
		{
			FNametextBox.Text = "..\\..\\..\\pict2\\M3.png";
			ZNametextBox.Text = "..\\..\\..\\pict2\\M3Z.png";
			OrgPictureBox.Load(FNametextBox.Text);
			DepthPictureBox.Load(ZNametextBox.Text);
			
			TestText.Text = OrgPictureBox.Image.Width.ToString() + " " + OrgPictureBox.Image.Height.ToString();
		}

		private void testPict3_Click(object sender, EventArgs e)
		{
			FNametextBox.Text = "..\\..\\..\\pict2\\R1.png";
			OrgPictureBox.Load(FNametextBox.Text);

			TestText.Text = OrgPictureBox.Image.Width.ToString() + " " + OrgPictureBox.Image.Height.ToString();
		}

		private void testPict4_Click(object sender, EventArgs e)
		{
			FNametextBox.Text = "..\\..\\..\\pict2\\P1.png";
			OrgPictureBox.Load(FNametextBox.Text);

			TestText.Text = OrgPictureBox.Image.Width.ToString() + " " + OrgPictureBox.Image.Height.ToString();
		}

		private void testPict5_Click(object sender, EventArgs e)
		{
			FNametextBox.Text = "..\\..\\..\\pict2\\dragonFs.png";
			ZNametextBox.Text = "..\\..\\..\\pict2\\dragonZs.png";
			OrgPictureBox.Load(FNametextBox.Text);
			DepthPictureBox.Load(ZNametextBox.Text);

			TestText.Text = OrgPictureBox.Image.Width.ToString() + " " + OrgPictureBox.Image.Height.ToString();
		}

		private void mikubutton_Click(object sender, EventArgs e)
		{
			FNametextBox.Text = "..\\..\\..\\pict2\\mikufg.png";
			ZNametextBox.Text = "..\\..\\..\\pict2\\mikuz.png";
			OrgPictureBox.Load(FNametextBox.Text);
			DepthPictureBox.Load(ZNametextBox.Text);
			alphacheck.Checked = true;

			TestText.Text = OrgPictureBox.Image.Width.ToString() + " " + OrgPictureBox.Image.Height.ToString();
		}

		private void DepthPictureLoadButton_Click(object sender, EventArgs e)
		{
			org_dpt = false;

			PictureOpenFileDialog.FileName = "*.png";
			PictureOpenFileDialog.DefaultExt = "png";
			PictureOpenFileDialog.Title = "Load";
			PictureOpenFileDialog.Filter = "Pict File(*.png)|*.png|Pict File(*.bmp)|*.bmp|ALL|*.*";

			PictureOpenFileDialog.ShowDialog();
		}

		private void OrgPictureBox_DragDrop(object sender, DragEventArgs e)
		{
			//ドロップされたファイル名を取得する
			string[] fileName =
					(string[])e.Data.GetData(DataFormats.FileDrop, false);
			OrgPictureBox.Load(fileName[0]);
			FNametextBox.Text = fileName[0];

			if (DepthPictureBox.Image != null)
			{
				if (DepthPictureBox.Image.Width != OrgPictureBox.Image.Width)
					MessageBox.Show("サイズが違う！", "タイトル");
			}
		}

		private void OrgPictureBox_DragEnter(object sender, DragEventArgs e)
		{
			//コントロール内にドラッグされたとき実行される
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
				//ドラッグされたデータ形式を調べ、ファイルのときはコピーとする
				e.Effect = DragDropEffects.Copy;
			else
				//ファイル以外は受け付けない
				e.Effect = DragDropEffects.None;
		}

		private void DepthPictureBox_DragEnter(object sender, DragEventArgs e)
		{
			//コントロール内にドラッグされたとき実行される
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
				//ドラッグされたデータ形式を調べ、ファイルのときはコピーとする
				e.Effect = DragDropEffects.Copy;
			else
				//ファイル以外は受け付けない
				e.Effect = DragDropEffects.None;
		}

		private void DepthPictureBox_DragDrop(object sender, DragEventArgs e)
		{
			//ドロップされたすべてのファイル名を取得する
			string[] fileName =
					(string[])e.Data.GetData(DataFormats.FileDrop, false);
			DepthPictureBox.Load(fileName[0]);
			ZNametextBox.Text = fileName[0];

			if (OrgPictureBox.Image != null)
			{
				if (DepthPictureBox.Image.Width != OrgPictureBox.Image.Width)
					MessageBox.Show("サイズが違う！", "タイトル");
			}
		}

		/// <summary>
		/// 生成データを保存する
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SaveButton_Click(object sender, EventArgs e)
		{
			string ta;
			if (constcheck.Checked) ta = "_" + mDepth.Text;
			else ta = "_" + upbox.Text + dnbox.Text;

			if (cZ5p.Checked)
				ta += "-z5p";
			if (nex5ck.Checked)
				ta += "-nx5";
			if (nex6ck.Checked)
			{
				ta = "n-" + (int.Parse(dnbox.Text) * -1).ToString() + "_" + (int.Parse(upbox.Text) * -1).ToString();
				ta += "-nx6";
			}
			if (nex10ck.Checked)
				ta += "-n10";
			if (k321ck.Checked)
				ta += "-k321";

			saveFileDialog1.FileName = System.DateTime.Now.ToString().Replace("/", "-").Replace(":", "-").Replace(" ", "_") 
				+ ta + ".png";

			saveFileDialog1.DefaultExt = "png";
			saveFileDialog1.Title = "SaveFile";
			saveFileDialog1.Filter = "PNG File(*.png)|*.png|BMP File(*.bmp)|*.bmp|JPEG File(*.jpg)|*.jpg|ALL|*.*";

			saveFileDialog1.ShowDialog();
		}

		/// <summary>
		/// 生成データを保存
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
		{
			//int mode = 0;
			//ConvPictureBox.Image.Save(saveFileDialog1.FileName);
			String name = saveFileDialog1.FileName;

			//if (nex5ck.Checked)
			//	nexPictOut(m_vForm.pictureBox1.Image, saveFileDialog1.FileName, 0.978d, 1.0d, 1920, 1080); //Nexus5
			//else if (nex10ck.Checked)
			//	nexPictOut(m_vForm.pictureBox1.Image, saveFileDialog1.FileName, 0.8443d, 0.8531d, 2560, 1600);//Nexus10
			//else if (k321ck.Checked)
			//	nexPictOut(m_vForm.pictureBox1.Image, saveFileDialog1.FileName, 0.684d, 0.684d, 3840, 2160);//K321
			if (nex5ck.Checked)
				saveMode(1, m_vForm.pictureBox1.Image, name); //Nexus5
			else if (nex10ck.Checked)
				saveMode(2, m_vForm.pictureBox1.Image, name);//Nexus10
			else if (k321ck.Checked)
				saveMode(3, m_vForm.pictureBox1.Image, name);//K321
			else if (nex6ck.Checked)
				saveMode(4, m_vForm.pictureBox1.Image, name);//Nexus6
			else if (cZ5p.Checked)
				saveMode(5, m_vForm.pictureBox1.Image, name);//Xperia Z5 Premium
			else
				savePict((Bitmap)m_vForm.pictureBox1.Image, name);
		}

		private void saveMode(int mode, Image img, String name)
		{
			switch(mode){
				case 1:
					//nexPictOut(img, name, 0.978d, 1.0d, 1920, 1080); //Nexus5
					nexPictOut(img, name, 0.978d, 1.000d, 1920, 1080); //Nexus5
					break;
				case 2:
					nexPictOut(img, name, 0.8443d, 0.8531d, 2560, 1600);//Nexus10
					break;
				case 3:
					nexPictOut(img, name, 0.684d, 0.684d, 3840, 2160);//K321
					break;
				case 4: 
					nexPictOut(img, name, 0.944d, 0.9556d, 2560, 1440);//Nexus6
					break;
				case 5:
					nexPictOut(img, name, 1.012d, 1.0175d, 3840, 2160);//Xperia Z5 Premium
					break;
				default:
					nexPictOut(img, name, 1d, 1d, img.Width, img.Height);
					break;
			}
		}

		/// <summary>
		/// リサイズして保存する
		/// </summary>
		/// <param name="img"></param>
		/// <param name="fname">ファイル名</param>
		/// <param name="xw">倍率</param>
		/// <param name="yw">倍率</param>
		/// <param name="wd"></param>
		/// <param name="ht"></param>
		/// </summary>
		private void nexPictOut(Image img,string fname, double xw, double yw, int wd, int ht)
		{
			int w0 = img.Width;
			int h0 = img.Height;

			int w1 = (int)Math.Round((double)w0 * xw); //各種MLA用に横方向を補正する
			int h1 = (int)Math.Round((double)h0 * yw); //各種MLA用に縦方向を補正する

			Bitmap BMP0 = reSizePic(img, w1, h1);

			//リサイズ
			int w2 = (w1 - wd) / 2;
			int h2 = (h1 - ht) / 2;
			Bitmap BMP2 = new Bitmap(wd, ht, PixelFormat.Format32bppArgb);
			Rectangle srcRect = new Rectangle(w2, h2, wd, ht);
			Rectangle desRect = new Rectangle(0, 0, wd, ht);
			using (Graphics g = Graphics.FromImage(BMP2))
			{
				g.DrawImage(BMP0, desRect, srcRect, GraphicsUnit.Pixel);
			}

			//BMP2.Save(fname);
			savePict(BMP2, fname);
		}

		/// <summary>
		/// 倍率変更
		/// </summary>
		/// <param name="src"></param>
		/// <param name="wd"></param>
		/// <param name="ht"></param>
		/// <returns></returns>
		private Bitmap reSizePic(Image src, int wd, int ht)
		{
			Bitmap BM = new Bitmap(wd, ht, PixelFormat.Format32bppArgb);
			using (Graphics g = Graphics.FromImage(BM))
			{
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;
				g.PixelOffsetMode = PixelOffsetMode.HighQuality;
				g.DrawImage(src, 0, 0, wd, ht);
			}
			return BM;
		}

		/// <summary>
		/// 実際に保存するところ
		/// </summary>
		/// <param name="bmp"></param>
		/// <param name="name"></param>
		private void savePict(Bitmap bmp, String name)
		{
			string ex = Path.GetExtension(name);
			if (ex == ".bmp")
			{
				bmp.Save(name, System.Drawing.Imaging.ImageFormat.Bmp);
			} 
			else if (ex == ".png")
			{
				bmp.Save(name, System.Drawing.Imaging.ImageFormat.Png);
			}
			else if (ex == ".jpg")
			{
				//Jpeg品質設定
				FREE_IMAGE_SAVE_FLAGS fREE_IMAGE_SAVE_FLAGS =
					(FREE_IMAGE_SAVE_FLAGS)((int)JpgQuality1.Value
					| (int)FREE_IMAGE_SAVE_FLAGS.JPEG_SUBSAMPLING_444
					| (int)FREE_IMAGE_SAVE_FLAGS.DEFAULT);

				FreeImageBitmap fib = new FreeImageBitmap(bmp);
				fib.Save(name, FREE_IMAGE_FORMAT.FIF_JPEG, fREE_IMAGE_SAVE_FLAGS);
			}
		}

		#endregion

		#region 旧タイプMLA
		/// <summary>
		/// 岩根さん関数
		/// </summary>
		/// <param name="y"></param>
		/// <param name="hdata"></param>
		/// <param name="vdata"></param>
		/// <returns></returns>
		private whichMla patnCalcAnlg(double y, double hdata, double vdata)
		{
			// y---- height from yhr plane of mla
			// hdata --- x position (normalized by radius)
			// vdata --- y position (normalized by radius)

			double phol, pver, xx, yy, tn, ubx, uby;
			//double phol, pver, tend;
			int i, kx, ky;
			double hoff, voff;
			whichMla mlaDelv;
			//int datanum = 170;

			//double PIXEL = 0.1245;          // 1 pixel [mm] 
			double PITCH_HOL = PIXEL * 7;   // 横幅
			double PITCH_VER = PIXEL * 12;  // 縦幅
			//double FOCUS = 7.0;             // 焦点距離

			mlaDelv.dpx = new int[DATANUM];
			mlaDelv.dpy = new int[DATANUM];

			hoff = hdata;
			voff = vdata;

			if (y == 0)
			{
				for (int r = 0; r < DATANUM; r++)
				{
					mlaDelv.dpx[r] = 0;
					mlaDelv.dpy[r] = 0;
				}
			}
			else
			{
				phol = PITCH_HOL * FOCUS / PIXEL / y;
				pver = PITCH_VER * FOCUS / PIXEL / y;

				tn = PITCH_HOL * PITCH_HOL / (PITCH_VER * PITCH_VER);

				for (i = 0; i < DATANUM; i++)
				{
					ubx = celx[i] / phol + hoff;
					uby = cely[i] / pver + voff;
					if (ubx > 0) kx = (int)ubx; else kx = (int)ubx - 1;
					if (uby > 0) ky = (int)uby; else ky = (int)uby - 1;

					xx = ubx - (double)kx;
					yy = uby - (double)ky;
					if ((kx + ky) % 2 == 0)
					{
						if (yy + tn * xx - tn / 2d - 0.5d >= 0)
						{
							mlaDelv.dpx[i] = kx + 1;
							mlaDelv.dpy[i] = ky + 1;
						}
						else
						{
							mlaDelv.dpx[i] = kx;
							mlaDelv.dpy[i] = ky;
						}
					}
					else
					{
						if (yy - tn * xx + tn / 2d - 0.5d >= 0)
						{
							mlaDelv.dpx[i] = kx;
							mlaDelv.dpy[i] = ky + 1;
						}
						else
						{
							mlaDelv.dpx[i] = kx + 1;
							mlaDelv.dpy[i] = ky;
						}
					}
				}
			}

			return mlaDelv;
		}


		/// <summary>
		/// 変換する
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void HenkanButton_Click(object sender, EventArgs e)
		{
			DateTime start = DateTime.Now;
			Image imgFg, imgZ; // Fg : RGB画像  Z : Depth MAP

			if (OrgPictureBox.Image != null)
				imgFg = OrgPictureBox.Image;
			else
				return;

			if (DepthPictureBox.Image != null)
				imgZ = DepthPictureBox.Image;
			else
				return;

			if (BMP1 != null)
				BMP1.Dispose();

			// 一旦配列に入れて扱いやすくする
			byte[] OrgRGB = toByte(imgFg, true);
			byte[] OrgDPT = toByte(imgZ, true);

			int wd = imgFg.Width, ht = imgFg.Height;
			int XX = 0, YY = 0, NUM = 0;
			xdata = new int[(wd + 14) * (ht + 12)];
			ydata = new int[(wd + 14) * (ht + 12)];
			bdata = new int[(wd + 14) * (ht + 12)];

			for (int i = 0; i < ht; i++)
				for (int j = 0; j < wd; j++)
				{
					int ppos = j + i * wd;

					mlaAddress(ref XX, ref YY, ref NUM, j, i);

					bdata[ppos] = NUM;
					xdata[ppos] = XX;
					ydata[ppos] = YY;

					// 画像の大きさでのMLA数を計算するために MAX/MIN を取る
					if (MaxXX < XX) MaxXX = XX;
					if (MaxYY < YY) MaxYY = YY;
					if (MinXX > XX) MinXX = XX;
					if (MinYY > YY) MinYY = YY;
				}

			// 画像サイズからMLAの数と割り振り用の大きさを求める
			// 元画像サイズより上下±12ずつ大きくする
			int Xmla = (MaxXX - MinXX) + 24;
			int Ymla = (MaxYY - MinYY) + 24;

			int PosMAX = Ymla * Xmla;
			uint[][] data2 = new uint[Xmla * Ymla][];	// [X,Y,RGB] 積算用の入れ物
			for (int i = 0; i < Xmla * Ymla; i++)
				data2[i] = new uint[DATANUM * 3];

			uint ValMax = 1; // 積算最大値の初期化
			double kyoriParam = double.Parse(MAXtextBox.Text) - double.Parse(MINtextBox.Text);
			double KMax = double.Parse(MAXtextBox.Text);

			Parallel.For(0, wd, j =>
			//for (int j = 0; j < bitmapZ.Width; j++)
			{
				for (int i = 0; i < ht; i++)
				{
					int BmpPos = j + i * wd;

					// 距離を直線補間 [mm]
					//  白:最小距離 
					//  黒:最大距離
					// 横幅偶数の為，0.5
					double kyori = KMax - (256d - (double)OrgDPT[BmpPos]) / 255d * kyoriParam; // Z値
					double hdata = Math.Abs(celx[bdata[BmpPos]] - 0.5d) * PIXEL; //水平距離
					double vdata = Math.Abs(cely[bdata[BmpPos]] - 0.5d) * PIXEL; //垂直距離

					// 岩根関数で割り振り配列が戻ってくる
					whichMla Mla = patnCalcAnlg(FOCUS * 50d / kyori - (FOCUS) * 3d, hdata, vdata);

					for (int k = 0; k < DATANUM; k++)
					{
						// MLA現在位置に対するオフセット量が入っている
						int posx = xdata[BmpPos] + Mla.dpx[k] + 12;
						int posy = ydata[BmpPos] + Mla.dpy[k] + 12;

						// 1次配列にしてる
						int Pos1 = posx + posy * Xmla;
						int Pos2 = k * 3;

						// 各色積算
						if (Pos1 >= 0 && Pos1 < PosMAX) // 範囲外は除外する
						{
							data2[Pos1][Pos2 + 0] = data2[Pos1][Pos2 + 0] + (uint)OrgRGB[BmpPos * 3 + 0]; //B
							data2[Pos1][Pos2 + 1] = data2[Pos1][Pos2 + 1] + (uint)OrgRGB[BmpPos * 3 + 1]; //G
							data2[Pos1][Pos2 + 2] = data2[Pos1][Pos2 + 2] + (uint)OrgRGB[BmpPos * 3 + 2]; //R

							// 後で正規化するために最大値をとっておく
							if (data2[Pos1][Pos2 + 0] > ValMax) ValMax = data2[Pos1][Pos2 + 0];
							if (data2[Pos1][Pos2 + 1] > ValMax) ValMax = data2[Pos1][Pos2 + 1];
							if (data2[Pos1][Pos2 + 2] > ValMax) ValMax = data2[Pos1][Pos2 + 2];
						}
					}
				}
			});


			DateTime end = DateTime.Now;
			timeTextBox.Text = (end - start).TotalMilliseconds.ToString();

			// 画像生成 32bit にしないと，バイト境界の関係で面倒くさい
			// http://xptn.dtiblog.com/blog-entry-99.html ←この辺参照
			// http://d.hatena.ne.jp/h0shu/20071122/p2
			// そうでない場合はDWORD境界に揃うよう一ラインつづパディングしなければならない

			BMP1 = new Bitmap(Xmla * 7, Ymla * 12, PixelFormat.Format24bppRgb);
			//BMP1 = new Bitmap(bitmapZ.Width, bitmapZ.Height, PixelFormat.Format24bppRgb);

			BitmapData bbmpData = BMP1.LockBits(
					new Rectangle(Point.Empty, BMP1.Size),
					ImageLockMode.ReadWrite,
					BMP1.PixelFormat);

			int pwd = bbmpData.Stride;

			//uint DevParam = uint.Parse(DevParaBox.Text);
			double gamma = 1 / double.Parse(DevParaBox.Text);

			unsafe
			{
				byte* pixel1 = (byte*)bbmpData.Scan0;
				byte* tmpP = pixel1;
				//int XX = 0, YY = 0, NUM = 0;

				for (int i = 0; i < Ymla * 12; i++) // 高さ合計
				{
					//pixel1 = tmpP + i * Xmla * 7 * 4;
					pixel1 = tmpP + i * pwd;
					for (int j = 0; j < Xmla * 7; j++) // 幅合計
					{
						int Pos = j + i * Xmla * 7;

						mlaAddress(ref XX, ref YY, ref NUM, j, i);

						int U1 = XX + YY * Xmla;
						int U2 = NUM * 3;

						if (U1 >= 0 && U1 < PosMAX)
						{
							// 積算値を変換するので 各色8bit に落とす 
							// ValMax を元にγを掛けるようにする
							*(pixel1++) =
								(byte)(255d * Math.Pow((double)data2[U1][U2 + 0] / ValMax, gamma)); // B
							*(pixel1++) =
								(byte)(255d * Math.Pow((double)data2[U1][U2 + 1] / ValMax, gamma)); // G
							*(pixel1++) =
								(byte)(255d * Math.Pow((double)data2[U1][U2 + 2] / ValMax, gamma)); // R

							//*(pixel1++) = 0; // Format32bppRgb では1バイトは未使用
						}
					}
				}
			}

			BMP1.UnlockBits(bbmpData);
			ConvPictureBox.Image = BMP1;
		}

		// アドレス判定用配列
		private int[] nl = { 
                -4, -4, -4, -4, -4, -4,  0,  1, -3, -3, -3, -3, -3, -3, // 1
                -4, -4, -4, -4,  2,  3,  4,  5,  6,  7, -3, -3, -3, -3, // 2
                -4, -4, -4,  8,  9, 10, 11, 12, 13, 14, 15, -3, -3, -3, // 3
                -4, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, -3, // 4
                28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, // 5
                42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, // 6
                56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, // 7
                70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, // 8
                84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, // 9
                98, 99,100,101,102,103,104,105,106,107,108,109,110,111, // 10
               112,113,114,115,116,117,118,119,120,121,122,123,124,125, // 11
               126,127,128,129,130,131,132,133,134,135,136,137,138,139, // 12
                -2,140,141,142,143,144,145,146,147,148,149,150,151, -1, // 13
                -2, -2, -2,152,153,154,155,156,157,158,159, -1, -1, -1, // 14
                -2, -2, -2, -2,160,161,162,163,164,165, -1, -1, -1, -1, // 15
                -2, -2, -2, -2, -2, -2,166,167, -1, -1, -1, -1, -1, -1,	// 16
                -2,	-2,	-2,	-2,	-2,	-2,	-2,	-1,	-1,	-1,	-1,	-1,	-1,	-1, // 17
                -2,	-2,	-2,	-2,	-2,	-2,	-2,	-1,	-1,	-1,	-1,	-1,	-1,	-1, // 18
                -2,	-2,	-2,	-2,	-2,	-2,	-2,	-1,	-1,	-1,	-1,	-1,	-1,	-1, // 19
                -2,	-2,	-2,	-2,	-2,	-2,	-2,	-1,	-1,	-1,	-1,	-1,	-1,	-1, // 20
                -2,	-2,	-2,	-2,	-2,	-2,	-2,	-1,	-1,	-1,	-1,	-1,	-1,	-1, // 21
                -2,	-2,	-2,	-2,	-2,	-2,	-2,	-1,	-1,	-1,	-1,	-1,	-1,	-1, // 22
                -2,	-2,	-2,	-2,	-2,	-2,	-2,	-1,	-1,	-1,	-1,	-1,	-1,	-1, // 23
                -2,	-2,	-2,	-2,	-2,	-2,	-2,	-1,	-1,	-1,	-1,	-1,	-1,	-1, // 24
                -2,	-2,	-2,	-2,	-2,	-2,	 0,  1,	-1,	-1,	-1,	-1,	-1,	-1,	
                -2,	-2,	-2,	-2,	 2,	 3,	 4,  5,	 6,	 7,	-1,	-1,	-1,	-1,	
                -2,	-2,	8,	9,	10,	11,	12, 13,	14,	15,	16,	-1,	-1,	-1,	
                -2,	17,	18,	19,	20,	21,	22, 23,	24,	25,	26,	27,	28,	-1	
							// 1   2   3   4   5   6   7   8   9  10  11  12  13  14
            };

		/// <summary>
		/// x , y を渡して MLA のアドレスを返す 
		/// </summary>
		/// <param name="XX"></param>
		/// <param name="YY"></param>
		/// <param name="NUM"></param>
		/// <param name="x">元のPIXEL X</param>
		/// <param name="y">元のPIXEL Y</param>
		private void mlaAddress(ref int XX, ref int YY, ref int NUM, int x, int y)
		{
			XX = x / 14;
			YY = (y + 4) / 24;

			int tmp = x % 14 + ((y + 4) % 24) * 14;
			NUM = nl[tmp];

			if (NUM == -4)
			{
				XX = XX * 2 - 1;
				YY = YY * 2 - 1;
				NUM = nl[tmp + 14 * 12 + 7];
			}
			else if (NUM == -3)
			{
				XX = XX * 2 + 1;
				YY = YY * 2 - 1;
				NUM = nl[tmp + 14 * 12 - 7];
			}
			else if (NUM == -2)
			{
				XX = XX * 2 - 1;
				YY = YY * 2 + 1;
				NUM = nl[tmp - 14 * 12 + 7];
			}
			else if (NUM == -1)
			{
				XX = XX * 2 + 1;
				YY = YY * 2 + 1;
				NUM = nl[tmp - 14 * 12 - 7];
			}
			else
			{
				XX = XX * 2;
				YY = YY * 2;
				if (x % 7 == 1) XX = XX--;
				if (y % 6 == 1) YY = YY--;
			}
		}

		#endregion

		#region その他
		[DllImport("kernel32")]
		static extern void GetSystemInfo(ref SYSTEM_INFO ptmpsi);


		/// <summary>
		/// システム情報を取得
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct SYSTEM_INFO
		{
			public uint dwOemId;
			public uint dwPageSize;
			public uint lpMinimumApplicationAddress;
			public uint lpMaximumApplicationAddress;
			public uint dwActiveProcessorMask;
			public uint dwNumberOfProcessors;
			public uint dwProcessorType;
			public uint dwAllocationGranularity;
			public uint dwProcessorLevel;
			public uint dwProcessorRevision;
		}

		/// <summary>
		/// CPUの数を調べて表示
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CpuCoreCountButton_Click(object sender, EventArgs e)
		{
			SYSTEM_INFO sysInfo = new SYSTEM_INFO();
			GetSystemInfo(ref sysInfo);

			TestText.Text = sysInfo.dwNumberOfProcessors.ToString();
		}

		/// <summary>
		/// 画像表示モードの変更（トグル式）
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ModeButton_Click(object sender, EventArgs e)
		{
			switch (ConvPictureBox.SizeMode)
			{
				case PictureBoxSizeMode.AutoSize:
					ConvPictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
					break;
				case PictureBoxSizeMode.CenterImage:
					ConvPictureBox.SizeMode = PictureBoxSizeMode.Normal;
					break;
				case PictureBoxSizeMode.Normal:
					ConvPictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
					break;
				case PictureBoxSizeMode.StretchImage:
					ConvPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
					break;
				case PictureBoxSizeMode.Zoom:
					ConvPictureBox.SizeMode = PictureBoxSizeMode.AutoSize;
					break;
			}

			TestText.Text = ConvPictureBox.SizeMode.ToString();
		}

		#endregion

		#region 新MLA画像処理
		/// <summary>
		/// 指定高さのハニカム像用
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void mDepthTrack_Scroll(object sender, EventArgs e)
		{
			mDepth.Text = mDepthTrack.Value.ToString();
			calculationB();
		}

		private int MaxXX = 0, MaxYY = 0, MinXX = 0, MinYY = 0;
		private int[] xdata, ydata, bdata;

		/// <summary>
		/// 一旦配列に放り込む
		/// </summary>
		/// <param name="imgFg">元のイメージ</param>
		/// <param name="ALP">アルファの処理フラグ</param>
		/// <returns>BGRA配列</returns>
		private byte[] toByte(Image imgFg, bool ALP)
		{
			byte[] OrgRGB = new byte[imgFg.Width * imgFg.Height * BYTEL];   // [X,Y,RGBA]BMPを配列に入れ替える

			Bitmap bitmapFg = new Bitmap(imgFg.Width, imgFg.Height, PixelFormat.Format32bppArgb); // 各色8bit

			// Img -> BMPに入れる
			using (Graphics g = Graphics.FromImage(bitmapFg))
			{
				g.DrawImage(imgFg, 0, 0, imgFg.Width, imgFg.Height);
				g.Dispose();
			}

			BitmapData oData = bitmapFg.LockBits(
					new Rectangle(Point.Empty, bitmapFg.Size),
					ImageLockMode.ReadOnly, bitmapFg.PixelFormat);

			//int XX = 0, YY = 0;

			if (ALP) 
				Marshal.Copy(oData.Scan0, OrgRGB, 0, OrgRGB.Length);
			else
			{
				// 高速化のためポインタ計算にする
				unsafe
				{
					byte* pixel1 = (byte*)oData.Scan0;
					byte* test = pixel1;
					int wd = bitmapFg.Width;

					//byte A = 0;

					for (int i = 0; i < bitmapFg.Height; i++)
					{
						pixel1 = test + i * oData.Stride;
						for (int j = 0; j < wd; j++)
						{
							int ppos = j + i * wd;

							OrgRGB[ppos * BYTEL + 0] = *(pixel1++);  // B
							OrgRGB[ppos * BYTEL + 1] = *(pixel1++);  // G
							OrgRGB[ppos * BYTEL + 2] = *(pixel1++);  // R
							OrgRGB[ppos * BYTEL + 3] = 0xFF;//A
							pixel1++;

						}
					}
				}
			}

			bitmapFg.UnlockBits(oData);

			return OrgRGB;
		}

		//private uint[] toUint(Image imgFg, bool ALP)
		//{
		//	uint[] OrgRGB = new byte[imgFg.Width * imgFg.Height * BYTEL];   // [X,Y,RGBA]BMPを配列に入れ替える

		//	Bitmap bitmapFg = new Bitmap(imgFg.Width, imgFg.Height, PixelFormat.Format32bppArgb); // 各色8bit

		//	// Img -> BMPに入れる
		//	using (Graphics g = Graphics.FromImage(bitmapFg))
		//	{
		//		g.DrawImage(imgFg, 0, 0, imgFg.Width, imgFg.Height);
		//		g.Dispose();
		//	}

		//	BitmapData oData = bitmapFg.LockBits(
		//			new Rectangle(Point.Empty, bitmapFg.Size),
		//			ImageLockMode.ReadOnly, bitmapFg.PixelFormat);

		//	//int XX = 0, YY = 0;

		//	if (ALP)
		//		Marshal.Copy(oData.Scan0, OrgRGB, 0, OrgRGB.Length);
		//	else
		//	{
		//		// 高速化のためポインタ計算にする
		//		unsafe
		//		{
		//			byte* pixel1 = (byte*)oData.Scan0;
		//			byte* test = pixel1;
		//			int wd = bitmapFg.Width;

		//			//byte A = 0;

		//			for (int i = 0; i < bitmapFg.Height; i++)
		//			{
		//				pixel1 = test + i * oData.Stride;
		//				for (int j = 0; j < wd; j++)
		//				{
		//					int ppos = j + i * wd;

		//					OrgRGB[ppos * BYTEL + 0] = *(pixel1++);  // B
		//					OrgRGB[ppos * BYTEL + 1] = *(pixel1++);  // G
		//					OrgRGB[ppos * BYTEL + 2] = *(pixel1++);  // R
		//					OrgRGB[ppos * BYTEL + 3] = 0xFF;
		//					pixel1++;

		//				}
		//			}
		//		}
		//	}

		//	bitmapFg.UnlockBits(oData);

		//	return OrgRGB;
		//}

		/// <summary>
		/// 一旦配列に放り込む
		/// </summary>
		/// <param name="imgFg">元のイメージ</param>
		/// <param name="ALP">アルファの処理フラグ</param>
		/// <returns>BGRA配列</returns>
		private byte[] toByteBMP(Bitmap bitmapFg, bool ALP)
		{
			byte[] OrgRGB = new byte[bitmapFg.Width * bitmapFg.Height * BYTEL];   // [X,Y,RGBA]BMPを配列に入れ替える

			BitmapData oData = bitmapFg.LockBits(
					new Rectangle(Point.Empty, bitmapFg.Size),
					ImageLockMode.ReadOnly, bitmapFg.PixelFormat);

			//int XX = 0, YY = 0;

			if (ALP)
				Marshal.Copy(oData.Scan0, OrgRGB, 0, OrgRGB.Length);
			else
			{
				// 高速化のためポインタ計算にする
				unsafe
				{
					byte* pixel1 = (byte*)oData.Scan0;
					byte* test = pixel1;
					int wd = bitmapFg.Width;

					//byte A = 0;

					for (int i = 0; i < bitmapFg.Height; i++)
					{
						pixel1 = test + i * oData.Stride;
						for (int j = 0; j < wd; j++)
						{
							int ppos = j + i * wd;

							OrgRGB[ppos * BYTEL + 0] = *(pixel1++);  // B
							OrgRGB[ppos * BYTEL + 1] = *(pixel1++);  // G
							OrgRGB[ppos * BYTEL + 2] = *(pixel1++);  // R
							OrgRGB[ppos * BYTEL + 3] = 0xFF;
							pixel1++;

						}
					}
				}
			}

			bitmapFg.UnlockBits(oData);

			return OrgRGB;
		}

		/// <summary>
		/// 指定高さの画像を作成する
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void zFix_Click(object sender, EventArgs e)
		{
			if (slopecheck.Checked) constcheck.Checked = false;
			else constcheck.Checked = true;

			DateTime start = DateTime.Now;
			Image imgFg; // Fg : RGB画像  Z : Depth MAP

			if (OrgPictureBox.Image != null)
				imgFg =  byChk(OrgPictureBox.Image);
			else
				return;

			if (BMP1 != null)
				BMP1.Dispose();

			byte[] OrgRGB;
			if (gaussianSharpencheck.Checked)
			{
				GaussianSharpen filter = new GaussianSharpen(6d, 7);
				Bitmap bb = new Bitmap(imgFg);
				filter.ApplyInPlace(bb);
				bb.Save("test.png", ImageFormat.Png);
				OrgRGB = toByteBMP(bb, alphacheck.Checked);
			}
			else
			{
				OrgRGB = toByte(imgFg, alphacheck.Checked);
			}
			// 一旦配列に入れて扱いやすくする		

			// 画像サイズからMLAの数と割り振り用の大きさを求める
			// 元画像サイズより上下±12ずつ大きくする
			int Xmla = (MaxXX - MinXX) + 24;
			int Ymla = (MaxYY - MinYY) + 24;

			int PosMAX = Ymla * Xmla;
			uint ValMax = 1; // 積算最大値の初期化
			int wd = imgFg.Width, ht = imgFg.Height;

			int xxn = (int)xnum.Value, yyn = (int)ynum.Value;

			// MLA pattern
			h_ptn(xxn, yyn);
			
			pattern p1 = new pattern();
			scaletrack.Value = 1;
			p1.dInput(
					(double)xxn, // 列幅
					(double)yyn, // 行幅
					(double)FtrackBar.Value/2d,  // 最大絞り径
					(double)FtrackBar.Value/2d, // 現在の絞り径
					//(int)scaletrack.Value,    // スケール
					1,    // スケール
					7.0d,  // MLA焦点距離[mm]
					0.2d); // ピクセルピッチ[mm]

			//最大範囲
			double kyori = 0;
			if (!slopecheck.Checked)
				kyori = double.Parse(mDepth.Text);
			else
			{
				double mmx = double.Parse(MAXtextBox.Text);
				double mmn = double.Parse(MINtextBox.Text);
				if (Math.Abs(mmn) > Math.Abs(mmx))
					kyori = Math.Abs(mmn);
				else
					kyori = Math.Abs(mmx);
			}

			pattern.oposM[] om = new pattern.oposM[DATANUM]; //新パターン
			for (int i = 0; i < DATANUM; i++)
				om[i] = p1.ptnDraw3((double)PG.celDx[i] - 0.5d, (double)PG.celDy[i] - 0.5d, kyori);

			//積算の入れ物 ptmin 等はパターン計算しないと出ない
			int hdd = (ht + p1.ptmaxY() - p1.ptminY()); //幅
			int wdd = (wd + p1.ptmaxX() - p1.ptminX()); //幅
			if (d3 != null) d3 = null;
			d3 = new uint[(hdd + 1) * (wdd + 1) * BYTEL];

			int xma = wd / xxn;
			int yma = ht / yyn;
			
			//スロープ
			double a, b;
			if (notcheck.Checked)
			{
				b = double.Parse(upbox.Text);
				a = double.Parse(dnbox.Text);
			}
			else
			{
				a = double.Parse(upbox.Text);
				b = double.Parse(dnbox.Text);
			}

			vmm = true;
			if (maxFixcheck.Checked)
			{
				ValMax = uint.Parse(vmax.Text);
				vmm = false;
			}
			else
				ValMax = 1;

			if (!slopecheck.Checked)
			{
				//並列化
				Parallel.For(0, yma, i =>
				//for (int i = 0; i < yma; i++)
				{
					uint B, G, R, A;
					int Pos1, posx0, posy0, BmpPos, DiXPos, DiYPos;
					for (int j = 0; j < xma; j++)
					{
						if ((i + j) % 2 == 0) //奇数・偶数列判定
						{
							for (int k = 0; k < DATANUM; k++)
							//for (int k = 0; k < 1; k++)
							{
								posx0 = j * xxn + PG.celDx[k]; //原画像の位置
								posy0 = i * yyn + PG.celDy[k]; //原画像の位置
								BmpPos = (posx0 + posy0 * wd) * BYTEL; //原画ポインタ
								DiXPos = j * xxn - p1.ptminX(); //積算ポインタ
								DiYPos = i * yyn - p1.ptminY(); //積算ポインタ

								if (posx0 > 0 && posx0 < wd && posy0 > 0 && posy0 < ht) //原画像範囲内
								{
									B = (uint)OrgRGB[BmpPos + 0]; //B
									G = (uint)OrgRGB[BmpPos + 1]; //G
									R = (uint)OrgRGB[BmpPos + 2]; //R
									A = (uint)OrgRGB[BmpPos + 3]; //A

									for (int l = 0; l < om[k].num; l++)
									{
										Pos1 = ((om[k].dx[l] + DiXPos) + (om[k].dy[l] + DiYPos) * wdd) * BYTEL;
										// 各色積算
										d3[Pos1 + 0] += B * om[k].di[l];
										d3[Pos1 + 1] += G * om[k].di[l];
										d3[Pos1 + 2] += R * om[k].di[l];
										d3[Pos1 + 3] += A * om[k].di[l];

										// 後で正規化するために最大値をとっておく
										if (d3[Pos1 + 0] > ValMax) ValMax = d3[Pos1 + 0];
										if (d3[Pos1 + 1] > ValMax) ValMax = d3[Pos1 + 1];
										if (d3[Pos1 + 2] > ValMax) ValMax = d3[Pos1 + 2];
										//if (d3[Pos1 + 3] > ZMax) ZMax = d3[Pos1 + 3];
									}
								}
							}
						}
					}
					//}
				});
			}
			else if (udButton.Checked)
			{
				//縦スロープ
				double haba = (b - a) / yma; 
				Parallel.For(0, yma, i =>
				{
					pattern.oposM[] omm = new pattern.oposM[DATANUM];
					for (int k = 0; k < DATANUM; k++)
						omm[k] = p1.ptnDraw3((double)PG.celDx[k] - 0.5d, (double)PG.celDy[k] - 0.5d, a + haba * (double)i);

					uint B, G, R, A;
					int Pos1, posx0, posy0, BmpPos, DiXPos, DiYPos;
					for (int j = 0; j < xma; j++)
					{
						if ((i + j) % 2 == 0) //奇数・偶数列判定
						{
							for (int k = 0; k < DATANUM; k++)
							//for (int k = 0; k < 1; k++)
							{
								posx0 = j * xxn + PG.celDx[k]; //原画像の位置
								posy0 = i * yyn + PG.celDy[k]; //原画像の位置
								BmpPos = (posx0 + posy0 * wd) * BYTEL; //原画ポインタ
								DiXPos = j * xxn - p1.ptminX(); //積算ポインタ
								DiYPos = i * yyn - p1.ptminY(); //積算ポインタ

								if (posx0 > 0 && posx0 < wd && posy0 > 0 && posy0 < ht) //原画像範囲内
								{
									B = (uint)OrgRGB[BmpPos + 0]; //B
									G = (uint)OrgRGB[BmpPos + 1]; //G
									R = (uint)OrgRGB[BmpPos + 2]; //R
									A = (uint)OrgRGB[BmpPos + 3]; //A

									for (int l = 0; l < omm[k].num; l++)
									{
										Pos1 = ((omm[k].dx[l] + DiXPos) + (omm[k].dy[l] + DiYPos) * wdd) * BYTEL;
										// 各色積算
										d3[Pos1 + 0] += B * omm[k].di[l];
										d3[Pos1 + 1] += G * omm[k].di[l];
										d3[Pos1 + 2] += R * omm[k].di[l];
										d3[Pos1 + 3] += A * omm[k].di[l];

										// 後で正規化するために最大値をとっておく
										if (d3[Pos1 + 0] > ValMax) ValMax = d3[Pos1 + 0];
										if (d3[Pos1 + 1] > ValMax) ValMax = d3[Pos1 + 1];
										if (d3[Pos1 + 2] > ValMax) ValMax = d3[Pos1 + 2];
										//if (d3[Pos1 + 3] > ZMax) ZMax = d3[Pos1 + 3];
									}
								}
							}
						}
					}
				});
			}
			else
			{
				//横スロープ
				double haba = (b - a) / xma;
				Parallel.For(0, xma, j =>
				{
					pattern.oposM[] omm = new pattern.oposM[DATANUM];
					for (int k = 0; k < DATANUM; k++)
						omm[k] = p1.ptnDraw3((double)PG.celDx[k] - 0.5d, (double)PG.celDy[k] - 0.5d, a + haba * (double)j);

					uint B, G, R, A;
					int Pos1, posx0, posy0, BmpPos, DiXPos, DiYPos;
					for (int i = 0; i < yma; i++)
					{
						if ((i + j) % 2 == 0) //奇数・偶数列判定
						{
							for (int k = 0; k < DATANUM; k++)
							//for (int k = 0; k < 1; k++)
							{
								posx0 = j * xxn + PG.celDx[k]; //原画像の位置
								posy0 = i * yyn + PG.celDy[k]; //原画像の位置
								BmpPos = (posx0 + posy0 * wd) * BYTEL; //原画ポインタ
								DiXPos = j * xxn - p1.ptminX(); //積算ポインタ
								DiYPos = i * yyn - p1.ptminY(); //積算ポインタ

								if (posx0 > 0 && posx0 < wd && posy0 > 0 && posy0 < ht) //原画像範囲内
								{
									B = (uint)OrgRGB[BmpPos + 0]; //B
									G = (uint)OrgRGB[BmpPos + 1]; //G
									R = (uint)OrgRGB[BmpPos + 2]; //R
									A = (uint)OrgRGB[BmpPos + 3]; //A

									for (int l = 0; l < omm[k].num; l++)
									{
										Pos1 = ((omm[k].dx[l] + DiXPos) + (omm[k].dy[l] + DiYPos) * wdd) * BYTEL;
										// 各色積算
										d3[Pos1 + 0] += B * omm[k].di[l];
										d3[Pos1 + 1] += G * omm[k].di[l];
										d3[Pos1 + 2] += R * omm[k].di[l];
										d3[Pos1 + 3] += A * omm[k].di[l];

										// 後で正規化するために最大値をとっておく
										if (d3[Pos1 + 0] > ValMax) ValMax = d3[Pos1 + 0];
										if (d3[Pos1 + 1] > ValMax) ValMax = d3[Pos1 + 1];
										if (d3[Pos1 + 2] > ValMax) ValMax = d3[Pos1 + 2];
										//if (d3[Pos1 + 3] > ZMax) ZMax = d3[Pos1 + 3];
									}
								}
							}
						}
					}
				});
			}



			maxVAset(ValMax);

			double gamma = 1 / double.Parse(DevParaBox.Text);
			BMP1 = byte2bmp(d3, wdd, hdd, ValMax, gamma, alphacheck.Checked);
			hvload(BMP1);
			//BMP1 = new Bitmap(wdd, hdd, PixelFormat.Format32bppArgb);

			//BitmapData bbmpData = BMP1.LockBits(
			//		new Rectangle(Point.Empty, BMP1.Size),
			//		ImageLockMode.ReadWrite,
			//		BMP1.PixelFormat);

			//int pwd = bbmpData.Stride;
			//double gamma = 1 / double.Parse(DevParaBox.Text);

			//unsafe
			//{
			//	byte* tmpP = (byte*)bbmpData.Scan0;
			//	wd = BMP1.Width;

			//	Parallel.For(0, BMP1.Height, i =>
			//	//for (int i = 0; i < BMP1.Height; i++) 
			//	{
			//		byte* pixel1;
			//		int poss;
			//		for (int j = 0; j < wd; j++)
			//		{
			//			pixel1 = tmpP + i * bbmpData.Stride + j * BYTEL;
			//			poss = (i * wdd + j) * BYTEL;
			//			*(pixel1++) = (byte)(255d * Math.Pow((double)d3[poss + 0] / ValMax, gamma)); // B
			//			*(pixel1++) = (byte)(255d * Math.Pow((double)d3[poss + 1] / ValMax, gamma)); // G
			//			*(pixel1++) = (byte)(255d * Math.Pow((double)d3[poss + 2] / ValMax, gamma)); // R
			//			*(pixel1++) = (byte)(255d * Math.Pow((double)d3[poss + 3] / ValMax, gamma)); // A
			//		}
			//	});
			//}

			//BMP1.UnlockBits(bbmpData);
			////ConvPictureBox.Image = BMP1;

			m_vForm.pictureBox1.Image = BMP1;

			DateTime end = DateTime.Now;
			timeTextBox.Text = (end - start).TotalMilliseconds.ToString();
		}

		/// <summary>
		/// 倍率変更
		/// </summary>
		/// <param name="src"></param>
		/// <returns></returns>
		private Image byChk(Image src)
		{
			if (bairitsutrack.Value == 10)
				return src;
			else
			{
				double test = ((double)bairitsutrack.Value / 10);
				int wd = (int)((double)src.Width * test);
				int ht = (int)((double)src.Height * test);

				return reSizePic(src, wd, ht);
			}
		}

		/// <summary>
		/// 新深度変換
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void depthConv2_Click(object sender, EventArgs e)
		{
			constcheck.Checked = false;
			DateTime start = DateTime.Now;
			Image imgFg, imgZ; // Fg : RGB画像  Z : Depth MAP

			if (OrgPictureBox.Image != null)
				imgFg = byChk(OrgPictureBox.Image);
			else
				return;

			if (DepthPictureBox.Image != null)
				imgZ = byChk(DepthPictureBox.Image);
			else
				return;

			if (BMP1 != null)
				BMP1.Dispose();

			// 一旦配列に入れて扱いやすくする
			byte[] OrgRGB = toByte(imgFg, alphacheck.Checked);
			byte[] OrgDPT = toByte(imgZ, true);

			// 画像サイズからMLAの数と割り振り用の大きさを求める
			// 元画像サイズより上下±12ずつ大きくする
			int Xmla = (MaxXX - MinXX) + 24;
			int Ymla = (MaxYY - MinYY) + 24;

			int PosMAX = Ymla * Xmla;
			int wd = imgFg.Width, ht = imgFg.Height;

			int xxn = (int)xnum.Value, yyn = (int)ynum.Value;

			// MLA pattern
			h_ptn(xxn, yyn);

			pattern p1 = new pattern();
			scaletrack.Value = 1;
			p1.dInput(
					(double)xxn, // 列幅
					(double)yyn, // 行幅
					(double)FtrackBar.Value / 2d,  // 最大絞り径
					(double)FtrackBar.Value / 2d, // 現在の絞り径
					1,    // スケール
					7.0d,  // MLA焦点距離[mm]
					0.2d); // ピクセルピッチ[mm]

			//深さスロープ
			double a, b;
			if (notcheck.Checked)
			{
				b = double.Parse(upbox.Text);
				a = double.Parse(dnbox.Text);
			}
			else
			{
				a = double.Parse(upbox.Text);
				b = double.Parse(dnbox.Text);
			}

			// 256段階のパターンを先に作成
			int DPS = 0x100;
			pattern.oposM[] omm = new pattern.oposM[0x100 * DATANUM];
			double haba = (b - a) / (double)DPS;
			for (int i = 0; i < DPS; i++)
				for (int j = 0; j < DATANUM; j++)
					omm[i + j * DPS] = p1.ptnDraw3((double)PG.celDx[j] - 0.5d, (double)PG.celDy[j] - 0.5d, a + haba * (double)i);

			vmm = true;
			if (maxFixcheck.Checked)
			{
				ValMax = uint.Parse(vmax.Text);
				vmm = false;
			}
			else
				ValMax = 1;

			double gamma = 1 / double.Parse(DevParaBox.Text);
			BMP1 = depthB(p1, ht, wd, xxn, yyn, OrgRGB, OrgDPT, omm, gamma, alphacheck.Checked);
			maxVAset(ValMax);
			hvload(BMP1);

			m_vForm.pictureBox1.Image = BMP1;

			DateTime end = DateTime.Now;
			timeTextBox.Text = (end - start).TotalMilliseconds.ToString();

			//d3 = new uint[] {}; //明示的にメモリ解放
			numcheck.Checked = false;
			dptcheck.Checked = false;
			//GC.Collect();
		}

		uint ValMax;
		private Bitmap depthB(pattern p1, int ht, int wd, int xxn, int yyn, byte[] OrgRGB, byte[] OrgDPT, pattern.oposM[] omm, double gm, bool alpha)
		{
			//積算の入れ物
			int hdd = (ht + p1.ptmaxY() - p1.ptminY()); //幅
			int wdd = (wd + p1.ptmaxX() - p1.ptminX()); //幅
			d3 = new uint[(hdd + 1) * (wdd + 1) * BYTEL];

			int xma = wd / xxn;
			int yma = ht / yyn;
			//uint ValMax = 1, ZMax = 1; // 積算最大値の初期化

			//並列化
			Parallel.For(0, yma, i =>
			//for (int i = 0; i < yma; i++)
			{
				//uint B, G, R, A, L, BB, GG, RR, AA;
				uint B, G, R, A, L;

				//Vector<uint> bgra = new Vector<uint>(new uint[] { 1, 1, 1, 1 });
				//Vector<uint> resbgra = new Vector<uint>(new uint[] { 0, 0, 0, 0 });

				byte Z;
				int Pos1, posx0, posy0, BmpPos, DiXPos, DiYPos;
				pattern.oposM om1;

				for (int j = 0; j < xma; j++)
				{
					if ((i + j) % 2 == 0) //奇数・偶数列判定
					{
						for (int k = 0; k < DATANUM; k++)
						//for (int k = 0; k < 1; k++)
						{
							posx0 = j * xxn + PG.celDx[k]; //原画像の位置
							posy0 = i * yyn + PG.celDy[k]; //原画像の位置
							BmpPos = (posx0 + posy0 * wd) * BYTEL; //原画ポインタ
							DiXPos = j * xxn - p1.ptminX(); //積算ポインタ
							DiYPos = i * yyn - p1.ptminY(); //積算ポインタ

							if (posx0 > 0 && posx0 < wd && posy0 > 0 && posy0 < ht) //原画像範囲内
							{
								//bgra = new Vector<uint>(new uint[] { OrgRGB[BmpPos], OrgRGB[BmpPos + 1], OrgRGB[BmpPos + 2], OrgRGB[BmpPos + 3] });
								B = (uint)OrgRGB[BmpPos]; //B
								G = (uint)OrgRGB[BmpPos + 1]; //G
								R = (uint)OrgRGB[BmpPos + 2]; //R
								A = (uint)OrgRGB[BmpPos + 3]; //A
								Z = OrgDPT[BmpPos]; ////Z
								//posOm = Z + k * DPS;
								om1 = omm[Z + k * 0x100];

								for (int l = 0; l < om1.num; l++)
								{
									//配列アクセスは遅い
									L = om1.di[l];
									Pos1 = ((om1.dx[l] + DiXPos) + (om1.dy[l] + DiYPos) * wdd) * BYTEL;
									// 各色積算
									//resbgra = bgra * L;
									//d3[Pos1] += resbgra[0];
									//d3[Pos1 + 1] += resbgra[1];
									//d3[Pos1 + 2] += resbgra[2];
									//d3[Pos1 + 3] += resbgra[3];
									d3[Pos1] += B * L;
									d3[Pos1 + 1] += G * L;
									d3[Pos1 + 2] += R * L;
									d3[Pos1 + 3] += A * L;
								}
							}
						}
					}
				}
				//}
			});

			if (vmm)
			{
				Parallel.For(0, d3.Length, i =>
				{
					if (d3[i] > ValMax) ValMax = d3[i];
				});
			}

			//maxVAset(ValMax);
			//double gamma = 1 / double.Parse(DevParaBox.Text);

			return byte2bmp(d3, wdd, hdd, ValMax, gm, alpha);
		}

		#endregion

		#region 新MLAハニカム設定
		/// <summary>
		/// ハニカムX方向
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void xnum_ValueChanged(object sender, EventArgs e)
		{
			double test, test0 = 1;

			if (!rendo)
			{
				rendo = true;
				for (int i = (int)xnum.Value / 2; i < (int)xnum.Value * 2; i++)
				{
					test = Math.Abs((((double)i / (double)xnum.Value) / Math.Sqrt(3d)) - 1);
					if (test < test0) // 最小値を探す
					{
						ynum.Value = i;
						test0 = test;
					}
				}

				h_ptn((int)xnum.Value, (int)ynum.Value);
				calculationB();
				rendo = false;
			}
		}

		/// <summary>
		/// ハニカムY方向
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ynum_ValueChanged(object sender, EventArgs e)
		{
			double test, test0 = 1;

			if (!rendo)
			{
				rendo = true;
				for (int i = (int)ynum.Value / 2; i < (int)ynum.Value * 2; i++)
				{
					test = Math.Abs((((double)ynum.Value / (double)i) / Math.Sqrt(3d)) - 1);
					if (test < test0)
					{
						xnum.Value = i;
						test0 = test;
					}
				}

				h_ptn((int)xnum.Value, (int)ynum.Value);
				calculationB();
				rendo = false;
			}
		}

		/// <summary>
		/// 1ハニカムパターンの計算
		/// </summary>
		/// <param name="xv">X方向</param>
		/// <param name="yv">Y方向</param>
		public void h_ptn(int xv, int yv)
		{
			// XY整数化長方形の配列
			int[,] pos = new int[xv, yv];

			var pointNum = 0;
			var ycmax = 0;


			// 1/4 整数化長方形に含まれるハニカムの位置判定と個数の確定
			for (int i = 0; i < xv; i++)
				for (int j = 0; j < yv; j++)
				{
					if (hantei(i, j, xv, yv))	// ここで判定
					{
						pointNum++;
						pos[i, j] = 1;
						if (j > ycmax)
							ycmax = j;
					}
				}

			PG.celDx = new int[pointNum * 4];
			PG.celDy = new int[pointNum * 4];
			int num = 0;

			for (int j = 0; j < yv; j++)
			{
				for (int i = 0; i < xv; i++)
				{
					if (pos[i, j] == 1)
					{
						// 4方向全て
						PG.celDx[num] = -i - 1; PG.celDy[num++] = -j - 1;
						PG.celDx[num] = i; PG.celDy[num++] = -j - 1;
						PG.celDx[num] = -i - 1; PG.celDy[num++] = j;
						PG.celDx[num] = i; PG.celDy[num++] = j;
					}
				}

			}
		}

		/// <summary>
		/// HEX内に含まれるかどうかの判定
		/// </summary>
		/// <param name="xpos"></param>
		/// <param name="ypos"></param>
		/// <param name="xx"></param>
		/// <param name="yy"></param>
		/// <returns></returns>
		private bool hantei(int xpos, int ypos, int xx, int yy)
		{
			//double b = (double)ynum.Value;
			double b = 2d / 3d * (double)yy;
			double a = -(double)yy / (3 * (double)xx);

			double y = (double)ypos + 0.5d; //重心位置
			double x = (double)xpos + 0.499d;

			double res = a * x + b;

			if (y < res) //重心が斜辺より上か下か？
				return true;
			else
				return false;
		}

		/// <summary>
		/// パターン
		/// </summary>
		private void calculationB()
		{
			pattern p1 = new pattern();

			try
			{
				p1.dInput(
					(double)xnum.Value, // 列幅
					(double)ynum.Value, // 行幅
					(double)FtrackBar.Value / 2d,  // 最大絞り径
					(double)FtrackBar.Value / 2d, // 現在の絞り径
					(int)scaletrack.Value,    // スケール
					7.0d,  // MLA焦点距離[mm]
					0.2d); // ピクセルピッチ[mm]

				Bitmap pa = p1.ptnimgf0(0xFF, 0x50, 0);
				ptnBox.BackgroundImage = pa;
				ptnBox.Width = pa.Width;
				ptnBox.Height = pa.Height;

				Bitmap p2 = p1.ptnDraw1(
					double.Parse(txBox.Text),
					double.Parse(tyBox.Text),
					(double)mDepthTrack.Value,
					x1.Checked,
					lineEn.Checked);

				ConvPictureBox.Image = p2;
				//ptnBox2.Image = p2;
			}
			catch
			{
				;
			}
		}

		/// <summary>
		/// マウスを置いた場所のパターン
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ptnBox_MouseDown(object sender, MouseEventArgs e)
		{
			// クリア
			ptnBox.Image = null;
			GC.Collect();
			ptnBox.Image = new Bitmap(ptnBox.Width, ptnBox.Height);
			Graphics grfdr2 = Graphics.FromImage(ptnBox.Image);

			float pstX = e.X;
			float pstY = e.Y;

			txBox.Text = ((float)(e.X - ptnBox.Width / 2f) / (float)scaletrack.Value).ToString();
			tyBox.Text = ((float)(e.Y - ptnBox.Height / 2f) / (float)scaletrack.Value).ToString();

			if (grfdr2 != null)
			{
				cross(grfdr2, Pens.LightGreen, e.X, e.Y, 2);
			}

			ptnBox.Refresh();

			calculationB();
		}

		/// <summary>
		/// g にクロスラインを描画
		/// </summary>
		/// <param name="g">グラフィック</param>
		/// <param name="clr">ペン</param>
		/// <param name="px">位置</param>
		/// <param name="py"></param>
		/// <param name="sz">サイズ</param>
		private void cross(Graphics g, Pen clr, int px, int py, int sz)
		{
			g.DrawLine(clr, px + sz, py, px - sz, py);
			g.DrawLine(clr, px, py + sz, px, py - sz);
		}

		/// <summary>
		/// 位置のリセット
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void resetbutton_Click(object sender, EventArgs e)
		{
			scaletrack.Value = 1;
			txBox.Text = "0";
			tyBox.Text = "0";
			mDepthTrack.Value = 0;
		}

		/// <summary>
		/// F値変更
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void FtrackBar_Scroll(object sender, EventArgs e)
		{
			calculationB();
			fBox.Text = (FtrackBar.Value/2).ToString();
		}


		/// <summary>
		/// x , y を渡して MLA のアドレスを返す 
		/// </summary>
		/// <param name="XX"></param>
		/// <param name="YY"></param>
		/// <param name="NUM"></param>
		/// <param name="x">元のPIXEL X</param>
		/// <param name="y">元のPIXEL Y</param>
		private void mlaAdd2(ref int XX, ref int YY, ref int NUM, int x, int y)
		{
			XX = x / 14;
			YY = (y + 4) / 24;

			int tmp = x % 14 + ((y + 4) % 24) * 14;
			NUM = nl[tmp];

			if (NUM == -4)
			{
				XX = XX * 2 - 1;
				YY = YY * 2 - 1;
				NUM = nl[tmp + 14 * 12 + 7];
			}
			else if (NUM == -3)
			{
				XX = XX * 2 + 1;
				YY = YY * 2 - 1;
				NUM = nl[tmp + 14 * 12 - 7];
			}
			else if (NUM == -2)
			{
				XX = XX * 2 - 1;
				YY = YY * 2 + 1;
				NUM = nl[tmp - 14 * 12 + 7];
			}
			else if (NUM == -1)
			{
				XX = XX * 2 + 1;
				YY = YY * 2 + 1;
				NUM = nl[tmp - 14 * 12 - 7];
			}
			else
			{
				XX = XX * 2;
				YY = YY * 2;
				if (x % 7 == 1) XX = XX--;
				if (y % 6 == 1) YY = YY--;
			}
		}

		/// <summary>
		/// パターン倍率変更
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void scaletrack_Scroll(object sender, EventArgs e)
		{
			sclabel.Text = scaletrack.Value.ToString();
		}
		#endregion

		#region 色処理
		private void upbox_TextChanged(object sender, EventArgs e)
		{
			numcheck.Checked = true;
		}

		private void dnbox_TextChanged(object sender, EventArgs e)
		{
			numcheck.Checked = true;
		}

		private void maxVAset(uint ValMax)
		{
			if (vmm)
			{
				vmax.Text = ValMax.ToString();
				vtrackBar.Maximum = (int)ValMax;
				vtrackBar.Minimum = (int)ValMax / 10;
				vtrackBar.Value = (int)ValMax;
				vtrackBar.TickFrequency = (int)ValMax / 100;
			}
		}

		private void trackBar1_Scroll(object sender, EventArgs e)
		{
			DevParaBox.Text = ((float)trackBar1.Value / 100).ToString();
			toBmp1();
		}

		private void vtrackBar_Scroll(object sender, EventArgs e)
		{
			vmax.Text = vtrackBar.Value.ToString();
			toBmp1();
		}

		private void atrackBar_Scroll(object sender, EventArgs e)
		{
			amax.Text = atrackBar.Value.ToString();
		}

		private void f48t24button_Click(object sender, EventArgs e)
		{
			//DateTime start = DateTime.Now;

			toBmp1();

			//DateTime end = DateTime.Now;
			//timeTextBox.Text = (end - start).TotalMilliseconds.ToString();
		}

		private void toBmp1()
		{
			int wdd = m_vForm.pictureBox1.Image.Width;
			int hdd = m_vForm.pictureBox1.Image.Height;
			double ValMax = double.Parse(vmax.Text);
			//double ZMax = double.Parse(amax.Text);
			double gamma = 1 / double.Parse(DevParaBox.Text);
			BMP1 = byte2bmp(d3, wdd, hdd, ValMax, gamma, alphacheck.Checked);
			hvload(BMP1);

			m_vForm.pictureBox1.Image = BMP1;
		}

		/// <summary>
		/// 配列のBMP化
		/// </summary>
		/// <param name="orgb">配列</param>
		/// <param name="wdd">幅</param>
		/// <param name="hdd">高さ</param>
		/// <param name="ValMax">値の上限</param>
		/// <param name="gamma">ガンマ</param>
		/// <returns></returns>
		private Bitmap byte2bmp(uint[] orgb, int wdd, int hdd, double ValMax, double gamma, bool alp)
		{
			Bitmap BMP = new Bitmap(wdd, hdd, PixelFormat.Format32bppArgb);

			BitmapData bbmpData = BMP.LockBits(
					new Rectangle(Point.Empty, BMP.Size),
					ImageLockMode.ReadWrite,
					BMP.PixelFormat);

			unsafe
			{
				byte* tmpP = (byte*)bbmpData.Scan0;

				Parallel.For(0, hdd, i =>
				{
					byte* pixel1;
					int poss;
					double R, G, B, A;

					for (int j = 0; j < wdd; j++)
					{
						pixel1 = tmpP + i * bbmpData.Stride + j * BYTEL;
						poss = (i * wdd + j) * BYTEL;

						B = (255d * Math.Pow(orgb[poss] / ValMax, gamma));
						G = (255d * Math.Pow(orgb[poss + 1] / ValMax, gamma));
						R = (255d * Math.Pow(orgb[poss + 2] / ValMax, gamma));
						A = (255d * Math.Pow(orgb[poss + 3] / ValMax, gamma));
						if (B < 0xFF) *(pixel1++) = (byte)B; else *(pixel1++) = 0xFF; // B
						if (G < 0xFF) *(pixel1++) = (byte)G; else *(pixel1++) = 0xFF; // G
						if (R < 0xFF) *(pixel1++) = (byte)R; else *(pixel1++) = 0xFF; // R
						if (alp)
							if (A < 0xFF) *(pixel1++) = (byte)A; else *(pixel1++) = 0xFF; // A
						else
							*(pixel1++) = 0xFF;
					}
				});

			}

			BMP.UnlockBits(bbmpData);
			
			return BMP;
		}


		/// <summary>
		/// 指定した画像の色を補正した画像を取得する
		/// </summary>
		/// <param name="img">色の補正をする画像</param>
		/// <param name="rScale">赤に掛ける倍率</param>
		/// <param name="gScale">緑に掛ける倍率</param>
		/// <param name="bScale">青に掛ける倍率</param>
		/// <param name="aScale">Aに掛ける倍率</param>
		/// <returns></returns>
		public static Image CreateColorCorrectedImage(Image img,
				float rScale, float gScale, float bScale, float aScale)
		{
			//補正された画像の描画先となるImageオブジェクトを作成
			Bitmap newImg = new Bitmap(img.Width, img.Height);
			//newImgのGraphicsオブジェクトを取得
			Graphics g = Graphics.FromImage(newImg);

			//ColorMatrixオブジェクトの作成
			//指定された倍率を掛けるための行列を指定する
			System.Drawing.Imaging.ColorMatrix cm =
					new System.Drawing.Imaging.ColorMatrix(
							new float[][] {
                new float[] {rScale, 0, 0, 0, 0},
                new float[] {0, gScale, 0, 0, 0},
                new float[] {0, 0, bScale, 0, 0}, 
                new float[] {0, 0, 0, aScale, 0},
                new float[] {0, 0, 0, 0, 1}
            });
			//次のようにしても同じ
			//System.Drawing.Imaging.ColorMatrix cm =
			//    new System.Drawing.Imaging.ColorMatrix();
			//cm.Matrix00 = rScale;
			//cm.Matrix11 = gScale;
			//cm.Matrix22 = bScale;
			//cm.Matrix33 = aScale;
			//cm.Matrix44 = 1;

			//ImageAttributesオブジェクトの作成
			System.Drawing.Imaging.ImageAttributes ia =
					new System.Drawing.Imaging.ImageAttributes();
			//ColorMatrixを設定する
			ia.SetColorMatrix(cm);

			//ImageAttributesを使用して描画
			g.DrawImage(img,
					new Rectangle(0, 0, img.Width, img.Height),
					0, 0, img.Width, img.Height, GraphicsUnit.Pixel, ia);

			//リソースを解放する
			g.Dispose();

			return newImg;
		}

		private HistogramView hv;
		/// <summary>
		/// HistogramViewを更新
		/// </summary>
		private void hvload(Bitmap bmp)
		{
			hv = hv2;
			hv.Image = bmp;
			hv.VisibleBlue = true;
			hv.VisibleGreen = true;
			hv.VisibleRed = true;
			//hv.VisibleAlpha = true;

			//hvR.Checked = true;
			//hvG.Checked = true;
			//hvB.Checked = true;
		}

		#endregion

		#region 連続処理

		string[] flist, blist;
		int ItemsCount, Iwd, Iht, xgn , ygn;
		double na, nb;
		pattern.oposM[] ogm;
		bool vmm; //最大値固定
		bool alp1; //アルファチャンネル処理

		/// <summary>
		/// リストのフォームを表示
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void batchb_Click(object sender, EventArgs e)
		{
			if (m_batch == null)
				m_batch = new batch();
			else
			{
				m_batch = null;
				m_batch = new batch();
			}
			m_batch.Show();
		}

		/// <summary>
		/// 連続処理スタート
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void renStartButton_Click(object sender, EventArgs e)
		{
			try
			{
				if (m_batch.htCngCheck.Checked) //高さ変化モード
				{
					htChenge();
				}
				else if (m_batch.fglist.Items.Count == m_batch.bglist.Items.Count) //テクスチャ＆デプスモード
				{
					fv = (double)FtrackBar.Value / 2d;
					ItemsCount = m_batch.fglist.Items.Count;
					flist = new String[ItemsCount];
					blist = new String[ItemsCount];
					alp1 = alphacheck.Checked;
					brt = bairitsutrack.Value / 10;
					vmm = true;
					if (nex5ck.Checked)	saveMD = 1; //Nexus5
					else if (nex10ck.Checked)	saveMD = 2;//Nexus10
					else if (k321ck.Checked)	saveMD = 3;//K321
					else if (nex6ck.Checked)	saveMD = 4;//Nexus6
					else if (cZ5p.Checked) saveMD = 5;//Xperia Z5 Premium
					else saveMD = 0;


					if (maxFixcheck.Checked)
					{
						ValMax = uint.Parse(vmax.Text);
						vmm = false;
					}
					else
						ValMax = 1;
					for (int i = 0; i < ItemsCount; i++)
					{
						flist[i] = m_batch.fglist.Items[i].ToString();
						blist[i] = m_batch.bglist.Items[i].ToString();
					}
					renStartButton.Enabled = false;
					scaletrack.Value = 1;
					bgW.RunWorkerAsync(100);
				}
				else
					MessageBox.Show("数が合わない"); 
			}
			catch
			{

			}
		}

		private void bgW_DoWork(object sender, DoWorkEventArgs e)
		{
			int xxn = (int)xnum.Value, yyn = (int)ynum.Value;
			//double fv = (double)FtrackBar.Value / 2d;

			// MLA pattern
			h_ptn(xxn, yyn);

			pattern p1 = new pattern();
			
			p1.dInput(
					(double)xxn, // 列幅
					(double)yyn, // 行幅
					fv,  // 最大絞り径
					fv, // 現在の絞り径
					1,    // スケール
					7.0d,  // MLA焦点距離[mm]
					0.2d); // ピクセルピッチ[mm]

			//深さスロープ
			if (notcheck.Checked)
			{
				nb = double.Parse(upbox.Text);
				na = double.Parse(dnbox.Text);
			}
			else
			{
				na = double.Parse(upbox.Text);
				nb = double.Parse(dnbox.Text);
			}

			double gam = 1 / double.Parse(DevParaBox.Text);
			xgn = (int)xnum.Value;
			ygn = (int)ynum.Value;

			// 256段階のパターンを先に作成
			int DPS = 0x100;
			ogm = new pattern.oposM[DPS * DATANUM];
			double haba = (nb - na) / (double)DPS;
			for (int i = 0; i < DPS; i++)
				for (int j = 0; j < DATANUM; j++)
					ogm[i + j * DPS] = p1.ptnDraw3((double)PG.celDx[j] - 0.5d, (double)PG.celDy[j] - 0.5d, na + haba * (double)i);

			for (int i = 0; i < ItemsCount; i++)
			{
				try
				{
					bgW.ReportProgress(i);
					BMP1 = bgwLoad(flist[i], blist[i], p1, gam);

					//BMP1.Save(System.IO.Path.ChangeExtension(blist[i], "_a.png"));
					saveMode(saveMD, BMP1, System.IO.Path.ChangeExtension(blist[i], "_a.png"));
				}
				catch
				{
					;
				}
			}
		}

		/// <summary>
		/// Progressが変更されたら処理する
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void bgW_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			TestText.Text = e.ProgressPercentage.ToString() + " / " + ItemsCount.ToString();
		}

		/// <summary>
		/// 終了したら処理する
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void bgW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			renStartButton.Enabled = true;
			TestText.Text = "完了";
		}

		/// <summary>
		/// ファイルからバイト列に変換
		/// </summary>
		/// <param name="fname"></param>
		/// <returns></returns>
		private byte[] BMPtoByte(String fname)
		{
			FileStream fs = File.OpenRead(fname);
			Image img = Image.FromStream(fs, false, false);
			int w = (int)((double)img.Width * brt);
			int h = (int)((double)img.Height * brt);
			Bitmap bitm1 = new Bitmap(w, h, PixelFormat.Format32bppArgb); // 各色8bit
			// Img -> BMPに入れる
			using (Graphics g = Graphics.FromImage(bitm1))
			{
				//g.DrawImage(img, 0, 0, img.Width, img.Height);
				g.DrawImage(img, 0, 0, w, h);
				g.Dispose();
			}

			byte[] OrgRGB = new byte[bitm1.Width * bitm1.Height * BYTEL];   // [X,Y,RGBA]BMPを配列に入れ替える

			BitmapData oData = bitm1.LockBits(
					new Rectangle(Point.Empty, bitm1.Size),
					ImageLockMode.ReadOnly, bitm1.PixelFormat);

			Iwd = bitm1.Width;
			Iht = bitm1.Height;

			Marshal.Copy(oData.Scan0, OrgRGB, 0, OrgRGB.Length);

			bitm1.UnlockBits(oData);

			return OrgRGB;
		}

		private Bitmap bgwLoad(String fname, String bname, pattern p1, double gam)
		{
			// 一旦配列に入れて扱いやすくする
			byte[] OrgRGB = BMPtoByte(fname);
			byte[] OrgDPT = BMPtoByte(bname);

			// 画像サイズからMLAの数と割り振り用の大きさを求める
			// 元画像サイズより上下±12ずつ大きくする
			int Xmla = (MaxXX - MinXX) + 24;
			int Ymla = (MaxYY - MinYY) + 24;

			return depthB(p1, Iht, Iwd, xgn, ygn, OrgRGB, OrgDPT, ogm, gam, alp1);
		}

		private void maxFixcheck_CheckedChanged(object sender, EventArgs e)
		{
			vmm = true;
			if (maxFixcheck.Checked)
			{
				ValMax = uint.Parse(vmax.Text);
				vmm = false;
			}
			else
				ValMax = 1;
		}
		#endregion

		#region View Form BG

		/// <summary>
		/// View Form BG Color のON/OFF
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void viewBGbutton_Click(object sender, EventArgs e)
		{
			if (viewFormBGgroup.Visible)
				viewFormBGgroup.Visible = false;
			else
				viewFormBGgroup.Visible = true;
		}

		private void Navycheck_CheckedChanged(object sender, EventArgs e)
		{
			m_vForm.pictureBox1.BackColor = Color.Navy;
		}

		private void DarkGreencheck_CheckedChanged(object sender, EventArgs e)
		{
			m_vForm.pictureBox1.BackColor = Color.DarkGreen;
		}

		private void Whitecheck_CheckedChanged(object sender, EventArgs e)
		{
			m_vForm.pictureBox1.BackColor = Color.White;
		}

		private void Browncheck_CheckedChanged(object sender, EventArgs e)
		{
			m_vForm.pictureBox1.BackColor = Color.Brown;
		}

		private void DarkGraycheck_CheckedChanged(object sender, EventArgs e)
		{
			m_vForm.pictureBox1.BackColor = Color.DarkGray;
		}

		private void Blackcheck_CheckedChanged(object sender, EventArgs e)
		{
			m_vForm.pictureBox1.BackColor = Color.Black;
		}

		#endregion 

		#region 解像度調整
		private void gurbutton_Click(object sender, EventArgs e)
		{
			if (GUR1 != null) GUR1 = null;
			GUR1 = gurb1(int.Parse(gurT1.Text));
			DepthPictureBox.Image = GUR1;
		}

		private Bitmap gurb1(int ang)
		{
			Bitmap test = new Bitmap(OrgPictureBox.Image.Width, OrgPictureBox.Image.Height, PixelFormat.Format24bppRgb);

			using (Graphics g = Graphics.FromImage(test))
			using (LinearGradientBrush oBrush = new LinearGradientBrush(
							g.VisibleClipBounds,
							Color.White,
							Color.Black,
							ang))
			{
				g.FillRectangle(oBrush, g.VisibleClipBounds);
			} 

			return test;
		}

		private void gurtrackBar_Scroll(object sender, EventArgs e)
		{
			gurT1.Text = gurtrackBar.Value.ToString();
		}

		private bool nexck = true;

		/// <summary>
		/// nexus5
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void nex5ck_CheckedChanged(object sender, EventArgs e)
		{
			if(nexck)
				//if (nex10ck.Checked)
				//{
					nexck = false;
					nex10ck.Checked = false;
					k321ck.Checked = false;
					nex6ck.Checked = false;
					nexck = true;
					xnum.Value = 7;
					cZ5p.Checked = false;
				//}
		}

		/// <summary>
		/// nexus6
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void nex6ck_CheckedChanged(object sender, EventArgs e)
		{
			if (nexck)
				//if (nex10ck.Checked)
				//{
					nexck = false;
					nex10ck.Checked = false;
					k321ck.Checked = false;
					nex5ck.Checked = false;
					nexck = true;
					xnum.Value = 8;
					cZ5p.Checked = false;
				//}
		}

		private void checkZ5p_CheckedChanged(object sender, EventArgs e)
		{
			if (nexck)
				//if (nex10ck.Checked)
				//{
				nexck = false;
			nex10ck.Checked = false;
			k321ck.Checked = false;
			nex5ck.Checked = false;
			nexck = true;
			xnum.Value = 7;
			nex6ck.Checked = false;
			//}
		}

		/// <summary>
		/// nexus10
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void nex10ck_CheckedChanged(object sender, EventArgs e)
		{
			if (nexck)
				//if (nex5ck.Checked)
				//{
					nexck = false;
					nex5ck.Checked = false;
					k321ck.Checked = false;
					nex6ck.Checked = false;
					nexck = true;
					xnum.Value = 7;
					cZ5p.Checked = false;
				//}
		}

		private void bairitsutrack_Scroll(object sender, EventArgs e)
		{
			bairitsutext.Text = ((double)bairitsutrack.Value / 10).ToString();
		}

		private void k321check_CheckedChanged(object sender, EventArgs e)
		{
			if (nexck)
				//if (nex5ck.Checked)
				//{
					nexck = false;
					nex5ck.Checked = false;
					nex10ck.Checked = false;
					nex6ck.Checked = false;
					nexck = true;
					cZ5p.Checked = false;
					xnum.Value = 7;
				//}
		}
		#endregion

		#region 高さ変化
		byte[] hgImg; // 画像の入れ物
		double[] hlist; //計算深度リスト
		double hMin, hMax; //高さ
		string hname, hext;
		int hxn, hyn, hwd, hht, hmode;
		double hgamma;
		uint vvmax;
		pattern pp1;

		/// <summary>
		/// 下準備
		/// </summary>
		private void htChenge()
		{
			int cnt = (int)m_batch.hgCnt.Value;
			if(hlist != null ) hlist = null;
			hlist = new double[cnt];

			hMin = double.Parse(m_batch.upbox.Text);
			hMax = double.Parse(m_batch.dnbox.Text);
			double kyori = 0;//最大範囲
			if (Math.Abs(hMin) > Math.Abs(hMax)) kyori = hMin;
			else kyori = hMax;
			if (maxFixcheck.Checked) vvmax = uint.Parse(vmax.Text);
			else vvmax = 0;

			double def = (hMax - hMin) / (double)(cnt-1);
			for (int i = 0; i < cnt; i++)
				hlist[i] = def * (double)i + hMin; // 計算深度リスト

			Image imgFg; // Fg : RGB画像  Z : Depth MAP

			if (OrgPictureBox.Image != null)
				imgFg = byChk(OrgPictureBox.Image);
			else
				return;

			if (BMP1 != null)
				BMP1.Dispose();

			// 一旦配列に入れて扱いやすくする
			hgImg = toByte(imgFg, alphacheck.Checked);

			// 画像サイズからMLAの数と割り振り用の大きさを求める
			// 元画像サイズより上下±12ずつ大きくする
			int Xmla = (MaxXX - MinXX) + 24;
			int Ymla = (MaxYY - MinYY) + 24;

			int PosMAX = Ymla * Xmla;
			hwd = imgFg.Width;
			hht = imgFg.Height;

			hxn = (int)xnum.Value;
			hyn = (int)ynum.Value;

			// MLA pattern
			h_ptn(hxn, hyn);

			if (pp1 != null) pp1 = null;
			pp1 = new pattern();
			//scaletrack.Value = 1;
			pp1.dInput(
					(double)hxn, // 列幅
					(double)hyn, // 行幅
					(double)FtrackBar.Value/ 2d,  // 最大絞り径
					(double)FtrackBar.Value/ 2d, // 現在の絞り径
				//(int)scaletrack.Value,    // スケール
					1,    // スケール
					7.0d,  // MLA焦点距離[mm]
					0.2d); // ピクセルピッチ[mm]

			hname = Path.GetDirectoryName(FNametextBox.Text) + 
				"\\" + 
				Path.GetFileNameWithoutExtension(FNametextBox.Text);

			if (nex5ck.Checked)
			{
				hname += "-nx5";
				hmode = 1;
			}
			else if (nex10ck.Checked)
			{
				hname += "-n10";
				hmode = 2;
			}
			else if (nex6ck.Checked)
			{
				hname += "-n6";
				hmode = 4;
			}
			else if (k321ck.Checked)
			{
				hname += "-k321";
				hmode = 3;
			}
			else
			{
				hname += "";
				hmode = 0;
			}

			if (m_batch.saveJpg.Checked) hext = ".jpg";
			else hext = ".png";

			hgamma = 1 / double.Parse(DevParaBox.Text);
			renStartButton.Enabled = false;
			//scaletrack.Value = 1;
			bgHgt.RunWorkerAsync(100);
		}

		/// <summary>
		/// 高さ変化用
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void bgHgt_DoWork(object sender, DoWorkEventArgs e)
		{
			Bitmap dst = null;
			for (int i = 0; i < hlist.Length; i++)
			{
				bgHgt.ReportProgress(i);

				pattern.oposM[] om = new pattern.oposM[DATANUM]; //新パターン
				for (int j = 0; j < DATANUM; j++)
					om[i] = pp1.ptnDraw3((double)PG.celDx[i] - 0.5d, (double)PG.celDy[i] - 0.5d, hlist[i]);

				//積算の入れ物 ptmin 等はパターン計算しないと出ない
				int hdd = (hht + pp1.ptmaxY() - pp1.ptminY()); //幅
				int wdd = (hwd + pp1.ptmaxX() - pp1.ptminX()); //幅
				if (d3 != null) d3 = null;
				d3 = new uint[(hdd + 1) * (wdd + 1) * BYTEL];

				int xma = hwd / hxn;
				int yma = hht / hyn;

				hgPict(om, xma, yma, wdd);

				if (dst != null) dst = null;
				dst = byte2bmp(d3, wdd, hdd, vvmax, hgamma, alphacheck.Checked);

				saveMode(hmode, (Image)dst, hname + "-" + i.ToString() + hext); 
			}
		}

		private void hgPict(pattern.oposM[] om, int xma, int yma,int wdd)
		{
			Parallel.For(0, yma, i =>
			//for (int i = 0; i < yma; i++)
			{
				uint B, G, R, A;
				int Pos1, posx0, posy0, BmpPos, DiXPos, DiYPos;
				for (int j = 0; j < xma; j++)
				{
					if ((i + j) % 2 == 0) //奇数・偶数列判定
					{
						for (int k = 0; k < DATANUM; k++)
						//for (int k = 0; k < 1; k++)
						{
							posx0 = j * hxn + PG.celDx[k]; //原画像の位置
							posy0 = i * hyn + PG.celDy[k]; //原画像の位置
							BmpPos = (posx0 + posy0 * hwd) * BYTEL; //原画ポインタ
							DiXPos = j * hxn - pp1.ptminX(); //積算ポインタ
							DiYPos = i * hyn - pp1.ptminY(); //積算ポインタ

							if (posx0 > 0 && posx0 < hwd && posy0 > 0 && posy0 < hht) //原画像範囲内
							{
								B = (uint)hgImg[BmpPos + 0]; //B
								G = (uint)hgImg[BmpPos + 1]; //G
								R = (uint)hgImg[BmpPos + 2]; //R
								A = (uint)hgImg[BmpPos + 3]; //A

								for (int l = 0; l < om[k].num; l++)
								{
									Pos1 = ((om[k].dx[l] + DiXPos) + (om[k].dy[l] + DiYPos) * wdd) * BYTEL;
									// 各色積算
									d3[Pos1 + 0] += B * om[k].di[l];
									d3[Pos1 + 1] += G * om[k].di[l];
									d3[Pos1 + 2] += R * om[k].di[l];
									d3[Pos1 + 3] += A * om[k].di[l];

								}
							}
						}
					}
				}
				//}
			});

			if (vvmax == 0)
			{
				ValMax = 1;
				Parallel.For(0, d3.Length, i =>
				{
					if (d3[i] > ValMax) ValMax = d3[i];
				});
				vvmax = ValMax;
			}
		}

		private void bgHgt_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			TestText.Text = e.ProgressPercentage.ToString() + " / " + hlist.Length.ToString();
		}

		private void bgHgt_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			renStartButton.Enabled = true;
			TestText.Text = "完了";
		}

		#endregion

		#region 画像調整
		private void gaussianSharpencheck_CheckedChanged(object sender, EventArgs e)
		{
			if (gaussianSharpencheck.Checked)
			{
				groupBox3.Visible = true;

				// オペレータサイズの取得
				int n = int.Parse(textGsSize.Text);
				if (n % 2 == 0)
				{
					textGsSize.SelectAll();
					textGsSize.Focus();
					return;
				}
				int m = n / 2; //中央

				// σ値の取得
				double a = double.Parse(textGsSigma.Text);

				// データグリッドビューの設定
				dgvGaussian.Columns.Clear();
				dgvGaussian.Rows.Clear();
				for (int i = 1; i <= n; i++)
				{
					dgvGaussian.Columns.Add("col" + i.ToString(), i.ToString());
				}
				for (int i = 1; i <= n; i++)
				{
					dgvGaussian.Rows.Add();
				}

				// ガウシアン係数の算出
				double[,] p = new double[n, n];
				Parallel.For(-m, m+1, y =>
				//for (int y = -m; y <= m; y++)
				{
					for (int x = -m; x <= m; x++)
					{
						p[x + m, y + m] = Math.Exp(-(x * x + y * y) / (2 * a * a));
					}
				});

				// パラメータの正規化
				double t = 0.0;
				for (int y = 0; y < n; y++)
				{
					for (int x = 0; x < n; x++)
					{
						t += p[x, y];
					}
				}

				Parallel.For(0, n, y =>
				//for (int y = 0; y < n; y++)
				{
					for (int x = 0; x < n; x++)
					{
						if (y == m && x == m) //unsharp masking
							p[x, y] = 1.0d - p[x, y] / t;
						else
							p[x, y] = -p[x, y] / t;
					}
				});

				// ガウシアン係数の表示
				gaustext.Clear();
				string tt = "";
				for (int y = 0; y < n; y++)
				{
					tt += y.ToString() + " ";
					for (int x = 0; x < n; x++)
					{
						dgvGaussian.Rows[y].Cells[x].Value = p[x, y];
						tt += " " + p[x, y].ToString();
					}
					tt += "\r\n";
				}
				gaustext.Text += tt;
			}
			else
			{
				groupBox3.Visible = false;
			}

		}
		#endregion



	}

}
