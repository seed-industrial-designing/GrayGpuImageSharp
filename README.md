# GrayGpuImageSharp

High-performance GPU grayscale image processing for Windows, powered by ComputeSharp.

For Apple platforms, [GrayGpuImageMetal](https://github.com/seed-industrial-designing/GrayGpuImageMetal) is also available.

## Dependencies

- ComputeSharp

- AutoConstructor

## Examples

### Applying filters

In the following example, the function applies filters to `WriteableBitmap` of Gray8 pixel format.

```csharp
void Blur(WriteableBitmap writeableBitmap, double blurRadius)
{
    var width = writeableBitmap.PixelWidth;
    var height = writeableBitmap.PixelHeight;

    var pixels = new byte[width * height];
    writeableBitmap.CopyPixels(pixels, stride: width, offset: 0);

    var graphicsDevice = GraphicsDevice.GetDefault();
    using var gpuImage = new GrayGpuImage(graphicsDevice, pixels, width, height);
    
    gpuImage.ApplyFilter(new GaussianBlurFilter(
        axis: GaussianBlurFilter.TAxis.X,
        radius: blurRadius
    ));
    gpuImage.ApplyFilter(new GaussianBlurFilter(
        axis: GaussianBlurFilter.TAxis.Y,
        radius: blurRadius
    ));

    gpuImage.CopyToBitmap(writeableBitmap);
}
```

If the processing is frequent, reuse instances of `GraphicsDevice` and `GrayGpuImage` for better performance.

### Define your own filters

You can define your own filters as below.

```csharp
public partial struct DoSomethingFilter : IGrayGpuImageFilter<DoSomethingFilter.Shader>
{
    public double Factor;
    public DoSomethingFilter(double factor = 1.0)
    {
        Factor = factor;
    }

    public Shader MakeShader(ReadWriteTexture2D<float> input, ReadWriteTexture2D<float> output) => new(
        input, output,
        factor: (float)Factor
    );

    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [AutoConstructor]
    public readonly partial struct Shader : IComputeShader
    {
        public readonly ReadWriteTexture2D<float> input;
        public readonly ReadWriteTexture2D<float> output;
        public readonly float factor;

        public void Execute()
        {
            int x = ThreadIds.X;
            int y = ThreadIds.Y;
            output[x, y] = (input[x, y] * factor);
        }
    }
}
```

## License

GrayGpuImageSharp is released under the [MIT License](LICENSE.txt).

See also the licenses of the dependencies.
