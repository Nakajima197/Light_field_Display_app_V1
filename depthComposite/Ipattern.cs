
namespace depthComposite
{
	interface Ipattern
	{
		void dInput(double x, double y, double fmax, double fnow, int s, double fm, double pp);
		void fInput(double fmax, double fnaw);
		double H();
		double V();
		void H(int x);
		void V(int x);
		int maxX();
		int maxY();
		System.Drawing.Bitmap ptnDraw1(double x, double y, double d, bool tb, bool cl);
		System.Drawing.Bitmap ptnimgf0(byte R, byte G, byte B);
	}
}
