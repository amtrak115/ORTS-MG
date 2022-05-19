﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;

using Microsoft.Xna.Framework;

using Orts.Common.DebugInfo;
using Orts.Common.Position;
using Orts.Formats.Msts.Models;

namespace Orts.Graphics.MapView.Widgets
{
    /// <summary>
    /// A single segment along a track, covering a single TrackNodeSection as part of a Track Node
    /// Main properties are Length, Direction at starting point, the endpoint
    /// and if this is a curved segment, Radius and the Angle (angular size)
    /// This is a base class for derived types like rail tracks, road tracks
    /// Multiple segments can form a path as part of a <see cref="SegmentPathBase{T}"/>, for paths following a track (train paths, platforms, sidings)
    /// </summary>
    internal abstract class SegmentBase : VectorPrimitive, INameValueInformationProvider
    {
        public abstract NameValueCollection DebugInfo { get; }
        public Dictionary<string, FormatOption> FormattingOptions { get; }

        internal readonly bool Curved;

        // Direction in Rad from -π to π from North (0) to South
        internal float Direction;
        internal float Length { get; private protected set; }
        // Angular Size (Length) of the Arc in Degree
        internal float Angle { get; private protected set; }
        internal float Radius { get; private protected set; }

        private protected PointD centerPoint;
        private protected float centerToStartDirection;
        private protected float centerToEndDirection;

        internal readonly int TrackNodeIndex;
        internal readonly int TrackVectorSectionIndex;

        private protected SegmentBase(): base(PointD.None, PointD.None)
        { }

        private protected SegmentBase(in PointD start, in PointD end): base(start, end)
        {
            Length = (float)Vector.Distance(Location);

            PointD origin = end - start;
            Direction = (float)Math.Atan2(origin.X, origin.Y) - MathHelper.PiOver2;
        }

        public SegmentBase(TrackVectorSection trackVectorSection, TrackSections trackSections, int trackNodeIndex, int trackVectorSectionIndex)
        {
            ref readonly WorldLocation location = ref trackVectorSection.Location;
            double cosA = Math.Cos(trackVectorSection.Direction.Y);
            double sinA = Math.Sin(trackVectorSection.Direction.Y);

            SetLocation(location);

            TrackSection trackSection = trackSections.TryGet(trackVectorSection.SectionIndex);

            if (null == trackSection)
                return;
            //                throw new System.IO.InvalidDataException($"TrackVectorSection {trackVectorSection.SectionIndex} not found in TSection.dat");

            Size = trackSection.Width;
            Curved = trackSection.Curved;
            Direction = MathHelper.WrapAngle(trackVectorSection.Direction.Y - MathHelper.PiOver2);
            TrackNodeIndex = trackNodeIndex;
            TrackVectorSectionIndex = trackVectorSectionIndex;

            if (trackSection.Curved)
            {
                Angle = MathHelper.ToRadians(trackSection.Angle);
                Radius = trackSection.Radius;
                Length = trackSection.Length;

                int sign = -Math.Sign(trackSection.Angle);

                double angleRadians = MathHelper.ToRadians(trackSection.Angle);
                double cosArotated = Math.Cos(trackVectorSection.Direction.Y + angleRadians);
                double sinArotated = Math.Sin(trackVectorSection.Direction.Y + angleRadians);
                double deltaX = sign * trackSection.Radius * (cosA - cosArotated);
                double deltaZ = sign * trackSection.Radius * (sinA - sinArotated);
                SetVector(new PointD(location.TileX * WorldLocation.TileSize + location.Location.X - deltaX, location.TileZ * WorldLocation.TileSize + location.Location.Z + deltaZ));

                centerPoint = base.Location - (new PointD(Math.Sin(Direction), Math.Cos(Direction)) * -sign * Radius);
                centerToStartDirection = MathHelper.WrapAngle(Direction + (sign * MathHelper.PiOver2));
                centerToEndDirection = MathHelper.WrapAngle(centerToStartDirection + Angle);
            }
            else
            {
                Length = trackSection.Length;

                // note, angle is 90 degrees off, and different sign. 
                // So Delta X = cos(90-A)=sin(A); Delta Y,Z = sin(90-A) = cos(A)    
                SetVector(new PointD(location.TileX * WorldLocation.TileSize + location.Location.X + sinA * Length, location.TileZ * WorldLocation.TileSize + location.Location.Z + cosA * Length));
            }
        }

