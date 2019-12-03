using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Sharpdelbrot
{
    public class FractalRenderer : INotifyPropertyChanged
    {
        private readonly Random _rnd = new Random();

        private Complex _viewportCenter = new Complex(-0.75, 0.0);

        private double _viewportScale;

        private WriteableBitmap _bmp;

        private bool _isReady = true;

        private bool _parallelModeOn = true;

        private int _progress;

        private TimeSpan _lastRenderTime = TimeSpan.Zero;

        private int _threadsCount = Environment.ProcessorCount;

        private int _resolutionMultiplier = 1;
        private int _maxSteps = 500;
        private int _kr;
        private int _kg;
        private int _kb;

        /// <summary>
        /// Magical coefficient of RED component 
        /// </summary>
        public int Kr
        {
            get => _kr;
            set
            {
                _kr = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Magical coefficient of GREEN component 
        /// </summary>
        public int Kg
        {
            get => _kg;
            set
            {
                _kg = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Magical coefficient of BLUE component 
        /// </summary>
        public int Kb
        {
            get => _kb;
            set
            {
                _kb = value;
                OnPropertyChanged();
            }
        }

        public ImageSource Image => _bmp;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Progress
        {
            get => _progress;
            private set
            {
                _progress = value;
                OnPropertyChanged();
            }
        }

        public bool IsReady
        {
            get => _isReady;
            private set
            {
                _isReady = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanChangeThreadsCount));
            }
        }

        public int MaxSteps
        {
            get => _maxSteps;
            set
            {
                _maxSteps = value < 2 ? 2 : value;
                OnPropertyChanged();
            }
        }

        public bool ParallelModeOn
        {
            get => _parallelModeOn;
            set
            {
                _parallelModeOn = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanChangeThreadsCount));
            }
        }

        public bool CanChangeThreadsCount => IsReady & ParallelModeOn;

        public int ThreadsCount
        {
            get => _threadsCount;
            set
            {
                _threadsCount = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan LastRenderTime
        {
            get => _lastRenderTime;
            private set
            {
                _lastRenderTime = value;
                OnPropertyChanged();
            }
        }

        public int ResolutionMultiplier
        {
            get => _resolutionMultiplier;
            set
            {
                _resolutionMultiplier = value;
                OnPropertyChanged();
            }
        }

        public FractalRenderer()
        {
            Kr = 10;
            Kg = 15;
            Kb = 20;
        }

        public void RandomizePallet()
        {
            Kr = _rnd.Next();
            Kg = _rnd.Next();
            Kb = _rnd.Next();
        }

        public void ResetViewport()
        {
            _viewportCenter = new Complex(-0.75, 0.0);
            _viewportScale = 0; // So it will be calculated again in Refresh()
        }

        private (int width, int height) CalcBitmapSizeInPixels(DpiScale dpi, double width, double height)
        {
            var bmpWidth = (int) Math.Ceiling(width * dpi.DpiScaleX);
            var bmpHeight = (int) Math.Ceiling(height * dpi.DpiScaleY);
            return (bmpWidth, bmpHeight);
        }

        public void ZoomIn(DpiScale imgDpi, double imgWidth, double imgHeight, SelectionRect zoomArea)
        {
            var (bmpWidth, bmpHeight) = CalcBitmapSizeInPixels(imgDpi, imgWidth, imgHeight);
            var currentVpUpperLeft = Mandelbrot.FindCorner(bmpWidth, bmpHeight, _viewportCenter, _viewportScale);

            var zoomCenterX = zoomArea.X + zoomArea.Width / 2;
            var zoomCenterY = zoomArea.Y + zoomArea.Height / 2;
            var zoomCenterXpx = (int) Math.Round(zoomCenterX * imgDpi.DpiScaleX);
            var zoomCenterYpx = (int) Math.Round(zoomCenterY * imgDpi.DpiScaleY);

            var newVpCenter = Mandelbrot.MapPixel(zoomCenterXpx, zoomCenterYpx, currentVpUpperLeft, _viewportScale);
            var newScale = Math.Max(imgWidth / zoomArea.Width, imgHeight / zoomArea.Height) * _viewportScale;

            _viewportCenter = newVpCenter;
            _viewportScale = newScale;
        }

        public void ZoomOut(uint zoomX)
        {
            _viewportScale /= zoomX;
        }

        public async Task Refresh(DpiScale dpi, double width, double height)
        {
            IsReady = false;

            var (bmpWidth, bmpHeight) = CalcBitmapSizeInPixels(dpi, width, height);
            bmpWidth *= ResolutionMultiplier;
            bmpHeight *= ResolutionMultiplier;

            const double tolerance = 0.0001; // Randomly chosen without any sense
            if (Math.Abs(_viewportScale) < tolerance)
                _viewportScale = Mandelbrot.CalcScale(bmpWidth, bmpHeight, (new Complex(-2.05, 1.3), new Complex(0.55, -1.3)));

            if (_bmp == null
                || Math.Abs(_bmp.Width - bmpWidth) > tolerance
                || Math.Abs(_bmp.Height - bmpHeight) > tolerance
                || Math.Abs(_bmp.DpiX - dpi.PixelsPerInchX * ResolutionMultiplier) > tolerance
                || Math.Abs(_bmp.DpiY - dpi.PixelsPerInchY * ResolutionMultiplier) > tolerance)
                _bmp = new WriteableBitmap(bmpWidth, bmpHeight,
                    dpi.PixelsPerInchX * ResolutionMultiplier, dpi.PixelsPerInchY * ResolutionMultiplier,
                    PixelFormats.Bgr32, null);

            await DrawFractal(_bmp);

            OnPropertyChanged(nameof(Image));
            Progress = 0;
            IsReady = true;
        }

        private async Task DrawFractal(WriteableBitmap bmp)
        {
            try
            {
                bmp.Lock();

                var pxW = bmp.PixelWidth;
                var pxH = bmp.PixelHeight;
                var viewportUpperLeft = Mandelbrot.FindCorner(pxW, pxH, _viewportCenter, _viewportScale * ResolutionMultiplier);
                var fractalPixels = Enumerable.Range(0, pxW)
                    .SelectMany(x => Enumerable.Range(0, pxH).Select(y => (x, y)))
                    .AsParallel()
                    .WithDegreeOfParallelism(ParallelModeOn ? ThreadsCount : 1)
                    .Select(coords =>
                    {
                        var point = Mandelbrot.MapPixel(coords.x, coords.y, viewportUpperLeft,
                            _viewportScale * ResolutionMultiplier);
                        var steps = Mandelbrot.CheckPoint(point, MaxSteps);
                        return (coords.x, coords.y, color: StepsToColor(steps));
                    });

                var progressStep = bmp.PixelWidth * bmp.PixelHeight / 100;
                Progress = 0;

                var bytesPerPx = bmp.Format.BitsPerPixel / 8;
                var pBackBuffer = bmp.BackBuffer;
                var backBufferStride = bmp.BackBufferStride;
                await Task.Run(() =>
                {
                    var sw = Stopwatch.StartNew();
                    unsafe
                    {
                        var pixelCount = 0;
                        foreach (var pixel in fractalPixels)
                        {
                            var pPosition = pBackBuffer;
                            pPosition += pixel.y * backBufferStride;
                            pPosition += pixel.x * bytesPerPx;

                            *((int*) pPosition) = pixel.color;

                            if (++pixelCount % progressStep == 0)
                                Progress++;
                        }
                    }

                    sw.Stop();
                    LastRenderTime = sw.Elapsed;
                });
                bmp.AddDirtyRect(new Int32Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight));
            }
            finally
            {
                bmp.Unlock();
            }
        }

        private int StepsToColor(int steps)
        {
            var r = (steps * Kr) % 255;
            var g = (steps * Kg) % 255;
            var b = (steps * Kb) % 255;
            var c = (r << 16) | (g << 8) | b;
            return steps == MaxSteps ? 0 : c;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}