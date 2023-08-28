using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using static System.Math;

namespace A25;

class MyWindow : Window {
   public MyWindow () {
      Width = 800; Height = 600;
      Left = 50; Top = 50;
      WindowStyle = WindowStyle.None;
      Image image = new Image () {
         Stretch = Stretch.None,
         HorizontalAlignment = HorizontalAlignment.Left,
         VerticalAlignment = VerticalAlignment.Top,
      };
      RenderOptions.SetBitmapScalingMode (image, BitmapScalingMode.NearestNeighbor);
      RenderOptions.SetEdgeMode (image, EdgeMode.Aliased);

      mBmp = new WriteableBitmap ((int)Width, (int)Height,
         96, 96, PixelFormats.Gray8, null);
      mStride = mBmp.BackBufferStride;
      image.Source = mBmp;
      Content = image;

      MouseDown += OnMouseDown;
   }

   void DrawMandelbrot (double xc, double yc, double zoom) {
      try {
         mBmp.Lock ();
         mBase = mBmp.BackBuffer;
         int dx = mBmp.PixelWidth, dy = mBmp.PixelHeight;
         double step = 2.0 / dy / zoom;
         double x1 = xc - step * dx / 2, y1 = yc + step * dy / 2;
         for (int x = 0; x < dx; x++) {
            for (int y = 0; y < dy; y++) {
               Complex c = new Complex (x1 + x * step, y1 - y * step);
               SetPixel (x, y, Escape (c));
            }
         }
         mBmp.AddDirtyRect (new Int32Rect (0, 0, dx, dy));
      } finally {
         mBmp.Unlock ();
      }
   }

   byte Escape (Complex c) {
      Complex z = Complex.Zero;
      for (int i = 1; i < 32; i++) {
         if (z.NormSq > 4) return (byte)(i * 8);
         z = z * z + c;
      }
      return 0;
   }

   static List<Point> mPts = new ();

   private void OnMouseDown (object sender, MouseButtonEventArgs e) {
      if (e.LeftButton == MouseButtonState.Pressed) {
         mPts.Add (e.GetPosition (this));
         if (mPts.Count == 2) {
            try {
               mBmp.Lock ();
               mBase = mBmp.BackBuffer;
               var (p1, p2) = (mPts[0], mPts[1]);
               PlotLine ((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y);
               mBmp.AddDirtyRect (GetBounds (p1, p2));
            } finally {
               mBmp.Unlock ();
            }
            mPts = new ();
         }
      }
   }

   void PlotLine (int x0, int y0, int x1, int y1) {
      var dx = Abs (x1 - x0);
      var sx = x0 < x1 ? 1 : -1;
      var dy = -Abs (y1 - y0);
      var sy = y0 < y1 ? 1 : -1;
      var error = dx + dy;
      while (true) {
         SetPixel (x0, y0, 255);
         if (x0 == x1 && y0 == y1) break;
         var e2 = 2 * error;
         if (e2 >= dy) {
            if (x0 == x1) break;
            error += dy;
            x0 += sx;
         }
         if (e2 <= dx) {
            if (y0 == y1) break;
            error += dx;
            y0 += sy;
         }
      }
   }

   static Int32Rect GetBounds (Point p1, Point p2) {
      int xmin = (int)Math.Min (p1.X, p2.X);
      int xmax = (int)Math.Max (p1.X, p2.X);
      int ymin = (int)Math.Min (p1.Y, p2.Y);
      int ymax = (int)Math.Max (p1.Y, p2.Y);
      return new Int32Rect (xmin, ymin, xmax - xmin, ymax - ymin);
   }

   void DrawGraySquare () {
      try {
         mBmp.Lock ();
         mBase = mBmp.BackBuffer;
         for (int x = 0; x <= 255; x++) {
            for (int y = 0; y <= 255; y++) {
               SetPixel (x, y, (byte)x);
            }
         }
         mBmp.AddDirtyRect (new Int32Rect (0, 0, 256, 256));
      } finally {
         mBmp.Unlock ();
      }
   }

   void SetPixel (int x, int y, byte gray) {
      unsafe {
         var ptr = (byte*)(mBase + y * mStride + x);
         *ptr = gray;
      }
   }

   WriteableBitmap mBmp;
   int mStride;
   nint mBase;
}

internal class Program {
   [STAThread]
   static void Main (string[] args) {
      Window w = new MyWindow ();
      w.Show ();
      Application app = new Application ();
      app.Run ();
   }
}
