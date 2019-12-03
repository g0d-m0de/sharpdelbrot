using System;
using System.Numerics;

namespace Sharpdelbrot
{
    internal static class Mandelbrot
    {
        public static int CheckPoint(Complex point, int maxSteps)
        {
            var z = new Complex(0, 0);
            for (var steps = 0; steps < maxSteps; steps++)
            {
                z = z * z + point;
                if (z.Real * z.Real + z.Imaginary * z.Imaginary > 4)
                    return steps;
            }

            return maxSteps;
        }

        public static Complex FindCorner(int imageWidth, int imageHeight, Complex viewportCenter, double scale)
        {
            var offset = new Complex(imageWidth / 2d / scale, -imageHeight / 2d / scale);
            var corner = viewportCenter - offset;
            return corner;
        }

        public static Complex MapPixel(int pixelX, int pixelY, Complex viewportUpperLeft, double scale)
        {
            var point = new Complex(viewportUpperLeft.Real + pixelX / scale,
                viewportUpperLeft.Imaginary - pixelY / scale);
            return point;
        }

        public static double CalcScale(int imageWidth, int imageHeight, (Complex upperLeft, Complex lowerRight) viewport)
        {
            var viewportSizes = viewport.upperLeft - viewport.lowerRight;
            var viewportMinSize = Math.Min(Math.Abs(viewportSizes.Real), Math.Abs(viewportSizes.Imaginary));

            var screenMinSize = Math.Min(imageWidth, imageHeight);

            return screenMinSize / viewportMinSize;
        }
    }
}