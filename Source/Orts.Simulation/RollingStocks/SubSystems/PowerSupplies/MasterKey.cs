// COPYRIGHT 2020 by the Open Rails project.
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
using System.Linq;
using System.Threading.Tasks;

using FreeTrainSimulator.Common;
using FreeTrainSimulator.Common.Api;
using FreeTrainSimulator.Models.Imported.State;

using Orts.Formats.Msts.Parsers;
using Orts.Scripting.Api;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{
    public class MasterKey : ISubSystem<MasterKey>, ISaveStateApi<CommandSwitchSaveState>
    {
        // Enums
        public enum ModeType
        {
            AlwaysOn,
            Manual
        }
        
        // Parameters
        public ModeType Mode { get; protected set; } = ModeType.AlwaysOn;
        public float DelayS { get; protected set; }
        public bool HeadlightControl;

        // Variables
        private readonly MSTSLocomotive Locomotive;
        protected Timer Timer;
        public bool CommandSwitch { get; protected set; }
        public bool On { get; protected set; }
        public bool OtherCabInUse {
            get
            {
                foreach (MSTSLocomotive locomotive in Locomotive.Train.Cars.OfType<MSTSLocomotive>())
                {
                    if (locomotive != Locomotive && locomotive.LocomotivePowerSupply.MasterKey.On)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public MasterKey(MSTSLocomotive locomotive)
        {
            Locomotive = locomotive;

            Timer = new Timer(Simulator.Instance);
            Timer.Setup(DelayS);
        }

        public virtual void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortsmasterkey(mode":
                    string text = stf.ReadStringBlock("").ToLower();
                    if (text == "alwayson")
                    {
                        Mode = ModeType.AlwaysOn;
                    }
                    else if (text == "manual")
                    {
                        Mode = ModeType.Manual;
                    }
                    else
                    {
                        STFException.TraceWarning(stf, "Skipped invalid master key mode");
                    }
                    break;

                case "engine(ortsmasterkey(delayoff":
                    DelayS = stf.ReadFloatBlock(STFReader.Units.Time, 0f);
                    break;

                case "engine(ortsmasterkey(headlightcontrol":
                    HeadlightControl = stf.ReadBoolBlock(false);
                    break;
            }
        }

        public void Copy(MasterKey source)
        {
            Mode = source.Mode;
            DelayS = source.DelayS;
            HeadlightControl = source.HeadlightControl;
        }

        public virtual void Initialize()
        {
        }

        /// <summary>
        /// Initialization when simulation starts with moving train
        /// </summary>
        public virtual void InitializeMoving()
        {
            if (Locomotive.IsLeadLocomotive())
            {
                CommandSwitch = true;
                On = true;
            }
        }

        public ValueTask<CommandSwitchSaveState> Snapshot()
        {
            return ValueTask.FromResult(new CommandSwitchSaveState()
            {
                CommandSwitch = CommandSwitch,
                State = On,
            });
        }

        public ValueTask Restore(CommandSwitchSaveState saveState)
        {
            ArgumentNullException.ThrowIfNull(saveState, nameof(saveState));

            CommandSwitch = saveState.CommandSwitch;
            On = saveState.State;

            return ValueTask.CompletedTask;
        }

        public virtual void Update(double elapsedClockSeconds)
        {
            switch (Mode)
            {
                case ModeType.AlwaysOn:
                    On = true;
                    break;

                case ModeType.Manual:
                    if (On)
                    {
                        if (!CommandSwitch)
                        {
                            if (!Timer.Started)
                            {
                                Timer.Start();
                            }

                            if (Timer.Triggered)
                            {
                                On = false;
                                Timer.Stop();
                            }
                        }
                        else
                        {
                            if (Timer.Started)
                            {
                                Timer.Stop();
                            }
                        }
                    }
                    else
                    {
                        if (CommandSwitch)
                        {
                            On = true;
                        }
                    }
                    break;
            }

            if (HeadlightControl)
            {
                if (On && Locomotive.LocomotivePowerSupply.LowVoltagePowerSupplyOn && Locomotive.Headlight == HeadLightState.HeadlightOff)
                {
                    Locomotive.Headlight = HeadLightState.HeadlightDimmed;
                }
                else if ((!On || !Locomotive.LocomotivePowerSupply.LowVoltagePowerSupplyOn) && Locomotive.Headlight > HeadLightState.HeadlightOff)
                {
                    Locomotive.Headlight = HeadLightState.HeadlightOff;
                }
            }
        }

        public virtual void HandleEvent(PowerSupplyEvent evt)
        {
            switch (evt)
            {
                case PowerSupplyEvent.TurnOnMasterKey:
                    if (Mode == ModeType.Manual)
                    {
                        CommandSwitch = true;
                        Locomotive.SignalEvent(TrainEvent.MasterKeyOn);
                    }
                    break;

                case PowerSupplyEvent.TurnOffMasterKey:
                    if (Mode == ModeType.Manual)
                    {
                        CommandSwitch = false;
                        Locomotive.SignalEvent(TrainEvent.MasterKeyOff);
                    }
                    break;
            }
        }
    }
}
