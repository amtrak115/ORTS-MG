// COPYRIGHT 2014 by the Open Rails project.
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

using System;
using System.Collections.Generic;

using Orts.Common;
using Orts.Common.Calc;
using Orts.Formats.Msts.Parsers;

using static Orts.Formats.Msts.Parsers.STFReader;

namespace Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS
{
    public class SingleTransferPipe : AirSinglePipe
    {
        public SingleTransferPipe(TrainCar car)
            : base(car)
        {
            DebugType = "-";
        }

        public override void Parse(string lowercasetoken, STFReader stf)
        {
            if (null == stf)
                throw new ArgumentNullException(nameof(stf));
            switch (lowercasetoken)
            {
                // OpenRails specific parameters
                case "wagon(brakepipevolume":
                    BrakePipeVolumeM3 = (float)Size.Volume.FromFt3(stf.ReadFloatBlock(STFReader.Units.VolumeDefaultFT3, null));
                    break;
            }
        }

        public override void Initialize(bool handbrakeOn, float maxPressurePSI, float fullServPressurePSI, bool immediateRelease)
        {
            base.Initialize(handbrakeOn, 0, 0, true);
            AuxResPressurePSI = 0;
            EmergResPressurePSI = 0;
            (Car as MSTSWagon).RetainerPositions = 0;
            (Car as MSTSWagon).EmergencyReservoirPresent = false;
            // Calculate brake pipe size depending upon whether vacuum or air braked
            if (Car.BrakeSystemType == Formats.Msts.BrakeSystemType.VacuumPiped)
            {
                BrakePipeVolumeM3 = (0.050f * 0.050f * (float)Math.PI / 4f) * Math.Max(5.0f, (1 + Car.CarLengthM)); // Using (2") pipe
            }
            else // air braked by default
            {
                BrakePipeVolumeM3 = (0.032f * 0.032f * (float)Math.PI / 4f) * Math.Max(5.0f, (1 + Car.CarLengthM)); // Using DN32 (1-1/4") pipe
            }
        }

        public override void InitializeFromCopy(BrakeSystem source)
        {
            BrakePipeVolumeM3 = (source as SingleTransferPipe)?.BrakePipeVolumeM3 ?? throw new ArgumentNullException(nameof(source));
        }

        public override string GetStatus(EnumArray<Pressure.Unit, BrakeSystemComponent> units)
        {
            if (null == units)
                throw new ArgumentNullException(nameof(units));
            // display differently depending upon whether vacuum or air braked system
            if (Car.BrakeSystemType == Formats.Msts.BrakeSystemType.VacuumPiped)
            {
                return Simulator.Catalog.GetString($" BP {FormatStrings.FormatPressure(Pressure.Vacuum.FromPressure(BrakeLine1PressurePSI), Pressure.Unit.InHg, Pressure.Unit.InHg, false)}");
            }
            else  // air braked by default
            {
                return Simulator.Catalog.GetString($"BP {FormatStrings.FormatPressure(BrakeLine1PressurePSI, Pressure.Unit.PSI, units[BrakeSystemComponent.BrakePipe], true)}");
            }
        }

        public override string GetFullStatus(BrakeSystem lastCarBrakeSystem, EnumArray<Pressure.Unit, BrakeSystemComponent> units)
        {
            if (null == units)
                throw new ArgumentNullException(nameof(units));
            // display differently depending upon whether vacuum or air braked system
            if (Car.BrakeSystemType == Formats.Msts.BrakeSystemType.VacuumPiped)
            {
                string s = Simulator.Catalog.GetString($" V {FormatStrings.FormatPressure(Car.Train.EqualReservoirPressurePSIorInHg, Pressure.Unit.InHg, Pressure.Unit.InHg, true)}");
                if (lastCarBrakeSystem != null && lastCarBrakeSystem != this)
                    s += Simulator.Catalog.GetString(" EOT ") + lastCarBrakeSystem.GetStatus(units);
                if (HandbrakePercent > 0)
                    s += Simulator.Catalog.GetString($" Handbrake {HandbrakePercent:F0}%");
                return s;
            }
            else // air braked by default
            {
                string s = Simulator.Catalog.GetString($"BP {FormatStrings.FormatPressure(BrakeLine1PressurePSI, Pressure.Unit.PSI, units[BrakeSystemComponent.BrakePipe], false)}");
                if (lastCarBrakeSystem != null && lastCarBrakeSystem != this)
                    s += Simulator.Catalog.GetString(" EOT ") + lastCarBrakeSystem.GetStatus(units);
                if (HandbrakePercent > 0)
                    s += Simulator.Catalog.GetString($" Handbrake {HandbrakePercent:F0}%");
                return s;
            }
        }

        // This overides the information for each individual wagon in the extended HUD  
        public override string[] GetDebugStatus(EnumArray<Pressure.Unit, BrakeSystemComponent> units)
        {
            if (null == units)
                throw new ArgumentNullException(nameof(units));
            // display differently depending upon whether vacuum or air braked system
            if (Car.BrakeSystemType == Formats.Msts.BrakeSystemType.VacuumPiped)
            {

                return new string[] {
                DebugType,
                string.Empty,
                FormatStrings.FormatPressure(Pressure.Vacuum.FromPressure(BrakeLine1PressurePSI), Pressure.Unit.InHg, Pressure.Unit.InHg, true),
                string.Empty,
                string.Empty, // Spacer because the state above needs 2 columns.
                HandbrakePercent > 0 ? $"{HandbrakePercent:F0}%" : string.Empty,
                FrontBrakeHoseConnected ? "I" : "T",
                $"A{(AngleCockAOpen ? "+" : "-")} B{(AngleCockBOpen ? "+" : "-")}",
                };
            }
            else  // air braked by default
            {

                return new string[] {
                DebugType,
                string.Empty,
                FormatStrings.FormatPressure(BrakeLine1PressurePSI, Pressure.Unit.PSI, units[BrakeSystemComponent.BrakePipe], true),
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty, // Spacer because the state above needs 2 columns.
                (Car as MSTSWagon).HandBrakePresent ? $"{HandbrakePercent:F0}%" : string.Empty,
                FrontBrakeHoseConnected ? "I" : "T",
                $"A{(AngleCockAOpen ? "+" : "-")} B{(AngleCockBOpen ? "+" : "-")}",
                BleedOffValveOpen ? Simulator.Catalog.GetString("Open") : string.Empty,
                };

            }
        }

        public override float GetCylPressurePSI()
        {
            return 0;
        }

        public override float GetVacResPressurePSI()
        {
            return 0;
        }

        public override void Update(double elapsedClockSeconds)
        {
            BleedOffValveOpen = false;
            Car.SetBrakeForce(Car.MaxHandbrakeForceN * HandbrakePercent / 100);
        }
    }
}
