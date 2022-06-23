﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

using Orts.Common.Position;
using Orts.Formats.Msts;
using Orts.Formats.Msts.Files;
using Orts.Formats.Msts.Models;
using Orts.Models.Track;

namespace Orts.Graphics.MapView.Widgets
{
    internal class TrainPath : TrackSegmentPathBase<TrainPathSegment>, IDrawable<VectorPrimitive>
    {
        private readonly List<TrainPathItem> pathPoints = new List<TrainPathItem>();

        private class TrainPathSection : TrackSegmentSectionBase<TrainPathSegment>, IDrawable<VectorPrimitive>
        {
            public TrainPathSection(int trackNodeIndex) :
                base(trackNodeIndex)
            {
            }

            public TrainPathSection(int trackNodeIndex, in PointD startLocation, in PointD endLocation) :
                base(trackNodeIndex, startLocation, endLocation)
            {
            }

            public virtual void Draw(ContentArea contentArea, ColorVariation colorVariation = ColorVariation.None, double scaleFactor = 1)
            {
                foreach (TrainPathSegment segment in SectionSegments)
                {
                    segment.Draw(contentArea, colorVariation, scaleFactor);
                }
            }

            protected override TrainPathSegment CreateItem(in PointD start, in PointD end)
            {
                return new TrainPathSegment(start, end);
            }

            protected override TrainPathSegment CreateItem(TrackSegmentBase source)
            {
                return new TrainPathSegment(source);
            }

            protected override TrainPathSegment CreateItem(TrackSegmentBase source, in PointD start, in PointD end)
            {
                return new TrainPathSegment(source, start, end);
            }
        }

        public TrainPath(in WorldLocation start, in WorldLocation end): base(PointD.FromWorldLocation(start), 0, PointD.FromWorldLocation(end), 0)
        {
        }

        public TrainPath(PathFile pathFile)
            : base(PointD.FromWorldLocation(pathFile.PathNodes[0].Location), PointD.FromWorldLocation(pathFile.PathNodes[^1].Location))
        {
            static TrackSegmentBase NodeSegmentByLocation(in PointD nodeLocation)
            {
                foreach (TrackSegmentSection trackNodes in TrackModel.Instance.SegmentSections)
                {
                    foreach (TrackSegmentBase trackSegment in trackNodes.SectionSegments)
                    {
                        if (trackSegment.DistanceSquared(nodeLocation) <= ProximityTolerance)
                        {
                            return trackSegment;
                        }
                    }
                }
                throw new MissingTrackNodeException();
            }

            foreach (PathNode node in pathFile.PathNodes)
            {

                PointD nodeLocation = PointD.FromWorldLocation(node.Location);
                TrackSegmentBase nodeSegment = NodeSegmentByLocation(nodeLocation);
                // if either one is on a junction, first get the junction
                // get all the connected track nodes
                // and find the connecting track nodes
                if (node.NextMainNode > -1)
                {
                    // valid cases
                    // both points are on a (the same) tracksegment
                    // one node is a junction
                    // both nodes are a junction
                    // in either variant, there could be any number of trailing junctions in between
                    PathNode nextNode = pathFile.PathNodes[node.NextMainNode];
                    PointD nextNodeLocation = PointD.FromWorldLocation(nextNode.Location);

                    JunctionNodeBase junctionNode = node.Junction ? TrackModel.Instance.JunctionBaseAt(nodeLocation) : null;
                    JunctionNodeBase nextJunctionNode = nextNode.Junction ? TrackModel.Instance.JunctionBaseAt(nextNodeLocation) : null;

                    if (node.Junction || nextNode.Junction)
                    {
                        if (node.Junction && nextNode.Junction)
                        {
                            TrackPin[] trackPins = RuntimeData.Instance.TrackDB.TrackNodes[junctionNode.TrackNodeIndex].TrackPins.
                                Intersect(RuntimeData.Instance.TrackDB.TrackNodes[nextJunctionNode.TrackNodeIndex].TrackPins, TrackPinComparer.LinkOnlyComparer).ToArray();
                            if (trackPins.Length == 1)
                            {
                                PathSections.Add(new TrainPathSection(TrackModel.Instance.SegmentSections[trackPins[0].Link].TrackNodeIndex));//, nodeLocation, nextNodeLocation);
                            }
                            else
                            {
                                Trace.TraceWarning($"Invalid Data.");
//                                Debug.Assert(false);
                            }
                        }
                        else if (node.Junction)
                        {
                            nodeSegment = NodeSegmentByLocation(nextNodeLocation);
                            PathSections.Add(new TrainPathSection(nodeSegment.TrackNodeIndex, nodeLocation, nextNodeLocation));
                        }
                        else if (nextNode.Junction)
                        {
                            PathSections.Add(new TrainPathSection(nodeSegment.TrackNodeIndex, nodeLocation, nextNodeLocation));
                        }
                    }
                    else
                    {
                        TrackSegmentBase nextNodeSegment = NodeSegmentByLocation(nextNodeLocation);
                        if (nodeSegment.TrackNodeIndex != nextNodeSegment.TrackNodeIndex)
                        { 
                        }
                        else
                        {
                            PathSections.Add(new TrainPathSection(nodeSegment.TrackNodeIndex, PointD.FromWorldLocation(node.Location), PointD.FromWorldLocation(nextNode.Location)));
                        }
                    }
                }
                pathPoints.Add(new TrainPathItem(nodeLocation, nodeSegment, node.NodeType));
            }
        }

        public override double DistanceSquared(in PointD point)
        {
            return double.NaN;
        }

        public virtual void Draw(ContentArea contentArea, ColorVariation colorVariation = ColorVariation.None, double scaleFactor = 1)
        {
            foreach(TrainPathSection pathSection in PathSections)
            {
                pathSection.Draw(contentArea, colorVariation, scaleFactor);
            }
            foreach (TrainPathItem pathItem in pathPoints)
            {
                pathItem.Draw(contentArea, colorVariation, scaleFactor);
            }
        }

        protected override TrackSegmentSectionBase<TrainPathSegment> AddSection(int trackNodeIndex, in PointD start, in PointD end)
        {
            return new TrainPathSection(trackNodeIndex, start, end);
        }

        protected override TrackSegmentSectionBase<TrainPathSegment> AddSection(int trackNodeIndex)
        {
            return new TrainPathSection(trackNodeIndex);
        }
    }
}
