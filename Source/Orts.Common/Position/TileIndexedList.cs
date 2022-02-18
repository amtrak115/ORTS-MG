﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Orts.Common.Position
{
    public class TileIndexedList<TTileCoordinate, T> : IEnumerable<ITileCoordinate<T>> where T : struct, ITile
    {
        private readonly SortedList<ITile, List<ITileCoordinate<T>>> tiles;
        private readonly List<ITile> sortedIndexes;

        public int Count => sortedIndexes.Count;

        public int ItemCount { get; }

        public IList<ITileCoordinate<T>> this[int index] { get => tiles[sortedIndexes[index]]; set => throw new InvalidOperationException(); }

        public TileIndexedList(IEnumerable<ITileCoordinate<T>> data)
        {
            if (data is IEnumerable<ITileCoordinateVector<T>> vectorData)
            {
                IEnumerable<ITileCoordinateVector<T>> singleTile = vectorData.Where(d => d.Tile.Equals(d.OtherTile));
                IEnumerable<ITileCoordinateVector<T>> multiTile = vectorData.Where(d => !d.Tile.Equals(d.OtherTile));

                tiles = new SortedList<ITile, List<ITileCoordinate<T>>>(
                    singleTile.Select(d => new { Segment = d, Tile = d.Tile as ITile }).
                    Concat(multiTile.Select(d => new { Segment = d, Tile = d.Tile as ITile })).
                    Concat(multiTile.Select(d => new { Segment = d, Tile = d.OtherTile as ITile })).GroupBy(d => d.Tile).
                    ToDictionary(g => g.Key, g => g.Select(f => f.Segment).Cast<ITileCoordinate<T>>().ToList()));
            }
            else
            {
                tiles = new SortedList<ITile, List<ITileCoordinate<T>>>(data.GroupBy(d => d.Tile as ITile).ToDictionary(g => g.Key, g => g.ToList()));
            }
            sortedIndexes = tiles.Keys.ToList();

            if (sortedIndexes.Count > 0 && (Tile.Zero == sortedIndexes[0] || Tile.Zero == sortedIndexes[^1]))
            {
                sortedIndexes.Remove(Tile.Zero);
                tiles.Remove(Tile.Zero);
            }
            ItemCount = data.Count();
        }

        public IEnumerator<ITileCoordinate<T>> GetEnumerator()
        {
            foreach (List<ITileCoordinate<T>> list in tiles.Values)
            {
                foreach (ITileCoordinate<T> item in list)
                    yield return item;
            }
        }

#pragma warning disable CA1043 // Use Integral Or String Argument For Indexers
        public IEnumerable<ITileCoordinate<T>> this[ITile tile]
#pragma warning restore CA1043 // Use Integral Or String Argument For Indexers
        {
            get
            {
                if (!tiles.ContainsKey(tile))
                    yield break;
                foreach (ITileCoordinate<T> item in tiles[tile])
                    yield return item;
            }
        }

        public IEnumerable<ITileCoordinate<T>> BoundingBox(ITile bottomLeft, ITile topRight)
        {
            if (bottomLeft == null)
                throw new ArgumentNullException(nameof(bottomLeft));
            if (topRight == null)
                throw new ArgumentNullException(nameof(topRight));
            if (bottomLeft.CompareTo(topRight) > 0)
                throw new ArgumentOutOfRangeException(nameof(bottomLeft), $"{nameof(bottomLeft)} can not be larger than {nameof(topRight)}");

            if (sortedIndexes.Count == 0)
                yield break;

            int tileLookupIndex = FindNearestIndexFloor(bottomLeft);
            ITile end = sortedIndexes[FindNearestIndexCeiling(topRight)];

            ITile key = sortedIndexes[tileLookupIndex];

            while (key.Z < bottomLeft.Z && key.CompareTo(end) < 0)
            {
                tileLookupIndex = FindNearestIndexFloor(new Tile(key.X, bottomLeft.Z));
                key = sortedIndexes[tileLookupIndex];

                if (key.CompareTo(end) > 0)
                    yield break;
            }

            while (key.CompareTo(end) <=0)
            {
                foreach (ITileCoordinate<T> item in tiles[key])
                    yield return item;

                tileLookupIndex++;
                if (tileLookupIndex >= sortedIndexes.Count)
                    yield break;
                key = sortedIndexes[tileLookupIndex];

                while (key.Z < bottomLeft.Z && key.CompareTo(end) < 0)
                {
                    tileLookupIndex = FindNearestIndexFloor(new Tile(key.X, bottomLeft.Z));
                    key = sortedIndexes[tileLookupIndex];

                    if (key.CompareTo(end) > 0)
                        yield break;
                }
            }
        }

        public IEnumerable<ITileCoordinate<T>> FindNearest(PointD position)
        {
            Tile current = new Tile(Tile.TileFromAbs(position.X), Tile.TileFromAbs(position.Y));
            ITile key = sortedIndexes[FindNearestIndexCeiling(current)];
            double minDistance = double.MaxValue;
            if (current != key)
            {
                int tileDistance = Math.Abs(current.X - key.X) + Math.Abs(current.Z - key.Z);
                ITile tileMin = new Tile(current.X - tileDistance, current.Z - tileDistance);
                ITile tileMax = new Tile(current.X + tileDistance, current.Z + tileDistance);
                int tileMaxIndex = FindNearestIndexCeiling(tileMax);
                for (int i = FindNearestIndexFloor(tileMin); i < tileMaxIndex; i++)
                {
                    double currentDistance;
                    if ((currentDistance = position.DistanceSquared(PointD.TileCenter(sortedIndexes[i]))) < minDistance)
                    {
                        minDistance = currentDistance;
                        key = sortedIndexes[i];
                    }
                }
            }
            return tiles[key];
        }

        public IEnumerable<ITileCoordinate<T>> FindNearest(PointD position, ITile bottomLeft, ITile topRight)
        {
            Tile current = new Tile(Tile.TileFromAbs(position.X), Tile.TileFromAbs(position.Y));
            ITile key = sortedIndexes[FindNearestIndexCeiling(current)];
            double minDistance = double.MaxValue;
            if (current != key)
            {
                int tileDistance = Math.Abs(current.X - key.X) + Math.Abs(current.Z - key.Z);
                ITile tileMin = new Tile(current.X - tileDistance, current.Z - tileDistance);
                if (tileMin.CompareTo(bottomLeft) < 0)
                    tileMin = bottomLeft;
                ITile tileMax = new Tile(current.X + tileDistance, current.Z + tileDistance);
                if (tileMax.CompareTo(topRight) > 0)
                    tileMax = topRight;
                int tileMaxIndex = FindNearestIndexCeiling(tileMax);
                for (int i = FindNearestIndexFloor(tileMin); i < tileMaxIndex; i++)
                {
                    double currentDistance;
                    if ((currentDistance = position.DistanceSquared(PointD.TileCenter(sortedIndexes[i]))) < minDistance)
                    {
                        minDistance = currentDistance;
                        key = sortedIndexes[i];
                    }
                }
            }
            return tiles[key];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private int FindNearestIndexFloor(ITile possibleKey)
        {
            int keyIndex = sortedIndexes.BinarySearch(possibleKey);
            if (keyIndex < 0)
            {
                keyIndex = ~keyIndex;
                if (keyIndex == sortedIndexes.Count)
                    keyIndex = sortedIndexes.Count - 1;
            }
            return keyIndex;
        }

        private int FindNearestIndexCeiling(ITile possibleKey)
        {
            int keyIndex = sortedIndexes.BinarySearch(possibleKey);
            if (keyIndex < 0)
            {
                keyIndex = ~keyIndex;
                if (keyIndex > 0)
                    keyIndex--;
            }
            return keyIndex;
        }
    }
}
