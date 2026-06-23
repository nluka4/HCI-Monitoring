namespace NetworkService.Model
{
    public class EntityDropRequest
    {
        public DER Entity { get; set; }

        public int TargetSlotIndex { get; set; }
    }
}