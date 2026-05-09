namespace Heros
{
    public class HeroBagSlotData
    {
        public string Name { get; private set; }
        public int UID { get; private set; }
        public int Evolution { get; private set; }

        public bool IsUseable => UID == 0;

        public void Init(int uid, int evolution, string name)
        {
            UID = uid;
            Evolution = evolution;
            Name = name;
        }

        public void Clear()
        {
            UID = 0;
            Evolution = 0;
        }
    }
}
