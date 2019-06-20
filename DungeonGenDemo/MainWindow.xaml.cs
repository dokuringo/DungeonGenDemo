using System;
using System.Collections.Generic;
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
using System.Windows.Media.Media3D;

namespace DungeonGenDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private Viewport3D mViewport;
        //private CameraController mCam;
        DungeonGraphVisual mVisual;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DungeonGraph graph = new DungeonGraph(8, 8, 8);
            int seed = new Random().Next();
            graph.Generate(seed);
            //graph.Generate(1045790396);

            mVisual = new DungeonGraphVisual(graph);

            canvas.Children.Add(mVisual.Viewport);
            mVisual.Viewport.Height = canvas.RenderSize.Height;
            mVisual.Viewport.Width = canvas.RenderSize.Width;
        }

        private void canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (mVisual != null)
            {
                Viewport3D mViewport = mVisual.Viewport;
                mViewport.Height = e.NewSize.Height;
                mViewport.Width = e.NewSize.Width;
            }
        }

        //Canvas mouse dragging
        private bool mLeftMouseDrag = false;
        private Point mLastMousePoint;

        private void canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mLeftMouseDrag = true;
            mLastMousePoint = e.GetPosition(canvas);
            canvas_MouseMove(sender, e);
        }

        private void canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mLeftMouseDrag = false;
        }

        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (mLeftMouseDrag)
            {
                Viewport3D mViewport = mVisual.Viewport;

                double camFOV = 60;

                Point clickPoint = e.GetPosition(canvas);
                double distX = mLastMousePoint.X - clickPoint.X;
                double distY = mLastMousePoint.Y - clickPoint.Y;
                double rotX = (distX / mViewport.Width) * camFOV;
                double rotY = (distY / mViewport.Width) * camFOV;

                if (rotX != 0 || rotY != 0)
                {
                    mVisual.Rotate(rotY, -rotX);
                }
                

                mLastMousePoint = clickPoint;
            }
        }
    }
}
