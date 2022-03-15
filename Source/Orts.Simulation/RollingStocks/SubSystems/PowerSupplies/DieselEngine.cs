// COPYRIGHT 2013, 2014 by the Open Rails project.
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

using Microsoft.Xna.Framework;

using Orts.Common;
using Orts.Common.Calc;
using Orts.Formats.Msts.Parsers;
using Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{
    public class DieselEngines : IEnumerable, ISubSystem<DieselEngines>
    {
        /// <summary>
        /// A list of auxiliaries
        /// </summary>
        public List<DieselEngine> DEList = new List<DieselEngine>();

        /// <summary>
        /// Number of Auxiliaries on the list
        /// </summary>
        public int Count { get { return DEList.Count; } }

        public DieselEngineState State
        {
            get
            {
                DieselEngineState state = DieselEngineState.Stopped;

                foreach (DieselEngine dieselEngine in DEList)
                {
                    if (dieselEngine.State > state)
                        state = dieselEngine.State;
                }

                return state;
            }
        }

        /// <summary>
        /// Reference to the locomotive carrying the auxiliaries
        /// </summary>
        protected readonly MSTSDieselLocomotive Locomotive;

        public MSTSGearBoxParams MSTSGearBoxParams = new MSTSGearBoxParams();

        /// <summary>
        /// Creates a set of auxiliaries connected to the locomotive
        /// </summary>
        /// <param name="loco">Host locomotive</param>
        public DieselEngines(MSTSDieselLocomotive loco)
        {
            Locomotive = loco;
        }

        public DieselEngine this[int i]
        {
            get { return DEList[i]; }
            set { DEList[i] = value; }
        }

        public void Add()
        {
            DEList.Add(new DieselEngine(Locomotive));
        }

        public void Add(DieselEngine de)
        {
            DEList.Add(de);
        }


        /// <summary>
        /// Parses all the parameters within the ENG file
        /// </summary>
        /// <param name="stf">reference to the ENG file reader</param>
        public void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortsdieselengines":
                    stf.MustMatch("(");
                    int count = stf.ReadInt(0);
                    for (int i = 0; i < count; i++)
                    {
                        string setting = stf.ReadString().ToLower();
                        if (setting == "diesel")
                        {
                            DEList.Add(new DieselEngine(Locomotive));

                            DEList[i].Parse(stf);
                            DEList[i].Initialize();

                            // sets flag to indicate that a diesel eng prime mover code block has been defined by user, otherwise OR will define one through the next code section using "MSTS" values
                            DEList[i].DieselEngineConfigured = true;
                        }

                        if ((!DEList[i].IsInitialized))
                        {
                            STFException.TraceWarning(stf, "Diesel engine model has some errors - loading MSTS format");
                            DEList[i].InitFromMSTS();
                            DEList[i].Initialize();
                        }
                    }
                    break;
                case "engine(gearboxnumberofgears":
                case "engine(gearboxdirectdrivegear":
                case "engine(gearboxoperation":
                case "engine(gearboxenginebraking":
                case "engine(gearboxmaxspeedforgears":
                case "engine(gearboxmaxtractiveforceforgears":
                case "engine(gearboxoverspeedpercentageforfailure":
                case "engine(gearboxbackloadforce":
                case "engine(gearboxcoastingforce":
                case "engine(gearboxupgearproportion":
                case "engine(gearboxdowngearproportion":
                    MSTSGearBoxParams.Parse(lowercasetoken, stf);
                    break;
            }
        }

        public void Copy(DieselEngines other)
        {
            DEList = new List<DieselEngine>();
            MSTSGearBoxParams.Copy(other.MSTSGearBoxParams);
            foreach (DieselEngine de in other.DEList)
            {
                DieselEngine dieselEngine = new DieselEngine(Locomotive);
                dieselEngine.Copy(de);

                DEList.Add(dieselEngine);
            }
        }

        public void Initialize()
        {
            foreach (DieselEngine de in DEList)
            {
                de.Initialize();
            }
        }

        public void InitializeMoving()
        {
            foreach (DieselEngine de in DEList)
            {
                de.InitializeMoving();
            }
        }

        /// <summary>
        /// Saves status of each auxiliary on the list
        /// </summary>
        /// <param name="outf"></param>
        public void Save(BinaryWriter outf)
        {
            outf.Write(DEList.Count);
            foreach (DieselEngine de in DEList)
                de.Save(outf);
        }

        /// <summary>
        /// Restores status of each auxiliary on the list
        /// </summary>
        /// <param name="inf"></param>
        public void Restore(BinaryReader inf)
        {
            int count = inf.ReadInt32();
            if (DEList.Count == 0)
            {
                for (int i = 0; i < count; i++)
                {
                    DEList.Add(new DieselEngine(Locomotive));
                    DEList[i].InitFromMSTS();
                    DEList[i].Initialize();
                }

            }
            foreach (DieselEngine de in DEList)
                de.Restore(inf);
        }

        /// <summary>
        /// A summary of power of all the diesels
        /// </summary>
        public float PowerW
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                {
                    temp += de.OutputPowerW;
                }
                return temp;
            }
        }

        /// <summary>
        /// A power-on indicator
        /// </summary>
        public bool PowerOn
        {
            get
            {
                bool temp = false;
                foreach (DieselEngine de in DEList)
                {
                    temp |= (de.State == DieselEngineState.Running) || (de.State == DieselEngineState.Starting);
                }
                return temp;
            }
        }

        /// <summary>
        /// A summary of maximal power of all the diesels
        /// </summary>
        public float MaxPowerW
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                {
                    temp += de.MaximumDieselPowerW;
                }
                return temp;
            }
        }

        /// <summary>
        /// A summary of maximal power of all the diesels
        /// </summary>
        public float MaxOutputPowerW
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                {
                    temp += de.CurrentDieselOutputPowerW;
                }
                return temp;
            }
        }

        /// <summary>
        /// Maximum rail output power for all diesl prime movers
        /// </summary>
        public float MaximumRailOutputPowerW
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                {
                    temp += de.MaximumRailOutputPowerW;
                }
                return temp;
            }
        }

        /// <summary>
        /// A summary of fuel flow of all the auxiliaries
        /// </summary>
        public float DieselFlowLps
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                {
                    temp += de.DieselFlowLps;
                }
                return temp;
            }
        }

        /// <summary>
        /// A summary of the throttle setting of all the auxiliaries
        /// </summary>
        public float ApparentThrottleSetting
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                {
                    temp += de.ApparentThrottleSetting;
                }
                return temp / Count;
            }
        }

        public bool HasGearBox
        {
            get
            {
                bool temp = false;
                foreach (DieselEngine de in DEList)
                {
                    temp |= (de.GearBox != null);
                }
                return temp;
            }
        }

        public float TractiveForceN
        {
            get
            {
                float temp = 0;
                foreach (DieselEngine de in DEList)
                {
                    if (de.GearBox != null)
                        temp += (de.DemandedThrottlePercent * 0.01f * de.GearBox.TractiveForceN);
                }
                return temp;
            }
        }

        /// <summary>
        /// Updates each auxiliary on the list
        /// </summary>
        /// <param name="elapsedClockSeconds">Time span within the simulation cycle</param>
        public void Update(double elapsedClockSeconds)
        {
            foreach (DieselEngine de in DEList)
            {
                de.Update(elapsedClockSeconds);
            }
        }

        public void HandleEvent(PowerSupplyEvent evt)
        {
            foreach (DieselEngine de in DEList)
            {
                de.HandleEvent(evt);
            }
        }

        public void HandleEvent(PowerSupplyEvent evt, int id)
        {
            if (id >= 0 && id < DEList.Count)
            {
                DEList[id].HandleEvent(evt);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        public DieselEnum GetEnumerator()
        {
            return new DieselEnum(DEList.ToArray());
        }

        public string GetStatus()
        {
            var result = new StringBuilder();
            //result.AppendFormat(Simulator.Catalog.GetString("Status"));
            foreach (var eng in DEList)
                result.AppendFormat("\t{0}", eng.State.GetLocalizedDescription());

            //result.AppendFormat("\t{0}\t{1}", Simulator.Catalog.GetParticularString("HUD", "Power"), FormatStrings.FormatPower(MaxOutputPowerW, Locomotive.IsMetric, false, false));
            foreach (var eng in DEList)
                result.AppendFormat("\t{0}", FormatStrings.FormatPower(eng.CurrentDieselOutputPowerW, Locomotive.IsMetric, false, false));

            //result.AppendFormat("\t{0}", Simulator.Catalog.GetString("Load"));
            foreach (var eng in DEList)
                result.AppendFormat("\t{0:F1}%", eng.LoadPercent);

            foreach (var eng in DEList)
                result.AppendFormat("\t{0:F0} {1}", eng.RealRPM, FormatStrings.rpm);

            //result.AppendFormat("\t{0}", Simulator.Catalog.GetString("00Flow"));
            foreach (var eng in DEList)
                result.AppendFormat("\t{0}/{1}", FormatStrings.FormatFuelVolume(Frequency.Periodic.ToHours(eng.DieselFlowLps), Locomotive.IsMetric, Locomotive.IsUK), FormatStrings.h);

            //result.Append("\t");
            foreach (var eng in DEList)
                result.AppendFormat("\t{0}", FormatStrings.FormatTemperature(eng.DieselTemperatureDeg, Locomotive.IsMetric));

            //result.AppendFormat("\t{0}", Simulator.Catalog.GetString("Oil"));
            foreach (var eng in DEList)
                result.AppendFormat("\t{0}", FormatStrings.FormatPressure(eng.DieselOilPressurePSI, Pressure.Unit.PSI, Locomotive.MainPressureUnit, true));

            return result.ToString();
        }

        public int NumOfActiveEngines
        {
            get
            {
                int num = 0;
                foreach (DieselEngine eng in DEList)
                {
                    if (eng.State == DieselEngineState.Running)
                        num++;
                }
                return num;
            }
        }

        // This calculates the percent of running power. If the locomotive has two prime movers, and 
        // one is shut down then power will be reduced by the size of the prime mover
        public float RunningPowerFraction
        {
            get
            {
                float totalpossiblepower = 0;
                float runningPower = 0;
                float percent = 0;
                foreach (DieselEngine eng in DEList)
                {
                    totalpossiblepower += eng.MaximumDieselPowerW;
                    if (eng.State == DieselEngineState.Running)
                    {
                        runningPower += eng.MaximumDieselPowerW;
                    }
                }
                percent = runningPower / totalpossiblepower;
                return percent;
            }
        }
    }

    public class DieselEnum : IEnumerator
    {
        public DieselEngine[] deList;

        // Enumerators are positioned before the first element
        // until the first MoveNext() call.
        private int position = -1;

        public DieselEnum(DieselEngine[] list)
        {
            deList = list;
        }

        public bool MoveNext()
        {
            position++;
            return (position < deList.Length);
        }

        public void Reset()
        {
            position = -1;
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        public DieselEngine Current
        {
            get
            {
                try
                {
                    return deList[position];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }

    public class DieselEngine : ISubSystem<DieselEngine>
    {
        [Description("Engine")]
        public enum Cooling
        {
            NoCooling = 0,
            Mechanical = 1,
            Hysteresis = 2,
            Proportional = 3
        }

        public enum SettingsFlags
        {
            IdleRPM = 0x0001,
            MaxRPM = 0x0002,
            StartingRPM = 0x0004,
            StartingConfirmRPM = 0x0008,
            ChangeUpRPMpS = 0x0010,
            ChangeDownRPMpS = 0x0020,
            RateOfChangeUpRPMpSS = 0x0040,
            RateOfChangeDownRPMpSS = 0x0080,
            MaximalDieselPowerW = 0x0100,
            IdleExhaust = 0x0200,
            MaxExhaust = 0x0400,
            ExhaustDynamics = 0x0800,
            ExhaustColor = 0x1000,
            ExhaustTransientColor = 0x2000,
            DieselPowerTab = 0x4000,
            DieselConsumptionTab = 0x8000,
            ThrottleRPMTab = 0x10000,
            DieselTorqueTab = 0x20000,
            MinOilPressure = 0x40000,
            MaxOilPressure = 0x80000,
            MaxTemperature = 0x100000,
            Cooling = 0x200000,
            TempTimeConstant = 0x400000,
            OptTemperature = 0x800000,
            IdleTemperature = 0x1000000
        }

        public int Id
        {
            get
            {
                return Locomotive.DieselEngines.DEList.IndexOf(this) + 1;
            }
        }

        #region Parameters and variables
        private float dRPM;
        /// <summary>
        /// Actual change rate of the engine's RPM - useful for exhaust effects
        /// </summary>
        public float EngineRPMchangeRPMpS { get { return dRPM; } }
        /// <summary>
        /// Actual RPM of the engine
        /// </summary>
        public float RealRPM;

        /// <summary>
        /// RPM treshold when the engine starts to combust fuel
        /// </summary>
        public float StartingRPM;

        /// <summary>
        /// RPM treshold when the engine is considered as succesfully started
        /// </summary>
        public float StartingConfirmationRPM;

        /// <summary>
        /// GearBox unit
        /// </summary>
        public GearBox GearBox;

        /// <summary>
        /// Parent locomotive
        /// </summary>
        public readonly MSTSDieselLocomotive Locomotive;
        private SettingsFlags initLevel;          //level of initialization
        protected MSTSGearBoxParams GearBoxParams => Locomotive.DieselEngines.MSTSGearBoxParams;

        /// <summary>
        /// Initialization flag - is true when sufficient number of parameters is read succesfully
        /// </summary>
        public bool IsInitialized
        {
            get
            {
                if (initLevel == (SettingsFlags.IdleRPM | SettingsFlags.MaxRPM | SettingsFlags.StartingRPM | SettingsFlags.StartingConfirmRPM | SettingsFlags.ChangeUpRPMpS | SettingsFlags.ChangeDownRPMpS
                    | SettingsFlags.RateOfChangeUpRPMpSS | SettingsFlags.RateOfChangeDownRPMpSS | SettingsFlags.MaximalDieselPowerW | SettingsFlags.IdleExhaust | SettingsFlags.MaxExhaust
                    | SettingsFlags.ExhaustDynamics | SettingsFlags.ExhaustColor | SettingsFlags.ExhaustTransientColor | SettingsFlags.DieselPowerTab | SettingsFlags.DieselConsumptionTab | SettingsFlags.ThrottleRPMTab
                    | SettingsFlags.DieselTorqueTab | SettingsFlags.MinOilPressure | SettingsFlags.MaxOilPressure | SettingsFlags.MaxTemperature | SettingsFlags.Cooling
                    | SettingsFlags.TempTimeConstant | SettingsFlags.OptTemperature | SettingsFlags.IdleTemperature))

                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Engine status
        /// </summary>
        public DieselEngineState State { get; protected set; } = DieselEngineState.Stopped;
        public bool PowerOn => State == DieselEngineState.Running || State == DieselEngineState.Starting;
        /// <summary>
        /// Type of engine cooling
        /// </summary>
        public Cooling EngineCooling = Cooling.Proportional;

        /// <summary>
        /// The RPM controller tries to reach this value
        /// </summary>
        public float DemandedRPM;
        private float demandedThrottlePercent;
        /// <summary>
        /// Demanded throttle percent, usually token from parent locomotive
        /// </summary>
        public float DemandedThrottlePercent { set { demandedThrottlePercent = value > 100f ? 100f : (value < 0 ? 0 : value); } get { return demandedThrottlePercent; } }
        /// <summary>
        /// Idle RPM
        /// </summary>
        public float IdleRPM;
        /// <summary>
        /// Maximal RPM
        /// </summary>
        public float MaxRPM;
        /// <summary>
        /// RPM change rate from ENG file
        /// </summary>
        public float RPMRange;
        /// <summary>
        /// Change rate when accelerating the engine
        /// </summary>
        public float ChangeUpRPMpS;
        /// <summary>
        /// Change rate when decelerating the engine
        /// </summary>
        public float ChangeDownRPMpS;
        /// <summary>
        /// "Jerk" of the RPM when accelerating the engine
        /// </summary>
        public float RateOfChangeUpRPMpSS;
        /// <summary>
        /// "Jerk" of the RPM when decelerating the engine
        /// </summary>
        public float RateOfChangeDownRPMpSS;
        /// <summary>
        /// Maximum Rated Power output of the diesel engine (prime mover)
        /// </summary>
        public float MaximumDieselPowerW;
        /// <summary>
        /// Current power available to the traction motors
        /// </summary>
        public float CurrentDieselOutputPowerW;
        /// <summary>
        /// Maximum power available to the rail
        /// </summary>
        public float MaximumRailOutputPowerW;
        /// <summary>
        /// Real power output of the engine (based upon previous cycle - ie equivalent to Previous Motive Force - to calculate difference in power
        /// </summary>
        public float OutputPowerW;
        /// <summary>
        /// Relative output power to the MaximalPowerW
        /// </summary>
        public float ThrottlePercent { get { return OutputPowerW / MaximumDieselPowerW * 100f; } }
        /// <summary>
        /// Fuel consumed at max power
        /// </summary>
        public float DieselUsedPerHourAtMaxPowerL = 1.0f;
        /// <summary>
        /// Fuel consumed at idle
        /// </summary>
        public float DieselUsedPerHourAtIdleL = 1.0f;
        /// <summary>
        /// Current fuel flow
        /// </summary>
        public float DieselFlowLps;
        /// <summary>
        /// Engine load table - Max output power vs. RPM
        /// </summary>
        public Interpolator DieselPowerTab;
        /// <summary>
        /// Engine consumption table - Consumption vs. RPM
        /// </summary>
        public Interpolator DieselConsumptionTab;
        /// <summary>
        /// Engine throttle settings table - RPM vs. throttle settings
        /// </summary>
        public Interpolator ThrottleRPMTab;
        /// <summary>
        /// Engine throttle settings table - Reverse of RPM vs. throttle settings
        /// </summary>
        public Interpolator ReverseThrottleRPMTab;
        /// <summary>
        /// Throttle setting as calculated from real RpM
        /// </summary>
        public float ApparentThrottleSetting;
        /// <summary>
        /// Engine output torque table - Torque vs. RPM
        /// </summary>
        public Interpolator DieselTorqueTab;
        /// <summary>
        /// Current exhaust number of particles
        /// </summary>
        public float ExhaustParticles = 10.0f;
        /// <summary>
        /// Current exhaust color
        /// </summary>
        public Color ExhaustColor;
        /// <summary>
        /// Exhaust color at steady state (no RPM change)
        /// </summary>
        public Color ExhaustSteadyColor = Color.Gray;
        /// <summary>
        /// Exhaust color when accelerating the engine
        /// </summary>
        public Color ExhaustTransientColor = Color.Black;
        /// <summary>
        /// Exhaust color when decelerating the engine
        /// </summary>
        public Color ExhaustDecelColor = Color.WhiteSmoke;

        public Color ExhaustCompressorBlownColor = Color.Gray;

        public float InitialMagnitude = 1.5f;
        public float MaxMagnitude = 1.5f;
        public float MagnitudeRange;
        public float ExhaustMagnitude = 1.5f;

        public float InitialExhaust = 0.7f;
        public float MaxExhaust = 2.8f;
        public float ExhaustRange;

        public float ExhaustDecelReduction = 0.75f; //Represents the percentage that exhaust will be reduced while engine is decreasing RPMs.
        public float ExhaustAccelIncrease = 2.0f; //Represents the percentage that exhaust will be increased while engine is increasing RPMs.

        public bool DieselEngineConfigured; // flag to indicate that the user has configured a diesel engine prime mover code block in the ENG file
        /// <summary>
        /// Current Engine oil pressure in PSI
        /// </summary>
        public float DieselOilPressurePSI
        {
            get
            {
                float k = (DieselMaxOilPressurePSI - DieselMinOilPressurePSI) / (MaxRPM - IdleRPM);
                float q = DieselMaxOilPressurePSI - k * MaxRPM;
                float res = k * RealRPM + q - dieseloilfailurePSI;
                if (res < 0f)
                    res = 0f;
                return res;
            }
        }
        /// <summary>
        /// Minimal oil pressure at IdleRPM
        /// </summary>
        public float DieselMinOilPressurePSI;
        /// <summary>
        /// Maximal oil pressure at MaxRPM
        /// </summary>
        public float DieselMaxOilPressurePSI;
        /// <summary>
        /// Oil failure/leakage is substracted from the DieselOilPressurePSI
        /// </summary>
        public float dieseloilfailurePSI;              //Intended to be implemented later
        /// <summary>
        /// Actual Engine temperature
        /// </summary>
        public float DieselTemperatureDeg = 40f;
        /// <summary>
        /// Maximal engine temperature
        /// </summary>
        public float DieselMaxTemperatureDeg;
        /// <summary>
        /// Time constant to heat up from zero to 63% of MaxTemperature
        /// </summary>
        public float DieselTempTimeConstantSec = 720f;
        /// <summary>
        /// Optimal temperature of the diesel at rated power
        /// </summary>
        public float DieselOptimalTemperatureDegC = 95f;
        /// <summary>
        /// Steady temperature when idling
        /// </summary>
        public float DieselIdleTemperatureDegC = 75f;
        /// <summary>
        /// Hysteresis of the cooling regulator
        /// </summary>
        public float DieselTempCoolingHyst = 20f;
        /// <summary>
        /// Cooling system indicator
        /// </summary>
        public bool DieselTempCoolingRunning;

        /// <summary>
        /// Load of the engine
        /// </summary>
        public float LoadPercent
        {
            get
            {
                return CurrentDieselOutputPowerW <= 0f ? 0f : ((OutputPowerW + (Locomotive.DieselEngines.NumOfActiveEngines > 0 ? Locomotive.LocomotivePowerSupply.ElectricTrainSupplyPowerW / Locomotive.DieselEngines.NumOfActiveEngines : 0f)) * 100f / CurrentDieselOutputPowerW);
            }
        }
        /// <summary>
        /// The engine is connected to the gearbox
        /// </summary>
        public bool HasGearBox { get { return GearBox != null; } }
        #endregion

        public DieselEngine(MSTSDieselLocomotive locomotive)
        {
            Locomotive = locomotive;
        }

        /// <summary>
        /// Parses parameters from the stf reader
        /// </summary>
        /// <param name="stf">Reference to the stf reader</param>
        /// <param name="loco">Reference to the locomotive</param>
        public virtual void Parse(STFReader stf)
        {
            stf.MustMatch("(");
            bool end = false;
            while (!end)
            {
                string lowercasetoken = stf.ReadItem().ToLower();
                switch (lowercasetoken)
                {
                    case "idlerpm": IdleRPM = stf.ReadFloatBlock(STFReader.Units.None, 0); initLevel |= SettingsFlags.IdleRPM; break;
                    case "maxrpm": MaxRPM = stf.ReadFloatBlock(STFReader.Units.None, 0); initLevel |= SettingsFlags.MaxRPM; break;
                    case "startingrpm": StartingRPM = stf.ReadFloatBlock(STFReader.Units.None, 0); initLevel |= SettingsFlags.StartingRPM; break;
                    case "startingconfirmrpm": StartingConfirmationRPM = stf.ReadFloatBlock(STFReader.Units.None, 0); initLevel |= SettingsFlags.StartingConfirmRPM; break;
                    case "changeuprpmps": ChangeUpRPMpS = stf.ReadFloatBlock(STFReader.Units.None, 0); initLevel |= SettingsFlags.ChangeUpRPMpS; break;
                    case "changedownrpmps": ChangeDownRPMpS = stf.ReadFloatBlock(STFReader.Units.None, 0); initLevel |= SettingsFlags.ChangeDownRPMpS; break;
                    case "rateofchangeuprpmpss": RateOfChangeUpRPMpSS = stf.ReadFloatBlock(STFReader.Units.None, 0); initLevel |= SettingsFlags.RateOfChangeUpRPMpSS; break;
                    case "rateofchangedownrpmpss": RateOfChangeDownRPMpSS = stf.ReadFloatBlock(STFReader.Units.None, 0); initLevel |= SettingsFlags.RateOfChangeDownRPMpSS; break;
                    case "maximalpower": MaximumDieselPowerW = stf.ReadFloatBlock(STFReader.Units.Power, 0); initLevel |= SettingsFlags.MaximalDieselPowerW; break;
                    case "idleexhaust": InitialExhaust = stf.ReadFloatBlock(STFReader.Units.None, 0); initLevel |= SettingsFlags.IdleExhaust; break;
                    case "maxexhaust": MaxExhaust = stf.ReadFloatBlock(STFReader.Units.None, 0); initLevel |= SettingsFlags.MaxExhaust; break;
                    case "exhaustdynamics": ExhaustAccelIncrease = stf.ReadFloatBlock(STFReader.Units.None, 0); initLevel |= SettingsFlags.ExhaustDynamics; break;
                    case "exhaustdynamicsdown": ExhaustDecelReduction = stf.ReadFloatBlock(STFReader.Units.None, null); initLevel |= SettingsFlags.ExhaustDynamics; break;
                    case "exhaustcolor":    ExhaustSteadyColor = stf.ReadColorBlock(Color.Gray); initLevel |= SettingsFlags.ExhaustColor; break;
                    case "exhausttransientcolor": ExhaustTransientColor = stf.ReadColorBlock(Color.Black);initLevel |= SettingsFlags.ExhaustTransientColor; break;
                    case "dieselpowertab": DieselPowerTab = stf.CreateInterpolator(); initLevel |= SettingsFlags.DieselPowerTab; break;
                    case "dieselconsumptiontab": DieselConsumptionTab = stf.CreateInterpolator(); initLevel |= SettingsFlags.DieselConsumptionTab; break;
                    case "throttlerpmtab":
                        ThrottleRPMTab = ThrottleRPMTab = stf.CreateInterpolator();
                        initLevel |= SettingsFlags.ThrottleRPMTab;
                        // This prevents rpm values being exactly the same for different throttle rates, as when this table is reversed, OR is unable to correctly determine a correct apparent throttle value.
                        // TO DO - would be good to be able to handle rpm values the same, and -ve if possible.
                        ThrottleRPMTab.CheckForConsistentIncrease(1.0);
                        break;
                    case "dieseltorquetab": DieselTorqueTab = stf.CreateInterpolator(); initLevel |= SettingsFlags.DieselTorqueTab; break;
                    case "minoilpressure": DieselMinOilPressurePSI = stf.ReadFloatBlock(STFReader.Units.PressureDefaultPSI, 0f); initLevel |= SettingsFlags.MinOilPressure; break;
                    case "maxoilpressure": DieselMaxOilPressurePSI = stf.ReadFloatBlock(STFReader.Units.PressureDefaultPSI, 0f); initLevel |= SettingsFlags.MaxOilPressure; break;
                    case "maxtemperature": DieselMaxTemperatureDeg = stf.ReadFloatBlock(STFReader.Units.Temperature, 0); initLevel |= SettingsFlags.MaxTemperature; break;
                    case "cooling": EngineCooling = (Cooling)stf.ReadIntBlock((int)Cooling.Proportional); initLevel |= SettingsFlags.Cooling; break; //ReadInt changed to ReadIntBlock
                    case "temptimeconstant": DieselTempTimeConstantSec = stf.ReadFloatBlock(STFReader.Units.Time, 0f); initLevel |= SettingsFlags.TempTimeConstant; break;
                    case "opttemperature": DieselOptimalTemperatureDegC = stf.ReadFloatBlock(STFReader.Units.Temperature, 95f); initLevel |= SettingsFlags.OptTemperature; break;
                    case "idletemperature": DieselIdleTemperatureDegC = stf.ReadFloatBlock(STFReader.Units.Temperature, 75f); initLevel |= SettingsFlags.IdleTemperature; break;
                    default:
                        end = true;
                        break;
                }
            }
        }

        public void Copy(DieselEngine other)
        {
            IdleRPM = other.IdleRPM;
            MaxRPM = other.MaxRPM;
            StartingRPM = other.StartingRPM;
            StartingConfirmationRPM = other.StartingConfirmationRPM;
            ChangeUpRPMpS = other.ChangeUpRPMpS;
            ChangeDownRPMpS = other.ChangeDownRPMpS;
            RateOfChangeUpRPMpSS = other.RateOfChangeUpRPMpSS;
            RateOfChangeDownRPMpSS = other.RateOfChangeDownRPMpSS;
            MaximumDieselPowerW = other.MaximumDieselPowerW;
            MaximumRailOutputPowerW = other.MaximumRailOutputPowerW;
            initLevel = other.initLevel;
            DieselPowerTab = new Interpolator(other.DieselPowerTab);
            DieselConsumptionTab = new Interpolator(other.DieselConsumptionTab);
            ThrottleRPMTab = new Interpolator(other.ThrottleRPMTab);
            ReverseThrottleRPMTab = new Interpolator(other.ReverseThrottleRPMTab);
            if (other.DieselTorqueTab != null) DieselTorqueTab = new Interpolator(other.DieselTorqueTab);
            DieselUsedPerHourAtMaxPowerL = other.DieselUsedPerHourAtMaxPowerL;
            DieselUsedPerHourAtIdleL = other.DieselUsedPerHourAtIdleL;
            InitialExhaust = other.InitialExhaust;
            InitialMagnitude = other.InitialMagnitude;
            MaxExhaust = other.MaxExhaust;
            MaxMagnitude = other.MaxMagnitude;
            ExhaustParticles = other.ExhaustParticles;
            ExhaustColor = other.ExhaustColor;
            ExhaustSteadyColor = other.ExhaustSteadyColor;
            ExhaustTransientColor = other.ExhaustTransientColor;
            ExhaustDecelColor = other.ExhaustDecelColor;
            DieselMaxOilPressurePSI = other.DieselMaxOilPressurePSI;
            DieselMinOilPressurePSI = other.DieselMinOilPressurePSI;
            DieselMaxTemperatureDeg = other.DieselMaxTemperatureDeg;
        }

        public void Initialize()
        {
            if (Simulator.Instance.Settings.DieselEngineStart)
            {
                RealRPM = IdleRPM;
                State = DieselEngineState.Running;
            }
            RPMRange = MaxRPM - IdleRPM;
            MagnitudeRange = MaxMagnitude - InitialMagnitude;
            ExhaustRange = MaxExhaust - InitialExhaust;
            ExhaustSteadyColor.A = 10;
            ExhaustDecelColor.A = 10;

            if (GearBoxParams.IsInitialized)
            {
                GearBox = new GearBox(this);
                GearBox.Initialize();
            }
        }

        public void InitializeMoving()
        {
            RealRPM = IdleRPM;
            State = DieselEngineState.Running;

            GearBox?.InitializeMoving();
        }

        public void Update(double elapsedClockSeconds)
        {
            if (Locomotive.DieselPowerSupply.MainPowerSupplyOn)
                DemandedThrottlePercent = Locomotive.ThrottlePercent;
            else
                DemandedThrottlePercent = 0f;

            DemandedThrottlePercent = (float)Math.Max(DemandedThrottlePercent, ReverseThrottleRPMTab[Locomotive.DieselPowerSupply.DieselEngineMinRpm]);

            if (Locomotive.Direction == MidpointDirection.Reverse)
                Locomotive.PrevMotiveForceN *= -1f;

            if ((State == DieselEngineState.Running) && (Locomotive.ThrottlePercent > 0))
            {
                OutputPowerW = (Locomotive.PrevMotiveForceN > 0 ? Locomotive.PrevMotiveForceN * Locomotive.AbsSpeedMpS : 0) / Locomotive.DieselEngines.NumOfActiveEngines;
            }
            else
            {
                OutputPowerW = 0.0f;
            }

            if ((ThrottleRPMTab != null) && (State == DieselEngineState.Running))
            {
                DemandedRPM = (float)ThrottleRPMTab[demandedThrottlePercent];
            }

            if (GearBox != null)
            {
                if (RealRPM > 0)
                    GearBox.ClutchPercent = (RealRPM - GearBox.ShaftRPM) / RealRPM * 100f;
                else
                    GearBox.ClutchPercent = 100f;

                if (GearBox.CurrentGear != null)
                {
                    if (GearBox.IsClutchOn)
                        DemandedRPM = GearBox.ShaftRPM;
                }
            }

            if (RealRPM == IdleRPM)
            {
                ExhaustParticles = InitialExhaust;
                ExhaustMagnitude = InitialMagnitude;
                ExhaustColor = ExhaustSteadyColor;
            }
            if (RealRPM < DemandedRPM)
            {
                dRPM = (float)Math.Min(Math.Sqrt(2 * RateOfChangeUpRPMpSS * (DemandedRPM - RealRPM)), ChangeUpRPMpS);
                if (dRPM > 1.0f) //The forumula above generates a floating point error that we have to compensate for so we can't actually test for zero.
                {
                    ExhaustParticles = (InitialExhaust + ((ExhaustRange * (RealRPM - IdleRPM) / RPMRange))) * ExhaustAccelIncrease;
                    ExhaustMagnitude = (InitialMagnitude + ((MagnitudeRange * (RealRPM - IdleRPM) / RPMRange))) * ExhaustAccelIncrease;
                    ExhaustColor = ExhaustTransientColor;
                }
                else
                {
                    dRPM = 0;
                    ExhaustParticles = InitialExhaust + ((ExhaustRange * (RealRPM - IdleRPM) / RPMRange));
                    ExhaustMagnitude = InitialMagnitude + ((MagnitudeRange * (RealRPM - IdleRPM) / RPMRange));
                    ExhaustColor = ExhaustSteadyColor;
                }
            }
            else if (RealRPM > DemandedRPM)
            {
                dRPM = (float)Math.Max(-Math.Sqrt(2 * RateOfChangeDownRPMpSS * (RealRPM - DemandedRPM)), -ChangeDownRPMpS);
                ExhaustParticles = (InitialExhaust + ((ExhaustRange * (RealRPM - IdleRPM) / RPMRange))) * ExhaustDecelReduction;
                ExhaustMagnitude = (InitialMagnitude + ((MagnitudeRange * (RealRPM - IdleRPM) / RPMRange))) * ExhaustDecelReduction;
                ExhaustColor = ExhaustDecelColor;
            }

            // Uncertain about the purpose of this code piece?? Does there need to be a corresponding code for RateOfChangeUpRPMpSS???
            //            if (DemandedRPM < RealRPM && (OutputPowerW > (1.1f * CurrentDieselOutputPowerW)) && (EngineStatus == Status.Running))
            //            {
            //                dRPM = (CurrentDieselOutputPowerW - OutputPowerW) / MaximumDieselPowerW * 0.01f * RateOfChangeDownRPMpSS;
            //            }
            // Deleted to see what impact it has - was holding rpm artificialy high - http://www.elvastower.com/forums/index.php?/topic/33739-throttle-bug-in-recent-or-builds/page__gopid__256086#entry256086

            RealRPM = (float)Math.Max(RealRPM + dRPM * elapsedClockSeconds, 0);

            // Calculate the apparent throttle setting based upon the current rpm of the diesel prime mover. This allows the Tractive effort to increase with rpm to the throttle setting selected.
            // This uses the reverse Tab of the Throttle vs rpm Tab.
            if ((ReverseThrottleRPMTab != null) && (State == DieselEngineState.Running))
            {
                ApparentThrottleSetting = (float)ReverseThrottleRPMTab[RealRPM];
            }

            ApparentThrottleSetting = MathHelper.Clamp(ApparentThrottleSetting, 0.0f, 100.0f);  // Clamp throttle setting within bounds

            if (DieselPowerTab != null)
            {
                CurrentDieselOutputPowerW = (float)(DieselPowerTab[RealRPM] * (1 - Locomotive.PowerReduction) <= MaximumDieselPowerW * (1 - Locomotive.PowerReduction) ? DieselPowerTab[RealRPM] * (1 - Locomotive.PowerReduction) : MaximumDieselPowerW * (1 - Locomotive.PowerReduction));
            }
            else
            {
                CurrentDieselOutputPowerW = (RealRPM - IdleRPM) / (MaxRPM - IdleRPM) * MaximumDieselPowerW * (1 - Locomotive.PowerReduction);
            }

            if (Locomotive.DieselEngines.NumOfActiveEngines > 0)
            {
                CurrentDieselOutputPowerW -= Locomotive.DieselPowerSupply.ElectricTrainSupplyPowerW / Locomotive.DieselEngines.NumOfActiveEngines;
                CurrentDieselOutputPowerW = CurrentDieselOutputPowerW < 0f ? 0f : CurrentDieselOutputPowerW;
            }

            if (State == DieselEngineState.Starting)
            {
                if ((RealRPM > (0.9f * StartingRPM)) && (RealRPM < StartingRPM))
                {
                    DemandedRPM = 1.1f * StartingConfirmationRPM;
                    ExhaustColor = ExhaustTransientColor;
                    ExhaustParticles = (MaxExhaust - InitialExhaust) / (0.5f * StartingRPM - StartingRPM) * (RealRPM - 0.5f * StartingRPM) + InitialExhaust;
                }
                if ((RealRPM > StartingConfirmationRPM))// && (RealRPM < 0.9f * IdleRPM))
                    State = DieselEngineState.Running;
            }

            if ((State != DieselEngineState.Starting) && (RealRPM == 0f))
                State = DieselEngineState.Stopped;

            if ((State == DieselEngineState.Stopped) || (State == DieselEngineState.Stopping) || ((State == DieselEngineState.Starting) && (RealRPM < StartingRPM)))
            {
                ExhaustParticles = 0;
                DieselFlowLps = 0;
            }
            else
            {
                if (DieselConsumptionTab != null)
                {
                    DieselFlowLps = (float)DieselConsumptionTab[RealRPM] / 3600.0f;
                }
                else
                {
                    if (ThrottlePercent == 0)
                        DieselFlowLps = DieselUsedPerHourAtIdleL / 3600.0f;
                    else
                        DieselFlowLps = ((DieselUsedPerHourAtMaxPowerL - DieselUsedPerHourAtIdleL) * ThrottlePercent / 100f + DieselUsedPerHourAtIdleL) / 3600.0f;
                }
            }

            if (ExhaustParticles > 100f)
                ExhaustParticles = 100f;

            if (Locomotive.PowerReduction == 1 && State != DieselEngineState.Stopped)     // Compressor blown, you get much smoke 
            {
                ExhaustColor = Color.WhiteSmoke;
                ExhaustParticles = 40f;
                ExhaustMagnitude = InitialMagnitude * 2;
            }

            DieselTemperatureDeg += (float)elapsedClockSeconds * (DieselMaxTemperatureDeg - DieselTemperatureDeg) / DieselTempTimeConstantSec;
            switch (EngineCooling)
            {
                case Cooling.NoCooling:
                    DieselTemperatureDeg += (float)elapsedClockSeconds * (LoadPercent * 0.01f * (95f - 60f) + 60f - DieselTemperatureDeg) / DieselTempTimeConstantSec;
                    DieselTempCoolingRunning = false;
                    break;
                case Cooling.Mechanical:
                    DieselTemperatureDeg += (float)elapsedClockSeconds * ((RealRPM - IdleRPM) / (MaxRPM - IdleRPM) * 95f + 60f - DieselTemperatureDeg) / DieselTempTimeConstantSec;
                    DieselTempCoolingRunning = true;
                    break;
                case Cooling.Hysteresis:
                    if (DieselTemperatureDeg > DieselMaxTemperatureDeg)
                        DieselTempCoolingRunning = true;
                    if (DieselTemperatureDeg < (DieselMaxTemperatureDeg - DieselTempCoolingHyst))
                        DieselTempCoolingRunning = false;

                    if (DieselTempCoolingRunning)
                        DieselTemperatureDeg += (float)elapsedClockSeconds * (DieselMaxTemperatureDeg - DieselTemperatureDeg) / DieselTempTimeConstantSec;
                    else
                        DieselTemperatureDeg -= (float)elapsedClockSeconds * (DieselMaxTemperatureDeg - 2f * DieselTempCoolingHyst - DieselTemperatureDeg) / DieselTempTimeConstantSec;
                    break;
                default:
                case Cooling.Proportional:
                    float cooling = (95f - DieselTemperatureDeg) * 0.01f;
                    cooling = cooling < 0f ? 0 : cooling;
                    if (DieselTemperatureDeg >= (80f))
                        DieselTempCoolingRunning = true;
                    if (DieselTemperatureDeg < (80f - DieselTempCoolingHyst))
                        DieselTempCoolingRunning = false;

                    if (!DieselTempCoolingRunning)
                        cooling = 0f;

                    DieselTemperatureDeg += (float)elapsedClockSeconds * (LoadPercent * 0.01f * 95f - DieselTemperatureDeg) / DieselTempTimeConstantSec;
                    if (DieselTemperatureDeg > DieselMaxTemperatureDeg - DieselTempCoolingHyst)
                        DieselTemperatureDeg = DieselMaxTemperatureDeg - DieselTempCoolingHyst;
                    break;
            }
            if (DieselTemperatureDeg < 40f)
                DieselTemperatureDeg = 40f;

            if (GearBox != null)
            {
                if ((Locomotive.IsLeadLocomotive()))
                {
                    if (GearBox.GearBoxOperation == GearBoxOperation.Manual)
                    {
                        if (Locomotive.GearBoxController.CurrentNotch > 0)
                            GearBox.NextGear = GearBox.Gears[Locomotive.GearBoxController.CurrentNotch - 1];
                        else
                            GearBox.NextGear = null;
                    }
                }
                else
                {
                    if (GearBox.GearBoxOperation == GearBoxOperation.Manual)
                    {
                        if (Locomotive.GearboxGearIndex > 0)
                            GearBox.NextGear = GearBox.Gears[Locomotive.GearboxGearIndex - 1];
                        else
                            GearBox.NextGear = null;
                    }
                }
                if (GearBox.CurrentGear == null)
                    OutputPowerW = 0f;

                GearBox.Update(elapsedClockSeconds);
            }
        }

        public void HandleEvent(PowerSupplyEvent evt)
        {
            switch (evt)
            {
                case PowerSupplyEvent.StopEngine:
                    if (State != DieselEngineState.Stopped)
                    {
                        DemandedRPM = 0;
                        State = DieselEngineState.Stopping;
                        if (RealRPM <= 0)
                            State = DieselEngineState.Stopped;
                    }

                    break;

                case PowerSupplyEvent.StartEngine:
                    if (State == DieselEngineState.Stopped || State == DieselEngineState.Stopping)
                    {
                        DemandedRPM = StartingRPM;
                        State = DieselEngineState.Starting;
                    }
                    break;
            }
        }

        public void Restore(BinaryReader inf)
        {
            State = (DieselEngineState)inf.ReadInt32();
            RealRPM = inf.ReadSingle();
            OutputPowerW = inf.ReadSingle();
            DieselTemperatureDeg = inf.ReadSingle();

            bool gearSaved = inf.ReadBoolean();  // read boolean which indicates gear data was saved

            if (gearSaved)
            {
                GearBox = new GearBox(this);
                GearBox.Restore(inf);
            }
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write((int)State);
            outf.Write(RealRPM);
            outf.Write(OutputPowerW);
            outf.Write(DieselTemperatureDeg);
            if (GearBox != null)
            {
                outf.Write(true);
                GearBox.Save(outf);
            }
            else
            {
                outf.Write(false);
            }
        }

        /// <summary>
        /// Fix or define a diesel prime mover engine code block. If the user has not defned a diesel eng, then OR will use this section to create one.
        /// If the user has left a parameter out of the code, then OR uses this section to try and set the missing values to a default value.
        /// Error code has been provided that will provide the user with an indication if a parameter has been left out.
        /// </summary>
        public void InitFromMSTS()
        {
            bool verboseConfigurationMessages = Simulator.Instance.Settings.VerboseConfigurationMessages;
            if ((initLevel & SettingsFlags.IdleRPM) == 0)
            {
                if (DieselEngineConfigured && Locomotive.IdleRPM != 0) // Advanced conf - Prime mover Eng block defined but no IdleRPM present
                {
                    IdleRPM = Locomotive.IdleRPM;
                    if (verboseConfigurationMessages)
                        Trace.TraceInformation("IdleRpM not found in Diesel Engine Prime Mover Configuration (ADVANCED Config): set to default value = {0}", IdleRPM);
                }
                else if (IdleRPM == 0 && Locomotive.IdleRPM != 0) // Basic conf - No prime mover ENG block defined, use the default "MSTS" value
                {
                    IdleRPM = Locomotive.IdleRPM;
                    if (verboseConfigurationMessages)
                        Trace.TraceInformation("IdleRpM: set at default value (BASIC Config) = {0}", IdleRPM);

                }
                else if (Locomotive.IdleRPM == 0) // No default "MSTS" value present, set to arbitary value
                {
                    IdleRPM = 300.0f;
                    Locomotive.IdleRPM = IdleRPM;
                    if (verboseConfigurationMessages)
                        Trace.TraceInformation("IdleRpM not found in Diesel Engine Configuration (BASIC Config): set at arbitary value = {0}", IdleRPM);
                }
            }

            if ((initLevel & SettingsFlags.MaxRPM) == 0)
            {
                if (DieselEngineConfigured && Locomotive.MaxRPM != 0) // Advanced conf - Prime mover Eng block defined but no IdleRPM present
                {
                    MaxRPM = Locomotive.MaxRPM;
                    if (verboseConfigurationMessages)
                        Trace.TraceInformation("MaxRpM not found in Diesel Engine Prime Mover Configuration (ADVANCED Config): set to default value = {0}", MaxRPM);
                }
                else if (MaxRPM == 0 && Locomotive.MaxRPM != 0)  // Basic conf - No prime mover ENG block defined, use the default "MSTS" value
                {
                    MaxRPM = Locomotive.MaxRPM;
                    if (verboseConfigurationMessages)
                        Trace.TraceInformation("MaxRpM: set at default value (BASIC Config) = {0}", MaxRPM);
                }
                else if (Locomotive.MaxRPM == 0) // No default "MSTS" value present, set to arbitary value
                {
                    MaxRPM = 600.0f;
                    Locomotive.MaxRPM = MaxRPM;
                    if (verboseConfigurationMessages)
                        Trace.TraceInformation("MaxRpM not found in Diesel Engine Configuration (BASIC Config): set at arbitary value = {0}", MaxRPM);
                }
            }

            // Undertake a test to ensure that MaxRPM > IdleRPM by a factor of 1.5x
            if (MaxRPM / IdleRPM < 1.5)
            {
                const float RPMFactor = 1.5f;
                MaxRPM = IdleRPM * RPMFactor;
                if (verboseConfigurationMessages)
                {
                    Trace.TraceInformation("MaxRPM < IdleRPM x 1.5, set MaxRPM at arbitary value = {0}", MaxRPM);
                }
            }

            InitialMagnitude = Locomotive.InitialMagnitude;
            MaxMagnitude = Locomotive.MaxMagnitude;
            if ((initLevel & SettingsFlags.MaxExhaust) == 0)
            {
                MaxExhaust = Locomotive.MaxExhaust;
                if (DieselEngineConfigured && verboseConfigurationMessages)
                    Trace.TraceInformation("MaxExhaust not found in Diesel Engine Prime Mover Configuration, set at default value = {0}", MaxExhaust);
            }

            if ((initLevel & SettingsFlags.ExhaustColor) == 0)
            {
                ExhaustSteadyColor = Locomotive.ExhaustSteadyColor;
                if (DieselEngineConfigured && verboseConfigurationMessages)
                    Trace.TraceInformation("ExhaustColour not found in Diesel Engine Prime Mover Configuration, set at default value = {0}", ExhaustSteadyColor);
            }
            ExhaustDecelColor = Locomotive.ExhaustDecelColor;

            if ((initLevel & SettingsFlags.ExhaustTransientColor) == 0)
            {
                ExhaustTransientColor = Locomotive.ExhaustTransientColor;
                if (DieselEngineConfigured && verboseConfigurationMessages)
                    Trace.TraceInformation("ExhaustTransientColour not found in Diesel Engine Prime Mover Configuration, set at default value = {0}", ExhaustTransientColor);
            }

            if ((initLevel & SettingsFlags.StartingRPM) == 0)
            {
                StartingRPM = Locomotive.IdleRPM * 2.0f / 3.0f;
                if (DieselEngineConfigured && verboseConfigurationMessages)
                    Trace.TraceInformation("StartingRpM not found in Diesel Engine Prime Mover Configuration, set at default value = {0}", StartingRPM);
            }

            if ((initLevel & SettingsFlags.StartingConfirmRPM) == 0)
            {
                StartingConfirmationRPM = Locomotive.IdleRPM * 1.1f;
                if (DieselEngineConfigured && verboseConfigurationMessages)
                    Trace.TraceInformation("StartingConfirmRpM not found in Diesel Engine Prime Mover Configuration, set at default value = {0}", StartingConfirmationRPM);
            }

            if ((initLevel & SettingsFlags.ChangeUpRPMpS) == 0)
            {
                if (DieselEngineConfigured && Locomotive.MaxRPMChangeRate != 0) // Advanced conf - Prime mover Eng block defined but no IdleRPM present
                {
                    ChangeUpRPMpS = Locomotive.MaxRPMChangeRate;
                    if (verboseConfigurationMessages)
                        Trace.TraceInformation("ChangeUpRPMpS not found in Diesel Engine Prime Mover Configuration (ADVANCED Config): set to default value = {0}", ChangeUpRPMpS);
                }
                else if (ChangeUpRPMpS == 0 && Locomotive.MaxRPMChangeRate != 0)  // Basic conf - No prime mover ENG block defined, use the default "MSTS" value
                {
                    ChangeUpRPMpS = Locomotive.MaxRPMChangeRate;
                    if (verboseConfigurationMessages)
                        Trace.TraceInformation("ChangeUpRPMpS: set at default value (BASIC Config) = {0}", ChangeUpRPMpS);

                }
                else if (Locomotive.MaxRPMChangeRate == 0) // No default "MSTS" value present, set to arbitary value
                {
                    ChangeUpRPMpS = 40.0f;
                    Locomotive.MaxRPMChangeRate = ChangeUpRPMpS;
                    if (verboseConfigurationMessages)
                        Trace.TraceInformation("ChangeUpRPMpS not found in Diesel Engine Configuration (BASIC Config): set at arbitary value = {0}", ChangeUpRPMpS);
                }
            }

            if ((initLevel & SettingsFlags.ChangeDownRPMpS) == 0)
            {
                if (DieselEngineConfigured && Locomotive.MaxRPMChangeRate != 0) // Advanced conf - Prime mover Eng block defined but no IdleRPM present
                {
                    ChangeDownRPMpS = Locomotive.MaxRPMChangeRate;
                    if (verboseConfigurationMessages)
                        Trace.TraceInformation("ChangeDownRPMpS not found in Diesel Engine Prime Mover Configuration (ADVANCED Config): set to default value = {0}", ChangeDownRPMpS);
                }
                else if (ChangeDownRPMpS == 0 && Locomotive.MaxRPMChangeRate != 0)  // Basic conf - No prime mover ENG block defined, use the default "MSTS" value
                {
                    ChangeDownRPMpS = Locomotive.MaxRPMChangeRate;
                    if (verboseConfigurationMessages)
                        Trace.TraceInformation("ChangeDownRPMpS: set at default value (BASIC Config) = {0}", ChangeDownRPMpS);
                }
                else if (Locomotive.MaxRPMChangeRate == 0) // No default "MSTS" value present, set to arbitary value
                {
                    ChangeDownRPMpS = 40.0f;
                    if (verboseConfigurationMessages)
                        Trace.TraceInformation("ChangeDownRPMpS not found in Diesel Engine Configuration (BASIC Config): set at arbitary value = {0}", ChangeDownRPMpS);
                }
            }

            if ((initLevel & SettingsFlags.RateOfChangeUpRPMpSS) == 0)
            {
                RateOfChangeUpRPMpSS = ChangeUpRPMpS;
                if (DieselEngineConfigured && verboseConfigurationMessages)
                    Trace.TraceInformation("RateofChangeUpRpMpS not found in Diesel Engine Prime Mover Configuration, set at default value = {0}", RateOfChangeUpRPMpSS);
            }

            if ((initLevel & SettingsFlags.RateOfChangeDownRPMpSS) == 0)
            {
                RateOfChangeDownRPMpSS = ChangeDownRPMpS;
                if (DieselEngineConfigured && verboseConfigurationMessages)
                    Trace.TraceInformation("RateofChangeDownRpMpS not found in Diesel Engine Prime Mover Configuration, set at default value = {0}", RateOfChangeDownRPMpSS);
            }

            if ((initLevel & SettingsFlags.MaximalDieselPowerW) == 0)
            {
                if (Locomotive.MaximumDieselEnginePowerW != 0)
                {
                    MaximumDieselPowerW = Locomotive.MaximumDieselEnginePowerW;
                    if (verboseConfigurationMessages)
                        Trace.TraceInformation("MaximalPower not found in Diesel Engine Prime Mover Configuration (ADVANCED Config): set to default value (ORTSDieselEngineMaxPower) = {0}", FormatStrings.FormatPower(MaximumDieselPowerW, Locomotive.IsMetric, false, false));
                }
                else if (Locomotive.MaxPowerW == 0)
                {
                    MaximumDieselPowerW = 2500000;
                    if (verboseConfigurationMessages)
                        Trace.TraceInformation("MaximalPower not found in Diesel Engine Prime Mover Configuration (ADVANCED Config): set at arbitary value = {0}", FormatStrings.FormatPower(MaximumDieselPowerW, Locomotive.IsMetric, false, false));
                }
                else
                {
                    MaximumDieselPowerW = Locomotive.MaxPowerW;
                    if (verboseConfigurationMessages)
                        Trace.TraceInformation("MaximalPower not found in Diesel Engine Prime Mover Configuration (ADVANCED Config): set to default value (MaxPower) = {0}", FormatStrings.FormatPower(MaximumDieselPowerW, Locomotive.IsMetric, false, false));
                }
            }


            if ((initLevel & SettingsFlags.MaxOilPressure) == 0)
            {
                if (DieselEngineConfigured && Locomotive.DieselMaxOilPressurePSI != 0) // Advanced conf - Prime mover Eng block defined but no IdleRPM present
                {
                    DieselMaxOilPressurePSI = Locomotive.DieselMaxOilPressurePSI;
                    if (verboseConfigurationMessages)
                        Trace.TraceInformation("MaxOilPressure not found in Diesel Engine Prime Mover Configuration (ADVANCED Config): set to default value = {0}", DieselMaxOilPressurePSI);
                }
                else if (DieselMaxOilPressurePSI == 0 && Locomotive.DieselMaxOilPressurePSI != 0)  // Basic conf - No prime mover ENG block defined, use the default "MSTS" value
                {
                    DieselMaxOilPressurePSI = Locomotive.DieselMaxOilPressurePSI;
                    if (verboseConfigurationMessages)
                        Trace.TraceInformation("MaxOilPressure: set at default value (BASIC Config) = {0}", DieselMaxOilPressurePSI);
                }
                else if (Locomotive.DieselMaxOilPressurePSI == 0) // No default "MSTS" value present, set to arbitary value
                {
                    DieselMaxOilPressurePSI = 120.0f;
                    Locomotive.DieselMaxOilPressurePSI = DieselMaxOilPressurePSI;
                    if (verboseConfigurationMessages)
                        Trace.TraceInformation("MaxOilPressure not found in Diesel Engine Configuration (BASIC Config): set at arbitary value = {0}", DieselMaxOilPressurePSI);
                }
            }

            if ((initLevel & SettingsFlags.MinOilPressure) == 0)
            {
                if (DieselEngineConfigured && Locomotive.DieselMinOilPressurePSI != 0) // Advanced conf - Prime mover Eng block defined but no IdleRPM present
                {
                    DieselMinOilPressurePSI = Locomotive.DieselMinOilPressurePSI;
                    if (verboseConfigurationMessages)
                        Trace.TraceInformation("MinOilPressure not found in Diesel Engine Prime Mover Configuration (ADVANCED Config): set to default value = {0}", DieselMinOilPressurePSI);
                }
                else if (DieselMinOilPressurePSI == 0 && Locomotive.DieselMinOilPressurePSI != 0)  // Basic conf - No prime mover ENG block defined, use the default "MSTS" value
                {
                    DieselMinOilPressurePSI = Locomotive.DieselMinOilPressurePSI;
                    if (verboseConfigurationMessages)
                        Trace.TraceInformation("MinOilPressure: set at default value (BASIC Config) = {0}", DieselMinOilPressurePSI);
                }
                else if (Locomotive.DieselMinOilPressurePSI == 0) // No default "MSTS" value present, set to arbitary value
                {
                    DieselMinOilPressurePSI = 40.0f;
                    Locomotive.DieselMinOilPressurePSI = DieselMinOilPressurePSI;
                    if (verboseConfigurationMessages)
                        Trace.TraceInformation("MinOilPressure not found in Diesel Engine Configuration (BASIC Config): set at arbitary value = {0}", DieselMinOilPressurePSI);
                }
            }

            if ((initLevel & SettingsFlags.MaxTemperature) == 0)
            {
                if (DieselEngineConfigured && Locomotive.DieselMaxTemperatureDeg != 0) // Advanced conf - Prime mover Eng block defined but no IdleRPM present
                {
                    DieselMaxTemperatureDeg = Locomotive.DieselMaxTemperatureDeg;
                    if (verboseConfigurationMessages)
                        Trace.TraceInformation("MaxTemperature not found in Diesel Engine Prime Mover Configuration (ADVANCED Config): set to default value = {0}", DieselMaxTemperatureDeg);
                }
                else if (DieselMaxTemperatureDeg == 0 && Locomotive.DieselMaxTemperatureDeg != 0)  // Basic conf - No prime mover ENG block defined, use the default "MSTS" value
                {
                    DieselMaxTemperatureDeg = Locomotive.DieselMaxTemperatureDeg;
                    if (verboseConfigurationMessages)
                        Trace.TraceInformation("MaxTemperature: set at default value (BASIC Config) = {0}", DieselMaxTemperatureDeg);
                }
                else if (Locomotive.DieselMaxTemperatureDeg == 0) // No default "MSTS" value present, set to arbitary value
                {
                    DieselMaxTemperatureDeg = 100.0f;
                    Locomotive.DieselMaxTemperatureDeg = DieselMaxTemperatureDeg;
                    if (verboseConfigurationMessages)
                        Trace.TraceInformation("MaxTemperature not found in Diesel Engine Configuration (BASIC Config): set at arbitary value = {0}", DieselMaxTemperatureDeg);
                }
            }

            if ((initLevel & SettingsFlags.Cooling) == 0)
            {
                EngineCooling = Locomotive.DieselEngineCooling;
            }
            // Advise user what cooling method is set
            if (verboseConfigurationMessages)
                Trace.TraceInformation("ORTSDieselCooling, set at default value = {0}", EngineCooling);

            if ((initLevel & SettingsFlags.TempTimeConstant) == 0)
            {
                DieselTempTimeConstantSec = 720f;
                if (DieselEngineConfigured && verboseConfigurationMessages)
                    Trace.TraceInformation("TempTimeConstant not found in Diesel Engine Config, set at arbitary value = {0}", DieselTempTimeConstantSec);
            }

            if ((initLevel & SettingsFlags.DieselConsumptionTab) == 0)
            {
                DieselConsumptionTab = new Interpolator(new double[] { IdleRPM, MaxRPM }, new double[] { Locomotive.DieselUsedPerHourAtIdleL, Locomotive.DieselUsedPerHourAtMaxPowerL });
                if (DieselEngineConfigured && verboseConfigurationMessages)
                    Trace.TraceInformation("DieselConsumptionTab not found in Diesel Engine Config, set at default values");
            }

            if ((initLevel & SettingsFlags.ThrottleRPMTab) == 0)
            {
                ThrottleRPMTab = new Interpolator(new double[] { 0, 100 }, new double[] { IdleRPM, MaxRPM });
                if (DieselEngineConfigured && verboseConfigurationMessages)
                    Trace.TraceInformation("ThrottleRpMTab not found in Diesel Engine Config, set at default values");
            }

            // If diesel power output curves not defined then set to "standard defaults" in ENG file
            // Set defaults for Torque and Power tables if both are not set.
            if (((initLevel & SettingsFlags.DieselTorqueTab) == 0) && ((initLevel & SettingsFlags.DieselPowerTab) == 0))
            {
                int count = 11;
                double[] rpm = new double[count + 1];
                double[] power = new double[] { 0.02034f, 0.09302f, 0.36628f, 0.60756f, 0.69767f, 0.81395f, 0.93023f, 0.9686f, 0.99418f, 0.99418f, 1f, 0.5f };
                double[] torque = new double[] { 0.05f, 0.2f, 0.7f, 0.95f, 1f, 1f, 0.98f, 0.95f, 0.9f, 0.86f, 0.81f, 0.3f };

                for (int i = 0; i < count; i++)
                {
                    if (i == 0)
                        rpm[i] = IdleRPM;
                    else
                        rpm[i] = rpm[i - 1] + (MaxRPM - IdleRPM) / (count - 1);
                    power[i] *= MaximumDieselPowerW;
                    torque[i] *= MaximumDieselPowerW / (MaxRPM * 2f * 3.1415f / 60f) / 0.81f;
                }
                rpm[count] = MaxRPM * 1.5f;
                DieselPowerTab = new Interpolator(rpm, power);
                DieselTorqueTab = new Interpolator(rpm, torque);
                if (DieselEngineConfigured)
                {
                    if (verboseConfigurationMessages)
                    {
                        Trace.TraceInformation("DieselPowerTab not found in Diesel Engine Config (ADVANCED Config): constructed from default values");
                        Trace.TraceInformation("DieselTorqueTab not found in Diesel Engine Config (ADVANCED Config): constructed from default values");
                    }
                }
                else
                {
                    if (verboseConfigurationMessages)
                    {
                        Trace.TraceInformation("DieselPowerTab constructed from default values (BASIC Config)");
                        Trace.TraceInformation("DieselTorqueTab constructed from default values (BASIC Config)");
                    }
                }
            }

            // Set defaults for Torque table if it is not set.
            if (((initLevel & SettingsFlags.DieselTorqueTab) == 0) && ((initLevel & SettingsFlags.DieselPowerTab) == SettingsFlags.DieselPowerTab))
            {
                double[] rpm = new double[DieselPowerTab.Size];
                double[] torque = new double[DieselPowerTab.Size];
                for (int i = 0; i < DieselPowerTab.Size; i++)
                {
                    rpm[i] = IdleRPM + i * (MaxRPM - IdleRPM) / DieselPowerTab.Size;
                    torque[i] = DieselPowerTab[rpm[i]] / (rpm[i] * 2 * Math.PI / 60.0);
                }
                if (DieselEngineConfigured)
                {
                    if (verboseConfigurationMessages)
                    {
                        Trace.TraceInformation("DieselTorqueTab not found in Diesel Engine Config (ADVANCED Config): constructed from default values");
                    }
                }
                else
                {
                    if (verboseConfigurationMessages)
                    {
                        Trace.TraceInformation("DieselTorqueTab constructed from default values (BASIC Config)");
                    }
                }
            }

            // Set defaults for Power table if it is not set.
            if (((initLevel & SettingsFlags.DieselTorqueTab) == SettingsFlags.DieselTorqueTab) && ((initLevel & SettingsFlags.DieselPowerTab) == 0))
            {
                double[] rpm = new double[DieselPowerTab.Size];
                double[] power = new double[DieselPowerTab.Size];
                for (int i = 0; i < DieselPowerTab.Size; i++)
                {
                    rpm[i] = IdleRPM + i * (MaxRPM - IdleRPM) / DieselPowerTab.Size;
                    power[i] = DieselPowerTab[rpm[i]] * rpm[i] * 2 * Math.PI / 60.0;
                }
                if (DieselEngineConfigured)
                {
                    if (verboseConfigurationMessages)
                    {
                        Trace.TraceInformation("DieselPowerTab not found in Diesel Engine Config (ADVANCED Config): constructed from default values");
                    }
                }
                else
                {
                    if (verboseConfigurationMessages)
                    {
                        Trace.TraceInformation("DieselPowerTab constructed from default values (BASIC Config)");
                    }
                }
            }

            if (Locomotive.MaximumDieselEnginePowerW == 0 && DieselPowerTab != null)
            {
                Locomotive.MaximumDieselEnginePowerW = (float)DieselPowerTab[MaxRPM];
                if (verboseConfigurationMessages)
                    Trace.TraceInformation("Maximum Diesel Engine Prime Mover Power set by DieselPowerTab {0} value", FormatStrings.FormatPower(DieselPowerTab[MaxRPM], Locomotive.IsMetric, false, false));
            }
            // Check whether this code check is really required.
            if (MaximumRailOutputPowerW == 0 && Locomotive.MaxPowerW != 0)
            {
                MaximumRailOutputPowerW = Locomotive.MaxPowerW; // set rail power to a default value on the basis that of the value specified in the MaxPowrW parameter
            }
            else
            {
                MaximumRailOutputPowerW = 0.8f * MaximumDieselPowerW; // set rail power on the basis that it is about 80% of the prime mover output power
            }

            InitialExhaust = Locomotive.InitialExhaust;
            MaxExhaust = Locomotive.MaxExhaust;

        }

        public void InitDieselRailPowers(MSTSDieselLocomotive loco)
        {
            // Set up the reverse ThrottleRPM table. This is used to provide an apparent throttle setting to the Tractive Force calculation, and allows the diesel engine to control the up/down time of 
            // tractive force. This table should be creeated with all locomotives, as they will either use (create) a default ThrottleRPM table, or the user will enter one. 

            if (ThrottleRPMTab != null)
            {
                ReverseThrottleRPMTab = Interpolator.InverseInterpolator(ThrottleRPMTab); // create reverse table
            }

            // TODO - this value needs to be divided by the number of diesel engines in the locomotive

            // Set MaximumRailOutputPower if not already set
            if (MaximumRailOutputPowerW == 0)
            {
                if (loco.TractiveForceCurves != null)
                {
                    float ThrottleSetting = 1;
                    MaximumRailOutputPowerW = (float)loco.TractiveForceCurves.Get(ThrottleSetting, loco.SpeedOfMaxContinuousForceMpS) * loco.SpeedOfMaxContinuousForceMpS;
                    if (Simulator.Instance.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("Maximum Rail Output Power set by Diesel Traction Curves {0} value", FormatStrings.FormatPower(MaximumRailOutputPowerW, loco.IsMetric, false, false));
                }
                else if (loco.MaxPowerW != 0)
                {
                    MaximumRailOutputPowerW = loco.MaxPowerW; // set rail power to a default value on the basis that of the value specified in the MaxPowerW parameter
                }
                else
                {
                    MaximumRailOutputPowerW = 0.8f * MaximumDieselPowerW; // set rail power to a default value on the basis that it is about 80% of the prime mover output power
                }
            }

            // Check MaxRpM for loco as it is needed as well
            if (loco.MaxRPM == 0)
            {
                if (MaxRPM != 0)
                {
                    loco.MaxRPM = MaxRPM;
                }
                else
                {
                    loco.MaxRPM = 600.0f;
                }
            }
        }
    }
}
