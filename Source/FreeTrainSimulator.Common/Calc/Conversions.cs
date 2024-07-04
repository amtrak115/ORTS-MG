﻿using System;

namespace FreeTrainSimulator.Common.Calc
{
#pragma warning disable CA1034 // Nested types should not be visible
    /// <summary>
    /// Frequency conversions
    /// </summary>
    public static class Frequency
    {
        /// <summary>
        /// Angular Frequency conversions
        /// </summary>
        public static class Angular
        {
            /// <summary>
            /// Frequency conversion from rad/s to Hz
            /// </summary>
            /// <param name="rad">Frequency in radians per second</param>
            /// <returns>Frequency in Hertz</returns>
            public static double RadToHz(double rad)
            {
                return rad / (2 * Math.PI);
            }

            /// <summary>
            /// Frequenc conversion from Hz to rad/s
            /// </summary>
            /// <param name="hz">Frequenc in Hertz</param>
            /// <returns>Frequency in radians per second</returns>
            public static double HzToRad(double hz)
            {
                return 2 * Math.PI * hz;
            }
        }

        /// <summary>
        /// Frequency conversions from and to Hz (events/sec)
        /// </summary>
        public static class Periodic
        {
            /// <summary>Convert from per Minute to per Second</summary>
            public static double FromMinutes(double eventsPerMinute) { return eventsPerMinute * (1.0 / 60.0); }
            /// <summary>Convert from per Second to per Minute</summary>
            public static double ToMinutes(double eventsPerSecond) { return eventsPerSecond * 60.0; }
            /// <summary>Convert from per Hour to per Second</summary>
            public static double FromHours(double eventsPerHour) { return eventsPerHour * (1.0 / 3600.0); }
            /// <summary>Convert from per Second to per Hour</summary>
            public static double ToHours(double eventsPerSecond) { return eventsPerSecond * 3600.0; }
        }

    }

    /// <summary>
    /// Time conversions
    /// </summary>
    public static class Time
    {
        /// <summary>
        /// Time conversions from and to Seconds
        /// </summary>
        public static class Second
        {
            /// <summary>Convert from minutes to seconds</summary>
            public static double FromM(double minutes) { return minutes * 60.0; }
            /// <summary>Convert from seconds to minutes</summary>
            public static double ToM(double seconds) { return seconds * (1.0 / 60.0); }
            /// <summary>Convert from hours to seconds</summary>
            public static double FromH(double hours) { return hours * 3600.0; }
            /// <summary>Convert from seconds to hours</summary>
            public static double ToH(double seconds) { return seconds * (1.0 / 3600.0); }
        }

        /// <summary>
        /// Compare daytimes (given in seconds) taking into account times after midnight
        /// (morning comes after night comes after evening, but morning is before afternoon, which is before evening)
        /// </summary>
        public static class Compare
        {
            private const int eightHundredHours = 8 * 3600;
            private const int sixteenHundredHours = 16 * 3600;

            /// <summary>
            /// Return the latest time of the two input times, keeping in mind that night/morning is after evening/night
            /// </summary>
            public static int Latest(int timeOfDay1, int timeOfDay2)
            {
                if (timeOfDay1 > sixteenHundredHours && timeOfDay2 < eightHundredHours)
                    return timeOfDay2;
                else if (timeOfDay1 < eightHundredHours && timeOfDay2 > sixteenHundredHours)
                    return timeOfDay1;
                else if (timeOfDay1 > timeOfDay2)
                    return timeOfDay1;
                return timeOfDay2;
            }

            /// <summary>
            /// Return the Earliest time of the two input times, keeping in mind that night/morning is after evening/night
            /// </summary>
            public static int Earliest(int timeOfDay1, int timetimeOfDay2)
            {
                if (timeOfDay1 > sixteenHundredHours && timetimeOfDay2 < eightHundredHours)
                    return timeOfDay1;
                else if (timeOfDay1 < eightHundredHours && timetimeOfDay2 > sixteenHundredHours)
                    return timetimeOfDay2;
                else if (timeOfDay1 > timetimeOfDay2)
                    return timetimeOfDay2;
                return timeOfDay1;
            }
        }
    }

    /// <summary>
    /// Current conversions
    /// </summary>
    public static class Current
    {
        /// <summary>
        /// Current conversions from and to Amps
        /// </summary>
        public static class Ampere
        {

        }
    }

