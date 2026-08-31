namespace Heros
{
    public interface IHeroInfoRepository
    {
        HeroData GetHeroData(int uid);
        bool TryGetHeroSaveData(int uid, out HeroSaveData data);
    }
}
