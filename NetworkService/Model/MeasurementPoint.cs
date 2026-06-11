using System;
using NetworkService.ViewModel;

namespace NetworkService.Model
{
    public class MeasurementPoint : BindableBase
    {
        private DateTime timestamp;
        private double value;
        private bool isValid;

        public DateTime Timestamp
        {
            get
            {
                return timestamp;
            }
            set
            {
                SetProperty(ref timestamp, value);
                OnPropertyChanged("FormattedTimestamp");
            }
        }

        public double Value
        {
            get
            {
                return value;
            }
            set
            {
                SetProperty(ref this.value, value);
                IsValid = value >= DER.MinValidMeasurement && value <= DER.MaxValidMeasurement;
                OnPropertyChanged("FormattedValue");
            }
        }

        public bool IsValid
        {
            get
            {
                return isValid;
            }
            set
            {
                SetProperty(ref isValid, value);
            }
        }

        public string FormattedTimestamp
        {
            get
            {
                return Timestamp.ToString("HH:mm:ss");
            }
        }

        public string FormattedValue
        {
            get
            {
                return Value.ToString("0.0") + " MW";
            }
        }
    }
}