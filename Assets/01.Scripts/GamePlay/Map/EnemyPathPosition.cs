using System;
using UnityEngine;

namespace WhatMerge.Map
{
    public readonly struct EnemyPathPosition
    {
        public int SegmentStartIndex { get; }
        public float DistanceFromSegmentStart { get; }

        public EnemyPathPosition(int segmentStartIndex, float distanceFromSegmentStart)
        {
            SegmentStartIndex = segmentStartIndex;
            DistanceFromSegmentStart = distanceFromSegmentStart;
        }

        public static EnemyPathPosition Start => new EnemyPathPosition(0, 0f);
    }

    public static class EnemyPathPositionUtility
    {
        private const float MinimumSegmentLength = 0.0001f;

        public static EnemyPathPosition Offset(
            IPathProvider provider,
            EnemyPathPosition origin,
            float signedDistance)
        {
            if (float.IsNaN(signedDistance) || float.IsInfinity(signedDistance))
                throw new ArgumentOutOfRangeException(nameof(signedDistance), signedDistance, "Path offset must be finite.");

            float originDistance = GetCumulativeDistance(provider, origin);
            return FromCumulativeDistance(provider, originDistance + signedDistance);
        }

        public static EnemyPathPosition Normalize(
            IPathProvider provider,
            EnemyPathPosition position)
        {
            return FromCumulativeDistance(provider, GetCumulativeDistance(provider, position));
        }

        public static Vector3 GetWorldPosition(
            IPathProvider provider,
            EnemyPathPosition position)
        {
            EnemyPathPosition normalized = Normalize(provider, position);
            int nextIndex = provider.GetNextIndex(normalized.SegmentStartIndex);
            Vector3 start = provider.GetDestination(normalized.SegmentStartIndex);
            Vector3 end = provider.GetDestination(nextIndex);
            float segmentLength = GetSegmentLength(start, end);
            float progress = normalized.DistanceFromSegmentStart / segmentLength;
            return Vector3.Lerp(start, end, progress);
        }

        private static float GetCumulativeDistance(
            IPathProvider provider,
            EnemyPathPosition position)
        {
            ValidateProvider(provider);
            ValidateIndex(provider, position.SegmentStartIndex);
            if (float.IsNaN(position.DistanceFromSegmentStart)
                || float.IsInfinity(position.DistanceFromSegmentStart))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(position),
                    position.DistanceFromSegmentStart,
                    "Path distance must be finite.");
            }

            float cumulativeDistance = 0f;
            int currentIndex = 0;

            for (int i = 0; i < provider.DestinationCount; i++)
            {
                int nextIndex = provider.GetNextIndex(currentIndex);
                ValidateIndex(provider, nextIndex);

                Vector3 start = provider.GetDestination(currentIndex);
                Vector3 end = provider.GetDestination(nextIndex);
                float segmentLength = GetSegmentLength(start, end);

                if (currentIndex == position.SegmentStartIndex)
                {
                    float distanceOnSegment = Mathf.Clamp(
                        position.DistanceFromSegmentStart,
                        0f,
                        segmentLength);
                    return cumulativeDistance + distanceOnSegment;
                }

                cumulativeDistance += segmentLength;
                currentIndex = nextIndex;
            }

            throw new InvalidOperationException(
                $"Path segment {position.SegmentStartIndex} is not reachable from destination zero.");
        }

        private static EnemyPathPosition FromCumulativeDistance(
            IPathProvider provider,
            float cumulativeDistance)
        {
            ValidateProvider(provider);
            float totalLength = GetTotalLength(provider);
            float remainingDistance = Mathf.Repeat(cumulativeDistance, totalLength);
            int currentIndex = 0;

            for (int i = 0; i < provider.DestinationCount; i++)
            {
                int nextIndex = provider.GetNextIndex(currentIndex);
                ValidateIndex(provider, nextIndex);

                float segmentLength = GetSegmentLength(
                    provider.GetDestination(currentIndex),
                    provider.GetDestination(nextIndex));

                if (remainingDistance < segmentLength)
                    return new EnemyPathPosition(currentIndex, remainingDistance);

                remainingDistance -= segmentLength;
                currentIndex = nextIndex;
            }

            return EnemyPathPosition.Start;
        }

        private static float GetTotalLength(IPathProvider provider)
        {
            float totalLength = 0f;
            int currentIndex = 0;

            for (int i = 0; i < provider.DestinationCount; i++)
            {
                int nextIndex = provider.GetNextIndex(currentIndex);
                ValidateIndex(provider, nextIndex);
                totalLength += GetSegmentLength(
                    provider.GetDestination(currentIndex),
                    provider.GetDestination(nextIndex));
                currentIndex = nextIndex;
            }

            if (currentIndex != 0)
                throw new InvalidOperationException("Enemy path must return to destination zero.");

            return totalLength;
        }

        private static float GetSegmentLength(Vector3 start, Vector3 end)
        {
            float length = Vector3.Distance(start, end);
            if (length < MinimumSegmentLength)
                throw new InvalidOperationException("Enemy path cannot contain a zero-length segment.");

            return length;
        }

        private static void ValidateProvider(IPathProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            if (provider.DestinationCount < 2)
                throw new InvalidOperationException("Enemy path requires at least two destinations.");
        }

        private static void ValidateIndex(IPathProvider provider, int index)
        {
            if (index < 0 || index >= provider.DestinationCount)
                throw new InvalidOperationException($"Enemy path returned invalid destination index {index}.");
        }
    }
}
