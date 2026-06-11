using System.Collections.Generic;
using NetworkService.ViewModel;

namespace NetworkService.Model.Validation
{
    public class ValidationErrors : BindableBase
    {
        private readonly Dictionary<string, string> validationErrors;

        public ValidationErrors()
        {
            validationErrors = new Dictionary<string, string>();
        }

        public bool IsValid
        {
            get
            {
                return validationErrors.Count < 1;
            }
        }

        public string this[string fieldName]
        {
            get
            {
                if (validationErrors.ContainsKey(fieldName))
                {
                    return validationErrors[fieldName];
                }

                return string.Empty;
            }
            set
            {
                if (validationErrors.ContainsKey(fieldName))
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        validationErrors.Remove(fieldName);
                    }
                    else
                    {
                        validationErrors[fieldName] = value;
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        validationErrors.Add(fieldName, value);
                    }
                }

                OnPropertyChanged("IsValid");
                OnPropertyChanged("Item[]");
            }
        }

        public void Clear()
        {
            validationErrors.Clear();
            OnPropertyChanged("IsValid");
            OnPropertyChanged("Item[]");
        }
    }
}