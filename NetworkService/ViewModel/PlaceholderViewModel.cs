namespace NetworkService.ViewModel
{
    public class PlaceholderViewModel : BindableBase
    {
        private string title;
        private string description;

        public PlaceholderViewModel(string title, string description)
        {
            Title = title;
            Description = description;
        }

        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        public string Description
        {
            get { return description; }
            set { SetProperty(ref description, value); }
        }
    }
}