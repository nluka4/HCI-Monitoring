using NetworkService.ViewModel;

namespace NetworkService.Model
{
    public class DEREntityType : BindableBase
    {
        private string typeName;
        private string imagePath;

        public string TypeName
        {
            get
            {
                return typeName;
            }
            set
            {
                SetProperty(ref typeName, value);
            }
        }

        public string ImagePath
        {
            get
            {
                return imagePath;
            }
            set
            {
                SetProperty(ref imagePath, value);
            }
        }

        public override string ToString()
        {
            return TypeName;
        }
    }
}