using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DungeonGenDemo
{
    public class DungeonGraphVisual
    {
        public Viewport3D Viewport { get; private set; }
        public OrthographicCamera Camera { get; private set; }
        public DungeonGraph DungeonGraph { get; private set; }
        public Model3DGroup ModelGroup { get; private set; }

        private static Color[] _colors =
        {
            Colors.LightGreen,
            Colors.LightBlue,
            Colors.LightPink,
            Colors.LightGray
        };

        public DungeonGraphVisual(DungeonGraph graph)
        {
            DungeonGraph = graph;
            Generate();
        }

        MeshGeometry3D MCube()
        {
            MeshGeometry3D cube = new MeshGeometry3D();
            Point3DCollection corners = new Point3DCollection();
            Point3D p0 = new Point3D(-0.5, 0.5, -0.5);
            Point3D p1 = new Point3D(-0.5, 0.5, 0.5);
            Point3D p2 = new Point3D(0.5, 0.5, -0.5);
            Point3D p3 = new Point3D(0.5, 0.5, 0.5);
            Point3D p4 = new Point3D(-0.5, -0.5, -0.5);
            Point3D p5 = new Point3D(-0.5, -0.5, 0.5);
            Point3D p6 = new Point3D(0.5, -0.5, -0.5);
            Point3D p7 = new Point3D(0.5, -0.5, 0.5);
            
            corners.Add(p0);
            corners.Add(p1);
            corners.Add(p2);
            corners.Add(p3);

            corners.Add(p1);
            corners.Add(p5);
            corners.Add(p3);
            corners.Add(p7);

            corners.Add(p3);
            corners.Add(p7);
            corners.Add(p2);
            corners.Add(p6);

            corners.Add(p2);
            corners.Add(p6);
            corners.Add(p0);
            corners.Add(p4);

            corners.Add(p0);
            corners.Add(p4);
            corners.Add(p1);
            corners.Add(p5);

            corners.Add(p5);
            corners.Add(p4);
            corners.Add(p7);
            corners.Add(p6);

            cube.Positions = corners;

            int[] indices = new int[3 * 2 * 6];
            for (int i = 0; i < 6; i++)
            {
                int s = i * 6;
                int o = i * 4;
                indices[s + 0] = o + 0;
                indices[s + 1] = o + 1;
                indices[s + 2] = o + 2;
                indices[s + 3] = o + 2;
                indices[s + 4] = o + 1;
                indices[s + 5] = o + 3;
            }

            Int32Collection tris = new Int32Collection();
            foreach (int i in indices) tris.Add(i);
            cube.TriangleIndices = tris;

            return cube;
        }

        public void Rotate(double x, double y)
        {
            Vector3D aim = Camera.LookDirection;
            aim.Normalize();
            Vector3D camRight = Vector3D.CrossProduct(new Vector3D(0, 1, 0), aim);
            
            Quaternion pitchRot = new Quaternion(camRight, x);
            Quaternion turnRot = new Quaternion(new Vector3D(0, 1, 0), y);
            Quaternion rotMul = pitchRot * turnRot;
            
            Matrix3D m = ModelGroup.Transform.Value;
            m.Rotate(rotMul);
            ModelGroup.Transform = new MatrixTransform3D(m);
        }

        private void Generate()
        {
            MeshGeometry3D plane = MCube();
            Model3DGroup modelGroup = new Model3DGroup();
            Model3DGroup lightGroup = new Model3DGroup();

            AmbientLight ambLight1 = new AmbientLight();
            ambLight1.Color = Color.FromRgb(50, 50, 50);
            lightGroup.Children.Add(ambLight1);

            DirectionalLight dirLight1 = new DirectionalLight();
            dirLight1.Color = Colors.White;
            dirLight1.Direction = new Vector3D(-2, -6, 5);
            lightGroup.Children.Add(dirLight1);

            foreach (CellEdge e in DungeonGraph.Edges)
            {
                if (e.ConnectionType == CellEdgeType.Door)
                {
                    double x = 0.5;
                    double y = 0.5;
                    double z = 0;
                    double scale = 0.2;
                    double zscale = 0.1;

                    Color floorCol = Colors.Red;

                    if (e.Alignment == CellEdgeAlignment.X)
                    {
                        y -= 0.5;
                        floorCol = _colors[e.PositionZ % _colors.Length];
                    }
                    else if (e.Alignment == CellEdgeAlignment.Y)
                    {
                        x -= 0.5;
                        floorCol = _colors[e.PositionZ % _colors.Length];
                    }
                    else
                    {
                        x -= 0.5;
                        y -= 0.5;
                        z += 0.5;
                        scale = 0.1;
                        zscale = 3.2;
                    }

                    x += e.PositionX;
                    y += e.PositionY;
                    z += e.PositionZ;

                    GeometryModel3D geom = new GeometryModel3D();
                    geom.Geometry = plane;
                    geom.Material = new DiffuseMaterial(new SolidColorBrush(floorCol));

                    Matrix3D m = Matrix3D.Identity;
                    m.Scale(new Vector3D(scale, zscale, scale));
                    m.Translate(new Vector3D(x + 0.5, z * 3, y + 0.5));
                    geom.Transform = new MatrixTransform3D(m);

                    modelGroup.Children.Add(geom);
                }

            }

            foreach (Node n in DungeonGraph.Nodes)
            {
                GeometryModel3D geom = new GeometryModel3D();
                geom.Geometry = plane;
                Color floorCol = _colors[n.PositionZ % _colors.Length];
                geom.Material = new DiffuseMaterial(new SolidColorBrush(floorCol));

                Matrix3D m = Matrix3D.Identity;
                m.Scale(new Vector3D(n.Width - 0.2, 0.1, n.Length - 0.2));
                m.Translate(new Vector3D(
                    n.PositionX + n.Width * 0.5,
                    n.PositionZ * 3,
                    n.PositionY + n.Length * 0.5));
                geom.Transform = new MatrixTransform3D(m);

                modelGroup.Children.Add(geom);
            }

            Camera = new OrthographicCamera();
            Camera.FarPlaneDistance = 400;
            Camera.NearPlaneDistance = 0.5;
            Camera.Position = new Point3D(-40, 30, -40);
            Camera.LookDirection = new Vector3D(4, -3, 4);
            Camera.UpDirection = new Vector3D(0, 1, 0);
            Camera.Width = 15;

            ModelVisual3D modelVisual = new ModelVisual3D();
            modelVisual.Content = modelGroup;

            ModelVisual3D lightVisual = new ModelVisual3D();
            lightVisual.Content = lightGroup;

            ModelGroup = modelGroup;
            TranslateTransform3D groupTrans = new TranslateTransform3D(
                -DungeonGraph.Width / 2.0,
                -DungeonGraph.Floors * 1.5,
                -DungeonGraph.Length / 2.0);
            modelGroup.Transform = groupTrans;

            Viewport = new Viewport3D();
            Viewport.Camera = Camera;
            Viewport.Children.Add(modelVisual);
            Viewport.Children.Add(lightVisual);
        }
    }
}
