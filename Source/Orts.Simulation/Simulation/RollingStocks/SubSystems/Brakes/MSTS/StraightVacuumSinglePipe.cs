﻿// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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

using Microsoft.Xna.Framework;

using Orts.Common;
using Orts.Common.Calc;

namespace Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS
{
    public class StraightVacuumSinglePipe : VacuumSinglePipe
    {
        private float decreaseSoundTriggerBandwidth;
        private float increaseSoundTriggerBandwidth;

        public StraightVacuumSinglePipe(TrainCar car)
            : base(car)
        {

        }

        public override void Initialize(bool handbrakeOn, float maxVacuumInHg, float fullServVacuumInHg, bool immediateRelease)
        {
            CylPressurePSIA = BrakeLine1PressurePSI = (float)Pressure.Vacuum.ToPressure(fullServVacuumInHg);
            HandbrakePercent = handbrakeOn & (Car as MSTSWagon).HandBrakePresent ? 100 : 0;
            VacResPressurePSIA = (float)Pressure.Vacuum.ToPressure(maxVacuumInHg); // Only used if car coupled to auto braked locomotive
        }

        public override void InitializeMoving() // used when initial speed > 0
        {

            BrakeLine1PressurePSI = (float)Pressure.Vacuum.ToPressure(Car.Train.EqualReservoirPressurePSIorInHg);
            CylPressurePSIA = (float)Pressure.Vacuum.ToPressure(Car.Train.EqualReservoirPressurePSIorInHg);
            VacResPressurePSIA = (float)Pressure.Vacuum.ToPressure(Car.Train.EqualReservoirPressurePSIorInHg); // Only used if car coupled to auto braked locomotive
            HandbrakePercent = 0;
        }


        // Principal Reference Materials for these two brake configurations -
        // Eames brake system - https://babel.hathitrust.org/cgi/pt?id=nnc1.cu50580116&view=1up&seq=7
        // Hardy brake system - https://archive.org/details/hardysvacuumbre00belcgoog

