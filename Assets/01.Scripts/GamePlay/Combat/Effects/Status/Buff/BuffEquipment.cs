using Skill.Data;
using System;
using WhatMerge.Combat;
using WhatMerge.Heros;

namespace WhatMerge.Combat.Effects
{
public sealed class BuffEquipment : IRuntimeEffectHandle
{
    private readonly IHeroStatModifier _statModifier;
    private readonly BuffData _buffData;
    private Action<BuffEquipment> _onDisposed;

    public bool IsDisposed { get; private set; }

    public BuffEquipment(
        IHeroStatModifier statModifier,
        BuffData buffData,
        Action<BuffEquipment> onDisposed)
    {
        _statModifier = statModifier ?? throw new ArgumentNullException(nameof(statModifier));
        _buffData = buffData ?? throw new ArgumentNullException(nameof(buffData));
        _onDisposed = onDisposed;

        _statModifier.AddMultiplier(_buffData.BuffType, _buffData.IncreaseRatio);
    }

    public void Dispose()
    {
        if (IsDisposed)
            return;

        IsDisposed = true;
        _statModifier.AddMultiplier(_buffData.BuffType, -_buffData.IncreaseRatio);

        Action<BuffEquipment> onDisposed = _onDisposed;
        _onDisposed = null;
        onDisposed?.Invoke(this);
    }
}
}
