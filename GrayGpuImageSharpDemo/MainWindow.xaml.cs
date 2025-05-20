//
// GrayGpuImageSharpDemo
// Copyright © 2025 Seed Industrial Designing Co., Ltd. All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the “Software”), to deal in the Software without
// restriction, including without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom
// the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ComputeSharp;
using GrayGpuImageSharp;
using GrayGpuImageSharp.Filters;
using GrayGpuImageSharp.Wpf;

namespace GrayGpuImageDemo
{
	public partial class MainWindow : Window
	{
		GraphicsDevice GraphicsDevice = GraphicsDevice.GetDefault();
		GrayGpuImage OriginalGpuImage;
		WriteableBitmap WriteableBitmap = new(
			pixelWidth: 1200,
			pixelHeight: 1200,
			dpiX: 96,
			dpiY: 96,
			PixelFormats.Gray8,
			palette: null
		);

		public MainWindow()
		{
			InitializeComponent();
			
			var width = WriteableBitmap.PixelWidth;
			var height = WriteableBitmap.PixelHeight;
			var pixels = new byte[width * height];
			{
				var originalGrayImage = new FormatConvertedBitmap(
					source: new BitmapImage(new Uri("sample.jpg", UriKind.Relative)),
					destinationFormat: PixelFormats.Gray8,
					destinationPalette: null,
					alphaThreshold: 0.5
				);
				originalGrayImage.CopyPixels(pixels, width, offset: 0);
			}
			OriginalGpuImage = new GrayGpuImage(GraphicsDevice, pixels, width, height);
			Image.Source = WriteableBitmap;

			ReloadImage();
		}
		protected override void OnClosed(EventArgs e)
		{
			OriginalGpuImage.Dispose();
			GraphicsDevice.Dispose();
			base.OnClosed(e);
		}

		void ReloadImage()
		{
			using var gpuImage = OriginalGpuImage.Clone();
			gpuImage.ApplyFilter(new LevelFilter(
				blackLevel: 0.0,
				gamma: GammaSlider.Value,
				whiteLevel: 0.95
			));
			gpuImage.ApplyFilter(new GaussianBlurFilter(
				axis: GaussianBlurFilter.TAxis.X,
				radius: BlurSlider.Value
			));
			gpuImage.ApplyFilter(new GaussianBlurFilter(
				axis: GaussianBlurFilter.TAxis.Y,
				radius: BlurSlider.Value
			));
			if (ThresholdCheckBox.IsChecked ?? false) {
				gpuImage.ApplyFilter(new ThresholdFilter(
					threshold: ThresholdSlider.Value
				));
			}
			gpuImage.ApplyFilter(new Rotate90Filter(turnCount: (int)OrientationSlider.Value));
			gpuImage.CopyToBitmap(WriteableBitmap);
		}

		private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (Image == null) { return; }
			ReloadImage();
		}
		private void CheckBox_CheckChanged(object sender, RoutedEventArgs e)
		{
			if (Image == null) { return; }
			ReloadImage();
		}
	}
}