//
// GrayGpuImageSharp
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;
using ComputeSharp.Descriptors;
using GrayGpuImageSharp.PrivateFilters;

namespace GrayGpuImageSharp
{
	public class GrayGpuImage: IDisposable
	{
		public GrayGpuImage(GraphicsDevice graphicsDevice, int width, int height)
		{
			GraphicsDevice = graphicsDevice;
			TextureA = GraphicsDevice.AllocateReadWriteTexture2D<float>(width, height);
		}
		public GrayGpuImage(GraphicsDevice graphicsDevice, IEnumerable<byte> gray8Data, int width, int height)
		{
			GraphicsDevice = graphicsDevice;
			var inputData = gray8Data.Select(b => ((float)b / 255.0f)).ToArray();
			TextureA = GraphicsDevice.AllocateReadWriteTexture2D(inputData, width, height);
		}
		private GrayGpuImage(GraphicsDevice graphicsDevice, ReadWriteTexture2D<float> texture)
		{
			GraphicsDevice = graphicsDevice;
			TextureA = GraphicsDevice.AllocateReadWriteTexture2D<float>(texture.Width, texture.Height);
			TextureA.CopyFrom(texture);
		}
		public GrayGpuImage Clone() => new GrayGpuImage(GraphicsDevice, TextureA);

		GraphicsDevice GraphicsDevice;
		ReadWriteTexture2D<float> TextureA;
		ReadWriteTexture2D<float>? TextureB;

		public void CopyFrom(byte[] gray8Data, int width, int height)
		{
			var inputData = gray8Data.Select(b => ((float)b / 255.0f)).ToArray();
			if ((TextureA.Width == width) && (TextureA.Height == height)) {
				TextureA.CopyFrom(inputData);
			} else {
				TextureA.Dispose();
				TextureB?.Dispose();
				TextureA = GraphicsDevice.AllocateReadWriteTexture2D(inputData, width, height);
				TextureB = null;
			}
		}

		private bool _disposed = false;
		public void Dispose()
		{
			if (!_disposed) {
				TextureA.Dispose();
				TextureB?.Dispose();
				_disposed = true;
			}
		}

		private void SwapTextures()
		{
			if (TextureB == null) { return; }
			var oldB = TextureB;
			TextureB = TextureA;
			TextureA = oldB;
		}

		#region Filters

		public void ApplyGenerator<TShader>(IGrayGpuImageGenerator<TShader> generator) where TShader : struct, IComputeShader, IComputeShaderDescriptor<TShader>
		{
			GraphicsDevice.For(TextureA.Width, TextureA.Height, generator.MakeShader(TextureA));
		}
		public void ApplyFilter<TShader>(IGrayGpuImageFilter<TShader> filter) where TShader : struct, IComputeShader, IComputeShaderDescriptor<TShader>
		{
			var textureB = TextureB;
			if (textureB == null) {
				textureB = GraphicsDevice.AllocateReadWriteTexture2D<float>(TextureA.Width, TextureA.Height);
				TextureB = textureB;
			}
			GraphicsDevice.For(TextureA.Width, TextureA.Height, filter.MakeShader(TextureA, textureB));
			SwapTextures();
		}

		#endregion

		public byte[] GetGray8Bytes(int stride)
		{
			var result_byte = new byte[stride * TextureA.Height];
			CopyGray8Bytes(result_byte.AsSpan(), stride);
			return result_byte;
		}
		public void CopyGray8Bytes(Span<byte> destination, int stride)
		{
			var width = TextureA.Width;
			var height = TextureA.Height;
			var stride_uint = ((TextureA.Width + (sizeof(uint) - 1)) / sizeof(uint));

			using var source_uint = GraphicsDevice.AllocateReadWriteBuffer<uint>(stride_uint * height);
			GraphicsDevice.For(stride_uint, height, new _FloatToByteShader(TextureA, source_uint, stride_uint));
			if ((stride_uint * sizeof(uint)) == stride) {
				var resultPtr_uint = MemoryMarshal.Cast<byte, uint>(destination);
				source_uint.CopyTo(resultPtr_uint);
			} else {
				var source_byte = MemoryMarshal.Cast<uint, byte>(source_uint.ToArray().AsSpan());
				var blockCopyableUintCount = (stride / sizeof(uint));
				for (var y = 0; y < height; y += 1) {
					source_byte
						.Slice((sizeof(uint) * stride_uint * y), length: stride)
						.CopyTo(destination.Slice(start: (stride * y), length: stride));
				}
			}
		}
	}
	public interface IGrayGpuImageGenerator<TShader> where TShader : struct, IComputeShader, IComputeShaderDescriptor<TShader>
	{
		TShader MakeShader(ReadWriteTexture2D<float> output);
	}
	public interface IGrayGpuImageFilter<TShader> where TShader: struct, IComputeShader, IComputeShaderDescriptor<TShader>
	{
		TShader MakeShader(ReadWriteTexture2D<float> input, ReadWriteTexture2D<float> output);
	}
}