    /// <summary>
    /// Size (length, area, volume) conversions
    /// </summary>
    public static class Size
    {
        /// <summary>
        /// Length (distance) conversions from and to metres
        /// </summary>
        public static class Length
        {
            /// <summary>Convert (statute or land) miles to metres</summary>
            public static double FromMi(double miles) { return miles * 1609.344; }
            /// <summary>Convert metres to (statute or land) miles</summary>
            public static double ToMi(double metres) { return metres * (1.0 / 1609.344); }
            /// <summary>Convert kilometres to metres</summary>
            public static double FromKM(double kilometer) { return kilometer * 1000.0; }
            /// <summary>Convert metres to kilometres</summary>
            public static double ToKM(double metres) { return metres * (1.0 / 1000.0); }
            /// <summary>Convert yards to metres</summary>
            public static double FromYd(double yards) { return yards * 0.9144; }
            /// <summary>Convert metres to yards</summary>
            public static double ToYd(double metres) { return metres * (1.0 / 0.9144); }
            /// <summary>Convert feet to metres</summary>
            public static double FromFt(double feet) { return feet * 0.3048; }
            /// <summary>Convert metres to feet</summary>
            public static double ToFt(double metres) { return metres * (1.0 / 0.3048); }
            /// <summary>Convert inches to metres</summary>
            public static double FromIn(double inches) { return inches * 0.0254; }
            /// <summary>Convert metres to inches</summary>
            public static double ToIn(double metres) { return metres * (1.0 / 0.0254); }

            /// <summary>
            /// Convert from metres into kilometres or miles, depending on the flag isMetric
            /// </summary>
            /// <param name="distance">distance in metres</param>
            /// <param name="isMetric">if true convert to kilometres, if false convert to miles</param>
            public static double FromM(double distance, bool isMetric)
            {
                return isMetric ? ToKM(distance) : ToMi(distance);
            }

            /// <summary>
            /// Convert to metres from kilometres or miles, depending on the flag isMetric
            /// </summary>
            /// <param name="distance">distance to be converted to metres</param>
            /// <param name="isMetric">if true convert from kilometres, if false convert from miles</param>
            public static double ToM(double distance, bool isMetric)
            {
                return isMetric ? FromKM(distance) : FromMi(distance);
            }

        }

        /// <summary>
        /// Area conversions from and to m^2
        /// </summary>
        public static class Area
        {
            /// <summary>Convert from feet squared to metres squared</summary>
            public static double FromFt2(double squareFeet) { return squareFeet * (1 / 10.763910449); }
            /// <summary>Convert from metres squared to feet squared</summary>
            public static double ToFt2(double squareMetres) { return squareMetres * 10.763910449; }
            /// <summary>Convert from inches squared to metres squared</summary>
            public static double FromIn2(double squareFeet) { return squareFeet * (1.0 / 1550.0031); }
            /// <summary>Convert from metres squared to inches squared</summary>
            public static double ToIn2(double squareMetres) { return squareMetres * 1550.0031; }
        }

        /// <summary>
        /// Volume conversions from and to m^3
        /// </summary>
        public static class Volume
        {
            /// <summary>Convert from cubic feet to cubic metres</summary>
            public static double FromFt3(double qubicFeet) { return qubicFeet * (1.0 / 35.3146665722); }
            /// <summary>Convert from cubic metres to cubic feet</summary>
            public static double ToFt3(double qubicMetres) { return qubicMetres * 35.3146665722; }
            /// <summary>Convert from cubic inches to cubic metres</summary>
            public static double FromIn3(double qubicInches) { return qubicInches * (1.0 / 61023.7441); }
            /// <summary>Convert from cubic metres to cubic inches</summary>
            public static double ToIn3(double qubicMetres) { return qubicMetres * 61023.7441; }

        }