        private protected SegmentBase(SegmentBase source): base(source.Location, source.Vector)
        {
            Size = source.Size;
            Curved = source.Curved;
            Direction = source.Direction;
            Length = source.Length;
            TrackNodeIndex = source.TrackNodeIndex;
            TrackVectorSectionIndex = source.TrackVectorSectionIndex;
            Angle = source.Angle;
            Radius = source.Radius;
            centerPoint = source.centerPoint;
            centerToStartDirection = source.centerToStartDirection;
            centerToEndDirection = source.centerToEndDirection;
        }

        private protected SegmentBase(SegmentBase source, float remainingLength, float startOffset, bool reverse) : this(source)
        {
            if (startOffset == 0 && remainingLength >= Length)//full path segment
                return;
            //remainingLength is in m down the track, startOffset is either in m for straight, or in Rad for Curved
            if (Curved)
            {
                int sign = Math.Sign(Angle);
                float remainingArc = remainingLength / Radius * sign;

                if (reverse)
                {
                    if (startOffset != 0)
                        Angle = startOffset * sign;
                    if (Math.Abs(remainingArc) < Math.Abs(Angle))
                    {
                        Direction += Angle - remainingArc;
                        Angle = remainingArc;
                        SetLocation(centerPoint + new PointD(sign * Math.Sin(Direction) * Radius, sign * Math.Cos(Direction) * Radius));
                    }
                }
                else
                {
                    Direction += sign * startOffset;
                    SetLocation(centerPoint + new PointD(sign * Math.Sin(Direction) * Radius, sign * Math.Cos(Direction) * Radius));
                    Angle -= sign * startOffset;
                    if (Math.Abs(remainingArc) < Math.Abs(Angle))
                        Angle = remainingArc;
                }
                Angle += 0.01f * sign;  // there seems to be a small rounding error somewhere leading to tiny gap in some cases
                Length = Radius * Angle * sign;
            }
            else
            {
                float endOffset = 0;
                if (reverse)
                {
                    if (startOffset == 0)
                        startOffset = Length;
                    else
                        Length = startOffset;
                    if (remainingLength < startOffset)
                    {
                        endOffset = startOffset - remainingLength;
                        Length = remainingLength;
                    }
                    (startOffset, endOffset) = (endOffset, startOffset);
                }
                else
                {
                    Length -= startOffset;
                    endOffset = Length;
                    if (remainingLength + startOffset < Length)
                    {
                        endOffset = remainingLength + startOffset;
                        Length = remainingLength;
                    }
                }

                double dx = Vector.X - Location.X;
                double dy = Vector.Y - Location.Y;
                double scale = startOffset / source.Length;
                SetLocation(new PointD(Location.X + dx * scale, Location.Y + dy * scale));
                scale = endOffset / source.Length;
                SetVector(new PointD(Location.X + dx * scale, Location.Y + dy * scale));
            }
        }

