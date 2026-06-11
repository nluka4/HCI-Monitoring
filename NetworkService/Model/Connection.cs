using NetworkService.ViewModel;

namespace NetworkService.Model
{
    public class Connection : BindableBase
    {
        private int firstSlotIndex;
        private int secondSlotIndex;

        public Connection()
        {
        }

        public Connection(int firstSlotIndex, int secondSlotIndex)
        {
            FirstSlotIndex = firstSlotIndex;
            SecondSlotIndex = secondSlotIndex;
        }

        public int FirstSlotIndex
        {
            get
            {
                return firstSlotIndex;
            }
            set
            {
                SetProperty(ref firstSlotIndex, value);
            }
        }

        public int SecondSlotIndex
        {
            get
            {
                return secondSlotIndex;
            }
            set
            {
                SetProperty(ref secondSlotIndex, value);
            }
        }

        public bool Matches(int first, int second)
        {
            return (FirstSlotIndex == first && SecondSlotIndex == second)
                   || (FirstSlotIndex == second && SecondSlotIndex == first);
        }
    }
}