        public override void Update(double elapsedClockSeconds)
        {
            // Two options are allowed for in this module -
            // i) Straight Brake operation - lead BP pressure, and brkae cylinder pressure are calculated in this module. BP pressure is reversed compared to vacuum brake, as vacuum 
            // creation applies the brake cylinder. Some functions, such as brake pipe propagation are handled in the vacuum single pipe module, with some functions in the vacuum brake 
            // module disabled if the lead locomotive is straight braked. 
            // ii) Vacuum brake operation - some cars could operate as straight braked cars (ie non auto), or as auto depending upon what type of braking system the locomotive had. In 
            // this case cars required an auxiliary reservoir. OR senses this and if a straight braked car is coupled to a auto (vacuum braked) locomotive, and it has an auxilary 
            // reservoir fitted then it will use the vacuum single pipe module to manage brakes. In this case relevant straight brake functions are disabled in this module.

            if (Car.Train.LeadLocomotive is MSTSLocomotive lead)
            {
                // Adjust brake cylinder pressures as brake pipe varies
                // straight braked cars will have separate calculations done, if locomotive is not straight braked, then revert car to vacuum single pipe  
                if (lead.CarBrakeSystemType == "straight_vacuum_single_pipe")
                {
                    (Car as MSTSWagon).NonAutoBrakePresent = true; // Set flag to indicate that non auto brake is set in train
                    bool skiploop;

                    // In hardy brake system, BC on tender and locomotive is not changed in the StrBrkApply brake position
                    if ((Car.WagonType == MSTSWagon.WagonTypes.Engine || Car.WagonType == MSTSWagon.WagonTypes.Tender) && (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.StraightApply || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.StraightLap))
                    {
                        skiploop = true;
                    }
                    else
                    {
                        skiploop = false;
                    }

                    if (!skiploop)
                    {
                        if (BrakeLine1PressurePSI < CylPressurePSIA && lead.BrakeFlagIncrease) // Increase BP pressure, hence vacuum brakes are being released
                        {
                            double dp = elapsedClockSeconds * MaxReleaseRatePSIpS;
                            double vr = NumBrakeCylinders * BrakeCylVolM3 / BrakePipeVolumeM3;
                            if (CylPressurePSIA - dp < BrakeLine1PressurePSI + dp * vr)
                                dp = (CylPressurePSIA - BrakeLine1PressurePSI) / (1 + vr);
                            CylPressurePSIA -= (float)dp;

                        }
                        else if (BrakeLine1PressurePSI > CylPressurePSIA && lead.BrakeFlagDecrease)  // Decrease BP pressure, hence vacuum brakes are being applied
                        {
                            double dp = elapsedClockSeconds * MaxApplicationRatePSIpS;
                            double vr = NumBrakeCylinders * BrakeCylVolM3 / BrakePipeVolumeM3;
                            if (CylPressurePSIA + dp > BrakeLine1PressurePSI - dp * vr)
                                dp = (BrakeLine1PressurePSI - CylPressurePSIA) / (1 + vr);
                            CylPressurePSIA += (float)dp;
                        }
                    }


                    // Record HUD display values for brake cylidners depending upon whether they are wagons or locomotives/tenders (which are subject to their own engine brakes)   
                    if (Car.WagonType == MSTSWagon.WagonTypes.Engine || Car.WagonType == MSTSWagon.WagonTypes.Tender)
                    {
                        Car.Train.HUDLocomotiveBrakeCylinderPSI = CylPressurePSIA;
                        Car.Train.HUDWagonBrakeCylinderPSI = Car.Train.HUDLocomotiveBrakeCylinderPSI;  // Initially set Wagon value same as locomotive, will be overwritten if a wagon is attached
                    }
                    else
                    {
                        // Record the Brake Cylinder pressure in first wagon, as EOT is also captured elsewhere, and this will provide the two extremeties of the train
                        // Identifies the first wagon based upon the previously identified UiD 
                        if (Car.UiD == Car.Train.FirstCarUiD)
                        {
                            Car.Train.HUDWagonBrakeCylinderPSI = CylPressurePSIA; // In Vacuum HUD BP is actually supposed to be dispalayed
                        }
                    }

                    // Adjust braking force as brake cylinder pressure varies.
                    float f;
                    if (!Car.BrakesStuck)
                    {

                        float brakecylinderfraction = ((OneAtmospherePSI - CylPressurePSIA) / MaxForcePressurePSI);
                        brakecylinderfraction = MathHelper.Clamp(brakecylinderfraction, 0, 1);

                        f = Car.MaxBrakeForceN * brakecylinderfraction;

                        if (f < Car.MaxHandbrakeForceN * HandbrakePercent / 100)
                            f = Car.MaxHandbrakeForceN * HandbrakePercent / 100;
                    }
                    else
                    {
                        f = Math.Max(Car.MaxBrakeForceN, Car.MaxHandbrakeForceN / 2);
                    }
                    Car.BrakeRetardForceN = f * Car.BrakeShoeRetardCoefficientFrictionAdjFactor; // calculates value of force applied to wheel, independent of wheel skid
                    if (Car.BrakeSkid) // Test to see if wheels are skiding due to excessive brake force
                    {
                        Car.BrakeForceN = f * Car.SkidFriction;   // if excessive brakeforce, wheel skids, and loses adhesion
                    }
                    else
                    {
                        Car.BrakeForceN = f * Car.BrakeShoeCoefficientFrictionAdjFactor; // In advanced adhesion model brake shoe coefficient varies with speed, in simple odel constant force applied as per value in WAG file, will vary with wheel skid.
                    }

                    // If wagons are not attached to the locomotive, then set wagon BC pressure to same as locomotive in the Train brake line
                    if (!Car.Train.WagonsAttached && (Car.WagonType == MSTSWagon.WagonTypes.Engine || Car.WagonType == MSTSWagon.WagonTypes.Tender))
                    {
                        Car.Train.HUDWagonBrakeCylinderPSI = CylPressurePSIA;
                    }

                    // sound trigger checking runs every 4th update, to avoid the problems caused by the jumping BrakeLine1PressurePSI value, and also saves cpu time :)
                    if (SoundTriggerCounter >= 4)
                    {
                        SoundTriggerCounter = 0;

                        if (Math.Abs(CylPressurePSIA - prevCylPressurePSIA) > 0.001)
                        {
                            if (!TrainBrakePressureChanging)
                            {

                                if (CylPressurePSIA < prevCylPressurePSIA && lead.BrakeFlagIncrease && CylPressurePSIA > increaseSoundTriggerBandwidth)  // Brake cylinder vacuum increases as pressure in pipe decreases
                                {
                                    Car.SignalEvent(TrainEvent.TrainBrakePressureIncrease);
                                    TrainBrakePressureChanging = true;
                                }
                                else if (CylPressurePSIA > prevCylPressurePSIA && lead.BrakeFlagDecrease && CylPressurePSIA < decreaseSoundTriggerBandwidth) // Brake cylinder vacuum decreases as pressure in pipe increases

                                {
                                    Car.SignalEvent(TrainEvent.TrainBrakePressureDecrease);
                                    TrainBrakePressureChanging = true;
                                }
                            }

                        }
                        else if (TrainBrakePressureChanging)
                        {
                            Car.SignalEvent(TrainEvent.TrainBrakePressureStoppedChanging);
                            TrainBrakePressureChanging = false;
                        }
                        prevCylPressurePSIA = CylPressurePSIA;


                        if (Math.Abs(BrakeLine1PressurePSI - prevBrakePipePressurePSI) > 0.001)
                        {
                            if (!BrakePipePressureChanging)
                            {
                                if (BrakeLine1PressurePSI < prevBrakePipePressurePSI && lead.BrakeFlagIncrease && BrakeLine1PressurePSI > increaseSoundTriggerBandwidth) // Brakepipe vacuum increases as pressure in pipe decreases
                                {
                                    Car.SignalEvent(TrainEvent.BrakePipePressureIncrease);
                                    BrakePipePressureChanging = true;
                                }
                                else if (BrakeLine1PressurePSI > prevBrakePipePressurePSI && lead.BrakeFlagDecrease && BrakeLine1PressurePSI < decreaseSoundTriggerBandwidth) // Brakepipe vacuum decreases as pressure in pipe increases
                                {
                                    Car.SignalEvent(TrainEvent.BrakePipePressureDecrease);
                                    BrakePipePressureChanging = true;
                                }
                            }

                        }
                        else if (BrakePipePressureChanging)
                        {
                            Car.SignalEvent(TrainEvent.BrakePipePressureStoppedChanging);
                            BrakePipePressureChanging = false;
                        }
                        prevBrakePipePressurePSI = BrakeLine1PressurePSI;

                    }
                    SoundTriggerCounter++;

                    // Straight brake is opposite of automatic brake, ie vacuum pipe goes from 14.503psi (0 InHg - Release) to 2.24 (25InHg - Apply)

                    // Calculate train pipe pressure at lead locomotive.

                    // Vaccum brake effectiveness decreases with increases in altitude because the atmospheric pressure increases as altitude increases.
                    // The formula for decrease in pressure:  P = P0 * Exp (- Mgh/RT) - https://www.math24.net/barometric-formula/

                    float massearthair = 0.02896f; // Molar mass of Earth's air = M = 0.02896 kg/mol
                                                   // float sealevelpressure = 101325f; // Average sea level pressure = P0 = 101,325 kPa
                    float sealevelpressure = 101325f; // Average sea level pressure = P0 = 101,325 kPa
                    float gravitationalacceleration = 9.807f; // Gravitational acceleration = g = 9.807 m/s^2
                    float standardtemperature = 288.15f; // Standard temperature = T = 288.15 K
                    float universalgasconstant = 8.3143f; // Universal gas constant = R = 8.3143 (N*m/mol*K)

                    float alititudereducedvacuum = sealevelpressure * (float)Math.Exp((-1.0f * massearthair * gravitationalacceleration * Car.CarHeightAboveSeaLevelM) / (standardtemperature * universalgasconstant));

                    float vacuumreductionfactor = alititudereducedvacuum / sealevelpressure;

                    float MaxVacuumPipeLevelPSI = lead.TrainBrakeController.MaxPressurePSI * vacuumreductionfactor;

                    // To stop sound triggers "bouncing" near end of increase/decrease operation a small dead (bandwith) zone is introduced where triggers will not change state
                    decreaseSoundTriggerBandwidth = OneAtmospherePSI - 0.2f;
                    increaseSoundTriggerBandwidth = (OneAtmospherePSI - MaxVacuumPipeLevelPSI) + 0.2f;

                    // Set value for large ejector to operate - it will depend upon whether the locomotive is a single or twin ejector unit.
                    float LargeEjectorChargingRateInHgpS;
                    if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.StraightApply)
                    {
                        LargeEjectorChargingRateInHgpS = lead.LargeEjectorBrakePipeChargingRatePSIorInHgpS;
                    }
                    else
                    {
                        LargeEjectorChargingRateInHgpS = lead.BrakePipeChargingRatePSIorInHgpS; // Single ejector model
                    }

                    float SmallEjectorChargingRateInHgpS = lead.SmallEjectorBrakePipeChargingRatePSIorInHgpS; // Set value for small ejector to operate - fraction set in steam locomotive

                    // Calculate adjustment times for varying lengths of trains

                    float AdjLargeEjectorChargingRateInHgpS = (float)(Size.Volume.FromFt3(200.0f) / Car.Train.TotalTrainBrakeSystemVolumeM3) * LargeEjectorChargingRateInHgpS;
                    float AdjSmallEjectorChargingRateInHgpS = (float)(Size.Volume.FromFt3(200.0f) / Car.Train.TotalTrainBrakeSystemVolumeM3) * SmallEjectorChargingRateInHgpS;

                    float AdjBrakeServiceTimeFactorS = (float)(Size.Volume.FromFt3(200.0f) / Car.Train.TotalTrainBrakeSystemVolumeM3) * lead.BrakeServiceTimeFactorS;
                    float AdjTrainPipeLeakLossPSI = (float)(Car.Train.TotalTrainBrakeSystemVolumeM3 / Size.Volume.FromFt3(200.0f)) * lead.TrainBrakePipeLeakPSIorInHgpS;

                    // Only adjust lead pressure when locomotive car is processed, otherwise lead pressure will be "over adjusted"
                    if (Car == lead)
                    {

                        // Hardy brake system
                        if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.StraightApply || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.StraightApplyAll)
                        {
                            lead.BrakeFlagIncrease = true;
                            lead.BrakeFlagDecrease = false;

                            // Apply brakes - brakepipe has to have vacuum increased to max vacuum value (ie decrease psi), vacuum is created by large ejector control
                            lead.BrakeSystem.BrakeLine1PressurePSI -= (float)elapsedClockSeconds * AdjLargeEjectorChargingRateInHgpS;
                            if (lead.BrakeSystem.BrakeLine1PressurePSI < (OneAtmospherePSI - MaxVacuumPipeLevelPSI))
                            {
                                lead.BrakeSystem.BrakeLine1PressurePSI = OneAtmospherePSI - MaxVacuumPipeLevelPSI;
                            }
                            // turn ejector on as required
                            lead.LargeSteamEjectorIsOn = true;
                            lead.LargeEjectorSoundOn = true;

                            // turn small ejector off
                            lead.SmallSteamEjectorIsOn = false;
                            lead.SmallEjectorSoundOn = false;
                        }

                        if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.StraightEmergency)
                        {

                            lead.BrakeFlagIncrease = true;
                            lead.BrakeFlagDecrease = false;

                            // Apply brakes - brakepipe has to have vacuum increased to max vacuum value (ie decrease psi), vacuum is created by large ejector control
                            lead.BrakeSystem.BrakeLine1PressurePSI -= (float)elapsedClockSeconds * (AdjLargeEjectorChargingRateInHgpS + AdjSmallEjectorChargingRateInHgpS);
                            if (lead.BrakeSystem.BrakeLine1PressurePSI < (OneAtmospherePSI - MaxVacuumPipeLevelPSI))
                            {
                                lead.BrakeSystem.BrakeLine1PressurePSI = OneAtmospherePSI - MaxVacuumPipeLevelPSI;
                            }
                            // turn ejectors on as required
                            lead.LargeSteamEjectorIsOn = true;
                            lead.LargeEjectorSoundOn = true;

                            lead.SmallSteamEjectorIsOn = true;
                            lead.SmallEjectorSoundOn = true;
                        }

                        if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.StraightLap)
                        {

                            // turn ejectors off if not required
                            lead.LargeSteamEjectorIsOn = false;
                            lead.LargeEjectorSoundOn = false;

                            lead.SmallSteamEjectorIsOn = false;
                            lead.SmallEjectorSoundOn = false;

                        }

