using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;

namespace LaunchMe.UI.Widgets.ShapePhoto;

public sealed partial class ShapePhotoWidgetView : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(ShapePhotoWidgetViewModel),
            typeof(ShapePhotoWidgetView),
            new PropertyMetadata(null, OnViewModelChanged));

    public ShapePhotoWidgetViewModel? ViewModel
    {
        get => (ShapePhotoWidgetViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public ShapePhotoWidgetView()
    {
        InitializeComponent();
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ShapePhotoWidgetView view) return;

        if (e.OldValue is ShapePhotoWidgetViewModel oldVm)
        {
            oldVm.PropertyChanged -= view.OnViewModelPropertyChanged;
        }

        if (e.NewValue is ShapePhotoWidgetViewModel newVm)
        {
            newVm.PropertyChanged += view.OnViewModelPropertyChanged;
            view.UpdateShapeVisual();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ShapePhotoWidgetViewModel.SelectedShape)
            or nameof(ShapePhotoWidgetViewModel.PresetSize)
            or nameof(ShapePhotoWidgetViewModel.ImagePath)
            or nameof(ShapePhotoWidgetViewModel.FillColorHex))
        {
            DispatcherQueue?.TryEnqueue(UpdateShapeVisual);
        }
    }

    private void UpdateShapeVisual()
    {
        if (ViewModel is null) return;

        ShapeMaskPath.Data = BuildGeometry(ViewModel.SelectedShape, 100, 100);

        if (!string.IsNullOrWhiteSpace(ViewModel.ImagePath))
        {
            try
            {
                ShapeMaskPath.Fill = new ImageBrush
                {
                    ImageSource = new BitmapImage(new Uri(ViewModel.ImagePath, UriKind.RelativeOrAbsolute)),
                    Stretch = Stretch.UniformToFill
                };
                return;
            }
            catch
            {
                // Fallback to fill brush when path cannot be loaded
            }
        }

        ShapeMaskPath.Fill = ViewModel.BackgroundBrush;
    }

    private static Geometry BuildGeometry(ShapeMaskType shape, double width, double height)
    {
        return shape switch
        {
            ShapeMaskType.Circle => new EllipseGeometry
            {
                Center = new Point(width / 2.0, height / 2.0),
                RadiusX = width / 2.0,
                RadiusY = height / 2.0
            },
            ShapeMaskType.Squircle => CreateSquircleGeometry(width, height, width * 0.24),
            ShapeMaskType.Hexagon => CreatePolygonGeometry(
                new Point(width * 0.50, 0),
                new Point(width, height * 0.25),
                new Point(width, height * 0.75),
                new Point(width * 0.50, height),
                new Point(0, height * 0.75),
                new Point(0, height * 0.25)),
            ShapeMaskType.Diamond => CreatePolygonGeometry(
                new Point(width * 0.50, 0),
                new Point(width, height * 0.50),
                new Point(width * 0.50, height),
                new Point(0, height * 0.50)),
            ShapeMaskType.Star => CreateStarGeometry(width / 2.0, height / 2.0, width * 0.48, width * 0.20, 5),
            ShapeMaskType.Heart => CreateHeartGeometry(width, height),
            _ => new RectangleGeometry { Rect = new Rect(0, 0, width, height) }
        };
    }

    private static PathGeometry CreateSquircleGeometry(double w, double h, double r)
    {
        double radius = Math.Min(r, Math.Min(w, h) / 2.0);
        var arcSize = new Size(radius, radius);
        var figure = new PathFigure
        {
            StartPoint = new Point(radius, 0),
            IsClosed = true,
            IsFilled = true
        };
        figure.Segments.Add(new LineSegment { Point = new Point(w - radius, 0) });
        figure.Segments.Add(new ArcSegment
        {
            Point = new Point(w, radius),
            Size = arcSize,
            SweepDirection = SweepDirection.Clockwise
        });
        figure.Segments.Add(new LineSegment { Point = new Point(w, h - radius) });
        figure.Segments.Add(new ArcSegment
        {
            Point = new Point(w - radius, h),
            Size = arcSize,
            SweepDirection = SweepDirection.Clockwise
        });
        figure.Segments.Add(new LineSegment { Point = new Point(radius, h) });
        figure.Segments.Add(new ArcSegment
        {
            Point = new Point(0, h - radius),
            Size = arcSize,
            SweepDirection = SweepDirection.Clockwise
        });
        figure.Segments.Add(new LineSegment { Point = new Point(0, radius) });
        figure.Segments.Add(new ArcSegment
        {
            Point = new Point(radius, 0),
            Size = arcSize,
            SweepDirection = SweepDirection.Clockwise
        });

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }

    private static PathGeometry CreatePolygonGeometry(params Point[] points)
    {
        var figure = new PathFigure { StartPoint = points[0], IsClosed = true, IsFilled = true };
        for (int i = 1; i < points.Length; i++)
        {
            figure.Segments.Add(new LineSegment { Point = points[i] });
        }
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }

    private static PathGeometry CreateStarGeometry(double cx, double cy, double outerR, double innerR, int points)
    {
        var figure = new PathFigure { IsClosed = true, IsFilled = true };
        double angleStep = Math.PI / points;
        for (int i = 0; i < points * 2; i++)
        {
            double r = (i % 2 == 0) ? outerR : innerR;
            double angle = (i * angleStep) - (Math.PI / 2.0);
            var pt = new Point(cx + (r * Math.Cos(angle)), cy + (r * Math.Sin(angle)));
            if (i == 0) figure.StartPoint = pt;
            else figure.Segments.Add(new LineSegment { Point = pt });
        }
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }

    private static PathGeometry CreateHeartGeometry(double w, double h)
    {
        var figure = new PathFigure
        {
            StartPoint = new Point(w * 0.50, h * 0.88),
            IsClosed = true,
            IsFilled = true
        };
        figure.Segments.Add(new BezierSegment
        {
            Point1 = new Point(w * 0.10, h * 0.62),
            Point2 = new Point(0, h * 0.30),
            Point3 = new Point(w * 0.25, h * 0.14)
        });
        figure.Segments.Add(new BezierSegment
        {
            Point1 = new Point(w * 0.40, h * 0.05),
            Point2 = new Point(w * 0.50, h * 0.20),
            Point3 = new Point(w * 0.50, h * 0.26)
        });
        figure.Segments.Add(new BezierSegment
        {
            Point1 = new Point(w * 0.50, h * 0.20),
            Point2 = new Point(w * 0.60, h * 0.05),
            Point3 = new Point(w * 0.75, h * 0.14)
        });
        figure.Segments.Add(new BezierSegment
        {
            Point1 = new Point(w, h * 0.30),
            Point2 = new Point(w * 0.90, h * 0.62),
            Point3 = new Point(w * 0.50, h * 0.88)
        });
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }
}
