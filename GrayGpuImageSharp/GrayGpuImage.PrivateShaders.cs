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
using ComputeSharp;

namespace GrayGpuImageSharp.PrivateFilters
{
	[GeneratedComputeShaderDescriptor]
	[ThreadGroupSize(DefaultThreadGroupSizes.XY)]
	[AutoConstructor]
	public readonly partial struct _FloatToByteShader : IComputeShader
	{
		public readonly ReadWriteTexture2D<float> input;
		public readonly ReadWriteBuffer<uint> output; //uint = 4 bytes
		public readonly int stride;

		public void Execute()
		{
			int x = ThreadIds.X;
			int y = ThreadIds.Y;
			output[(y * stride) + x] = ((uint)(SafeInput(((x * 4) + 3), y) * 255.0f) << 24) | ((uint)(SafeInput(((x * 4) + 2), y) * 255.0f) << 16) | ((uint)(SafeInput(((x * 4) + 1), y) * 255.0f) << 8) | (uint)(SafeInput((x * 4), y) * 255.0f);
		}
		float SafeInput(int x, int y) => input[Hlsl.Clamp(x, 0, (input.Width - 1)), y];
	}
}