        /// <summary>
        /// Liquid volume conversions from and to Litres
        /// </summary>
        public static class LiquidVolume
        {
            /// <summary>Convert from cubic metres to litres</summary>
            public static double FromM3(double qubicMetres) { return qubicMetres * 1000; }
            /// <summary>Convert litres to cubic metres</summary>
            public static double ToM3(double litre) { return litre / (1.0 * 1000); }
            /// <summary>Convert from UK Gallons to litres</summary>
            public static double FromGallonUK(double gallonUK) { return gallonUK * 4.54609; }
            /// <summary>Convert from litres to UK Gallons</summary>
            public static double ToGallonUK(double litre) { return litre * (1.0 / 4.54609); }
            /// <summary>Convert from US Gallons to litres</summary>
            public static double FromGallonUS(double gallonUS) { return gallonUS * 3.78541; }
            /// <summary>Convert from litres to US Gallons</summary>
            public static double ToGallonUS(double litre) { return litre * (1.0 / 3.78541); }
            /// <summary>Convert from cubic feet to litres</summary>
            public static double FromFt3(double qubicFeet) { return FromM3(Volume.FromFt3(qubicFeet)); }
            /// <summary>Convert from litres to cubic feet</summary>
            public static double ToFt3(double litre) { return Volume.ToFt3(ToM3(litre)); }
            /// <summary>Convert from cubic inches to litres</summary>
            public static double FromIn3(double qubicInches) { return FromM3(Volume.FromIn3(qubicInches)); }
            /// <summary>Convert from litres to cubic inches</summary>
            public static double ToIn3(double litre) { return Volume.FromFt3(ToM3(litre)); }
        }
    }

    /// <summary>
    /// Mass conversions
    /// </summary>
    public static class Mass
    {
        /// <summary>
        /// Mass conversions from and to Kilograms
        /// </summary>
        public static class Kilogram
        {
            /// <summary>Convert from pounds (lb) to kilograms</summary>
            public static double FromLb(double lb) { return lb * (1.0f / 2.20462); }
            /// <summary>Convert from kilograms to pounds (lb)</summary>
            public static double ToLb(double kg) { return kg * 2.20462; }
            /// <summary>Convert from US Tons to kilograms</summary>
            public static double FromTonsUS(double tonsUS) { return tonsUS * 907.1847; }
            /// <summary>Convert from kilograms to US Tons</summary>
            public static double ToTonsUS(double kg) { return kg * (1.0 / 907.1847); }
            /// <summary>Convert from UK Tons to kilograms</summary>
            public static double FromTonsUK(double tonsUK) { return tonsUK * 1016.047; }
            /// <summary>Convert from kilograms to UK Tons</summary>
            public static double ToTonsUK(double kg) { return kg * (1.0 / 1016.047); }
            /// <summary>Convert from kilogram to metric tonnes</summary>
            public static double ToTonnes(double kg) { return kg * (1.0 / 1000.0); }
            /// <summary>Convert from metrix tonnes to kilogram</summary>
            public static double FromTonnes(double tonnes) { return tonnes * 1000.0; }
        }
    }

    /// <summary>
    /// Energy related conversions like Power, Force, Resistance, Stiffness
    /// </summary>
    public static class Dynamics
    {
        /// <summary>
        /// Stiffness conversions from and to Newtons/metre
        /// </summary>
        public static class Stiffness
        {
        }

        /// <summary>
        /// Resistance conversions from and to Newtons/metre/sec
        /// </summary>
        public static class Resistance
        {
            /// <summary>Convert from pound-force per mile per hour to Newton per meter per second</summary>
            public static double FromLbfpMpH(double lbfMpH) { return Force.FromLbf(lbfMpH) / Speed.MeterPerSecond.FromMpH(1); }
            /// <summary>Convert from pound-force per mile per hour squared to Newton per meter per second</summary>
            public static double FromLbfpMpH2(double lbfMpH) { return Force.FromLbf(lbfMpH) / (Speed.MeterPerSecond.FromMpH(1) * Speed.MeterPerSecond.FromMpH(1)); }
        }

        /// <summary>
        /// Power conversions from and to Watts
        /// </summary>
        public static class Power
        {
            /// <summary>Convert from kiloWatts to Watts</summary>
            public static double FromKW(double kiloWatts) { return kiloWatts * 1000; }
            /// <summary>Convert from Watts to kiloWatts</summary>
            public static double ToKW(double watts) { return watts * (1.0 / 1000); }
            /// <summary>Convert from HorsePower to Watts</summary>
            public static double FromHp(double horsePowers) { return horsePowers * 745.699872; }
            /// <summary>Convert from Watts to HorsePower</summary>
            public static double ToHp(double watts) { return watts * (1.0 / 745.699872); }
            /// <summary>Convert from BoilerHorsePower to Watts</summary>
            public static double FromBhp(double horsePowers) { return horsePowers * 9809.5; }
            /// <summary>Convert from Watts to BoilerHorsePower</summary>
            public static double ToBhp(double watts) { return watts * (1.0 / 9809.5); }
            /// <summary>Convert from British Thermal Unit (BTU) per second to watts</summary>
            public static double FromBTUpS(double btuPerSecond) { return btuPerSecond * 1055.05585; }
            /// <summary>Convert from Watts to British Thermal Unit (BTU) per second</summary>
            public static double ToBTUpS(double watts) { return watts * (1.0 / 1055.05585); }
        }

