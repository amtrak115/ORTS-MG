﻿using System.Globalization;
using System.Text;

namespace Orts.Common.Position
{
    public static class TileHelper
    {
        public enum Zoom
        {
            Invalid = 0,
            /// <summary>
            /// 32KM^2
            /// </summary>
            DMLarge = 11,
            /// <summary>
            /// 16KM^2
            /// </summary>
            DMSmall = 12,
            /// <summary>
            /// 8KM^2 
            /// </summary>
            Normal = 13,    // not used
            /// <summary>
            /// 4KM^2
            /// </summary>
            Large = 14, 
            /// <summary>
            /// 2KM^2
            /// </summary>
            Small = 15, 
        }

        public static string FromTileXZ(int tileX, int tileZ, Zoom zoom)
        {
            int rectX = -16384;
            int rectZ = -16384;
            int rectW = 16384;
            int rectH = 16384;
            StringBuilder name = new StringBuilder((int)zoom % 2 == 1 ? "-" : "_");
            int partial = 0;

            for (int z = 0; z < (int)zoom; z++)
            {
                bool east = tileX >= rectX + rectW;
                bool north = tileZ >= rectZ + rectH;
                partial <<= 2;
                partial += (north ? 0 : 2) + (east ^ north ? 0 : 1);
                if (z % 2 == 1)
                {
                    name.Append(partial.ToString("X", CultureInfo.InvariantCulture));
                    partial = 0;
                }
                if (east) rectX += rectW;
                if (north) rectZ += rectH;
                rectW /= 2;
                rectH /= 2;
            }
            if ((int)zoom % 2 == 1)
                name.Append((partial << 2).ToString("X", CultureInfo.InvariantCulture));
            return name.ToString();
        }

        public static void Snap(ref int tileX, ref int tileZ, Zoom zoom)
        {
            int step = 15 - (int)zoom;
            tileX >>= step;
            tileX <<= step;
            tileZ >>= step;
            tileZ <<= step;
        }
    }
}
