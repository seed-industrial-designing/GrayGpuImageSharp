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

namespace GrayGpuImageSharp.Filters
{
	public partial struct LevelFilter : IGrayGpuImageFilter<LevelFilter.Shader>
	{
		public double BlackLevel;
		public double WhiteLevel;
		public LevelFilter(double blackLevel = 0.0, double whiteLevel = 1.0)
		{
			BlackLevel = blackLevel;
			WhiteLevel = whiteLevel;
		}

		public Shader MakeShader(ReadWriteTexture2D<float> input, ReadWriteTexture2D<float> output) => new(
			input, output,
			blackLevel: (float)BlackLevel,
			whiteLevel: (float)WhiteLevel
		);

		[GeneratedComputeShaderDescriptor]
		[ThreadGroupSize(DefaultThreadGroupSizes.XY)]
		[AutoConstructor]
		public readonly partial struct Shader : IComputeShader
		{
			public readonly ReadWriteTexture2D<float> input;
			public readonly ReadWriteTexture2D<float> output;
			public readonly float blackLevel;
			public readonly float whiteLevel;

			public void Execute()
			{
				int x = ThreadIds.X;
				int y = ThreadIds.Y;
				float value = input[x, y];
				float result = ((value - blackLevel) / (whiteLevel - blackLevel));
				result = Hlsl.Clamp(result, 0.0f, 1.0f);
				output[x, y] = result;
			}
		}
	}
	public partial struct GammaFilter : IGrayGpuImageFilter<GammaFilter.Shader>
	{
		public double Gamma;
		public GammaFilter(double gamma = 1.0)
		{
			Gamma = gamma;
		}

		public Shader MakeShader(ReadWriteTexture2D<float> input, ReadWriteTexture2D<float> output) => new(
			input, output,
			gamma: (float)Gamma
		);

		[GeneratedComputeShaderDescriptor]
		[ThreadGroupSize(DefaultThreadGroupSizes.XY)]
		[AutoConstructor]
		public readonly partial struct Shader : IComputeShader
		{
			public readonly ReadWriteTexture2D<float> input;
			public readonly ReadWriteTexture2D<float> output;
			public readonly float gamma;

			public void Execute()
			{
				int x = ThreadIds.X;
				int y = ThreadIds.Y;
				float value = input[x, y];
				float result = Hlsl.Pow(value, gamma);
				result = Hlsl.Clamp(result, 0.0f, 1.0f);
				output[x, y] = result;
			}
		}
	}
	public partial struct ThresholdFilter : IGrayGpuImageFilter<ThresholdFilter.Shader>
	{
		public double Threshold;
		public ThresholdFilter(double threshold = 0.5)
		{
			Threshold = threshold;
		}

		public Shader MakeShader(ReadWriteTexture2D<float> input, ReadWriteTexture2D<float> output) => new(
			input, output,
			threshold: (float)Threshold
		);

		[GeneratedComputeShaderDescriptor]
		[ThreadGroupSize(DefaultThreadGroupSizes.XY)]
		[AutoConstructor]
		public readonly partial struct Shader : IComputeShader
		{
			public readonly ReadWriteTexture2D<float> input;
			public readonly ReadWriteTexture2D<float> output;
			public readonly float threshold;

			public void Execute()
			{
				int x = ThreadIds.X;
				int y = ThreadIds.Y;
				output[x, y] = ((input[x, y] > threshold) ? 1.0f : 0.0f);
			}
		}
	}
	public partial struct GaussianBlurFilter : IGrayGpuImageFilter<GaussianBlurFilter.Shader>
	{
		public enum TAxis { X, Y }
		public TAxis Axis;
		public double Radius;
		public GaussianBlurFilter(TAxis axis, double radius)
		{
			Axis = axis;
			Radius = radius;
		}

		public Shader MakeShader(ReadWriteTexture2D<float> input, ReadWriteTexture2D<float> output) => new(
			input, output,
			axis: Axis switch { TAxis.X => 0, _ => 1 },
			sigma: (float)Radius
		);

