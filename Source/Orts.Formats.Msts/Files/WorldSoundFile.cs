﻿// COPYRIGHT 2010, 2012 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using Orts.Formats.Msts.Models;
using Orts.Formats.Msts.Parsers;

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Orts.Formats.Msts.Files
{
    public class WorldSoundFile
    {
        public TrackItemSound TrackItemSound { get; private set; }

        public WorldSoundFile(string fileName, int trackItemsCount)
        {
            if (File.Exists(fileName))
            {
                Trace.Write("$");
                using (STFReader stf = new STFReader(fileName, false))
                {
                    stf.ParseFile(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("tr_worldsoundfile", ()=>{ TrackItemSound = new TrackItemSound(stf, trackItemsCount); }),
                    });
                    if (TrackItemSound == null)
                        STFException.TraceWarning(stf, "Missing TR_WorldSoundFile statement");
                }
            }
        }
    }
}