        /// <summary>
        /// Force conversions from and to Newtons
        /// </summary>
        public static class Force
        {
            /// <summary>Convert from pound-force to Newtons</summary>
            public static double FromLbf(double lbf) { return lbf * (1.0f / 0.224808943871); }
            /// <summary>Convert from Newtons to Pound-force</summary>
            public static double ToLbf(double newton) { return newton * 0.224808943871; }
        }

    }

    /// <summary>
    /// Temperature conversions
    /// </summary>
    public static class Temperature
    {
        /// <summary>
        /// Temperature conversions from and to Celsius
        /// </summary>
        public static class Celsius
        {
            /// <summary>Convert from degrees Fahrenheit to degrees Celcius</summary>
            public static double FromF(double fahrenheit) { return (fahrenheit - 32) * (100.0 / 180.0); }
            /// <summary>Convert from degrees Celcius to degrees Fahrenheit</summary>
            public static double ToF(double celcius) { return celcius * (180.0 / 100.0) + 32; }
            /// <summary>Convert from Kelvin to degrees Celcius</summary>
            public static double FromK(double kelvin) { return kelvin - 273.15; }
            /// <summary>Convert from degress Celcius to Kelvin</summary>
            public static double ToK(double celcius) { return celcius + 273.15; }
        }

        /// <summary>
        /// Temperature conversions from and to Kelvin
        /// </summary>
        public static class Kelvin
        {
            /// <summary>Convert from degrees Fahrenheit to degrees Celcius</summary>
            public static double FromF(double fahrenheit) { return (fahrenheit - 32) * (100.0 / 180.0) + 273.15; }
            /// <summary>Convert from degrees Celcius to degrees Fahrenheit</summary>
            public static double ToF(double kelvin) { return (kelvin - 273.15) * (180.0 / 100.0) + 32; }
            /// <summary>Convert from Celcisu degress to Kelvin</summary>
            public static double FromC(double celcius) { return celcius + 273.15; }
            /// <summary>Convert from Kelvin to degress Celcius</summary>
            public static double ToC(double kelvin) { return kelvin - 273.15; }

        }
    }

    /// <summary>
    /// Speed conversions 
    /// </summary>
    public static class Speed
    {
        /// <summary>
        /// Speed conversions from and to metres/sec
        /// </summary>
        public static class MeterPerSecond
        {
            /// <summary>Convert miles/hour to metres/second</summary>
            public static double FromMpH(double milesPerHour) { return milesPerHour * (1.0 / 2.23693629); }
            /// <summary>Convert metres/second to miles/hour</summary>
            public static double ToMpH(double metersPerSecond) { return metersPerSecond * 2.23693629; }
            /// <summary>Convert kilometre/hour to metres/second</summary>
            public static double FromKpH(double kilometersPerHour) { return kilometersPerHour * (1.0 / 3.6); }
            /// <summary>Convert metres/second to kilometres/hour</summary>
            public static double ToKpH(double metersPerSecond) { return metersPerSecond * 3.6; }

            /// <summary>
            /// Convert from metres/second to kilometres/hour or miles/hour, depending on value of isMetric
            /// </summary>
            /// <param name="speed">speed in metres/second</param>
            /// <param name="isMetric">true to convert to kilometre/hour, false to convert to miles/hour</param>
            public static double FromMpS(double speed, bool isMetric)
            {
                return isMetric ? ToKpH(speed) : ToMpH(speed);
            }

            /// <summary>
            /// Convert to metres/second from kilometres/hour or miles/hour, depending on value of isMetric
            /// </summary>
            /// <param name="speed">speed to be converted to metres/second</param>
            /// <param name="isMetric">true to convert from kilometre/hour, false to convert from miles/hour</param>
            public static double ToMpS(double speed, bool isMetric)
            {
                return isMetric ? FromKpH(speed) : FromMpH(speed);
            }
        }

    }

