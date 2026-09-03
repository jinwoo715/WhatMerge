using System.Collections.Generic;
using UnityEngine;

namespace WhatMerge.Heros
{
    public class NomalMergeRepository
    {
        private readonly Dictionary<(int, int), int> _mergeData = new();

        public void Init(List<MergeData> mergeDatas)
        {
            for (int i = 0; i < mergeDatas.Count; i++)
            {
                MergeData data = mergeDatas[i];
                var key = SortUID(data.First, data.Second);

                Debug.Log($"{key}");
                _mergeData.Add(key, data.Result);
            }
        }

        public int GetMergeResult(int first, int second)
        {
            var key = SortUID(first, second);

            if (_mergeData.TryGetValue(key, out int value))
            {
                return value;
            }

            return 0;
        }

        public bool IsCanMerge(int first, int second)
        {
            var key = SortUID(first, second);

            return _mergeData.ContainsKey(key);
        }

        public (int, int) SortUID(int first, int second)
        {
            int min = Mathf.Min(first, second);
            int max = Mathf.Max(first, second);

            return (min, max);
        }
    }
}
