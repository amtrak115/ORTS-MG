﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

using Orts.Common.Position;
using Orts.Formats.Msts.Files;
using Orts.Formats.Msts.Models;
using Orts.Graphics.Track.Widgets;

namespace Orts.Graphics.Track
{
    public class TrackContent
    {

        private TrackDB trackDB;
        private RoadTrackDB roadTrackDB;
        private TrackSectionsFile trackSectionsFile;

        internal TileIndexedList<TrackSegment, Tile> TrackSegments { get; private set; }
        internal TileIndexedList<TrackEndSegment, Tile> TrackEndSegments { get; private set; }
        internal TileIndexedList<JunctionSegment, Tile> JunctionSegments { get; private set; }
        internal TileIndexedList<TrackItemBase, Tile> TrackItems { get; private set; }
        internal TileIndexedList<GridTile, Tile> Tiles { get; private set; }
        internal TileIndexedList<RoadSegment, Tile> RoadSegments { get; private set; }
        internal TileIndexedList<RoadEndSegment, Tile> RoadEndSegments { get; private set; }
        internal Dictionary<uint, List<TrackSegment>> TrackNodeSegments { get; private set; }
        internal Dictionary<uint, List<TrackSegment>> RoadTrackNodeSegments { get; private set; }

        internal SignalConfigurationFile SignalConfigFile { get; }
        public bool UseMetricUnits { get; }

        public Rectangle Bounds { get; private set; }

        public TrackContent(TrackDB trackDB, RoadTrackDB roadTrackDB, TrackSectionsFile trackSections, SignalConfigurationFile signalConfig, bool metricUnits)
        {
            this.trackDB = trackDB;
            this.roadTrackDB = roadTrackDB;
            trackSectionsFile = trackSections;
            UseMetricUnits = metricUnits;
            SignalConfigFile = signalConfig;
        }

        public async Task Initialize()
        {
            List<Task> initializer = new List<Task>
            {
                Task.Run(async () => await InitializeTrackSegments().ConfigureAwait(false))
            };

            await Task.WhenAll(initializer).ConfigureAwait(false);
            trackDB = null;
            roadTrackDB = null;
            trackSectionsFile = null;
        }

        private async Task InitializeTrackSegments()
        {
            await Task.Run(() => AddTrackSegments()).ConfigureAwait(false);
            await Task.Run(() => AddTrackItems()).ConfigureAwait(false);
        }