    /// <summary>
    /// Rate changes conversions
    /// </summary>
    public static class Rate
    {
        /// <summary>
        /// Flow rate conversions
        /// </summary>
        public static class Flow
        {
            /// <summary>
            /// Mass rate conversions from and to Kg/s
            /// </summary>
            public static class Mass
            {
                /// <summary>Convert from pound/hour to kilograms/second</summary>
                public static double FromLbpH(double poundsPerHour) { return poundsPerHour * (1.0 / 7936.64144); }
                /// <summary>Convert from kilograms/second to pounds/hour</summary>
                public static double ToLbpH(double kilogramsPerSecond) { return kilogramsPerSecond * 7936.64144; }
            }
        }

        /// <summary>
        /// Pressure rate conversions from and to bar/s
        /// </summary>
        public static class Pressure
        {
            /// <summary>Convert from Pounds per square Inch per second to bar per second</summary>
            public static double FromPSIpS(double psi) { return psi * (1.0 / 14.5037738); }
            /// <summary>Convert from bar per second to Pounds per square Inch per second</summary>
            public static double ToPSIpS(double bar) { return bar * 14.5037738; }
        }
    }

    /// <summary>
    /// Energy conversions
    /// </summary>
    public static class Energy
    {
        /// <summary>
        /// Energy conversions from and to Joule
        /// </summary>
        public static class Transfer
        {
            /// <summary>Convert from kiloJoules to Joules</summary>
            public static double FromKJ(double kiloJoules) { return kiloJoules * 1000f; }
            /// <summary>Convert from Joules to kileJoules</summary>
            public static double ToKJ(double joules) { return joules * (1.0f / 1000f); }
        }

        /// <summary>
        /// Energy density conversions
        /// </summary>
        public static class Density
        {
            /// <summary>
            /// Energy density conversions from and to kJ/Kg
            /// </summary>
            public static class Mass
            {
                /// <summary>Convert from Britisch Thermal Units per Pound to kiloJoule per kilogram</summary>
                public static double FromBTUpLb(double btuPerPound) { return btuPerPound * 2.326; }
                /// <summary>Convert from kiloJoule per kilogram to Britisch Thermal Units per Pound</summary>
                public static double ToBTUpLb(double kJPerkg) { return kJPerkg * (1.0f / 2.326); }
            }

            /// <summary>
            /// Energy density conversions from and to kJ/m^3
            /// </summary>
            public static class Volume
            {
                /// <summary>Convert from Britisch Thermal Units per ft^3 to kiloJoule per m^3</summary>
                public static double FromBTUpFt3(double btuPerFt3) { return btuPerFt3 * (1.0 / 37.3); }
                /// <summary>Convert from kiloJoule per m^3 to Britisch Thermal Units per ft^3</summary>
                public static double ToBTUpFt3(double kJPerM3) { return kJPerM3 * 37.3; }
            }

        }
    }

    /// <summary>
    /// Pressure conversion
    /// </summary>
    public static class Pressure
    {
        /// <summary>
        /// Various units of pressure that are used
        /// </summary>
        public enum Unit
        {
            /// <summary>non-defined unit</summary>
            None,
            /// <summary>kiloPascal</summary>
            KPa,
            /// <summary>bar</summary>
            Bar,
            /// <summary>Pounds Per Square Inch</summary>
            PSI,
            /// <summary>Inches Mercury</summary>
            InHg,
            /// <summary>Mass-force per square centimetres</summary>
            KgfpCm2
        }

        /// <summary>
        /// convert vacuum values to psia for vacuum brakes
        /// </summary>
        public static class Vacuum
        {
            private static readonly double OneAtmospherePSI = Atmospheric.ToPSI(1);
            /// <summary>vacuum in inhg to pressure in psia</summary>
            public static double ToPressure(double vacuum) { return OneAtmospherePSI - Atmospheric.ToPSI(Atmospheric.FromInHg(vacuum)); }
            /// <summary>convert pressure in psia to vacuum in inhg</summary>
            public static double FromPressure(double pressure) { return Atmospheric.ToInHg(Atmospheric.FromPSI(OneAtmospherePSI - pressure)); }
        }