                        // Eames type brake with separate release and ejector operating handles
                        if (lead.LargeEjectorControllerFitted && lead.LargeSteamEjectorIsOn)
                        {
                            // Apply brakes - brakepipe has to have vacuum increased to max vacuum value (ie decrease psi), vacuum is created by large ejector control
                            lead.BrakeSystem.BrakeLine1PressurePSI -= (float)elapsedClockSeconds * AdjLargeEjectorChargingRateInHgpS;
                            if (lead.BrakeSystem.BrakeLine1PressurePSI < (OneAtmospherePSI - MaxVacuumPipeLevelPSI))
                            {
                                lead.BrakeSystem.BrakeLine1PressurePSI = OneAtmospherePSI - MaxVacuumPipeLevelPSI;
                            }
                            lead.BrakeFlagIncrease = true;
                        }
                        // Release brakes - brakepipe has to have brake pipe decreased back to atmospheric pressure to apply brakes (ie psi increases).
                        if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.StraightReleaseOn || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.StraightRelease)
                        {
                            lead.BrakeFlagIncrease = false;
                            lead.BrakeFlagDecrease = true;

                            lead.BrakeSystem.BrakeLine1PressurePSI *= (float)(1 + elapsedClockSeconds / AdjBrakeServiceTimeFactorS); ;
                            if (lead.BrakeSystem.BrakeLine1PressurePSI > OneAtmospherePSI)
                            {
                                lead.BrakeSystem.BrakeLine1PressurePSI = OneAtmospherePSI;
                            }
                        }

