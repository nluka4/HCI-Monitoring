using NetworkService.Model;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Collections.Generic;

namespace NetworkService.ViewModel
{
    public class GraphViewModel : BindableBase
    {
        private readonly ObservableCollection<DER> allEntities;
        private readonly DispatcherTimer refreshTimer;

        private DER selectedEntity;
        private string latestMeasurementText;
        private string chartTitle;

        public GraphViewModel(ObservableCollection<DER> allEntities)
        {
            this.allEntities = allEntities;

            EntityOptions = allEntities;
            LastFiveMeasurements = new ObservableCollection<MeasurementPoint>();

            RefreshCommand = new MyICommand(RefreshChartData);

            if (EntityOptions.Count > 0)
            {
                SelectedEntity = EntityOptions[0];
            }

            this.allEntities.CollectionChanged += AllEntitiesCollectionChanged;

            refreshTimer = new DispatcherTimer();
            refreshTimer.Interval = TimeSpan.FromSeconds(2);
            refreshTimer.Tick += RefreshTimer_Tick;
            refreshTimer.Start();

            RefreshChartData();
        }

        public ObservableCollection<DER> EntityOptions { get; private set; }

        public ObservableCollection<MeasurementPoint> LastFiveMeasurements { get; private set; }

        public MyICommand RefreshCommand { get; private set; }

        public DER SelectedEntity
        {
            get { return selectedEntity; }
            set
            {
                if (SetProperty(ref selectedEntity, value))
                {
                    RefreshChartData();
                    OnPropertyChanged("SelectedEntityName");
                }
            }
        }

        public string LatestMeasurementText
        {
            get { return latestMeasurementText; }
            set { SetProperty(ref latestMeasurementText, value); }
        }

        public string ChartTitle
        {
            get { return chartTitle; }
            set { SetProperty(ref chartTitle, value); }
        }

        public string SelectedEntityName
        {
            get
            {
                if (SelectedEntity == null)
                {
                    return "No selected entity";
                }

                return SelectedEntity.Name;
            }
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            RefreshChartData();
        }

        private void AllEntitiesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (SelectedEntity == null && EntityOptions.Count > 0)
            {
                SelectedEntity = EntityOptions[0];
                return;
            }

            if (SelectedEntity != null && !EntityOptions.Contains(SelectedEntity))
            {
                SelectedEntity = EntityOptions.Count > 0 ? EntityOptions[0] : null;
                return;
            }

            RefreshChartData();
        }

        public void RefreshChartData()
        {
            LastFiveMeasurements.Clear();

            if (SelectedEntity == null)
            {
                LatestMeasurementText = "-";
                ChartTitle = "No entity selected";
                return;
            }

            MeasurementPoint[] points = ReadLastFivePointsForSelectedEntity();

            if (points.Length < 5)
            {
                points = BuildFivePointPreview(points);
            }

            foreach (MeasurementPoint point in points)
            {
                LastFiveMeasurements.Add(point);
            }

            MeasurementPoint latest = LastFiveMeasurements.LastOrDefault();

            LatestMeasurementText = latest == null
                ? "-"
                : latest.Value.ToString("0.0", CultureInfo.InvariantCulture) + " MW";

            ChartTitle = "G2 Bar Chart · " + SelectedEntity.Name + " (#" + SelectedEntity.Id + ")";

            OnPropertyChanged("LastFiveMeasurements");
            OnPropertyChanged("LatestMeasurementText");
            OnPropertyChanged("ChartTitle");
        }

        private MeasurementPoint[] BuildFivePointPreview(MeasurementPoint[] existingPoints)
        {
            List<MeasurementPoint> result = new List<MeasurementPoint>();

            double currentValue = SelectedEntity.LastMeasurement;

            double[] previewValues =
            {
        currentValue - 0.6,
        currentValue - 0.3,
        currentValue - 0.1,
        currentValue + 0.1,
        currentValue
    };

            DateTime startTime = DateTime.Now.AddSeconds(-40);

            for (int i = 0; i < 5; i++)
            {
                result.Add(new MeasurementPoint
                {
                    Timestamp = startTime.AddSeconds(i * 10),
                    Value = NormalizePreviewValue(previewValues[i])
                });
            }

            if (existingPoints != null && existingPoints.Length > 0)
            {
                int startIndex = 5 - existingPoints.Length;

                for (int i = 0; i < existingPoints.Length && startIndex + i < 5; i++)
                {
                    result[startIndex + i] = existingPoints[i];
                }
            }

            return result.ToArray();
        }

        private double NormalizePreviewValue(double value)
        {
            if (value < 0)
            {
                return 0;
            }

            return Math.Round(value, 1);
        }

        private MeasurementPoint[] ReadLastFivePointsForSelectedEntity()
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");

            if (!File.Exists(logPath))
            {
                return new MeasurementPoint[0];
            }

            string[] lines;

            try
            {
                lines = File.ReadAllLines(logPath);
            }
            catch
            {
                return new MeasurementPoint[0];
            }

            return lines
                .Select(ParseLogLine)
                .Where(point => point != null)
                .Where(point => IsPointForSelectedEntity(point))
                .Reverse()
                .Take(5)
                .Reverse()
                .ToArray();
        }

        private MeasurementPoint ParseLogLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return null;
            }

            Match match = Regex.Match(
                line,
                @"^\[(?<time>\d{2}:\d{2}:\d{2})\]\sEntity\s'(?<name>[^']*)'\s\(ID=(?<id>\d+)\):\s(?<value>-?\d+([\.,]\d+)?)\sMW");

            if (!match.Success)
            {
                return null;
            }

            int id;

            if (!int.TryParse(match.Groups["id"].Value, out id))
            {
                return null;
            }

            if (SelectedEntity == null || id != SelectedEntity.Id)
            {
                return null;
            }

            string valueText = match.Groups["value"].Value.Replace(',', '.');

            double value;

            if (!double.TryParse(valueText, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                return null;
            }

            DateTime time;

            if (!DateTime.TryParseExact(
                    match.Groups["time"].Value,
                    "HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out time))
            {
                time = DateTime.Now;
            }

            DateTime timestamp = DateTime.Today
                .AddHours(time.Hour)
                .AddMinutes(time.Minute)
                .AddSeconds(time.Second);

            return new MeasurementPoint
            {
                Timestamp = timestamp,
                Value = value
            };
        }

        private bool IsPointForSelectedEntity(MeasurementPoint point)
        {
            return point != null && SelectedEntity != null;
        }
    }
}