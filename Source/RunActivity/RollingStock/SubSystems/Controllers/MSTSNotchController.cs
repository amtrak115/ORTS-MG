﻿// COPYRIGHT 2010, 2011, 2012, 2013 by the Open Rails project.
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
using System.Diagnostics;   // needed for Debug
using System.IO;
using Microsoft.Xna.Framework;
using MSTS.Parsers;
using ORTS.Scripting.Api;
using ORTS.Viewer3D.Popups;  // needed for Confirmations

namespace ORTS
{
    public class MSTSNotch {
        public float Value;
        public bool Smooth;
        public ControllerState Type;
        public MSTSNotch(float v, int s, string type, STFReader stf)
        {
            Value = v;
            Smooth = s == 0 ? false : true;
            Type = ControllerState.Dummy;
            string lower = type.ToLower();
            if (lower.StartsWith("trainbrakescontroller"))
                lower = lower.Substring(21);
            if (lower.StartsWith("enginebrakescontroller"))
                lower = lower.Substring(22);
            switch (lower)
            {
                case "dummy": break;
                case ")": break;
                case "releasestart": Type = ControllerState.Release; break;
                case "fullquickreleasestart": Type = ControllerState.FullQuickRelease; break;
                case "runningstart": Type = ControllerState.Running; break;
                case "selflapstart": Type = ControllerState.SelfLap; break;
                case "holdstart": Type = ControllerState.Lap; break;
                case "holdlappedstart": Type = ControllerState.Lap; break;
                case "neutralhandleoffstart": Type = ControllerState.Neutral; break;
                case "graduatedselflaplimitedstart": Type = ControllerState.GSelfLap; break;
                case "graduatedselflaplimitedholdingstart": Type = ControllerState.GSelfLapH; break;
                case "applystart": Type = ControllerState.Apply; break;
                case "continuousservicestart": Type = ControllerState.ContServ; break;
                case "suppressionstart": Type = ControllerState.Suppression; break;
                case "fullservicestart": Type = ControllerState.FullServ; break;
                case "emergencystart": Type = ControllerState.Emergency; break;
                case "epapplystart": Type = ControllerState.EPApply; break;
                case "epholdstart": Type = ControllerState.Lap; break;
                case "minimalreductionstart": Type = ControllerState.Lap; break;
                default:
                    STFException.TraceInformation(stf, "Skipped unknown notch type " + type);
                    break;
            }
        }
        public MSTSNotch(float v, bool s, int t)
        {
            Value = v;
            Smooth = s;
            Type = (ControllerState)t;
        }

        public MSTSNotch(MSTSNotch other)
        {
            Value = other.Value;
            Smooth = other.Smooth;
            Type = other.Type;
        }

        public MSTSNotch(BinaryReader inf)
        {
            Value = inf.ReadSingle();
            Smooth = inf.ReadBoolean();
            Type = (ControllerState)inf.ReadInt32();
        }

        public MSTSNotch Clone()
        {
            return new MSTSNotch(this);
        }

        public string GetName()
        {
            return ControllerStateDictionary.Dict[Type];
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(Value);
            outf.Write(Smooth);
            outf.Write((int)Type);
        }
    }

    /**
     * This is the most used controller. The main use is for diesel locomotives' Throttle control.
     * 
     * It is used with single keypress, this means that when the user press a key, only the keydown event is handled.
     * The user need to press the key multiple times to update this controller.
     * 
     */
    public class MSTSNotchController: IController
    {
        public float CurrentValue { get; set; }
        public float IntermediateValue;
        public float MinimumValue;
        public float MaximumValue = 1;
        public float StepSize;
        private List<MSTSNotch> Notches = new List<MSTSNotch>();
        public int CurrentNotch;

        //Does not need to persist
        //this indicates if the controller is increasing or decreasing, 0 no changes
        public float UpdateValue { get; set; }
        private float? controllerTarget;
        public double CommandStartTime { get; set; }

