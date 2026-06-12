using NetworkService.ViewModel;

namespace NetworkService.Model
{
    public class CanvasSlot : BindableBase
    {
        private int index;
        private DER entity;

        public CanvasSlot(int index)
        {
            Index = index;
        }

        public int Index
        {
            get { return index; }
            set
            {
                SetProperty(ref index, value);
                OnPropertyChanged("DisplayName");
            }
        }

        public DER Entity
        {
            get { return entity; }
            set
            {
                if (SetProperty(ref entity, value))
                {
                    OnPropertyChanged("IsEmpty");
                    OnPropertyChanged("HasInvalidEntity");
                    OnPropertyChanged("DisplayName");
                }
            }
        }

        public bool IsEmpty
        {
            get { return Entity == null; }
        }

        public bool HasInvalidEntity
        {
            get
            {
                return Entity != null && !Entity.IsMeasurementValid;
            }
        }

        public string DisplayName
        {
            get { return "slot " + (Index + 1); }
        }
    }
}