		[GeneratedComputeShaderDescriptor]
		[ThreadGroupSize(DefaultThreadGroupSizes.XY)]
		[AutoConstructor]
		public readonly partial struct Shader : IComputeShader
		{
			public readonly ReadWriteTexture2D<float> input;
			public readonly ReadWriteTexture2D<float> output;
			public readonly int axis;
			public readonly float sigma;

			public void Execute()
			{
				var width = input.Width;
				var height = input.Height;
				int x = ThreadIds.X;
				int y = ThreadIds.Y;
				if ((ThreadIds.X >= width) || (ThreadIds.Y >= height)) { return; }

				var radius = (int)Hlsl.Ceil(sigma * 3.0f);
				if (radius == 0) {
					output[x, y] = input[x, y];
				} else {
					var sum = 0.0f;
					var weightSum = 0.0f;

					for (var i = -radius; i <= radius; i += 1) {
						switch (axis) {
							case 0: {
								var sampleX = x + i;
								if ((sampleX < 0) || (width <= sampleX)) {
									continue;
								}
								var weight = Hlsl.Exp(-(float)(i * i) / (2.0f * sigma * sigma));

								var pixel = input[sampleX, y];
								sum += (pixel * weight);
								weightSum += weight;

								break;
							}
							case 1: {
								var sampleY = y + i;
								if ((sampleY < 0) || (height <= sampleY)) {
									continue;
								}
								var weight = Hlsl.Exp(-(float)(i * i) / (2.0f * sigma * sigma));

								var pixel = input[x, sampleY];
								sum += (pixel * weight);
								weightSum += weight;

								break;
							}
						}
					}
					output[x, y] = (sum / weightSum);
				}
			}
		}
	}
	public partial struct Rotate90Filter : IGrayGpuImageFilter<Rotate90Filter.Shader>
	{
		public int TurnCount;
		public Rotate90Filter(int turnCount)
		{
			TurnCount = turnCount;
		}

		public Shader MakeShader(ReadWriteTexture2D<float> input, ReadWriteTexture2D<float> output) => new(
			input, output,
			turnCount: TurnCount
		);

		[GeneratedComputeShaderDescriptor]
		[ThreadGroupSize(DefaultThreadGroupSizes.XY)]
		[AutoConstructor]
		public readonly partial struct Shader : IComputeShader
		{
			public readonly ReadWriteTexture2D<float> input;
			public readonly ReadWriteTexture2D<float> output;
			public readonly int turnCount;

			public void Execute()
			{
				var inputWidth = input.Width;
				var inputHeight = input.Height;
				int x = ThreadIds.X;
				int y = ThreadIds.Y;
				if ((ThreadIds.X >= inputWidth) || (ThreadIds.Y >= inputHeight)) { return; }
				int2 inputCoord;
				switch (turnCount % 4) {
					case 1:
						inputCoord = new(y, (inputWidth - 1 - x));
						break;
					case 2:
						inputCoord = new((inputWidth - 1 - x), (inputHeight - 1 - y));
						break;
					case 3:
						inputCoord = new((inputHeight - 1 - y), x);
						break;
					default:
						inputCoord = new(x, y);
						break;
				}
				output[x, y] = input[inputCoord];
			}
		}
	}

	public partial struct FlipFilter : IGrayGpuImageFilter<FlipFilter.Shader>
	{
		public enum TAxis { X, Y }
		public TAxis Axis;
		public FlipFilter(TAxis axis)
		{
			Axis = axis;
		}

		public Shader MakeShader(ReadWriteTexture2D<float> input, ReadWriteTexture2D<float> output) => new(
			input, output,
			axis: Axis switch { TAxis.X => 0, _ => 1 }
		);

		[GeneratedComputeShaderDescriptor]
		[ThreadGroupSize(DefaultThreadGroupSizes.XY)]
		[AutoConstructor]
		public readonly partial struct Shader : IComputeShader
		{
			public readonly ReadWriteTexture2D<float> input;
			public readonly ReadWriteTexture2D<float> output;
			public readonly int axis;

			public void Execute()
			{
				var inputWidth = input.Width;
				var inputHeight = input.Height;
				int x = ThreadIds.X;
				int y = ThreadIds.Y;
				if ((ThreadIds.X >= inputWidth) || (ThreadIds.Y >= inputHeight)) { return; }
				int2 inputCoord;
				switch (axis) {
					case 0:
					default:
						inputCoord = new((inputWidth - 1 - x), y);
						break;
					case 1:
						inputCoord = new(x, (inputHeight - 1 - y));
						break;
				}
				output[x, y] = input[inputCoord];
			}
		}
	}
}
