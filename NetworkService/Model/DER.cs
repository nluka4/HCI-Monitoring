using System.Collections.Generic;
using System.Linq;
using NetworkService.Model.Validation;

namespace NetworkService.Model
{
    public class DER : ValidationBase
    {
        public const double MinValidMeasurement = 1.0;
        public const double MaxValidMeasurement = 5.0;

        private int id;
        private string name;
        private DEREntityType entityType;
        private double lastMeasurement;
        private IEnumerable<DER> existingEntities;

        public DER()
        {
        }

        public DER(int id, string name, DEREntityType entityType, double lastMeasurement)
        {
            Id = id;
            Name = name;
            EntityType = entityType;
            LastMeasurement = lastMeasurement;
        }

        public int Id
        {
            get
            {
                return id;
            }
            set
            {
                SetProperty(ref id, value);
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                SetProperty(ref name, value);
            }
        }

        public DEREntityType EntityType
        {
            get
            {
                return entityType;
            }
            set
            {
                SetProperty(ref entityType, value);
                OnPropertyChanged("TypeName");
                OnPropertyChanged("ImagePath");
            }
        }

        public double LastMeasurement
        {
            get
            {
                return lastMeasurement;
            }
            set
            {
                SetProperty(ref lastMeasurement, value);
                OnPropertyChanged("IsMeasurementValid");
                OnPropertyChanged("StatusText");
                OnPropertyChanged("FormattedMeasurement");
            }
        }

        public string TypeName
        {
            get
            {
                return EntityType == null ? string.Empty : EntityType.TypeName;
            }
        }

        public string ImagePath
        {
            get
            {
                return EntityType == null ? string.Empty : EntityType.ImagePath;
            }
        }

        public bool IsMeasurementValid
        {
            get
            {
                return LastMeasurement >= MinValidMeasurement && LastMeasurement <= MaxValidMeasurement;
            }
        }

        public string StatusText
        {
            get
            {
                return IsMeasurementValid ? "VALID" : "INVALID";
            }
        }

        public string FormattedMeasurement
        {
            get
            {
                return LastMeasurement.ToString("0.0") + " MW";
            }
        }

        public void SetExistingEntities(IEnumerable<DER> entities)
        {
            existingEntities = entities;
        }

        protected override void ValidateSelf()
        {
            if (Id <= 0)
            {
                ValidationErrors["Id"] = "ID must be a positive integer.";
            }

            if (existingEntities != null && existingEntities.Any(entity => entity != this && entity.Id == Id))
            {
                ValidationErrors["Id"] = "ID must be unique.";
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                ValidationErrors["Name"] = "Name is required.";
            }

            if (EntityType == null)
            {
                ValidationErrors["EntityType"] = "Entity type is required.";
            }
        }

        public DER Clone()
        {
            return new DER(Id, Name, EntityType, LastMeasurement);
        }
    }
}