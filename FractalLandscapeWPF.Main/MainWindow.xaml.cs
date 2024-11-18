
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace FractalLandscapeWPF.Main
{
    public partial class MainWindow : Window
    {
        private FractalLandscapeGenerator landscapeGenerator;

        private Point lastMousePosition; // Последняя позиция мыши
        private PerspectiveCamera camera;
        private double cameraDistance = 5; // Расстояние от центра сцены
        private double cameraAngleX = 30; // Угол камеры по оси X
        private double cameraAngleY = 45; // Угол камеры по оси Y

        public MainWindow()
        {
            InitializeComponent();
            InitializeCamera();
            landscapeGenerator = new FractalLandscapeGenerator();
        }
        private void InitializeCamera()
        {
            // Инициализация камеры
            camera = new PerspectiveCamera
            {
                Position = new Point3D(0, -5, cameraDistance),
                LookDirection = new Vector3D(0, 5, -5),
                UpDirection = new Vector3D(0, 0, 1),
                FieldOfView = 60
            };
            Viewport.Camera = camera;
            UpdateCameraPosition();
        }

        private void UpdateCameraPosition()
        {
            // Обновляем позицию камеры на основе углов
            double radX = cameraAngleX * Math.PI / 180;
            double radY = cameraAngleY * Math.PI / 180;

            double x = cameraDistance * Math.Sin(radY) * Math.Cos(radX);
            double y = cameraDistance * Math.Sin(radX);
            double z = cameraDistance * Math.Cos(radY) * Math.Cos(radX);

            camera.Position = new Point3D(x, y, z);
            camera.LookDirection = new Vector3D(-x, -y, -z);
        }

        // Событие для обработки перемещения мыши
        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Поворот камеры с помощью мыши
                Point currentMousePosition = e.GetPosition(this);
                double dx = currentMousePosition.X - lastMousePosition.X;
                double dy = currentMousePosition.Y - lastMousePosition.Y;

                cameraAngleY += dx * 0.5; // Скорость вращения по горизонтали
                cameraAngleX -= dy * 0.5; // Скорость вращения по вертикали

                // Ограничиваем угол вращения по вертикали
                cameraAngleX = Math.Clamp(cameraAngleX, -89, 89);

                UpdateCameraPosition();
                lastMousePosition = currentMousePosition;
            }
        }

        // Событие для обработки нажатия мыши
        private void Viewport_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                lastMousePosition = e.GetPosition(this);
            }
        }

        // Событие для обработки колёсика мыши (масштабирование)
        private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            cameraDistance -= e.Delta * 0.001; // Скорость масштабирования
            cameraDistance = Math.Clamp(cameraDistance, 1, 20); // Ограничиваем диапазон
            UpdateCameraPosition();
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получение параметров из полей ввода
                int depth = int.Parse(DepthInput.Text);
                double displacement = double.Parse(DisplacementInput.Text);
                double seaLevel = double.Parse(SeaLevelInput.Text);

                landscapeGenerator.MaxDepth = depth;
                landscapeGenerator.DisplacementFactor = displacement;
                landscapeGenerator.SeaLevel = seaLevel;

                // Генерация ландшафта
                var landscape = landscapeGenerator.GenerateLandscape();

                // Отображение в 3D
                Viewport.Children.Clear();
                Viewport.Children.Add(new ModelVisual3D { Content = landscape });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
    }

    public class FractalLandscapeGenerator
    {
        public int MaxDepth { get; set; } = 3;
        public double DisplacementFactor { get; set; } = 0.35;
        public double SeaLevel { get; set; } = 0.3;

        private Dictionary<(double, double), double> heightCache = new();

        public Model3DGroup GenerateLandscape()
        {
            heightCache.Clear();

            // Начальный треугольник
            var p1 = new Point3D(-2, -2, GetRandomHeight());
            var p2 = new Point3D(2, -2, GetRandomHeight());
            var p3 = new Point3D(0, 2, GetRandomHeight());

            var triangles = new List<Triangle> { new Triangle(p1, p2, p3) };
                
            SubdivideTriangles(triangles, MaxDepth);

            var modelGroup = new Model3DGroup();

            // Добавляем свет
            modelGroup.Children.Add(new DirectionalLight(Colors.White, new Vector3D(-1, -1, -1)));

            foreach (var triangle in triangles)
            {
                modelGroup.Children.Add(CreateTriangleModel(triangle));
            }

            return modelGroup;
        }

        private void SubdivideTriangles(List<Triangle> triangles, int depth)
        {
            if (depth == 0) return;

            var newTriangles = new List<Triangle>();
            foreach (var triangle in triangles)
            {
                var m1 = GetMidpointWithCachedHeight(triangle.P1, triangle.P2, depth);
                var m2 = GetMidpointWithCachedHeight(triangle.P2, triangle.P3, depth);
                var m3 = GetMidpointWithCachedHeight(triangle.P3, triangle.P1, depth);

                newTriangles.Add(new Triangle(triangle.P1, m1, m3));
                newTriangles.Add(new Triangle(m1, triangle.P2, m2));
                newTriangles.Add(new Triangle(m3, m2, triangle.P3));
                newTriangles.Add(new Triangle(m1, m2, m3));
            }

            triangles.Clear();
            triangles.AddRange(newTriangles);

            SubdivideTriangles(triangles, depth - 1);
        }

        private Point3D GetMidpointWithCachedHeight(Point3D p1, Point3D p2, int depth)
        {
            var x = (p1.X + p2.X) / 2;
            var y = (p1.Y + p2.Y) / 2;

            if (!heightCache.TryGetValue((x, y), out var height))
            {
                height = (p1.Z + p2.Z) / 2 + GetRandomDisplacement(depth);
                heightCache[(x, y)] = height;
            }

            return new Point3D(x, y, height);
        }

        private double GetRandomHeight()
        {
            return new Random().NextDouble();
        }

        private double GetRandomDisplacement(int depth)
        {
            var scale = DisplacementFactor / depth;
            return (new Random().NextDouble() * 2 - 1) * scale;
        }

        private GeometryModel3D CreateTriangleModel(Triangle triangle)
        {
            var mesh = new MeshGeometry3D();

            mesh.Positions.Add(triangle.P1);
            mesh.Positions.Add(triangle.P2);
            mesh.Positions.Add(triangle.P3);

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);

            var averageHeight = (triangle.P1.Z + triangle.P2.Z + triangle.P3.Z) / 3;
            var material = new DiffuseMaterial(new SolidColorBrush(GetHeightBasedColor(averageHeight)));

            return new GeometryModel3D(mesh, material);
        }

        private Color GetHeightBasedColor(double height)
        {
            if (height < SeaLevel) return Colors.Blue;
            if (height < SeaLevel + 0.1) return Colors.SandyBrown;
            return Colors.Green;
        }

        private class Triangle
        {
            public Point3D P1 { get; }
            public Point3D P2 { get; }
            public Point3D P3 { get; }

            public Triangle(Point3D p1, Point3D p2, Point3D p3)
            {
                P1 = p1;
                P2 = p2;
                P3 = p3;
            }
        }
    }
}

