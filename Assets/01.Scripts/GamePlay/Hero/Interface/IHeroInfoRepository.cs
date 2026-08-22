namespace Heros
{
    public interface IHeroInfoRepository
    {
        HeroData GetHeroData(int uid);
        HeroSaveData GetHeroSaveData(int uid);
    }
}
