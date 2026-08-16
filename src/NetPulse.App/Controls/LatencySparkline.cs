using System.Windows;
using System.Windows.Media;

namespace NetPulse.App.Controls;

public sealed class LatencySparkline : FrameworkElement
{
    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
        nameof(Points),
        typeof(IReadOnlyList<double?>),
        typeof(LatencySparkline),
        new FrameworkPropertyMetadata(
            Array.Empty<double?>(),
            FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        nameof(Stroke),
        typeof(Brush),
        typeof(LatencySparkline),
        new FrameworkPropertyMetadata(
            Brushes.MediumSpringGreen,
            FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty EmptyTextBrushProperty =
        DependencyProperty.Register(
            nameof(EmptyTextBrush),
            typeof(Brush),
            typeof(LatencySparkline),
            new FrameworkPropertyMetadata(
                Brushes.Gray,
                FrameworkPropertyMetadataOptions.AffectsRender));

    public IReadOnlyList<double?> Points
    {
        get => (IReadOnlyList<double?>)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public Brush Stroke
    {
        get => (Brush)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public Brush EmptyTextBrush
    {
        get => (Brush)GetValue(EmptyTextBrushProperty);
        set => SetValue(EmptyTextBrushProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var bounds = new Rect(0, 0, ActualWidth, ActualHeight);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        DrawGuides(drawingContext, bounds);
        var points = Points ?? Array.Empty<double?>();
        var measured = points.Where(static value => value.HasValue).ToArray();
        if (points.Count < 2 || measured.Length == 0)
        {
            DrawEmptyState(drawingContext, bounds);
            return;
        }

        var minimum = measured.Min(static value => value!.Value);
        var maximum = measured.Max(static value => value!.Value);
        var range = Math.Max(1, maximum - minimum);
        var plot = new Rect(3, 5, Math.Max(0, bounds.Width - 6), Math.Max(0, bounds.Height - 10));
        var step = plot.Width / (points.Count - 1);
        var pen = new Pen(Stroke, 2);
        pen.Freeze();
        Point? previous = null;
        Point? latest = null;

        for (var index = 0; index < points.Count; index++)
        {
            var value = points[index];
            if (!value.HasValue)
            {
                previous = null;
                continue;
            }

            var normalized = (value.Value - minimum) / range;
            var current = new Point(
                plot.Left + (index * step),
                plot.Bottom - (normalized * plot.Height));

            if (previous.HasValue)
            {
                drawingContext.DrawLine(pen, previous.Value, current);
            }

            previous = current;
            latest = current;
        }

        if (latest.HasValue)
        {
            drawingContext.DrawEllipse(Stroke, null, latest.Value, 3, 3);
        }
    }

    private static void DrawGuides(DrawingContext drawingContext, Rect bounds)
    {
        var guideBrush = new SolidColorBrush(Color.FromArgb(70, 55, 76, 84));
        guideBrush.Freeze();
        var guidePen = new Pen(guideBrush, 1);
        guidePen.Freeze();

        for (var index = 1; index <= 3; index++)
        {
            var y = bounds.Height * index / 4;
            drawingContext.DrawLine(
                guidePen,
                new Point(0, y),
                new Point(bounds.Width, y));
        }
    }

    private void DrawEmptyState(DrawingContext drawingContext, Rect bounds)
    {
        var text = new FormattedText(
            "Awaiting measurable samples",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Bahnschrift SemiCondensed"),
            11,
            EmptyTextBrush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        drawingContext.DrawText(
            text,
            new Point(
                Math.Max(0, (bounds.Width - text.Width) / 2),
                Math.Max(0, (bounds.Height - text.Height) / 2)));
    }
}