        private protected SegmentBase(SegmentBase source, in PointD start, in PointD end) : this(source)
        {
            bool reverse = false;

            //figure which end is closer to start vs end
            if (start.DistanceSquared(Location) > start.DistanceSquared(Vector) && end.DistanceSquared(Location) < end.DistanceSquared(Vector))
                reverse = true;

            //TODO 20220407 may need/want to map the start/end point onto the actual track, as they may be slightly skewed/offset from the track
            //however at this point it should already be determined that the points are perpendicular to (along) the track, and within a certain distance limit

            if (reverse)
            {
                SetVector(end, start);
            }
            else
            {
                SetVector(start, end);
            }

            if (Curved)
            {
                PointD deltaStart = Location - centerPoint;
                float deltaAngle = (float)Math.Atan2(deltaStart.X, deltaStart.Y) - MathHelper.PiOver2;
                deltaAngle = MathHelper.WrapAngle(centerToStartDirection - deltaAngle);
                Direction -= deltaAngle;
                Angle += deltaAngle;
                PointD deltaEnd = Vector - centerPoint;
                deltaAngle = (float)Math.Atan2(deltaEnd.X, deltaEnd.Y) - MathHelper.PiOver2;
                deltaAngle = MathHelper.WrapAngle(deltaAngle - centerToEndDirection);
                Angle += deltaAngle;
            }
            else
            {
                Length = (float)end.Distance(start);
            }
        }

        #region math
        public override double DistanceSquared(in PointD point)
        {
            double distanceSquared;
            if (Curved)
            {
                PointD delta = point - centerPoint;
                float angle = MathHelper.WrapAngle((float)Math.Atan2(delta.X, delta.Y) - MathHelper.PiOver2);
                if (Angle < 0 && ((angle < centerToStartDirection && angle > centerToEndDirection)
                    || (centerToStartDirection < centerToEndDirection && (angle > centerToEndDirection || angle < centerToStartDirection)))
                    || (Angle > 0 && ((angle > centerToStartDirection && angle < centerToEndDirection)
                    || (centerToStartDirection > centerToEndDirection && (angle > centerToStartDirection || angle < centerToEndDirection)))))
                    return (distanceSquared = (centerPoint.Distance(point) - Radius)) * distanceSquared;

                if (Angle > 0 && ((angle < centerToStartDirection) || (centerToStartDirection > centerToEndDirection && (angle > centerToStartDirection || angle < centerToEndDirection))))
                    return (distanceSquared = point.DistanceSquared(Location)) > ProximityTolerance ? double.NaN : distanceSquared;
                if (Angle < 0 && ((angle < centerToEndDirection) || (centerToEndDirection > centerToStartDirection && (angle > centerToEndDirection || angle < centerToStartDirection))))
                    return (distanceSquared = point.DistanceSquared(Vector)) > ProximityTolerance ? double.NaN : distanceSquared;

                return double.NaN;
            }
            else
            {
                distanceSquared = Length * Length;
                // Calculate the t that minimizes the distance.
                double t = (point - Location).DotProduct(Vector - Location) / distanceSquared;

                // if t < 0 or > 1 the point is basically not perpendicular to the line, so we return NaN if this is even beyond the tolerance
                // (else if needed could return the distance from either start or end point)
                if (t < 0)
                    return (distanceSquared = point.DistanceSquared(Location)) > ProximityTolerance ? double.NaN : distanceSquared;
                else if (t > 1)
                    return (distanceSquared = point.DistanceSquared(Vector)) > ProximityTolerance ? double.NaN : distanceSquared;
                return point.DistanceSquared(Location + (Vector - Location) * t);
            }
        }
        #endregion

        public static SegmentBase SegmentBaseAt(in PointD location, IEnumerable<SegmentBase> segments)
        {
            foreach (SegmentBase segment in segments)
            {
                //find the start vector section
                if (segment.DistanceSquared(location) < ProximityTolerance)
                {
                    return segment;
                }
            }
            return null;
        }

        public float DirectionAt(in PointD location)
        {
            if (Curved)
            {
                PointD delta = location - centerPoint;
                float deltaAngle = (float)Math.Atan2(delta.X, delta.Y) - MathHelper.PiOver2;
                deltaAngle = MathHelper.WrapAngle(centerToStartDirection - deltaAngle);
                return Direction - deltaAngle;
            }
            else
            {
                return Direction;
            }

        }
    }
}