        private void AddTrackSegments()
        {
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;

            List<TrackSegment> trackSegments = new List<TrackSegment>();
            List<TrackEndSegment> endSegments = new List<TrackEndSegment>();
            List<JunctionSegment> junctionSegments = new List<JunctionSegment>();
            List<TrackSegment> roadSegments = new List<TrackSegment>();
            List<TrackEndSegment> roadEndSegments = new List<TrackEndSegment>();
            foreach (TrackNode trackNode in trackDB?.TrackNodes ?? Enumerable.Empty<TrackNode>())
            {
                switch (trackNode)
                {
                    case TrackEndNode trackEndNode:
                        TrackVectorNode connectedVectorNode = trackDB.TrackNodes[trackEndNode.TrackPins[0].Link] as TrackVectorNode;
                        endSegments.Add(new TrackEndSegment(trackEndNode, connectedVectorNode, trackSectionsFile.TrackSections));
                        break;
                    case TrackVectorNode trackVectorNode:
                        foreach (TrackVectorSection trackVectorSection in trackVectorNode.TrackVectorSections)
                        {
                            TrackSection trackSection = trackSectionsFile.TrackSections.Get(trackVectorSection.SectionIndex);
                            if (trackSection != null)
                                trackSegments.Add(new TrackSegment(trackVectorSection, trackSection, trackVectorNode.Index));
                        }
                        break;
                    case TrackJunctionNode trackJunctionNode:
                        foreach (TrackPin pin in trackJunctionNode.TrackPins)
                        {
                            if (trackDB.TrackNodes[pin.Link] is TrackVectorNode vectorNode && vectorNode.TrackVectorSections.Length > 0)
                            {
                                TrackVectorSection item = pin.Direction == Common.TrackDirection.Reverse ? vectorNode.TrackVectorSections.First() : vectorNode.TrackVectorSections.Last();
                            }
                        }
                        junctionSegments.Add(new JunctionSegment(trackJunctionNode));
                        break;
                }
            }

            TrackSegments = new TileIndexedList<TrackSegment, Tile>(trackSegments);
            JunctionSegments = new TileIndexedList<JunctionSegment, Tile>(junctionSegments);
            TrackEndSegments = new TileIndexedList<TrackEndSegment, Tile>(endSegments);
            TrackNodeSegments = trackSegments.GroupBy(t => t.TrackNodeIndex).ToDictionary(i => i.Key, i => i.ToList());

            foreach (TrackNode trackNode in roadTrackDB?.TrackNodes ?? Enumerable.Empty<TrackNode>())
            {
                switch (trackNode)
                {
                    case TrackEndNode trackEndNode:
                        TrackVectorNode connectedVectorNode = roadTrackDB.TrackNodes[trackEndNode.TrackPins[0].Link] as TrackVectorNode;
                        roadEndSegments.Add(new RoadEndSegment(trackEndNode, connectedVectorNode, trackSectionsFile.TrackSections));
                        break;
                    case TrackVectorNode trackVectorNode:
                        foreach (TrackVectorSection trackVectorSection in trackVectorNode.TrackVectorSections)
                        {
                            TrackSection trackSection = trackSectionsFile.TrackSections.Get(trackVectorSection.SectionIndex);
                            if (trackSection != null)
                                roadSegments.Add(new RoadSegment(trackVectorSection, trackSection, trackVectorNode.Index));
                        }
                        break;
                }
            }

            RoadSegments = new TileIndexedList<RoadSegment, Tile>(roadSegments);
            RoadEndSegments = new TileIndexedList<RoadEndSegment, Tile>(roadEndSegments);
            RoadTrackNodeSegments = roadSegments.GroupBy(t => t.TrackNodeIndex).ToDictionary(i => i.Key, i => i.ToList());

            Tiles = new TileIndexedList<GridTile, Tile>(
                TrackSegments.Select(d => d.Tile as ITile).Distinct()
                .Union(TrackEndSegments.Select(d => d.Tile as ITile).Distinct())
                .Union(RoadSegments.Select(d => d.Tile as ITile).Distinct())
                .Union(RoadEndSegments.Select(d => d.Tile as ITile).Distinct())
                .Select(t => new GridTile(t)));

            if (Tiles.Count == 1)
            {
                foreach (TrackEndSegment trackEndSegment in TrackEndSegments)
                {
                    minX = Math.Min(minX, trackEndSegment.Location.X);
                    minY = Math.Min(minY, trackEndSegment.Location.Y);
                    maxX = Math.Max(maxX, trackEndSegment.Location.X);
                    maxY = Math.Max(maxY, trackEndSegment.Location.Y);
                }
            }
            else
            {
                minX = Math.Min(minX, Tiles[0][0].Tile.X);
                maxX = Math.Max(maxX, Tiles[Tiles.Count - 1][0].Tile.X);
                foreach (GridTile tile in Tiles)
                {
                    minY = Math.Min(minY, tile.Tile.Z);
                    maxY = Math.Max(maxY, tile.Tile.Z);
                }
                minX = minX * WorldLocation.TileSize - WorldLocation.TileSize / 2;
                maxX = maxX * WorldLocation.TileSize + WorldLocation.TileSize / 2;
                minY = minY * WorldLocation.TileSize - WorldLocation.TileSize / 2;
                maxY = maxY * WorldLocation.TileSize + WorldLocation.TileSize / 2;
            }
            Bounds = new Rectangle((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));

        }

        private void AddTrackItems()
        {
            TrackItems = new TileIndexedList<TrackItemBase, Tile>(TrackItemBase.Create(trackDB?.TrackItems, SignalConfigFile, trackDB, TrackNodeSegments).Concat(TrackItemBase.Create(roadTrackDB?.TrItemTable)));
        }
    }
}