        /// <summary>
        /// Pressure conversions from and to bar
        /// </summary>
        public static class Atmospheric
        {
            /// <summary>Convert from kiloPascal to Bar</summary>
            public static double FromKPa(double kiloPascal) { return kiloPascal * (1.0 / 100.0); }
            /// <summary>Convert from bar to kiloPascal</summary>
            public static double ToKPa(double bar) { return bar * 100.0; }
            /// <summary>Convert from Pounds per Square Inch to Bar</summary>
            public static double FromPSI(double poundsPerSquareInch) { return poundsPerSquareInch * (1.0 / 14.5037738); }
            /// <summary>Convert from Bar to Pounds per Square Inch</summary>
            public static double ToPSI(double bar) { return bar * 14.5037738; }
            /// <summary>Convert from Inches Mercury to bar</summary>
            public static double FromInHg(double inchesMercury) { return inchesMercury * 0.03386389; }
            /// <summary>Convert from bar to Inches Mercury</summary>
            public static double ToInHg(double bar) { return bar * (1.0 / 0.03386389); }
            /// <summary>Convert from cm Mercury to bar</summary>
            public static double FromCmHg(double inchesMercury) { return inchesMercury * 0.0133322387415; }
            /// <summary>Convert from bar to cm Mercury</summary>
            public static double ToCmHg(double bar) { return bar * (1.0 / 0.0133322387415); }
            /// <summary>Convert from mass-force per square metres to bar</summary>
            public static double FromKgfpCm2(double f) { return f * (1.0 / 1.0197); }
            /// <summary>Convert from bar to mass-force per square metres</summary>
            public static double ToKgfpCm2(double bar) { return bar * 1.0197; }
        }

        /// <summary>
        /// Pressure conversions from and to kilopascals
        /// </summary>
        public static class Standard
        {
            /// <summary>Convert from Pounds per Square Inch to kiloPascal</summary>
            public static double FromPSI(double psi) { return psi * 6.89475729; }
            /// <summary>Convert from kiloPascal to Pounds per Square Inch</summary>
            public static double ToPSI(double kiloPascal) { return kiloPascal * (1.0 / 6.89475729); }
            /// <summary>Convert from Inches Mercury to kiloPascal</summary>
            public static double FromInHg(double inchesMercury) { return inchesMercury * 3.386389; }
            /// <summary>Convert from kiloPascal to Inches Mercury</summary>
            public static double ToInHg(double kiloPascal) { return kiloPascal * (1.0 / 3.386389); }
            /// <summary>Convert from Bar to kiloPascal</summary>
            public static double FromBar(double bar) { return bar * 100.0; }
            /// <summary>Convert from kiloPascal to Bar</summary>
            public static double ToBar(double kiloPascal) { return kiloPascal * (1.0 / 100.0); }
            /// <summary>Convert from mass-force per square metres to kiloPascal</summary>
            public static double FromKgfpCm2(double f) { return f * 98.068059; }
            /// <summary>Convert from kiloPascal to mass-force per square centimetres</summary>
            public static double ToKgfpCm2(double kiloPascal) { return kiloPascal * (1.0 / 98.068059); }

            /// <summary>
            /// Convert from KPa to any pressure unit
            /// </summary>
            /// <param name="pressure">pressure to convert from</param>
            /// <param name="outputUnit">Unit to convert To</param>
            public static double FromKPa(double pressure, Unit outputUnit)
            {
                return outputUnit switch
                {
                    Unit.KPa => pressure,
                    Unit.Bar => ToBar(pressure),
                    Unit.InHg => ToInHg(pressure),
                    Unit.KgfpCm2 => ToKgfpCm2(pressure),
                    Unit.PSI => ToPSI(pressure),
                    _ => throw new ArgumentOutOfRangeException(nameof(outputUnit), $"Pressure unit '{outputUnit}' not recognized"),
                };
            }

            /// <summary>
            /// Convert from any pressure unit to KPa
            /// </summary>
            /// <param name="pressure">pressure to convert from</param>
            /// <param name="inputUnit">Unit to convert from</param>
            public static double ToKPa(double pressure, Unit inputUnit)
            {
                return inputUnit switch
                {
                    Unit.KPa => pressure,
                    Unit.Bar => FromBar(pressure),
                    Unit.InHg => FromInHg(pressure),
                    Unit.KgfpCm2 => FromKgfpCm2(pressure),
                    Unit.PSI => FromPSI(pressure),
                    _ => throw new ArgumentOutOfRangeException(nameof(inputUnit), $"Pressure unit '{inputUnit}' not recognized"),
                };
            }
        }
    }
#pragma warning restore CA1034 // Nested types should not be visible

}
