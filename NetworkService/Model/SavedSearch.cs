using NetworkService.ViewModel;

namespace NetworkService.Model
{
    public class SavedSearch : BindableBase
    {
        private string label;
        private string searchText;
        private bool searchByName;
        private bool searchByType;
        private DEREntityType filterType;
        private string filterOperator;
        private int? filterIdValue;

        public string Label
        {
            get
            {
                return label;
            }
            set
            {
                SetProperty(ref label, value);
            }
        }

        public string SearchText
        {
            get
            {
                return searchText;
            }
            set
            {
                SetProperty(ref searchText, value);
            }
        }

        public bool SearchByName
        {
            get
            {
                return searchByName;
            }
            set
            {
                SetProperty(ref searchByName, value);
            }
        }

        public bool SearchByType
        {
            get
            {
                return searchByType;
            }
            set
            {
                SetProperty(ref searchByType, value);
            }
        }

        public DEREntityType FilterType
        {
            get
            {
                return filterType;
            }
            set
            {
                SetProperty(ref filterType, value);
            }
        }

        public string FilterOperator
        {
            get
            {
                return filterOperator;
            }
            set
            {
                SetProperty(ref filterOperator, value);
            }
        }

        public int? FilterIdValue
        {
            get
            {
                return filterIdValue;
            }
            set
            {
                SetProperty(ref filterIdValue, value);
            }
        }

        public override string ToString()
        {
            return Label;
        }
    }
}