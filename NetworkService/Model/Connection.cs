using NetworkService.ViewModel;

namespace NetworkService.Model
{
    public class Connection : BindableBase
    {
        private int firstEntityId;
        private int secondEntityId;

        public Connection()
        {
        }

        public Connection(int firstEntityId, int secondEntityId)
        {
            FirstEntityId = firstEntityId;
            SecondEntityId = secondEntityId;
        }

        public int FirstEntityId
        {
            get { return firstEntityId; }
            set { SetProperty(ref firstEntityId, value); }
        }

        public int SecondEntityId
        {
            get { return secondEntityId; }
            set { SetProperty(ref secondEntityId, value); }
        }

        public bool MatchesEntities(int firstId, int secondId)
        {
            return (FirstEntityId == firstId && SecondEntityId == secondId)
                   || (FirstEntityId == secondId && SecondEntityId == firstId);
        }

        public bool ContainsEntity(int entityId)
        {
            return FirstEntityId == entityId || SecondEntityId == entityId;
        }
    }
}