                        // leaks in train pipe will reduce vacuum (increase pressure)
                        lead.BrakeSystem.BrakeLine1PressurePSI += (float)elapsedClockSeconds * AdjTrainPipeLeakLossPSI;

                        // Keep brake line within relevant limits - ie between 21 or 25 InHg and Atmospheric pressure.
                        lead.BrakeSystem.BrakeLine1PressurePSI = MathHelper.Clamp(lead.BrakeSystem.BrakeLine1PressurePSI, OneAtmospherePSI - MaxVacuumPipeLevelPSI, OneAtmospherePSI);

                    }
                }

                if (((lead.CarBrakeSystemType == "vacuum_single_pipe" || lead.CarBrakeSystemType == "vacuum_twin_pipe") && (Car as MSTSWagon).AuxiliaryReservoirPresent))
                {
                    // update non calculated values using vacuum single pipe class
                    base.Update(elapsedClockSeconds);
                }
            }
        }

        // This overides the information for each individual wagon in the extended HUD  
        public override string[] GetDebugStatus(Dictionary<BrakeSystemComponent, Pressure.Unit> units)
        {

            if (!(Car as MSTSWagon).NonAutoBrakePresent)
            {
                // display as a automatic vacuum brake

                return new string[] {
                "1V",
                FormatStrings.FormatPressure(Pressure.Vacuum.FromPressure(CylPressurePSIA), Pressure.Unit.InHg, Pressure.Unit.InHg, true),
                FormatStrings.FormatPressure(Pressure.Vacuum.FromPressure(BrakeLine1PressurePSI), Pressure.Unit.InHg, Pressure.Unit.InHg, true),
                FormatStrings.FormatPressure(Pressure.Vacuum.FromPressure(VacResPressureAdjPSIA()), Pressure.Unit.InHg, Pressure.Unit.InHg, true),
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                HandbrakePercent > 0 ? $"{HandbrakePercent:F0}%" : string.Empty,
                FrontBrakeHoseConnected? "I" : "T",
                $"A{(AngleCockAOpen? "+" : "-")} B{(AngleCockBOpen? "+" : "-")}",
                };
            }
            else
            {
                // display as a straight vacuum brake

                return new string[] {
                "1VS",
                FormatStrings.FormatPressure(Pressure.Vacuum.FromPressure(CylPressurePSIA), Pressure.Unit.InHg, Pressure.Unit.InHg, true),
                FormatStrings.FormatPressure(Pressure.Vacuum.FromPressure(BrakeLine1PressurePSI), Pressure.Unit.InHg, Pressure.Unit.InHg, true),
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                HandbrakePercent > 0 ? $"{HandbrakePercent:F0}%" : string.Empty,
                FrontBrakeHoseConnected? "I" : "T",
                $"A{(AngleCockAOpen? "+" : "-")} B{(AngleCockBOpen? "+" : "-")}",
                };
            }
        }

    }
}