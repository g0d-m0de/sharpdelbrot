using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Sharpdelbrot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FractalRenderer Renderer { get; } = new FractalRenderer();

        private SelectionRect Selection { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = Renderer;
        }

        private async void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            await RefreshView();
        }

        private async Task RefreshView()
        {
            try
            {
                var imgDpi = VisualTreeHelper.GetDpi(CanvasFractalView);

                if (Selection != null)
                {
                    Renderer.ZoomIn(imgDpi, CanvasFractalView.ActualWidth, CanvasFractalView.ActualHeight, Selection);
                    HideSelection();
                }

                await Renderer.Refresh(imgDpi, CanvasFractalView.ActualWidth, CanvasFractalView.ActualHeight);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImageFractalView_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            HideSelection();

            var selectionRect = new Rectangle
            {
                DataContext = Selection,
                Stroke = Brushes.Blue
            };

            var mouse = e.GetPosition(CanvasFractalView);
            Selection = new SelectionRect(selectionRect)
            {
                X = mouse.X,
                Y = mouse.Y
            };

            var bndX = new Binding("X") {Source = Selection};
            selectionRect.SetBinding(Canvas.LeftProperty, bndX);

            var bndY = new Binding("Y") {Source = Selection};
            selectionRect.SetBinding(Canvas.TopProperty, bndY);

            var bndWidth = new Binding("Width") {Source = Selection};
            selectionRect.SetBinding(WidthProperty, bndWidth);

            var bndHeight = new Binding("Height") {Source = Selection};
            selectionRect.SetBinding(HeightProperty, bndHeight);

            selectionRect.MouseLeftButtonUp += async (o, args) => { await RefreshView(); };

            CanvasFractalView.Children.Add(selectionRect);
        }

        private void ImageFractalView_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (Selection == null || e.LeftButton == MouseButtonState.Released)
                return;

            var mouse = e.GetPosition(Selection.View);
            Selection.Width = mouse.X;
            Selection.Height = mouse.Y;
        }

        private void HideSelection()
        {
            if (Selection == null)
                return;

            CanvasFractalView.Children.Remove(Selection.View);
            Selection = null;
        }

        private void ImageFractalView_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            HideSelection();
        }

        private async void ButtonZoomOut_Click(object sender, RoutedEventArgs e)
        {
            HideSelection();
            Renderer.ZoomOut(4);
            await RefreshView();
        }

        private async void ButtonRandomize_Click(object sender, RoutedEventArgs e)
        {
            HideSelection();
            Renderer.RandomizePallet();
            await RefreshView();
        }

        private async void ButtonReset_Click(object sender, RoutedEventArgs e)
        {
            HideSelection();
            Renderer.ResetViewport();
            await RefreshView();
        }
    }
}