        #region CONSTRUCTORS

        public MSTSNotchController()
        {
        }

        public MSTSNotchController(int numOfNotches)
        {
            MinimumValue = 0;
            MaximumValue = numOfNotches - 1;
            StepSize = 1;
            for (int i = 0; i < numOfNotches; i++)
                Notches.Add(new MSTSNotch(i, false, 0));
        }

        public MSTSNotchController(float min, float max, float stepSize)
        {
            MinimumValue = min;
            MaximumValue = max;
            StepSize = stepSize;
        }

        public MSTSNotchController(MSTSNotchController other)
        {
            CurrentValue = other.CurrentValue;
            IntermediateValue = other.IntermediateValue;
            MinimumValue = other.MinimumValue;
            MaximumValue = other.MaximumValue;
            StepSize = other.StepSize;
            CurrentNotch = other.CurrentNotch;

            foreach (MSTSNotch notch in other.Notches)
            {
                Notches.Add(notch.Clone());
            }
        }

        public MSTSNotchController(STFReader stf)
        {
            Parse(stf);
        }

        public MSTSNotchController(List<MSTSNotch> notches)
        {
            Notches = notches;
        }
        #endregion

        public virtual IController Clone()
        {
            return new MSTSNotchController(this);
        }

        public virtual bool IsValid()
        {
            return StepSize != 0;
        }

