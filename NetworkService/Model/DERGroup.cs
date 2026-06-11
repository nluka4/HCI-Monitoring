using System.Collections.ObjectModel;
using NetworkService.ViewModel;

namespace NetworkService.Model
{
    public class DERGroup : BindableBase
    {
        private string groupName;

        public DERGroup(string groupName)
        {
            GroupName = groupName;
            Entities = new ObservableCollection<DER>();
        }

        public string GroupName
        {
            get { return groupName; }
            set { SetProperty(ref groupName, value); }
        }

        public ObservableCollection<DER> Entities { get; private set; }
    }
}