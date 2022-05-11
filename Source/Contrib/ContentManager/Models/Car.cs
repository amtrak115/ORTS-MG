﻿// COPYRIGHT 2015 by the Open Rails project.
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

using Orts.Formats.Msts;
using Orts.Formats.Msts.Files;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Orts.ContentManager.Models
{
    public enum CarType
    {
        Engine,
        Wagon,
    }

    public class Car
    {
        public CarType Type { get; }
        public string Name { get; }
        public string Description { get; }

        public Car(ContentBase content)
        {
            Debug.Assert(content?.Type == ContentType.Car);
            if (System.IO.Path.GetExtension(content.PathName).Equals(".eng", StringComparison.OrdinalIgnoreCase))
            {
                EngineFile file = new EngineFile(content.PathName);
                Type = CarType.Engine;
                Name = file.Name;
                Description = file.Description;
            }
            else if (System.IO.Path.GetExtension(content.PathName).Equals(".wag", StringComparison.OrdinalIgnoreCase))
            {
                WagonFile file = new WagonFile(content.PathName);
                Type = CarType.Wagon;
                Name = file.Name;
                Description = "";
            }
        }
    }
}
