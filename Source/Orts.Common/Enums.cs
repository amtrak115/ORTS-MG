﻿// COPYRIGHT 2009, 2011 by the Open Rails project.
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

using System.ComponentModel;

namespace Orts.Common
{
    [Description("Reverser")]
    public enum Direction
    {
        [Description("Reverse")] Reverse = -1,
        [Description("N")] N = 0,
        [Description("Forward")] Forward = 1,
    }

    [Description("Rotation")]
    public enum Rotation
    {
        CounterClockwise = -1,
        None = 0,
        Clockwise = 1,
    }

    [Description("Separator")]
    public enum SeparatorChar
    {
        [Description("Comma")] Comma = ',',
        [Description("Semicolon")] Semicolon = ';',
        [Description("Tab")] Tab = '\t',
        [Description("Space")] Space = ' '
    };

    [Description("Measurement units Preference")]
    public enum MeasurementUnit
    {
        [Description("Route")] Route,
        [Description("Player's location")] System,
        [Description("Metric")] Metric,
        [Description("Imperial US")] US,
        [Description("Imperial UK")] UK,
    }

    [Description("Pressure units Preference")]
    public enum PressureUnit
    {
        [Description("Automatic")] Automatic,
        /// <summary>bar</summary>
        [Description("bar")] Bar,
        /// <summary>Pounds Per Square Inch</summary>
        [Description("psi")] Psi,
        /// <summary>Inches Mercury</summary>
        [Description("inHg")] InHg,
        /// <summary>Mass-force per square centimetres</summary>
        [Description("kgf/cm²")] Kgfcm2,
    }

    public enum ClockType
    {
        Unknown,
        Analog,
        Digital,
    }

    public enum TrackMonitorSignalAspect
    {
        None,
        Clear_2,
        Clear_1,
        Approach_3,
        Approach_2,
        Approach_1,
        Restricted,
        StopAndProceed,
        Stop,
        Permission,
    }

    public enum CabSetting
    {
        Name,       // name of control
        Off,        // 2 or 3 state control/reset/initialise
        Neutral,    // 2 or 3 state control
        On,         // 2 or 3 state control/apply/change
        Decrease,   // continuous control
        Increase,   // continuous control
        Warn1,
        Warn2,
        Range1,     // sub-range
        Range2,
        Range3,
        Range4,
    }

    public enum ConfirmLevel
    {
        [Description("None")] None,
        [Description("Information")] Information,
        [Description("Warning")] Warning,
        [Description("Error")] Error,
        [Description("Message")] Message,
    };

    #region CabControl
    public enum CabControl
    {
        None,
        // Power
        Reverser,
        Throttle,
        Wheelslip,
        // Electric Power
        Power,
        Pantograph1,
        Pantograph2,
        Pantograph3,
        Pantograph4,
        CircuitBreakerClosingOrder,
        CircuitBreakerOpeningOrder,
        CircuitBreakerClosingAuthorization,
        // Diesel Power
        PlayerDiesel,
        HelperDiesel,
        DieselFuel,
        SteamHeatBoilerWater,
        // Steam power
        SteamLocomotiveReverser,
        Regulator,
        Injector1,
        Injector2,
        BlowdownValve,
        Blower,
        SteamHeat,
        Damper,
        FireboxDoor,
        FiringRate,
        FiringIsManual,
        FireShovelfull,
        CylinderCocks,
        CylinderCompound,
        LargeEjector,
        SmallEjector,
        VacuumExhauster,
        TenderCoal,
        TenderWater,
        // General
        WaterScoop,
        // Braking
        TrainBrake,
        EngineBrake,
        DynamicBrake,
        EmergencyBrake,
        BailOff,
        InitializeBrakes,
        Handbrake,
        Retainers,
        BrakeHose,
        // Cab Devices
        Sander,
        Alerter,
        Horn,
        Whistle,
        Bell,
        Headlight,
        CabLight,
        Wipers,
        ChangeCab,
        Odometer,
        // Train Devices
        DoorsLeft,
        DoorsRight,
        Mirror,
        // Track Devices
        SwitchAhead,
        SwitchBehind,
        // Simulation
        SimulationSpeed,
        Uncouple,
        Activity,
        Replay,
        GearBox,
        SignalMode,
        // Freight Load
        FreightLoad,
        CabRadio,
    }
    #endregion

