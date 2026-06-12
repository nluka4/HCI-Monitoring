using NetworkService.Model;
using NetworkService.ViewModel;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NetworkService.Views
{
    public partial class GraphView : UserControl
    {
        private GraphViewModel subscribedViewModel;

        public GraphView()
        {
            InitializeComponent();
            DataContextChanged += GraphView_DataContextChanged;
        }

        private GraphViewModel ViewModel
        {
            get { return DataContext as GraphViewModel; }
        }

        private void GraphView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UnsubscribeFromViewModel();

            subscribedViewModel = e.NewValue as GraphViewModel;

            if (subscribedViewModel != null)
            {
                subscribedViewModel.PropertyChanged += ViewModel_PropertyChanged;
                subscribedViewModel.LastFiveMeasurements.CollectionChanged += LastFiveMeasurements_CollectionChanged;
            }

            DrawChartLater();
        }

        private void UnsubscribeFromViewModel()
        {
            if (subscribedViewModel == null)
            {
                return;
            }

            subscribedViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            subscribedViewModel.LastFiveMeasurements.CollectionChanged -= LastFiveMeasurements_CollectionChanged;
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedEntity" ||
                e.PropertyName == "LastFiveMeasurements" ||
                e.PropertyName == "LatestMeasurementText" ||
                e.PropertyName == "ChartTitle")
            {
                DrawChartLater();
            }
        }

        private void LastFiveMeasurements_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            DrawChartLater();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            DrawChartLater();
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawChartLater();
        }

        private void DrawChartLater()
        {
            Dispatcher.BeginInvoke(new Action(DrawChart));
        }

        private void DrawChart()
        {
            if (ChartCanvas == null || ViewModel == null)
            {
                return;
            }

            ChartCanvas.Children.Clear();

            double width = ChartCanvas.ActualWidth;
            double height = ChartCanvas.ActualHeight;

            if (width < 100 || height < 100)
            {
                return;
            }

            MeasurementPoint[] points = ViewModel.LastFiveMeasurements.ToArray();

            if (points.Length == 0)
            {
                DrawNoDataMessage(width, height);
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
            double yStep = 1.0;

            DrawAxes(left, top, bottom, width, height);
            DrawHorizontalTicks(left, right, top, bottom, width, height, chartHeight, maxValue, yStep);
            DrawBars(points, left, bottom, height, chartWidth, chartHeight, maxValue);
            DrawAxisLabels(width, height);
        }

        private void DrawNoDataMessage(double width, double height)
        {
            TextBlock textBlock = new TextBlock
            {
                Text = "No measurements available for selected entity.",
                FontFamily = new FontFamily("Courier New"),
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)Application.Current.Resources["UIMutedTextBrush"]
            };

            Canvas.SetLeft(textBlock, width / 2 - 190);
            Canvas.SetTop(textBlock, height / 2 - 15);

            ChartCanvas.Children.Add(textBlock);
        }

        private void DrawAxes(double left, double top, double bottom, double width, double height)
        {
            Brush axisBrush = (Brush)Application.Current.Resources["UIPrimaryBrush"];

            Line yAxis = new Line
            {
                X1 = left,
                Y1 = top,
                X2 = left,
                Y2 = height - bottom,
                Stroke = axisBrush,
                StrokeThickness = 2
            };

            Line xAxis = new Line
            {
                X1 = left,
                Y1 = height - bottom,
                X2 = width - 25,
                Y2 = height - bottom,
                Stroke = axisBrush,
                StrokeThickness = 2
            };

            ChartCanvas.Children.Add(yAxis);
            ChartCanvas.Children.Add(xAxis);
        }

        private void DrawHorizontalTicks(
            double left,
            double right,
            double top,
            double bottom,
            double width,
            double height,
            double chartHeight,
            double maxValue,
            double yStep)
        {
            Brush gridBrush = (Brush)Application.Current.Resources["UISubtleBorderBrush"];
            Brush textBrush = (Brush)Application.Current.Resources["UIPrimaryBrush"];

            for (double value = 0; value <= maxValue; value += yStep)
            {
                double y = height - bottom - (value / maxValue) * chartHeight;

                Line gridLine = new Line
                {
                    X1 = left,
                    Y1 = y,
                    X2 = width - right,
                    Y2 = y,
                    Stroke = gridBrush,
                    StrokeThickness = 0.6
                };

                TextBlock label = new TextBlock
                {
                    Text = value.ToString("0", CultureInfo.InvariantCulture),
                    FontFamily = new FontFamily("Courier New"),
                    FontSize = 11,
                    Foreground = textBrush
                };

                Canvas.SetLeft(label, left - 28);
                Canvas.SetTop(label, y - 8);

                ChartCanvas.Children.Add(gridLine);
                ChartCanvas.Children.Add(label);
            }
        }

        private void DrawBars(
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

                double barHeight = (point.Value / maxValue) * chartHeight;

                if (barHeight < 2)
                {
                    barHeight = 2;
                }

                double x = left + gap + i * (barWidth + gap);
                double y = height - bottom - barHeight;

                Rectangle bar = new Rectangle
                {
                    Width = barWidth,
                    Height = barHeight,
                    Stroke = (Brush)Application.Current.Resources["UIPrimaryBrush"],
                    StrokeThickness = 1.5,
                    Fill = point.IsValid
                        ? new SolidColorBrush(Color.FromRgb(136, 136, 136))
                        : new SolidColorBrush(Color.FromRgb(34, 34, 34))
                };

                Canvas.SetLeft(bar, x);
                Canvas.SetTop(bar, y);

                ChartCanvas.Children.Add(bar);

                DrawValueLabel(point, x, y, barWidth);
                DrawTimeLabel(point, x, height - bottom + 10, barWidth);
            }
        }

        private void DrawValueLabel(MeasurementPoint point, double x, double y, double barWidth)
        {
            TextBlock valueText = new TextBlock
            {
                Text = point.Value.ToString("0.0", CultureInfo.InvariantCulture) + (point.IsValid ? "" : " !"),
                FontFamily = new FontFamily("Courier New"),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)Application.Current.Resources["UIPrimaryBrush"]
            };

            Canvas.SetLeft(valueText, x + barWidth / 2 - 18);
            Canvas.SetTop(valueText, y - 22);

            ChartCanvas.Children.Add(valueText);
        }

        private void DrawTimeLabel(MeasurementPoint point, double x, double y, double barWidth)
        {
            TextBlock timeText = new TextBlock
            {
                Text = point.FormattedTimestamp,
                FontFamily = new FontFamily("Courier New"),
                FontSize = 11,
                Foreground = (Brush)Application.Current.Resources["UIPrimaryBrush"]
            };

            Canvas.SetLeft(timeText, x + barWidth / 2 - 25);
            Canvas.SetTop(timeText, y);

            ChartCanvas.Children.Add(timeText);
        }

        private void DrawAxisLabels(double width, double height)
        {
            TextBlock xLabel = new TextBlock
            {
                Text = "X: time moments",
                FontFamily = new FontFamily("Courier New"),
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["UIMutedTextBrush"]
            };

            Canvas.SetLeft(xLabel, width / 2 - 55);
            Canvas.SetTop(xLabel, height - 30);

            TextBlock yLabel = new TextBlock
            {
                Text = "Y: MW",
                FontFamily = new FontFamily("Courier New"),
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["UIMutedTextBrush"]
            };

            Canvas.SetLeft(yLabel, 12);
            Canvas.SetTop(yLabel, 12);

            ChartCanvas.Children.Add(xLabel);
            ChartCanvas.Children.Add(yLabel);
        }
    }
}