        public void Parse(STFReader stf)
        {
            stf.MustMatch("(");
            MinimumValue = stf.ReadFloat(STFReader.UNITS.None, null);
            MaximumValue = stf.ReadFloat(STFReader.UNITS.None, null);
            StepSize = stf.ReadFloat(STFReader.UNITS.None, null);
            IntermediateValue = CurrentValue = stf.ReadFloat(STFReader.UNITS.None, null);
            string token = stf.ReadItem(); // s/b numnotches
            if (string.Compare(token, "NumNotches", true) != 0) // handle error in gp38.eng where extra parameter provided before NumNotches statement 
                stf.ReadItem();
            stf.MustMatch("(");
            stf.ReadInt(null);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("notch", ()=>{
                    stf.MustMatch("(");
                    float value = stf.ReadFloat(STFReader.UNITS.None, null);
                    int smooth = stf.ReadInt(null);
                    string type = stf.ReadString();
                    Notches.Add(new MSTSNotch(value, smooth, type, stf));
                    if (type != ")") stf.SkipRestOfBlock();
                }),
            });
            SetValue(CurrentValue);
        }

        public int NotchCount()
        {
            return Notches.Count;
        }

        static float GetNotchBoost()
        {            
            return 5;
        }

        public void SetValue(float v)
        {
            CurrentValue = IntermediateValue = MathHelper.Clamp(v, MinimumValue, MaximumValue);

            for (CurrentNotch = Notches.Count - 1; CurrentNotch > 0; CurrentNotch--)
            {
                if (Notches[CurrentNotch].Value <= CurrentValue)
                    break;
            }

            if (CurrentNotch >= 0 && !Notches[CurrentNotch].Smooth)
                CurrentValue = Notches[CurrentNotch].Value;
        }

        public float SetPercent(float percent)
        {
            float v = (MinimumValue < 0 && percent < 0 ? -MinimumValue : MaximumValue) * percent / 100;
            CurrentValue = MathHelper.Clamp(v, MinimumValue, MaximumValue);

            if (CurrentNotch >= 0)
            {
                if (Notches[Notches.Count - 1].Type == ControllerState.Emergency)
                    v = Notches[Notches.Count - 1].Value * percent / 100;
                for (; ; )
                {
                    MSTSNotch notch = Notches[CurrentNotch];
                    if (CurrentNotch > 0 && v < notch.Value)
                    {
                        MSTSNotch prev = Notches[CurrentNotch-1];
                        if (!notch.Smooth && !prev.Smooth && v - prev.Value > .45 * (notch.Value - prev.Value))
                            break;
                        CurrentNotch--;
                        continue;
                    }
                    if (CurrentNotch < Notches.Count - 1)
                    {
                        MSTSNotch next = Notches[CurrentNotch + 1];
                        if (next.Type != ControllerState.Emergency)
                        {
                            if ((notch.Smooth || next.Smooth) && v < next.Value)
                                break;
                            if (!notch.Smooth && !next.Smooth && v - notch.Value < .55 * (next.Value - notch.Value))
                                break;
                            CurrentNotch++;
                            continue;
                        }
                    }
                    break;
                }
                if (Notches[CurrentNotch].Smooth)
                    CurrentValue = v;
                else
                    CurrentValue = Notches[CurrentNotch].Value;
            }
            IntermediateValue = CurrentValue;
            return 100 * CurrentValue;
        }

        public void StartIncrease( float? target ) {
            controllerTarget = target;
            StartIncrease();
        }

        public void StartIncrease()
        {
            UpdateValue = 1;

            // When we have notches and the current Notch does not require smooth, we go directly to the next notch
            if ((Notches.Count > 0) && (CurrentNotch < Notches.Count - 1) && (!Notches[CurrentNotch].Smooth))
            {
                ++CurrentNotch;
                IntermediateValue = CurrentValue = Notches[CurrentNotch].Value;
            }
		}

        public void StopIncrease()
        {
            UpdateValue = 0;
        }

        public void StartDecrease( float? target ) {
            controllerTarget = target;
            StartDecrease();
        }
        
        public void StartDecrease()
        {
            UpdateValue = -1;

            //If we have notches and the previous Notch does not require smooth, we go directly to the previous notch
            if ((Notches.Count > 0) && (CurrentNotch > 0) && SmoothMin() == null)
            {
                //Keep intermediate value with the "previous" notch, so it will take a while to change notches
                //again if the user keep holding the key
                IntermediateValue = Notches[CurrentNotch].Value;
                CurrentNotch--;
                CurrentValue = Notches[CurrentNotch].Value;
            }
        }

        public void StopDecrease()
        {
            UpdateValue = 0;
        }

        public float Update(float elapsedSeconds)
        {
            if (UpdateValue == 1 || UpdateValue == -1)
            {
                CheckControllerTargetAchieved();
                UpdateValues(elapsedSeconds, UpdateValue);
            }
            return CurrentValue;
        }

        /// <summary>
        /// If a target has been set, then stop once it's reached and also cancel the target.
        /// </summary>
        public void CheckControllerTargetAchieved() {
            if( controllerTarget != null ) {
                if( UpdateValue > 0.0 ) {
                    if( CurrentValue >= controllerTarget ) {
                        StopIncrease();
                        controllerTarget = null;
                    }
                } else {
                    if( CurrentValue <= controllerTarget ) {
                        StopDecrease();
                        controllerTarget = null;
                    }
                }
            }
        }

        private float UpdateValues(float elapsedSeconds, float direction)
        {
            //We increment the intermediate value first
            IntermediateValue += StepSize * elapsedSeconds * GetNotchBoost() * direction;
            IntermediateValue = MathHelper.Clamp(IntermediateValue, MinimumValue, MaximumValue);

            //Do we have notches
            if (Notches.Count > 0)
            {
                //Increasing, check if the notch has changed
                if ((direction > 0) && (CurrentNotch < Notches.Count - 1) && (IntermediateValue >= Notches[CurrentNotch + 1].Value))
                {
                    // Prevent TrainBrake to continuously switch to emergency
                    if (Notches[CurrentNotch + 1].Type == ControllerState.Emergency)
                        IntermediateValue = Notches[CurrentNotch + 1].Value - StepSize;
                    else
                        CurrentNotch++;
                }
                //decreasing, again check if the current notch has changed
                else if((direction < 0) && (CurrentNotch > 0) && (IntermediateValue < Notches[CurrentNotch].Value))
                {
                    CurrentNotch--;
                }

                //If the notch is smooth, we use intermediate value that is being update smooth thought the frames
                if (Notches[CurrentNotch].Smooth)
                    CurrentValue = IntermediateValue;
                else
                    CurrentValue = Notches[CurrentNotch].Value;
            }
            else
            {
                //if no notches, we just keep updating the current value directly
                CurrentValue = IntermediateValue;
            }
            return CurrentValue;
        }

        public float GetNotchFraction()
        {
            if (Notches.Count == 0)
                return 0;
            MSTSNotch notch = Notches[CurrentNotch];
            if (!notch.Smooth)
                return 1;
            float x = 1;
            if (CurrentNotch + 1 < Notches.Count)
                x = Notches[CurrentNotch + 1].Value;
            x = (CurrentValue - notch.Value) / (x - notch.Value);
            if (notch.Type == ControllerState.Release)
                x = 1 - x;
            return x;
        }

        public float? SmoothMin()
        {
            float? target = null;
            if (Notches.Count > 0)
            {
                if (CurrentNotch > 0 && Notches[CurrentNotch - 1].Smooth)
                    target = Notches[CurrentNotch - 1].Value;
                else if (Notches[CurrentNotch].Smooth && CurrentValue > Notches[CurrentNotch].Value)
                    target = Notches[CurrentNotch].Value;
            }
            else
                target = MinimumValue;
            return target;
        }

        public float? SmoothMax()
        {
            float? target = null;
            if (Notches.Count > 0 && CurrentNotch < Notches.Count - 1 && Notches[CurrentNotch].Smooth)
                target = Notches[CurrentNotch + 1].Value;
            else if (Notches.Count == 0
                || (Notches.Count == 1 && Notches[CurrentNotch].Smooth))
                target = MaximumValue;
            return target;
        }

        public virtual string GetStatus()
        {
            if (Notches.Count == 0)
                return string.Format("{0:F0}%", 100 * CurrentValue);
            MSTSNotch notch = Notches[CurrentNotch];
            if (!notch.Smooth && notch.Type == ControllerState.Dummy)
                return string.Format("{0:F0}%", 100 * CurrentValue);
            if (!notch.Smooth)
                return notch.GetName();
            if (notch.GetName().Length > 0)
                return string.Format("{0} {1:F0}%", notch.GetName(), 100 * GetNotchFraction());
            return string.Format("{0:F0}%", 100 * GetNotchFraction());
        }

        public virtual void Save(BinaryWriter outf)
        {
            outf.Write((int)ControllerTypes.MSTSNotchController);

            this.SaveData(outf);
        }

        protected virtual void SaveData(BinaryWriter outf)
        {            
            outf.Write(CurrentValue);            
            outf.Write(MinimumValue);
            outf.Write(MaximumValue);
            outf.Write(StepSize);
            outf.Write(CurrentNotch);            
            outf.Write(Notches.Count);
            
            foreach(MSTSNotch notch in Notches)
            {
                notch.Save(outf);                
            }            
        }

        public virtual void Restore(BinaryReader inf)
        {
            Notches.Clear();

            IntermediateValue = CurrentValue = inf.ReadSingle();            
            MinimumValue = inf.ReadSingle();
            MaximumValue = inf.ReadSingle();
            StepSize = inf.ReadSingle();
            CurrentNotch = inf.ReadInt32();

            UpdateValue = 0;

            int count = inf.ReadInt32();

            for (int i = 0; i < count; ++i)
            {
                Notches.Add(new MSTSNotch(inf));
            }           
        }

        public MSTSNotch GetCurrentNotch()
        {
            return Notches.Count == 0 ? null : Notches[CurrentNotch];
        }

        protected void SetCurrentNotch(ControllerState type)
        {
            for (int i = 0; i < Notches.Count; i++)
            {
                if (Notches[i].Type == type)
                {
                    CurrentNotch = i;
                    CurrentValue = Notches[i].Value;

                    break;
                }
            }
        }

    }
}