    public enum TrainEvent
    {
        None,
        BellOff,
        BellOn,
        BlowerChange,
        BrakesStuck,
        CabLightSwitchToggle,
        CabRadioOn,
        CabRadioOff,
        CircuitBreakerOpen,
        CircuitBreakerClosing,
        CircuitBreakerClosed,
        CircuitBreakerClosingOrderOff,
        CircuitBreakerClosingOrderOn,
        CircuitBreakerOpeningOrderOff,
        CircuitBreakerOpeningOrderOn,
        CircuitBreakerClosingAuthorizationOff,
        CircuitBreakerClosingAuthorizationOn,
        CompressorOff,
        CompressorOn,
        ControlError,
        Couple,
        CoupleB, // NOTE: Currently not used in Open Rails.
        CoupleC, // NOTE: Currently not used in Open Rails.
        CrossingClosing,
        CrossingOpening,
        CylinderCocksToggle,
        CylinderCompoundToggle,
        DamperChange,
        Derail1, // NOTE: Currently not used in Open Rails.
        Derail2, // NOTE: Currently not used in Open Rails.
        Derail3, // NOTE: Currently not used in Open Rails.
        DoorClose,
        DoorOpen,
        DynamicBrakeChange,
        DynamicBrakeIncrease, // NOTE: Currently not used in Open Rails.
        DynamicBrakeOff,
        EngineBrakeChange,
        EngineBrakePressureDecrease,
        EngineBrakePressureIncrease,
        EnginePowerOff,
        EnginePowerOn,
        FireboxDoorChange,
        FireboxDoorOpen,
        FireboxDoorClose,
        FuelTowerDown,
        FuelTowerTransferEnd,
        FuelTowerTransferStart,
        FuelTowerUp,
        GearDown,
        GearUp,
        GenericEvent1,
        GenericEvent2,
        GenericEvent3,
        GenericEvent4,
        GenericEvent5,
        GenericEvent6,
        GenericEvent7,
        GenericEvent8,
        HornOff,
        HornOn,
        LargeEjectorChange,
        LightSwitchToggle,
        MirrorClose,
        MirrorOpen,
        MovingTableMovingEmpty,
        MovingTableMovingLoaded,
        MovingTableStopped,
        Pantograph1Down,
        PantographToggle,
        Pantograph1Up,
        Pantograph2Down,
        Pantograph2Up,
        Pantograph3Down,
        Pantograph3Up,
        Pantograph4Down,
        Pantograph4Up,
        PermissionDenied,
        PermissionGranted,
        PermissionToDepart,
        ReverserChange,
        ReverserToForwardBackward,
        ReverserToNeutral,
        SanderOff,
        SanderOn,
        SemaphoreArm,
        SmallEjectorChange,
        WaterInjector1Off,
        WaterInjector1On,
        WaterInjector2Off,
        WaterInjector2On,
        BlowdownValveToggle,
        SteamHeatChange,
        SteamPulse1,
        SteamPulse2,
        SteamPulse3,
        SteamPulse4,
        SteamPulse5,
        SteamPulse6,
        SteamPulse7,
        SteamPulse8,
        SteamPulse9,
        SteamPulse10,
        SteamPulse11,
        SteamPulse12,
        SteamPulse13,
        SteamPulse14,
        SteamPulse15,
        SteamPulse16,
        SteamSafetyValveOff,
        SteamSafetyValveOn,
        ThrottleChange,
        TrainBrakeChange,
        TrainBrakePressureDecrease,
        TrainBrakePressureIncrease,
        TrainControlSystemActivate,
        TrainControlSystemAlert1,
        TrainControlSystemAlert2,
        TrainControlSystemDeactivate,
        TrainControlSystemInfo1,
        TrainControlSystemInfo2,
        TrainControlSystemPenalty1,
        TrainControlSystemPenalty2,
        TrainControlSystemWarning1,
        TrainControlSystemWarning2,
        Uncouple,
        UncoupleB, // NOTE: Currently not used in Open Rails.
        UncoupleC, // NOTE: Currently not used in Open Rails.
        VacuumExhausterOn,
        VacuumExhausterOff,
        VigilanceAlarmOff,
        VigilanceAlarmOn,
        VigilanceAlarmReset,
        WaterScoopDown,
        WaterScoopUp,
        WiperOff,
        WiperOn,
        _HeadlightDim,
        _HeadlightOff,
        _HeadlightOn,
        _ResetWheelSlip,

        TrainBrakePressureStoppedChanging,
        EngineBrakePressureStoppedChanging,
        BrakePipePressureIncrease,
        BrakePipePressureDecrease,
        BrakePipePressureStoppedChanging,
        CylinderCocksOpen,
        CylinderCocksClose,
        SecondEnginePowerOff,
        SecondEnginePowerOn,

        HotBoxBearingOn,
        HotBoxBearingOff,

