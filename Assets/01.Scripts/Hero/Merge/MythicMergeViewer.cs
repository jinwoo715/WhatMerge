using System;
using UnityEngine;

namespace WhatMerge.Heros
{
    public class MythicMergeViewer : MonoBehaviour
    {
        [SerializeField] private MythicMergeButtonSlot[] _slots;

        public event Action<MythicMergeCandidate> OnMergeRequested;

        private void Awake()
        {
            if (_slots == null || _slots.Length != MythicMergeController.MaxVisibleCandidateCount)
            {
                throw new InvalidOperationException(
                    $"MythicMergeViewer requires exactly {MythicMergeController.MaxVisibleCandidateCount} button slots.");
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                MythicMergeButtonSlot slot = _slots[i]
                    ?? throw new InvalidOperationException($"Mythic merge button slot {i} is not assigned.");
                slot.OnClick += HandleSlotClick;
                slot.Clear();
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _slots.Length; i++)
                _slots[i].Clear();
        }

        public void SetCandidate(
            int index,
            MythicMergeCandidate candidate,
            Sprite heroSprite)
        {
            if (index < 0 || index >= _slots.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            _slots[index].SetCandidate(candidate, heroSprite);
        }

        private void HandleSlotClick(MythicMergeCandidate candidate)
        {
            OnMergeRequested?.Invoke(candidate);
        }
    }
}
