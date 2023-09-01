using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace depthComposite
{
	/// <summary>
	/// ハニカムパターンの計算
	/// </summary>

	class pattern : depthComposite.Ipattern
  {
    #region 変数

    private double h; //横
    private double v; //縦
    private int hh; //スケール倍した横
    private int vv; //スケール倍した縦
    private int sc; //倍率
    private int[] celDx; //瞳孔径に含まれる部分の配列(等倍)
    private int[] celDy;
    private int celDnum;
    private int[] celx;			// ハニカムに含まれる座標X(絞り最大)
    private int[] cely;			// ハニカムに含まれる座標Y(絞り最大)
    private int celmaxy;    // ハニカムに含まれる最大Y
    private int DATANUM = 0;	// MLA 1個に含まれる画素数

    struct posMla				// 結果配列
    {
      public int[] dx;
      public int[] dy;
    }

		public struct oposM //外から見るのでpub
		{
			public int num;		//ゼロ以外の個数
			public int[] dx;	// x
			public int[] dy;	// y
			public byte[] di;	// 強度
		}

    private int maxx;	//最大範囲X
		private int maxy;	//最大範囲Y
		private int minx;	//最小範囲X
		private int miny;	//最小範囲Y

		private int ptxmin;		//配列範囲
		private int ptxmax;		//範囲
		private int ptymin;		//範囲
		private int ptymax;		//範囲

    private double f0, f0s; //絞り径元の値
    private double fn, fns; //絞り径倍率をかけた値
    private double fmm; //焦点距離
    private double ppt; //ピクセルピッチ

    private double tx, ty;//計算ポイント

    private double df; //設定デフォーカス

    #endregion

		#region パラメータ設定
		public double H() { return this.h; }	// 取り出す
    public void H(int x) { this.h = x; }	// 書き換え
    public double V() { return this.v; }	// 取り出す
    public void V(int x) { this.v = x; }	// 書き換え
    public int maxX() { return this.maxx; }
    public int maxY() { return this.maxy; }
		public int minX() { return this.minx; }
		public int minY() { return this.miny; }

		public int ptmaxX() { return this.ptxmax; }
		public int ptmaxY() { return this.ptymax; }
		public int ptminX() { return this.ptxmin; }
		public int ptminY() { return this.ptymin; }


    /// <summary>
    /// 最大絞り径と現在の絞り径をセットする
    /// </summary>
    /// <param name="fmax">最大絞り径</param>
		/// <param name="fnaw">現在の絞り径</param>
    public void fInput(double fmax, double fnaw)
    {
      if (this.sc == 0)
        this.sc = 1;

      this.f0 = fmax;
      this.fn = fnaw;
      this.f0s = fmax * (double)sc;
      this.fns = fnaw * (double)sc;
    }

    /// <summary>
		/// パラメータセット
		/// </summary>
		/// <param name="x">h(列幅)</param>
		/// <param name="y">v(行幅)</param>
		/// <param name="fmax">最大絞り径</param>
		/// <param name="fnow">現在の絞り径</param>
		/// <param name="s">スケール</param>
		/// <param name="fm">MLA焦点距離[mm]</param>
		/// <param name="pp">ピクセルピッチ[mm]</param>
    public void dInput(double x, double y,double fmax, double fnow, int s, double fm, double pp)
    {
      this.h = x;
      this.v = y;
      this.hh = (int)Math.Round(x * (double)s);
      this.vv = (int)Math.Round(y * (double)s);
      this.sc = s;
      this.f0 = fmax;
      this.fn = fnow;
      this.f0s = fmax * (double)sc;
      this.fns = fnow * (double)sc;
      this.fmm = fm;
      this.ppt = pp;

      // XY整数化長方形の配列
      celx = new int[hh * vv * 2];
      cely = new int[hh * vv * 2];
      DATANUM = 0;

      //var pointNum = 0;
      var ymax = 0;

      // 中心位置
      // 1/4 整数化長方形に含まれるハニカムの位置判定と個数の確定
			for (int i = 0; i < hh; i++)
				for (int j = 0; j < vv; j++)
				{
					if (hantei(i, j, hh, vv))	// ここで判定
					{
						// 4方向全て
						celx[DATANUM] = i; cely[DATANUM++] = j;
						celx[DATANUM] = -i - 1; cely[DATANUM++] = j;
						celx[DATANUM] = i; cely[DATANUM++] = -j - 1;
						celx[DATANUM] = -i - 1; cely[DATANUM++] = -j - 1;
						if (j > ymax)
							ymax = j;
					}
				}

			this.celmaxy = ymax;

			ptxmin = 0;
			ptxmax = 0;
			ptymin = 0;
			ptymax = 0;

      fcheck2(fns);
    }

		#endregion

		#region パターン計算
		/// <summary>
		/// HEX内に含まれるかどうかの判定
		/// </summary>
		/// <param name="xpos">X pix</param>
		/// <param name="ypos">Y pix</param>
    /// <param name="xv"></param>
    /// <param name="yv"></param>
    /// <returns></returns>
    private bool hantei(int xpos, int ypos, double xv, double yv)
    {
      //double b = (double)ynum.Value;
      double b = 2d / 3d * yv;
      double a = -yv / (3 * xv);

      double y = (double)ypos + 0.5d; //重心位置
      double x = (double)xpos + 0.5d;

      double res = a * x + b;

      if (y < res) //重心が斜辺より上か下か？
        return true;
      else
        return false;
    }

    /// <summary>
		/// 最大のF値パターンの作図
    /// </summary>
    /// <param name="R"></param>
    /// <param name="G"></param>
    /// <param name="B"></param>
    /// <returns></returns>
    public Bitmap ptnimgf0(Byte R,Byte G,Byte B)
    {
      // 半径pix
      //double test = (double)vv / 3d * 2d;
      double test = (vv + 2d / 3d * hh) / 2;
      double t2 = hh * 2 / Math.Sqrt(3);
      //fchange(test);

      int jj = (int)((test + 1d) * 2d); // 片側2pix大きめ
      Bitmap BM8 = new Bitmap(jj, jj, PixelFormat.Format24bppRgb);
      jj = (jj / 2) - 1;
      Graphics g = Graphics.FromImage(BM8);
      g.DrawLine(Pens.Red, jj, jj - 2, jj, jj + 2); //中心とは0.5pixズレる
      g.DrawLine(Pens.Red, jj - 2, jj, jj + 2, jj);
      g.DrawEllipse(Pens.Gold,
                   ((float)jj - 0.5f) - (float)t2,
                   ((float)jj - 0.5f) - (float)t2,
                   (float)t2 * 2,
                   (float)t2 * 2);

      BitmapData bData = BM8.LockBits(
                    new Rectangle(System.Drawing.Point.Empty, BM8.Size),
                    ImageLockMode.ReadWrite,
                    BM8.PixelFormat);
      //int wd = BM8.Width;

      unsafe
      {
        Byte* rImg = (Byte*)bData.Scan0;

        int pos1 = 0;
        for (int j = 0; j < DATANUM; j++)
        {
          pos1 = (cely[j] + jj) * bData.Stride + (celx[j] + jj) * 3;
          //rImg[pos1 + 0] = B;  //B
          rImg[pos1 + 1] = G;  //G
          //rImg[pos1 + 2] = R;  //R
        }
      }
      BM8.UnlockBits(bData);
      return BM8;
    }


    /// <summary>
    /// F値判定
    /// </summary>
    /// <param name="xp">位置X pixel</param>
    /// <param name="yp">位置Y pixel</param>
    /// <param name="max">最大距離pixel</param>
    /// <returns></returns>
    private bool fhantei(int xp, int yp, double max)
    {
      double y = Math.Sqrt(Math.Pow((double)xp + 0.5d, 2) + Math.Pow((double)yp + 0.5d, 2));

      if (y < max) //F値より上か下か？
        return true;
      else
        return false;
    }

    /// <summary>
    /// F値径内に含まれるものをcelDx,celDyに入れる
    /// </summary>
    /// <param name="mx"></param>
    private void fcheck2(double mx)
    {
      //cel p1;
      int num = 0;

      bool[] fg = new bool[DATANUM]; // F値内に含まれれば true
      for (int i = 0; i < DATANUM; i++)
        if (fhantei(celx[i], cely[i], mx))
        {
          fg[i] = true;
          num++;
        }
        else
          fg[i] = false;

      celDx = new int[num];
      celDy = new int[num];
      int j = 0;

      for (int i = 0; i < DATANUM; i++)
      {
        if (fg[i])
        {
          celDx[j] = celx[i];
          celDy[j] = cely[i];
          j++;
        }
      }

      celDnum = j;
    }

		//public NefLoad1.Form1.cel1 tt1()
		//{

		//}

    /// <summary>
		/// 計算ポイントに応じたパターンを返す
		/// </summary>
		/// <param name="x">x位置[pixel]</param>
		/// <param name="y">y位置[pixel]</param>
		/// <param name="d">デフォーカス[mm]</param>
		/// <param name="tb">等倍に戻す</param>
    /// <param name="cl">中心線の有無</param>
    /// <returns></returns>
    public Bitmap ptnDraw1(double x,double y,double d,bool tb,bool cl)
    {
      this.tx = x;
      this.ty = y;
      this.df = d;

      Bitmap BM1 = null;

			//int mx, my, mxc = 0, myc = 0;
      try
      {
        double ax = x / f0;
        double ay = y / f0;

        //int 
				maxx = 1; maxy = 1;
				minx = 0; miny = 0;

        posMla tmp = patnCalcFn(df, ax, ay); //パターン

        // 実オフセット座標値を入れる入れ物
        posMla ra;
        int cnt = tmp.dx.Count();
        ra.dx = new int[cnt];
        ra.dy = new int[cnt];

        //// 描画範囲を求める
        for (int i = 0; i < cnt; i++)
        {
          ra.dx[i] = (int)((double)tmp.dx[i] * hh) + celDx[i];
          ra.dy[i] = (int)((double)tmp.dy[i] * vv) + celDy[i];

					if (Math.Abs(ra.dx[i]) > maxx)
						maxx = (Math.Abs(ra.dx[i]) + 1);
					if (Math.Abs(ra.dy[i]) > maxy)
						maxy = (Math.Abs(ra.dy[i]) + 1);
        }

				int mx, my, mxc, myc;
				mx = ((maxx * 2 + 1) / (int)h + 1) * (int)h;	// 整数化ハニカムの倍数になるようにする
				my = ((maxy * 2 + 1) / (int)v + 1) * (int)v;
				mxc = mx / 2; minx = mxc;
				myc = my / 2; miny = myc;

				//BM1 = new Bitmap(mx, my, PixelFormat.Format24bppRgb);
				BM1 = new Bitmap(mx, my, PixelFormat.Format32bppRgb);
        BitmapData bData = BM1.LockBits(
                      new Rectangle(System.Drawing.Point.Empty, BM1.Size),
                      ImageLockMode.ReadWrite,
                      BM1.PixelFormat);

        unsafe
        {
          Byte* rImg = (Byte*)bData.Scan0;

          int pos1 = 0;
          for (int i = 0; i < cnt; i++)
          {
            pos1 = (ra.dy[i] + myc) * bData.Stride + (ra.dx[i] + mxc) * 4;
            rImg[pos1 + 1] = 0xFF;
            rImg[pos1 + 2] = 0xFF;
          }
        }
        BM1.UnlockBits(bData);
      }
      catch
      {
				;
      }

      if (tb)
      {
				minx = minx / sc;
				miny = miny / sc;

        //元のサイズにリサイズ
        int wd = BM1.Width / sc;
        int ht = BM1.Height / sc;
        Bitmap BMP24 = new Bitmap(wd, ht, PixelFormat.Format32bppRgb);
				Pen p = new Pen(Color.FromArgb(128, Color.Blue), 1);
        using (Graphics g = Graphics.FromImage(BMP24))
        {
          g.InterpolationMode = InterpolationMode.HighQualityBicubic;
          g.PixelOffsetMode = PixelOffsetMode.HighQuality;
          g.DrawImage(BM1, 0, 0, wd, ht);
					if (cl)
					{
						g.DrawLine(p, 0, miny + 1, wd, miny + 1); //中心線
						g.DrawLine(p, minx + 1, 0, minx + 1, ht);

						double ctx = (double)BMP24.Width / 2;
						double cty = (double)BMP24.Height / 2 - 1; 
						double xwd = (double)h;
						double yht = (double)v;
						double xanum = (ctx / xwd + 1);
						double yanum = (cty / yht + 1);

						double di, dj;

						for (int i = 0; i < xanum; i++)
							for (int j = 0; j < yanum; j++)
							{
								// 中心以外
								di = (double)i;
								dj = (double)j;
								if ((i + j) % 2 == 0)
								{
									cross(g, Pens.Purple, ctx + xwd * di + 1, cty + yht * dj + 1, 1);
									cross(g, Pens.Purple, ctx - xwd * di + 1, cty + yht * dj + 1, 1);
									cross(g, Pens.Purple, ctx + xwd * di + 1, cty - yht * dj + 1, 1);
									cross(g, Pens.Purple, ctx - xwd * di + 1, cty - yht * dj + 1, 1);
								}
								else
								{
									honeycomb(g, p, ctx + xwd * di + 1, cty + yht * dj + 1, h, v, true);
									honeycomb(g, p, ctx - xwd * di + 1, cty + yht * dj + 1, h, v, true);
									honeycomb(g, p, ctx + xwd * di + 1, cty - yht * dj + 1, h, v, true);
									honeycomb(g, p, ctx - xwd * di + 1, cty - yht * dj + 1, h, v, true);
								}
							}
					}
        }

        return BMP24;
      }
      else
      {
				if (cl)
				{
					using (Graphics g = Graphics.FromImage(BM1))
					{
						g.DrawLine(Pens.Red, 0, miny, BM1.Width, miny); //中心線
						g.DrawLine(Pens.Red, minx, 0, minx, BM1.Height);
					}
				}
        return BM1;
      }
    }

		/// <summary>
		/// g にクロスラインを描画
		/// </summary>
		/// <param name="g">描画</param>
		/// <param name="p">ペン</param>
		/// <param name="px">X</param>
		/// <param name="py">Y</param>
		/// <param name="sz">サイズ</param>
		private void cross(Graphics g, Pen p, double px, double py, int sz)
		{
			g.DrawLine(p, (int)px + sz, (int)py, (int)px - sz, (int)py);
			g.DrawLine(p, (int)px, (int)py + sz, (int)px, (int)py - sz);
		}

		/// <summary>
		/// ハニカム境界
		/// </summary>
		/// <param name="g">描画</param>
		/// <param name="p">ペン</param>
		/// <param name="px">X</param>
		/// <param name="py">Y</param>
		/// <param name="xnum"></param>
		/// <param name="ynum"></param>
		/// <param name="t">倍率</param>
		private void honeycomb(Graphics g, Pen p, double px, double py, double xnum, double ynum, bool t)
		{
			double ssc;
			if (t) ssc = 1d; else ssc = sc;
			double yht = (ynum / 3.0d * ssc);
			double xwd = (xnum * ssc);

			g.DrawLine(p, (int)px, (int)(py - yht), (int)px, (int)(py + yht));

			g.DrawLine(p, (int)(px - xwd), (int)(py - 2 * yht), (int)px, (int)(py - yht));
			g.DrawLine(p, (int)(px + xwd), (int)(py - 2 * yht), (int)px, (int)(py - yht));
		}

    /// <summary>
    /// パターン計算(F値考慮)
    /// </summary>
    /// <param name="dfz">デフォーカス量</param>
    /// <param name="hdata">横方向の位置</param>
    /// <param name="vdata">縦方向の位置</param>
    /// <returns></returns>
    private posMla patnCalcFn(double dfz, double hdata, double vdata)
    {
      double phol, pver, xx, yy, tend, ubx, uby;
      int i, kx, ky;
      posMla mlaD;

			double PCH = ppt * (double)hh;			// 横幅[mm]
			double PCV = ppt * (double)vv;			// 縦幅[mm]

      mlaD.dx = new int[celDnum];
      mlaD.dy = new int[celDnum];
      maxx = 0;
      maxy = 0;

      if (dfz == 0)
      {
        for (i = 0; i < celDnum; i++)
        {
          mlaD.dx[i] = 0;
          mlaD.dy[i] = 0;
        }
      }
      else
      {
				phol = (double)hh * fmm / dfz;
				pver = (double)vv * fmm / dfz;

        tend = PCH * PCH / (PCV * PCV);

        int mx, my;

        for (i = 0; i < celDnum; i++)
        {
					//ubx = (double)celDx[i] / phol + hdata * (Math.Sqrt(3) / 2); //#######
					ubx = (double)celDx[i] / phol + hdata; //#######
					uby = (double)celDy[i] / pver + vdata * 2 / 3; //#######
					//ubx = ((double)celDx[i] + hdata * (double)sc) / phol;
					//uby = ((double)celDy[i] + vdata * (double)sc) / pver;
					if (ubx > 0) kx = (int)ubx; else kx = (int)ubx - 1;
          if (uby > 0) ky = (int)uby; else ky = (int)uby - 1;

          xx = ubx - (double)kx;
          yy = uby - (double)ky;
          if ((kx + ky) % 2 == 0)
          {
            if (yy + tend * xx - tend / 2 - 0.5 >= 0)
            {
              mlaD.dx[i] = kx + 1;
              mlaD.dy[i] = ky + 1;
            }
            else
            {
              mlaD.dx[i] = kx;
              mlaD.dy[i] = ky;
            }
          }
          else
          {
            if (yy - tend * xx + tend / 2 - 0.5 >= 0)
            {
              mlaD.dx[i] = kx;
              mlaD.dy[i] = ky + 1;
            }
            else
            {
              mlaD.dx[i] = kx + 1;
              mlaD.dy[i] = ky;
            }
          }
          mx = Math.Abs(mlaD.dx[i]);
          my = Math.Abs(mlaD.dy[i]);
          if (mx > maxx) maxx = mx;
          if (my > maxy) maxy = my;
        }
      }
      return mlaD;
		}

		#endregion


		/// <summary>
		/// 計算ポイントに応じたパターンを返す(配列版)
		/// </summary>
		/// <param name="x">x位置[pixel]</param>
		/// <param name="y">y位置[pixel]</param>
		/// <param name="d">デフォーカス[mm]</param>
		/// <returns></returns>
		public oposM ptnDraw2(double x, double y, double d)
		{
			this.tx = x;
			this.ty = y;
			this.df = d;

			Bitmap BM1 = null;

			double ax = x / f0;
			double ay = y / f0;

			//int 
			maxx = 1; maxy = 1;
			minx = 0; miny = 0;

			posMla tmp = patnCalcFn(df, ax, ay); //パターン

			// 実オフセット座標値を入れる入れ物
			posMla ra;
			int cnt = tmp.dx.Count();
			ra.dx = new int[cnt];
			ra.dy = new int[cnt];

			//// 描画範囲を求める
			for (int i = 0; i < cnt; i++)
			{
				ra.dx[i] = (int)((double)tmp.dx[i] * hh) + celDx[i];
				ra.dy[i] = (int)((double)tmp.dy[i] * vv) + celDy[i];

				if (Math.Abs(ra.dx[i]) > maxx) maxx = (Math.Abs(ra.dx[i]) + 1);
				if (Math.Abs(ra.dy[i]) > maxy) maxy = (Math.Abs(ra.dy[i]) + 1);
			}

			int mx, my, mxc = 0, myc = 0;
			//mx = ((maxx * 2 + 1) / (int)h + 1) * (int)h;	// 整数化ハニカムの倍数になるようにする
			//my = ((maxy * 2 + 1) / (int)v + 1) * (int)v;
			mx = (maxx * 2) + 2;
			my = (maxy * 2) + 2;
			mxc = mx / 2; minx = mxc;
			myc = my / 2; miny = myc;

			BM1 = new Bitmap(mx, my, PixelFormat.Format24bppRgb);
			BitmapData bData = BM1.LockBits(
					new Rectangle(System.Drawing.Point.Empty, BM1.Size),
					ImageLockMode.ReadWrite,
					BM1.PixelFormat);

			unsafe
			{
				Byte* rImg = (Byte*)bData.Scan0;

				for (int i = 0; i < cnt; i++)
					rImg[(ra.dy[i] + myc) * bData.Stride + (ra.dx[i] + mxc) * 3] = 0xFF;	// B Bのみで十分
			}
			BM1.UnlockBits(bData);

			minx = minx / sc;
			miny = miny / sc;

			//元のサイズにリサイズ -> 拡縮はBMPのアルゴを使う
			int wd = BM1.Width / sc;
			int ht = BM1.Height / sc;

			Bitmap BMP24 = new Bitmap(wd, ht, PixelFormat.Format32bppRgb);
			Pen p = new Pen(Color.FromArgb(128, Color.Blue), 1);
			using (Graphics g = Graphics.FromImage(BMP24))
			{
				//g.InterpolationMode = InterpolationMode.HighQualityBicubic;
				g.InterpolationMode = InterpolationMode.Bilinear;
				g.PixelOffsetMode = PixelOffsetMode.HighQuality;
				g.DrawImage(BM1, 0, 0, wd, ht); //ココが超遅い...
			}

			//BM1.Save("aO.png");
			//BMP24.Save("aB.png");
			////BM1 = null;

			byte[] AAA = toByteBMP8(BMP24);

			oposM oa;
			int num = BMP24.Width * BMP24.Height;
			oa.dx = new int[num];
			oa.dy = new int[num];
			oa.di = new byte[num];
			oa.num = 0;
			int addr;
			wd = BMP24.Width;

			for (int i = 0; i < BMP24.Height; i++)
			{
				for (int j = 0; j < wd; j++)
				{
					addr = (i * wd + j) * 3;
					if (AAA[addr] != 0)
					{
						oa.dx[oa.num] = j - mxc / sc;	// X
						oa.dy[oa.num] = i - myc / sc;	// Y

						oa.di[oa.num] = AAA[addr];			// 強度(B)

						if (ptxmin > oa.dx[oa.num]) ptxmin = oa.dx[oa.num];
						if (ptxmax < oa.dx[oa.num]) ptxmax = oa.dx[oa.num];
						if (ptymin > oa.dy[oa.num]) ptymin = oa.dy[oa.num];
						if (ptymax < oa.dy[oa.num]) ptymax = oa.dy[oa.num];

						oa.num++;
					}
				}
			}
			//BMP24 = null;

			return oa;
		}


		/// <summary>
		/// 計算ポイントに応じたパターンを返す(高速等倍版)
		/// </summary>
		/// <param name="x">x位置[pixel]</param>
		/// <param name="y">y位置[pixel]</param>
		/// <param name="d">デフォーカス[mm]</param>
		/// <returns></returns>
		public oposM ptnDraw3(double x, double y, double d)
		{
			this.tx = x;
			this.ty = y;
			this.df = d;

			double ax = x / f0;
			double ay = y / f0;

			//int 
			maxx = 1; maxy = 1;
			minx = 0; miny = 0;

			posMla tmp = patnCalcFn(df, ax, ay); //パターン

			// 実オフセット座標値を入れる入れ物
			posMla ra;
			int cnt = tmp.dx.Count();
			ra.dx = new int[cnt];
			ra.dy = new int[cnt];

			oposM oa;
			oa.num = tmp.dx.Count();
			oa.dx = new int[oa.num];
			oa.dy = new int[oa.num];
			oa.di = new byte[oa.num];

			// コントラスト低下対策
			byte tt = (byte)(127 + Math.Abs(d)); 

			for (int i = 0; i < oa.num; i++)
			{
				oa.dx[i] = (int)((double)tmp.dx[i] * hh) + celDx[i];	// X
				oa.dy[i] = (int)((double)tmp.dy[i] * vv) + celDy[i];	// Y
				oa.di[i] = tt;			// 強度(B)

				if (ptxmin > oa.dx[i]) ptxmin = oa.dx[i];
				if (ptxmax < oa.dx[i]) ptxmax = oa.dx[i];
				if (ptymin > oa.dy[i]) ptymin = oa.dy[i];
				if (ptymax < oa.dy[i]) ptymax = oa.dy[i];
			}

			return oa;
		}

		/// <summary>
		/// BITMAP -> Byte配列変換(8bitのみ)
		/// </summary>
		/// <param name="ORG"></param>
		/// <returns></returns>
		private byte[] toByteBMP8(Bitmap ORG)
		{
			byte[] RTN = new byte[ORG.Width * ORG.Height * 3];   // [X,Y,RGB]BMPを配列に入れ替える

			BitmapData oData = ORG.LockBits(
					new Rectangle(Point.Empty, ORG.Size),
					ImageLockMode.ReadOnly, ORG.PixelFormat);
			int wd = ORG.Width;

			// 高速化のためポインタ計算
			unsafe
			{
				byte* pixel1 = (byte*)oData.Scan0;
				byte* test = pixel1;

				//Parallel.For(0, ORG.Height, i =>
				for (int i = 0; i < ORG.Height; i++)
				{
					pixel1 = test + i * oData.Stride;
					for (int j = 0; j < wd; j++)
					{
						int ppos = j + i * wd;

						RTN[ppos * 3 + 0] = *(pixel1++);  // B
						RTN[ppos * 3 + 1] = *(pixel1++);  // G
						RTN[ppos * 3 + 2] = *(pixel1++);  // R
						pixel1++;
					}
				}
			}

			ORG.UnlockBits(oData);

			return RTN;
		}
	}
}