        BoilerBlowdownOn,
        BoilerBlowdownOff,

        SteamGearLeverToggle,
        AIFiremanSoundOn,
        AIFiremanSoundOff,

        GearPosition0,
        GearPosition1,
        GearPosition2,
        GearPosition3,
        GearPosition4,
        GearPosition5,
        GearPosition6,
        GearPosition7,
        GearPosition8,
    }

    public enum PowerSupplyEvent
    {
        RaisePantograph,
        LowerPantograph,
        CloseCircuitBreaker,
        OpenCircuitBreaker,
        CloseCircuitBreakerButtonPressed,
        CloseCircuitBreakerButtonReleased,
        OpenCircuitBreakerButtonPressed,
        OpenCircuitBreakerButtonReleased,
        GiveCircuitBreakerClosingAuthorization,
        RemoveCircuitBreakerClosingAuthorization,
        StartEngine,
        StopEngine,
        ClosePowerContactor,
        OpenPowerContactor,
        GivePowerContactorClosingAuthorization,
        RemovePowerContactorClosingAuthorization
    }

    [Description("PowerSupply")]
    public enum PowerSupplyState
    {
        [Description("Off")] PowerOff,
        [Description("On ongoing")] PowerOnOngoing,
        [Description("On")] PowerOn
    }

    [Description("Pantograph")]
    public enum PantographState
    {
        [Description("Down")] Down,
        [Description("Lowering")] Lowering,
        [Description("Raising")] Raising,
        [Description("Up")] Up
    }

    [Description("CircuitBreaker")]
    public enum CircuitBreakerState
    {
        [Description("Open")] Open,
        [Description("Closing")] Closing,
        [Description("Closed")] Closed
    }

    public enum BrakeControllerEvent
    {
        /// <summary>
        /// Starts the pressure increase (may have a target value)
        /// </summary>
        StartIncrease,
        /// <summary>
        /// Stops the pressure increase
        /// </summary>
        StopIncrease,
        /// <summary>
        /// Starts the pressure decrease (may have a target value)
        /// </summary>
        StartDecrease,
        /// <summary>
        /// Stops the pressure decrease
        /// </summary>
        StopDecrease,
        /// <summary>
        /// Sets the value of the brake controller using a RailDriver peripheral (must have a value)
        /// </summary>
        SetCurrentPercent,
        /// <summary>
        /// Sets the current value of the brake controller (must have a value)
        /// </summary>
        SetCurrentValue,
        /// <summary>
        /// Starts a full quick brake release.
        /// </summary>
        FullQuickRelease,
        /// <summary>
        /// Starts a pressure decrease to zero (may have a target value)
        /// </summary>
        StartDecreaseToZero
    }

    //TrainBrakesController
    [Description("Brake Controller")]
    public enum ControllerState
    {
        // MSTS values (DO NOT CHANGE THE ORDER !)
        [Description("")]
        Dummy,              // Dummy
        [Description("Release")]
        Release,            // ReleaseStart 
        [Description("Quick Release")]
        FullQuickRelease,   // FullQuickReleaseStart
        [Description("Running")]
        Running,            // RunningStart 
        [Description("Neutral")]
        Neutral,            // NeutralhandleOffStart
        [Description("Self Lap")]
        SelfLap,            // SelfLapStart 
        [Description("Lap")]
        Lap,                // HoldLapStart 
        [Description("Apply")]
        Apply,              // ApplyStart 
        [Description("EPApply")]
        EPApply,            // EPApplyStart 
        [Description("Service")]
        GSelfLap,           // GraduatedSelfLapLimitedStart
        [Description("Service")]
        GSelfLapH,          // GraduatedSelfLapLimitedHoldStart
        [Description("Suppression")]
        Suppression,        // SuppressionStart 
        [Description("Cont. Service")]
        ContServ,           // ContinuousServiceStart 
        [Description("Full Service")]
        FullServ,           // FullServiceStart 
        [Description("Emergency")]
        Emergency,          // EmergencyStart

        // Extra MSTS values
        [Description("Minimum Reduction")]
        MinimalReduction,  // MinimalReductionStart,
        [Description("Hold")]
        Hold,                   // HoldStart

        // OR values
        [Description("Overcharge")]
        Overcharge,         // Overcharge
        [Description("Emergency Braking Push Button")]
        EBPB,               // Emergency Braking Push Button
        [Description("TCS Emergency Braking")]
        TCSEmergency,       // TCS Emergency Braking
        [Description("TCS Full Service Braking")]
        TCSFullServ,        // TCS Full Service Braking
        [Description("Vac. Cont. Service")]
        VacContServ,         // VacuumContinuousServiceStart
        [Description("Vac. Apply Cont.Service")]
        VacApplyContServ,    // TrainBrakesControllerVacuumApplyContinuousServiceStart
        [Description("Notch")]
        BrakeNotch           // EngineBrakesControllerBrakeNotchStart
    }

