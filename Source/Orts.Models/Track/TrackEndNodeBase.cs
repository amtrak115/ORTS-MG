﻿using System;

using Microsoft.Xna.Framework;

using Orts.Common.Position;
using Orts.Formats.Msts.Models;

namespace Orts.Models.Track
{
    public abstract class EndNodeBase: PointPrimitive, ITrackNode
    {
        public float Direction { get; }
        public int TrackNodeIndex { get; }

        protected EndNodeBase(TrackEndNode trackEndNode, TrackVectorNode connectedVectorNode, TrackSections trackSections): 
            base(trackEndNode?.UiD.Location ?? throw new ArgumentNullException(nameof(trackEndNode)))
        {
            if (null == connectedVectorNode)
                return;
            if (null == trackSections)
                throw new ArgumentNullException(nameof(trackSections));

            if (connectedVectorNode.TrackPins[0].Link == trackEndNode.Index)
            {
                //find angle at beginning of vector node
                TrackVectorSection tvs = connectedVectorNode.TrackVectorSections[0];
                Direction = tvs.Direction.Y;
            }
            else
            {
                //find angle at end of vector node
                TrackVectorSection trackVectorSection = connectedVectorNode.TrackVectorSections[^1];
                Direction = trackVectorSection.Direction.Y;
                // try to get even better in case the last section is curved
                TrackSection trackSection = trackSections.TryGet(trackVectorSection.SectionIndex);
                if (null == trackSection)
                    throw new System.IO.InvalidDataException($"TrackVectorSection {trackVectorSection.SectionIndex} not found in TSection.dat");
                if (trackSection.Curved)
                {
                    Direction += MathHelper.ToRadians(trackSection.Angle);
                }
            }
            Direction -= MathHelper.PiOver2;
            TrackNodeIndex = trackEndNode.Index;

        }
    }
}
