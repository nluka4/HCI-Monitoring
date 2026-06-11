using NetworkService.ViewModel;

namespace NetworkService.Model.Validation
{
    public abstract class ValidationBase : BindableBase
    {
        private bool isValid;

        protected ValidationBase()
        {
            ValidationErrors = new ValidationErrors();
            isValid = true;
        }

        public ValidationErrors ValidationErrors { get; private set; }

        public bool IsValid
        {
            get
            {
                return isValid;
            }
            protected set
            {
                SetProperty(ref isValid, value);
            }
        }

        public void Validate()
        {
            ValidationErrors.Clear();
            ValidateSelf();

            IsValid = ValidationErrors.IsValid;

            OnPropertyChanged("ValidationErrors");
            OnPropertyChanged("IsValid");
        }

        protected abstract void ValidateSelf();
    }
}