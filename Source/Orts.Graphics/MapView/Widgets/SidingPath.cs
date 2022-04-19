﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Orts.Common.Position;
using Orts.Graphics.DrawableComponents;

namespace Orts.Graphics.MapView.Widgets
{
    internal class SidingPath: SegmentPathBase<SidingSegment>
    {
        internal string SidingName { get; }

        public SidingPath(SidingTrackItem start, SidingTrackItem end, Dictionary<int, List<SegmentBase>> trackNodeSegments) : base(start, start.TrackVectorNode.Index, end, end.TrackVectorNode.Index, trackNodeSegments)
        {
            SidingName = string.IsNullOrEmpty(start.SidingName) ? end.SidingName : start.SidingName;
        }

        public static List<SidingPath> CreateSidings(IEnumerable<SidingTrackItem> sidingItems, Dictionary<int, List<SegmentBase>> trackNodeSegments)
        {
            List<SidingPath> sidings = new List<SidingPath>();

            Dictionary<int, SidingTrackItem> sidingItemMappings = sidingItems.ToDictionary(p => p.Id);
            while (sidingItemMappings.Count > 0)
            {
                int sourceId = sidingItemMappings.Keys.First();
                SidingTrackItem start = sidingItemMappings[sourceId];
                sidingItemMappings.Remove(sourceId);
                if (sidingItemMappings.TryGetValue(start.LinkedId, out SidingTrackItem end))
                {
                    if (end.LinkedId != start.Id)
                    {
                        Trace.TraceWarning($"Siding Item Pair has inconsistent linking from Source Id {start.Id} to target {start.LinkedId} vs Target id {end.Id} to source {end.LinkedId}.");
                    }
                    sidingItemMappings.Remove(end.Id);
                    sidings.Add(new SidingPath(start, end, trackNodeSegments));
                }
                else
                {
                    Trace.TraceWarning($"Linked Siding Item {start.LinkedId} for Siding Item {start.Id} not found.");
                }
            }
            return sidings;
        }

        internal override void Draw(ContentArea contentArea, ColorVariation colorVariation = ColorVariation.None, double scaleFactor = 1)
        {
            foreach (SidingSegment segment in pathSegments)
            {
                segment.Draw(contentArea, colorVariation, scaleFactor);
            }
        }

        protected override SidingSegment CreateItem(in PointD start, in PointD end)
        {
            return new SidingSegment(start, end);
        }

        protected override SidingSegment CreateItem(SegmentBase source)
        {
            return new SidingSegment(source);
        }

        protected override SidingSegment CreateItem(SegmentBase source, in PointD start, in PointD end)
        {
            return new SidingSegment(source, start, end);
        }
    }
}
