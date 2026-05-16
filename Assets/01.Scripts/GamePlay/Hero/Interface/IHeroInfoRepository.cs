namespace Heros
{
    public interface IHeroInfoRepository
    {
        HeroData GetHeroData(int uid);
        ATKData GetATKData(int heroUid);
        HeroSaveData GetHeroSaveData(int uid);
    }
}