using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NetworkService.Model;

namespace NetworkService.Controls
{
    public class MeasurementBarChart : Canvas
    {
        private INotifyCollectionChanged subscribedCollection;

        public static readonly DependencyProperty MeasurementsProperty =
            DependencyProperty.Register(
                "Measurements",
                typeof(IEnumerable),
                typeof(MeasurementBarChart),
                new PropertyMetadata(null, OnMeasurementsChanged));

        public IEnumerable Measurements
        {
            get { return (IEnumerable)GetValue(MeasurementsProperty); }
            set { SetValue(MeasurementsProperty, value); }
        }

        private static void OnMeasurementsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MeasurementBarChart chart = d as MeasurementBarChart;

            if (chart == null)
            {
                return;
            }

            chart.Unsubscribe();
            chart.Subscribe(e.NewValue as INotifyCollectionChanged);
            chart.InvalidateVisual();
        }

        private void Subscribe(INotifyCollectionChanged collection)
        {
            subscribedCollection = collection;

            if (subscribedCollection != null)
            {
                subscribedCollection.CollectionChanged += MeasurementsCollectionChanged;
            }
        }

        private void Unsubscribe()
        {
            if (subscribedCollection != null)
            {
                subscribedCollection.CollectionChanged -= MeasurementsCollectionChanged;
                subscribedCollection = null;
            }
        }

        private void MeasurementsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double width = ActualWidth;
            double height = ActualHeight;

            if (width < 100 || height < 100)
            {
                return;
            }

            MeasurementPoint[] points = Measurements == null
                ? new MeasurementPoint[0]
                : Measurements.OfType<MeasurementPoint>().ToArray();

            if (points.Length == 0)
            {
                DrawText(
                    dc,
                    "No measurements available for selected entity.",
                    width / 2 - 190,
                    height / 2 - 15,
                    ResourceBrush("UIMutedTextBrush", Brushes.Gray),
                    13,
                    FontWeights.Bold);

                return;
            }

            double left = 65;
            double right = 25;
            double top = 35;
            double bottom = 70;

            double chartWidth = width - left - right;
            double chartHeight = height - top - bottom;

            if (chartWidth <= 0 || chartHeight <= 0)
            {
                return;
            }

            double maxValue = Math.Max(6.0, points.Max(point => point.Value));

            DrawAxes(dc, left, top, bottom, width, height);
            DrawHorizontalTicks(dc, left, right, top, bottom, width, height, chartHeight, maxValue);
            DrawBars(dc, points, left, bottom, height, chartWidth, chartHeight, maxValue);
            DrawAxisLabels(dc, width, height);
        }

        private void DrawAxes(DrawingContext dc, double left, double top, double bottom, double width, double height)
        {
            Pen axisPen = new Pen(ResourceBrush("UIPrimaryBrush", Brushes.Black), 2);

            dc.DrawLine(axisPen, new Point(left, top), new Point(left, height - bottom));
            dc.DrawLine(axisPen, new Point(left, height - bottom), new Point(width - 25, height - bottom));
        }

        private void DrawHorizontalTicks(
            DrawingContext dc,
            double left,
            double right,
            double top,
            double bottom,
            double width,
            double height,
            double chartHeight,
            double maxValue)
        {
            Pen gridPen = new Pen(ResourceBrush("UISubtleBorderBrush", Brushes.LightGray), 0.6);
            Brush textBrush = ResourceBrush("UIPrimaryBrush", Brushes.Black);

            for (double value = 0; value <= maxValue; value += 1.0)
            {
                double y = height - bottom - (value / maxValue) * chartHeight;

                dc.DrawLine(gridPen, new Point(left, y), new Point(width - right, y));

                DrawText(
                    dc,
                    value.ToString("0", CultureInfo.InvariantCulture),
                    left - 28,
                    y - 8,
                    textBrush,
                    11,
                    FontWeights.Normal);
            }
        }

        private void DrawBars(
            DrawingContext dc,
            MeasurementPoint[] points,
            double left,
            double bottom,
            double height,
            double chartWidth,
            double chartHeight,
            double maxValue)
        {
            double gap = 24;
            double barWidth = (chartWidth - gap * (points.Length + 1)) / points.Length;

            if (barWidth < 18)
            {
                barWidth = 18;
            }

            for (int i = 0; i < points.Length; i++)
            {
                MeasurementPoint point = points[i];

                double normalizedValue = Math.Max(0.0, point.Value);
                double barHeight = (normalizedValue / maxValue) * chartHeight;

                if (barHeight < 2)
                {
                    barHeight = 2;
                }

                double x = left + gap + i * (barWidth + gap);
                double y = height - bottom - barHeight;

                Brush fill = point.IsValid
                    ? new SolidColorBrush(Color.FromRgb(136, 136, 136))
                    : new SolidColorBrush(Color.FromRgb(34, 34, 34));

                Rect barRect = new Rect(x, y, barWidth, barHeight);

                dc.DrawRectangle(
                    fill,
                    new Pen(ResourceBrush("UIPrimaryBrush", Brushes.Black), 1.5),
                    barRect);

                DrawText(
                    dc,
                    point.Value.ToString("0.0", CultureInfo.InvariantCulture) + (point.IsValid ? "" : " !"),
                    x + barWidth / 2 - 18,
                    y - 22,
                    ResourceBrush("UIPrimaryBrush", Brushes.Black),
                    11,
                    FontWeights.Bold);

                DrawText(
                    dc,
                    point.FormattedTimestamp,
                    x + barWidth / 2 - 25,
                    height - bottom + 10,
                    ResourceBrush("UIPrimaryBrush", Brushes.Black),
                    11,
                    FontWeights.Normal);
            }
        }

        private void DrawAxisLabels(DrawingContext dc, double width, double height)
        {
            DrawText(
                dc,
                "X: time moments",
                width / 2 - 55,
                height - 30,
                ResourceBrush("UIMutedTextBrush", Brushes.Gray),
                12,
                FontWeights.Normal);

            DrawText(
                dc,
                "Y: MW",
                12,
                12,
                ResourceBrush("UIMutedTextBrush", Brushes.Gray),
                12,
                FontWeights.Normal);
        }

        private void DrawText(
            DrawingContext dc,
            string text,
            double x,
            double y,
            Brush brush,
            double fontSize,
            FontWeight fontWeight)
        {
            FormattedText formattedText = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Courier New"), FontStyles.Normal, fontWeight, FontStretches.Normal),
                fontSize,
                brush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            dc.DrawText(formattedText, new Point(x, y));
        }

        private Brush ResourceBrush(string key, Brush fallback)
        {
            object value = Application.Current.Resources[key];

            Brush brush = value as Brush;

            return brush ?? fallback;
        }
    }
}