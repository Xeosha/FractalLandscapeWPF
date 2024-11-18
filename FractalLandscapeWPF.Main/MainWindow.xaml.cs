
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace FractalLandscapeWPF.Main
{
    public partial class MainWindow : Window
    {
        private const int MaxDepth = 3; // Максимальная глубина рекурсии
        private const double DisplacementFactor = 0.35; // Коэффициент смещения
        private const double SeaLevel = 0.3; // Уровень моря
        private readonly Random random = new Random();

        private GeometryModel3D landscapeModel;
        private MeshGeometry3D landscapeMesh;

        public MainWindow()
        {
            InitializeComponent();
            GenerateLandscape();
        }

        private void GenerateLandscape()
        {
            // Создаём 3D-сцену
            MainViewport.Children.Clear();

            var camera = new PerspectiveCamera
            {
                Position = new Point3D(0, -5, 5),
                LookDirection = new Vector3D(0, 5, -5),
                UpDirection = new Vector3D(0, 0, 1),
                FieldOfView = 60
            };
            MainViewport.Camera = camera;

            var light = new DirectionalLight(Colors.White, new Vector3D(-1, -1, -1));
            var lightModel = new ModelVisual3D { Content = light };
            MainViewport.Children.Add(lightModel);

            landscapeMesh = new MeshGeometry3D();
            var material = new DiffuseMaterial(new SolidColorBrush(Colors.Green));
            landscapeModel = new GeometryModel3D(landscapeMesh, material);
            var modelVisual = new ModelVisual3D { Content = landscapeModel };
            MainViewport.Children.Add(modelVisual);

            // Начальный треугольник
            var p1 = new Point3D(-2, -2, GetRandomHeight());
            var p2 = new Point3D(2, -2, GetRandomHeight());
            var p3 = new Point3D(0, 2, GetRandomHeight());

            var triangles = new List<Triangle3D> { new Triangle3D(p1, p2, p3) };
            SubdivideTriangles(triangles, MaxDepth);

            foreach (var triangle in triangles)
            {
                AddTriangleToMesh(triangle);
            }
        }

        private void SubdivideTriangles(List<Triangle3D> triangles, int depth)
        {
            if (depth == 0) return;

            var newTriangles = new List<Triangle3D>();

            foreach (var triangle in triangles)
            {
                // Вычисляем середины сторон
                var m1 = MidPoint(triangle.P1, triangle.P2);
                var m2 = MidPoint(triangle.P2, triangle.P3);
                var m3 = MidPoint(triangle.P3, triangle.P1);

                // Смещаем середины вверх/вниз
                m1.Z += GetRandomDisplacement(depth);
                m2.Z += GetRandomDisplacement(depth);
                m3.Z += GetRandomDisplacement(depth);

                // Создаём четыре новых треугольника
                newTriangles.Add(new Triangle3D(triangle.P1, m1, m3));
                newTriangles.Add(new Triangle3D(m1, triangle.P2, m2));
                newTriangles.Add(new Triangle3D(m3, m2, triangle.P3));
                newTriangles.Add(new Triangle3D(m1, m2, m3));
            }

            triangles.Clear();
            triangles.AddRange(newTriangles);

            SubdivideTriangles(triangles, depth - 1);
        }

        private Point3D MidPoint(Point3D p1, Point3D p2)
        {
            return new Point3D(
                (p1.X + p2.X) / 2,
                (p1.Y + p2.Y) / 2,
                (p1.Z + p2.Z) / 2
            );
        }

        private double GetRandomHeight()
        {
            return random.NextDouble();
        }

        private double GetRandomDisplacement(int depth)
        {
            return (random.NextDouble() * 2 - 1) * DisplacementFactor / depth;
        }

        private void AddTriangleToMesh(Triangle3D triangle)
        {
            // Словарь для хранения индексов вершин
            var vertexDictionary = new Dictionary<Point3D, int>(new Point3DEqualityComparer());

            // Добавляем вершины
            int baseIndex = landscapeMesh.Positions.Count;

            AddVertex(triangle.P1, vertexDictionary);
            AddVertex(triangle.P2, vertexDictionary);
            AddVertex(triangle.P3, vertexDictionary);

            // Добавляем индексы для треугольника
            landscapeMesh.TriangleIndices.Add(vertexDictionary[triangle.P1]);
            landscapeMesh.TriangleIndices.Add(vertexDictionary[triangle.P2]);
            landscapeMesh.TriangleIndices.Add(vertexDictionary[triangle.P3]);

            // Разделяем треугольник на части, если он пересекает уровень моря
            if (IsTriangleAboveSeaLevel(triangle))
            {
                landscapeMesh.TextureCoordinates.Add(new Point(1, 1)); // Над уровнем моря
            }
            else
            {
                landscapeMesh.TextureCoordinates.Add(new Point(0, 0)); // Под уровнем моря
            }
        }

        private bool IsTriangleAboveSeaLevel(Triangle3D triangle)
        {
            return (triangle.P1.Z >= SeaLevel && triangle.P2.Z >= SeaLevel && triangle.P3.Z >= SeaLevel);
        }

        private void AddVertex(Point3D point, Dictionary<Point3D, int> vertexDictionary)
        {
            if (!vertexDictionary.ContainsKey(point))
            {
                landscapeMesh.Positions.Add(point);
                vertexDictionary[point] = landscapeMesh.Positions.Count - 1;
            }
        }

        private Color GetHeightBasedColor(double height)
        {
            if (height < SeaLevel) return Colors.Blue;
            if (height < SeaLevel + 0.1) return Colors.SandyBrown;
            return Colors.Green;
        }

        private void WireframeMode_Click(object sender, RoutedEventArgs e)
        {
            landscapeModel.Material = new DiffuseMaterial(new SolidColorBrush(Colors.Black));
        }

        private void SolidMode_Click(object sender, RoutedEventArgs e)
        {
            landscapeModel.Material = new DiffuseMaterial(new SolidColorBrush(Colors.Green));
        }

        private void ShadedMode_Click(object sender, RoutedEventArgs e)
        {
            landscapeModel.Material = new DiffuseMaterial(new SolidColorBrush(Colors.Gray));
        }
    }

    public class Triangle3D
    {
        public Point3D P1, P2, P3;

        public Triangle3D(Point3D p1, Point3D p2, Point3D p3)
        {
            P1 = p1;
            P2 = p2;
            P3 = p3;
        }
    }

    public class Point3DEqualityComparer : IEqualityComparer<Point3D>
    {
        public bool Equals(Point3D p1, Point3D p2)
        {
            // Проверка на точность совпадения координат с допустимой погрешностью
            double tolerance = 0.0001;
            return Math.Abs(p1.X - p2.X) < tolerance &&
                   Math.Abs(p1.Y - p2.Y) < tolerance &&
                   Math.Abs(p1.Z - p2.Z) < tolerance;
        }

        public int GetHashCode(Point3D p)
        {
            return p.X.GetHashCode() ^ p.Y.GetHashCode() ^ p.Z.GetHashCode();
        }
    }

}

