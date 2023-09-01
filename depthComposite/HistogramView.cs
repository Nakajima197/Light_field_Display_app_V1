/// <summary>HistogramViewパネル</summary>

using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace depthComposite
{
	/// <summary>
	/// チャンネル
	/// </summary>
	enum Channel
	{
		Alpha,	/// α
		Red,	/// 赤
		Green,	/// 緑
		Blue,	/// 青
		GrayScale,	/// グレー
		Count,
	}

	/// <summary>
	/// HistogramViewパネル
	/// </summary>
	class HistogramView : Panel
	{
		#region 変数

		private Bitmap image;

		/// <summary>
		/// 線の色配列
		/// </summary>
		private Color[] lineColor = new Color[(int)Channel.Count];

		/// <summary>
		/// 塗りつぶし色配列
		/// </summary>
		private Color[] fillColor = new Color[(int)Channel.Count];

		/// <summary>
		/// 表示/非表示の配列
		/// </summary>
		private bool[] visibleChannel = new bool[(int)Channel.Count];

		private int[][] iArray = new int[(int)Channel.Count - 1][];

		/// <summary>
		/// 要素の最大値
		/// </summary>
		private float maxCount;

		#endregion

		#region プロパティ

		/// <summary>
		/// ヒストグラムを取得する画像
		/// </summary>
		public Bitmap Image
		{
			get { return image; }
			set
			{
				image = value;
				SetData();
			}
		}

		/// <summary>
		/// ヒストグラムの表示　グレースケール
		/// </summary>
		public bool VisibleGrayScale
		{
			get { return visibleChannel[(int)Channel.GrayScale]; }
			set
			{
				visibleChannel[(int)Channel.GrayScale] = value;
				Refresh();
			}
		}

		/// <summary>
		/// ヒストグラムの表示　アルファチャンネル
		/// </summary>
		public bool VisibleAlpha
		{
			get { return visibleChannel[(int)Channel.Alpha]; }
			set
			{
				visibleChannel[(int)Channel.Alpha] = value;
				Refresh();
			}
		}

		/// <summary>
		/// ヒストグラムの表示　赤
		/// </summary>
		public bool VisibleRed
		{
			get { return visibleChannel[(int)Channel.Red]; }
			set
			{
				visibleChannel[(int)Channel.Red] = value;
				Refresh();
			}
		}

		/// <summary>
		/// ヒストグラムの表示　緑
		/// </summary>
		public bool VisibleGreen
		{
			get { return visibleChannel[(int)Channel.Green]; }
			set
			{
				visibleChannel[(int)Channel.Green] = value;
				Refresh();
			}
		}

		/// <summary>
		/// ヒストグラムの表示　青
		/// </summary>
		public bool VisibleBlue
		{
			get { return visibleChannel[(int)Channel.Blue]; }
			set
			{
				visibleChannel[(int)Channel.Blue] = value;
				Refresh();
			}
		}

		/// <summary>
		/// ヒストグラム線の色　グレースケール
		/// </summary>
		public Color LineColorGrayScale
		{
			get { return lineColor[(int)Channel.GrayScale]; }
			set
			{
				lineColor[(int)Channel.GrayScale] = value;
				this.Refresh();
			}
		}

		/// <summary>
		/// ヒストグラム線の色　アルファチャンネル
		/// </summary>
		public Color LineColorAlpha
		{
			get { return lineColor[(int)Channel.Alpha]; }
			set
			{
				lineColor[(int)Channel.Alpha] = value;
				this.Refresh();
			}
		}

		/// <summary>
		/// ヒストグラム線の色　赤
		/// </summary>
		public Color LineColorRed
		{
			get { return lineColor[(int)Channel.Red]; }
			set
			{
				lineColor[(int)Channel.Red] = value;
				this.Refresh();
			}
		}

		/// <summary>
		/// ヒストグラム線の色　緑
		/// </summary>
		public Color LineColorGreen
		{
			get { return lineColor[(int)Channel.Green]; }
			set
			{
				lineColor[(int)Channel.Green] = value;
				this.Refresh();
			}
		}

		/// <summary>
		/// ヒストグラム線の色　青
		/// </summary>
		public Color LineColorBlue
		{
			get { return lineColor[(int)Channel.Blue]; }
			set
			{
				lineColor[(int)Channel.Blue] = value;
				this.Refresh();
			}
		}

		/// <summary>
		/// ヒストグラム塗りつぶし色　グレースケール
		/// </summary>
		public Color FillGrayScale
		{
			get { return fillColor[(int)Channel.GrayScale]; }
			set
			{
				fillColor[(int)Channel.GrayScale] = value;
				this.Refresh();
			}
		}

		/// <summary>
		/// ヒストグラム塗りつぶし色　アルファチャンネル
		/// </summary>
		public Color FillAlpha
		{
			get { return fillColor[(int)Channel.Alpha]; }
			set
			{
				fillColor[(int)Channel.Alpha] = value;
				this.Refresh();
			}
		}

		/// <summary>
		/// ヒストグラム塗りつぶし色　赤
		/// </summary>
		public Color FillRed
		{
			get { return fillColor[(int)Channel.Red]; }
			set
			{
				fillColor[(int)Channel.Red] = value;
				this.Refresh();
			}
		}

		/// <summary>
		/// ヒストグラム塗りつぶし色　緑
		/// </summary>
		public Color FillGreen
		{
			get { return fillColor[(int)Channel.Green]; }
			set
			{
				fillColor[(int)Channel.Green] = value;
				this.Refresh();
			}
		}

		/// <summary>
		/// ヒストグラム塗りつぶし色　青
		/// </summary>
		public Color FillBlue
		{
			get { return fillColor[(int)Channel.Blue]; }
			set
			{
				fillColor[(int)Channel.Blue] = value;
				this.Refresh();
			}
		}

		#endregion

		#region HistogramView本体
		public HistogramView()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.ResizeRedraw, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

			//初期値の代入
			this.BackColor = Color.White;

			lineColor[(int)Channel.GrayScale] = Color.FromArgb(100, 127, 127, 127);
			lineColor[(int)Channel.Alpha] = Color.FromArgb(100, 192, 0, 192);
			lineColor[(int)Channel.Red] = Color.FromArgb(100, 255, 0, 0);
			lineColor[(int)Channel.Green] = Color.FromArgb(100, 0, 255, 0);
			lineColor[(int)Channel.Blue] = Color.FromArgb(100, 0, 0, 255);

			fillColor[(int)Channel.GrayScale] = Color.FromArgb(60, 127, 127, 127);
			fillColor[(int)Channel.Alpha] = Color.FromArgb(60, 192, 0, 192);
			fillColor[(int)Channel.Red] = Color.FromArgb(60, 255, 0, 0);
			fillColor[(int)Channel.Green] = Color.FromArgb(60, 0, 255, 0);
			fillColor[(int)Channel.Blue] = Color.FromArgb(60, 0, 0, 255);

			for (int i = 0; i < (int)Channel.Count; i++) visibleChannel[i] = true;
		}

		/// <summary>
		/// 画像からヒストグラムのデータ生成
		/// </summary>
		private void SetData()
		{
			//配列の初期化
			for (int i = 0; i < (int)Channel.Count - 1; i++) iArray[i] = new int[256];

			if (image == null)
			{
				this.Refresh();
				return;
			}

			//色の要素数
			int pixelSize = 0;
			if (image.PixelFormat == PixelFormat.Format24bppRgb) { pixelSize = 3; }
			else if (image.PixelFormat == PixelFormat.Format32bppArgb || image.PixelFormat == PixelFormat.Format32bppRgb) { pixelSize = 4; }
			if (image.PixelFormat == PixelFormat.Format48bppRgb) { pixelSize = 6; }

			if (pixelSize == 0)
			{
				Bitmap tmp = new Bitmap(image.Width, image.Height, image.PixelFormat);
				using (Graphics g = Graphics.FromImage(tmp))
				{
					g.DrawImage(image, 0, 0, image.Width, image.Height);
				}

				image = tmp;
				pixelSize = 3;
			}

			//ImageをByte配列化
			BitmapData bmpData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
			int stride = bmpData.Stride;
			byte[] bmpByte = new byte[stride * image.Height];
			Marshal.Copy(bmpData.Scan0, bmpByte, 0, bmpByte.Length);

			//ushort[] bmpUST;
			if (pixelSize == 3 || pixelSize == 4)
			{
				//bmpByte = new byte[stride * image.Height];
				//Marshal.Copy(bmpData.Scan0, bmpByte, 0, bmpByte.Length);
			}
			else
			{
				//bmpUST = new ushort[stride * image.Height];
			}

			image.UnlockBits(bmpData);

			int w = image.Width;
			int h = image.Height;

			if (pixelSize == 3)
			{
				for (int j = 0; j < h; j++)
				{
					for (int i = 0; i < w * pixelSize; i += pixelSize)
					{
						int index = (j * stride) + i;
						iArray[(int)Channel.Red][(bmpByte[index + 2])]++;
						iArray[(int)Channel.Green][(bmpByte[index + 1])]++;
						iArray[(int)Channel.Blue][(bmpByte[index + 0])]++;
					}
				}
			}
			else if (pixelSize == 4)
			{
				for (int j = 0; j < h; j++)
				{
					for (int i = 0; i < w * pixelSize; i += pixelSize)
					{
						int index = (j * stride) + i;
						iArray[(int)Channel.Alpha][(bmpByte[index + 3])]++;
						iArray[(int)Channel.Red][(bmpByte[index + 2])]++;
						iArray[(int)Channel.Green][(bmpByte[index + 1])]++;
						iArray[(int)Channel.Blue][(bmpByte[index + 0])]++;
					}
				}
			}

			//各要素の最大値を取得
			maxCount = Max(new int[] { Max(iArray[(int)Channel.Alpha]), Max(iArray[(int)Channel.Red]),
                Max(iArray[(int)Channel.Green]), Max(iArray[(int)Channel.Blue]) });

			Refresh();
		}

		/// <summary>
		/// 配列から最大値を取得
		/// </summary>
		/// <param name="intArray"></param>
		/// <returns></returns>
		private int Max(int[] intArray)
		{
			int max = 0;
			// 0 : 黒
			// 255 : 白(最大)は除く
			for (int i = 1; i < (intArray.Length - 1); i++)
			{
				max = max < intArray[i] ? intArray[i] : max;
			}
			return max;
		}

		/// <summary>
		/// 描画処理
		/// </summary>
		/// <param name="pe"></param>
		protected override void OnPaint(PaintEventArgs pe)
		{
			base.OnPaint(pe);

			if (image != null && 0 < maxCount)
			{
				List<PointF>[] PointList = new List<PointF>[(int)Channel.Count];
				Pen[] pens = new Pen[(int)Channel.Count];
				Brush[] brushs = new Brush[(int)Channel.Count];

				//new
				for (int i = 0; i < (int)Channel.Count; i++) PointList[i] = new List<PointF>();
				for (int i = 0; i < (int)Channel.Count; i++) pens[i] = new Pen(lineColor[i], 1);
				for (int i = 0; i < (int)Channel.Count; i++) brushs[i] = new SolidBrush(fillColor[i]);

				int width = this.ClientSize.Width;
				int height = this.ClientSize.Height;

				pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

				for (int i = 0; i < 256; i++)
				{
					float x1, x2, yA, yR, yG, yB;

					x1 = i * (width / 256f);
					x2 = (i + 1) * (width / 256f);
					yA = height - (iArray[(int)Channel.Alpha][i] / maxCount * height);

					PointList[(int)Channel.Alpha].Add(new PointF(x1, yA));
					PointList[(int)Channel.Alpha].Add(new PointF(x2, yA));

					yR = height - (iArray[(int)Channel.Red][i] / maxCount * height);
					PointList[(int)Channel.Red].Add(new PointF(x1, yR));
					PointList[(int)Channel.Red].Add(new PointF(x2, yR));

					yG = height - (iArray[(int)Channel.Green][i] / maxCount * height);
					PointList[(int)Channel.Green].Add(new PointF(x1, yG));
					PointList[(int)Channel.Green].Add(new PointF(x2, yG));

					yB = height - (iArray[(int)Channel.Blue][i] / maxCount * height);
					PointList[(int)Channel.Blue].Add(new PointF(x1, yB));
					PointList[(int)Channel.Blue].Add(new PointF(x2, yB));

					PointList[(int)Channel.GrayScale].Add(new PointF(x1, (yR + yG + yB) / 3));
					PointList[(int)Channel.GrayScale].Add(new PointF(x2, (yR + yG + yB) / 3));
				}

				for (int i = 0; i < (int)Channel.Count; i++)
				{
					if (visibleChannel[i])
					{
						//ポイントを元にパスを作成
						GraphicsPath path = new GraphicsPath();
						path.AddLines(PointList[i].ToArray());

						//塗りつぶし用ポイント
						List<PointF> drawPos = new List<PointF>();
						drawPos.AddRange(PointList[i]);
						drawPos.Add(new PointF(width, height));
						drawPos.Add(new PointF(0, height));

						//描画
						pe.Graphics.FillPolygon(brushs[i], drawPos.ToArray(), FillMode.Winding);
						pe.Graphics.DrawPath(pens[i], path);
					}
				}
			}
		}
		#endregion
	}
}
