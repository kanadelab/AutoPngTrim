using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using Path = System.IO.Path;
using System.Runtime.InteropServices;
using Rectangle = System.Drawing.Rectangle;
using Point = System.Drawing.Point;

namespace AutoPngTrim
{
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//変換処理
				string path = PathTextBox.Text;

				//変換リザルト
				List<Tuple<string, Rectangle>> items = new List<Tuple<string, Rectangle>>();

				var directoryName = "generated/" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
				Directory.CreateDirectory("generated");

				var files = Directory.GetFiles(path, "*.png", SearchOption.TopDirectoryOnly);
				foreach (var pngPath in files)
				{
					//pngがあるかチェック
					if (!File.Exists(pngPath))
						continue;

					var pngBitmap = (Bitmap)Bitmap.FromFile(pngPath);
					var trimRect = GetTrimBitmapRect(pngBitmap);
					var cloneBitmap = pngBitmap.Clone(trimRect, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
					items.Add(new Tuple<string, Rectangle>(Path.GetFileName(pngPath), trimRect));

					var relativePath = new Uri(path + @"\").MakeRelativeUri(new Uri(pngPath)).ToString();
					Directory.CreateDirectory(Path.Combine(directoryName, Path.GetDirectoryName(relativePath)));
					cloneBitmap.Save(Path.Combine(directoryName, relativePath));
				}

				//テキスト化
				var resultStr = string.Join("\r\n", items.Select(o =>
				{
					return string.Format("{0},{1},{2}", o.Item1, o.Item2.X, o.Item2.Y);
				}));

				File.WriteAllText(Path.Combine(Environment.CurrentDirectory, directoryName, "offsets.txt"), resultStr, Encoding.GetEncoding("Shift_JIS"));
				Process.Start(Path.Combine(Environment.CurrentDirectory, directoryName));
			}
			catch
			{
				MessageBox.Show("失敗しました。");
			}
		}

		private Rectangle GetTrimBitmapRect(Bitmap png)
		{
			var lockedPng = png.LockBits(new Rectangle(0, 0, png.Width, png.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			byte[] alpha = new byte[1];

			int trimX1 = 0;
			int trimY1 = 0;
			int trimX2 = lockedPng.Width;
			int trimY2 = lockedPng.Height;

			//トリム領域の検出
			{
				//左スキャン
				bool found = false;
				
				for (int x = 0; x < lockedPng.Width && !found; x++)
				{
					for (int y = 0; y < lockedPng.Height && !found; y++)
					{
						//座標決定
						IntPtr pngAddress = lockedPng.Scan0 + lockedPng.Stride * y + x * 4 + 3;
						Marshal.Copy(pngAddress, alpha, 0, 1);

						//透明度が0以外の場合:決定
						if (alpha[0] != 0)
						{
							trimX1 = x;
							found = true;
							break;
						}
					}
				}
			}

			{
				//上スキャン
				bool found = false;
				for (int y = 0; y < lockedPng.Height && !found; y++)
				{
					for (int x = 0; x < lockedPng.Width && !found; x++)
					{
						//座標決定
						IntPtr pngAddress = lockedPng.Scan0 + lockedPng.Stride * y + x * 4 + 3;	//4チャネル目のalphaがいるので3byteオフセット
						Marshal.Copy(pngAddress, alpha, 0, 1);

						//透明度が0以外の場合:決定
						if (alpha[0] != 0)
						{
							trimY1 = y;
							found = true;
							break;
						}
					}
				}
			}

			{
				//右スキャン
				bool found = false;
				for (int x = lockedPng.Width-1; x >= 0  && !found; x--)
				{
					for (int y = lockedPng.Height-1; y >= 0  && !found; y--)
					{
						//座標決定
						IntPtr pngAddress = lockedPng.Scan0 + lockedPng.Stride * y + x * 4 + 3;
						Marshal.Copy(pngAddress, alpha, 0, 1);

						//透明度が0以外の場合:決定
						if (alpha[0] != 0)
						{
							trimX2 = x;
							found = true;
							break;
						}
					}
				}
			}

			{
				//上スキャン
				bool found = false;
				for (int y = lockedPng.Height-1; y >= 0  && !found; y--)
				{
					for (int x = lockedPng.Width-1; x >= 0 && !found; x--)
					{
						//座標決定
						IntPtr pngAddress = lockedPng.Scan0 + lockedPng.Stride * y + x * 4 + 3;
						Marshal.Copy(pngAddress, alpha, 0, 1);

						//透明度が0以外の場合:決定
						if (alpha[0] != 0)
						{
							trimY2 = y;
							found = true;
							break;
						}
					}
				}
			}

			png.UnlockBits(lockedPng);
			return new Rectangle(trimX1, trimY1, trimX2 - trimX1, trimY2 - trimY1);
		}
	}
}
