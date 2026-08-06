using System;
using UnityEngine;

public static class StatCalculator
{
    public static int BaseATK(int evolution, ATKData atkData)
    {
        float[] multiple = { atkData.FirstMuliplier, atkData.SecondMultiplier};

        float finalBaseATK = atkData.BaseATK;

        for (int i = 0; i < evolution; i++)
        {
            finalBaseATK *= multiple[i];
        }

        return RoundInt(finalBaseATK);
    }
    public static int ATK(int level, int baseValue, float growthRate, float tierJumpValue)
    {
        float finalATK = baseValue;

        float growthPivot = baseValue;

        for (int i = 2; i <= level; i++)
        {
            finalATK += growthPivot * growthRate;

            if (i % 10 == 0)
            {
                finalATK = finalATK * tierJumpValue;
                growthPivot = finalATK;
            }
        }

        return RoundInt(finalATK);
    }

    public static float GetDamageReductionRate(float amour, float percentPenetration, float flatPenetration)
    {
        // 1. % 방관 먼저 적용 (적 방어력의 30%를 날려버림)
        float armorAfterPercent = amour * (1f - percentPenetration);

        // 2. 그 다음 고정 방관 적용 (남은 방어력에서 15를 뺌)
        // 방어력이 0 이하로 내려가진 않게 처리
        float effectiveArmor = Mathf.Max(0, armorAfterPercent - flatPenetration);

        // 3. 롤(LoL)식 데미지 감소율 반환 (100 / 100 + 방어력)
        // effectiveArmor가 100이면 0.5 반환 (50% 데미지 감소)
        return effectiveArmor / (100f + effectiveArmor);
    }

    public static float AS(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Attack speed must be greater than zero.");

        return 1f / value;
    }
    public static int RoundInt(float value)
    {
        return (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }
}
