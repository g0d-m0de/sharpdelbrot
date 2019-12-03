using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Shapes;

namespace Sharpdelbrot
{
    public class SelectionRect : INotifyPropertyChanged
    {
        private double _x;
        private double _y;
        private double _width;
        private double _height;

        public double X
        {
            get => _x;
            set
            {
                _x = value;
                OnPropertyChanged();
            }
        }

        public double Y
        {
            get => _y;
            set
            {
                _y = value;
                OnPropertyChanged();
            }
        }

        public double Width
        {
            get => _width;
            set
            {
                _width = value; 
                OnPropertyChanged();
            }
        }

        public double Height
        {
            get => _height;
            set
            {
                _height = value; 
                OnPropertyChanged();
            }
        }

        public Rectangle View { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public SelectionRect(Rectangle view)
        {
            View = view;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}