    public enum TrainControlMode
    {
        AutoSignal,
        AutoNode,
        Manual,
        Explorer,
        OutOfControl,
        Inactive,
        TurnTable,
        Undefined,
    }


    public enum TCSEvent
    {
        /// <summary>
        /// Reset request by pressing the alerter button.
        /// </summary>
        AlerterPressed,
        /// <summary>
        /// Alerter button was released.
        /// </summary>
        AlerterReleased,
        /// <summary>
        /// Internal reset request by touched systems other than the alerter button.
        /// </summary>
        AlerterReset,
        /// <summary>
        /// Internal reset request by the reverser.
        /// </summary>
        ReverserChanged,
        /// <summary>
        /// Internal reset request by the throttle controller.
        /// </summary>
        ThrottleChanged,
        /// <summary>
        /// Internal reset request by the gear box controller.
        /// </summary>
        GearBoxChanged,
        /// <summary>
        /// Internal reset request by the train brake controller.
        /// </summary>
        TrainBrakeChanged,
        /// <summary>
        /// Internal reset request by the engine brake controller.
        /// </summary>
        EngineBrakeChanged,
        /// <summary>
        /// Internal reset request by the dynamic brake controller.
        /// </summary>
        DynamicBrakeChanged,
        /// <summary>
        /// Internal reset request by the horn handle.
        /// </summary>
        HornActivated,
        /// <summary>
        /// Generic TCS button pressed.
        /// </summary>
        GenericTCSButtonPressed,
        /// <summary>
        /// Generic TCS button released.
        /// </summary>
        GenericTCSButtonReleased,
        /// <summary>
        /// Circuit breaker has been closed.
        /// </summary>
        CircuitBreakerClosed,
        /// <summary>
        /// Circuit breaker has been opened.
        /// </summary>
        CircuitBreakerOpen,
        /// <summary>
        /// Save request.
        /// </summary>
        Save,
        /// <summary>
        /// Restore request.
        /// </summary>
        Restore,
        /// Generic TCS switch toggled off.
        /// </summary>
        GenericTCSSwitchOff,
        /// <summary>
        /// Generic TCS switch toggled on.
        /// </summary>
        GenericTCSSwitchOn,
        /// <summary>
    }

    /// <summary>
    /// Controls what color the speed monitoring display uses.
    /// </summary>
    public enum MonitoringStatus
    {
        /// <summary>
        /// Grey color. No speed restriction is ahead.
        /// </summary>
        Normal,
        /// <summary>
        /// White color. Pre-indication, that the next signal is restricted. No manual intervention is needed yet.
        /// </summary>
        Indication,
        /// <summary>
        /// Yellow color. Next signal is restricted, driver should start decreasing speed.
        /// (Please note, it is not for indication of a "real" overspeed. In this state the locomotive still runs under the actual permitted speed.)
        /// </summary>
        Overspeed,
        /// <summary>
        /// Orange color. The locomotive is very close to next speed restriction, driver should start strong braking immediately.
        /// </summary>
        Warning,
        /// <summary>
        /// Red color. Train control system intervention speed. Computer has to apply full service or emergency brake to maintain speed restriction.
        /// </summary>
        Intervention,
    }

    public enum UpdateCheckFrequency
    {
        [Description("Manually check for updates")] Never = -1,
        [Description("Check for updates on each start")] Always = 0,
        [Description("Check for updates once a day")] Daily,
        [Description("Check for updates once a week")] Weekly,
        [Description("Check for updates every other week")] Biweekly,
        [Description("Check for updates every month")] Monthly,
    }

    public enum ActivityMode
    {
        Introductory = 0,
        Player = 2,
        Tutorial = 3,
    }

    [Description("Season")]
    public enum SeasonType
    {
        [Description("Spring")] Spring = 0,
        [Description("Summer")] Summer,
        [Description("Autumn")] Autumn,
        [Description("Winter")] Winter
    }

    [Description("Weather")]
    public enum WeatherType
    {
        [Description("Clear")] Clear = 0,
        [Description("Snow")] Snow,
        [Description("Rain")] Rain
    }

    [Description("Difficulty")]
    public enum Difficulty
    {
        [Description("Easy")] Easy = 0,
        [Description("Medium")] Medium,
        [Description("Hard")] Hard
    }
}
