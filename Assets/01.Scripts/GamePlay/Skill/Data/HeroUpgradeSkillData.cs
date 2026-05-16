using System.Collections.Generic;

[System.Serializable]
public class HeroUpgradeSkillData
{
    public string HeroUID;

    public string Lv1;
    public string Lv10;
    public string Lv20;
    public string Lv30;
    public string Lv40;
    public string Lv50;
    public string Lv60;
    public string Lv70;
    public string Lv80;
    public string Lv90;
    public string Lv100;
    public string Lv110;
    public string Lv120;
    public string Lv130;
    public string Lv140;
    public string Lv150;

    public List<string> GetSkills(int level)
    {
        List<string> result = new();

        AddIfUnlocked(result, level, 1, Lv1);
        AddIfUnlocked(result, level, 10, Lv10);
        AddIfUnlocked(result, level, 20, Lv20);
        AddIfUnlocked(result, level, 30, Lv30);
        AddIfUnlocked(result, level, 40, Lv40);
        AddIfUnlocked(result, level, 50, Lv50);
        AddIfUnlocked(result, level, 60, Lv60);
        AddIfUnlocked(result, level, 70, Lv70);
        AddIfUnlocked(result, level, 80, Lv80);
        AddIfUnlocked(result, level, 90, Lv90);
        AddIfUnlocked(result, level, 100, Lv100);
        AddIfUnlocked(result, level, 110, Lv110);
        AddIfUnlocked(result, level, 120, Lv120);
        AddIfUnlocked(result, level, 130, Lv130);
        AddIfUnlocked(result, level, 140, Lv140);
        AddIfUnlocked(result, level, 150, Lv150);

        return result;
    }

    private void AddIfUnlocked(List<string> result, int currentLevel, int unlockLevel, string upgradeUID)
    {
        if (currentLevel >= unlockLevel && !string.IsNullOrEmpty(upgradeUID))
        {
            result.Add(upgradeUID);
        }
    }

}
