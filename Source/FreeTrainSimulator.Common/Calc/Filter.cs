﻿using System;

namespace FreeTrainSimulator.Common.Calc
{
    public enum IIRFilterType
    {
        Exponential = 0,
        Chebychev = 1,
        Butterworth = 2,
        Bessel = 3
    }

    /// <summary>
    /// by Matej Pacha
    /// IIRFilter class provides discreet Infinite impulse response (IIR) filter
    /// Transfer function in general:
    ///                          -1      -2          -n
    ///         A(z)    a0 + a1*z  + a2*z  + ... an*z
    /// H(z) = ----- = ---------------------------------
    ///         B(z)             -1      -2          -m
    ///                 1  + b1*z  + b2*z  + ... bm*z
    /// IIRFilter class includes:
    /// - Exponential filter - not implemented!
    /// - Butterworth filter - only 1st order low pass with warping effect eliminated
    /// - Chebychev filter - not implemented!
    /// - Bessel filter - not implemented!
    /// 
    /// With every filter it is possible to use constant or variable sampling frequency (now only with Butterworth 1st order!!!)
    /// - Use Filter(NewSample) for constant sampling period
    /// - Use Filter(NewSample, samplingPeriod) for variable sampling period
    /// 
    /// Note: Sampling frequency MUST be always higher than cutoff frequency - if variable sampling period is used the Filter() function
    /// checks this condition and is skipped if not passed (may cause problems with result stability)
    /// </summary>
    public class IIRFilter
    {
        /// <summary>
        /// 2018-08 Code refactoring, replacing ArrayLists and Convert.ToFloat with [] instead
        /// also replacing float by double internally to improve precision, however external interfaces still offering float (truncating double)
        /// Only Butterworth  filter 1st order is implemented. If other filters ever needed/implemented, the fixed length arrays may need replacements
        /// </summary>
        private readonly int numberCoefficients;
        private readonly double[] aCoef;
        private readonly double[] bCoef;

        private double samplingPeriod;
        private double cuttoffFreqRadpS;

        private readonly double[] y;
        private readonly double[] x;

        public IIRFilter()
        {
            /**************************************************************
             * Addition of some coeficients to make filter working
             * If needed use following to get constant sampling period filter             
             * WinFilter version 0.8
            http://www.winfilter.20m.com
            akundert@hotmail.com

            Filter type: Low Pass
            Filter model: Chebyshev
            Filter order: 2
            Sampling Frequency: 10 Hz
            Cut Frequency: 1.000000 Hz
            Pass band Ripple: 1.000000 dB
            Coefficents Quantization: float

            Z domain Zeros
            z = -1.000000 + j 0.000000
            z = -1.000000 + j 0.000000

            Z domain Poles
            z = 0.599839 + j -0.394883
            z = 0.599839 + j 0.394883
            ***************************************************************/

            aCoef = new double[] { 0.00023973435363423468, 0.00047946870726846936, 0.00023973435363423468 };
            bCoef = new double[] { 1.00000000000000000000, -1.94607498611971570000, 0.94703573071858904000 };

            numberCoefficients = aCoef.Length;

            x = new double[numberCoefficients];
            y = new double[numberCoefficients];

            FilterType = IIRFilterType.Bessel;
        }

        /// <summary>
        /// Creates an instance of IIRFilter class
        /// </summary>
        /// <param name="a">A coefficients of the filter</param>
        /// <param name="b">B coefficients of the filter</param>
        /// <param name="type">Filter type</param>
        public IIRFilter(double[] aCoefficients, double[] bCoefficients, IIRFilterType type)
        {
            ArgumentNullException.ThrowIfNull(aCoefficients);
            FilterType = type;
            numberCoefficients = aCoefficients.Length;

            aCoef = aCoefficients;
            bCoef = bCoefficients;

            x = new double[numberCoefficients];
            y = new double[numberCoefficients];
        }

        /// <summary>
        /// Creates an instance of IIRFilter class
        /// </summary>
        /// <param name="type">Filter type</param>
        /// <param name="cutoffFrequency">Filter cutoff frequency in radians per second</param>
        /// <param name="samplingPeriod">Filter sampling period</param>
        public IIRFilter(IIRFilterType type, double cutoffFrequency, double samplingPeriod)
        {
            aCoef = new double[2];
            bCoef = new double[2];

            cuttoffFreqRadpS = cutoffFrequency;
            FilterType = type;

            switch (type)
            {
                case IIRFilterType.Butterworth:

                    ComputeButterworth(cutoffFrequency, samplingPeriod);
                    break;
                default:
                    throw new NotImplementedException("Other filter types are not implemented yet.");
            }

            numberCoefficients = aCoef.Length;

            x = new double[numberCoefficients];
            y = new double[numberCoefficients];
        }

        /// <summary>
        /// Filter Cut off frequency in Radians
        /// </summary>
        public double CutoffFrequencyRadpS
        {
            set => cuttoffFreqRadpS = value >= 0.0 ? value : throw new NotSupportedException("Filter cutoff frequency must be positive number");
            get => cuttoffFreqRadpS;
        }

        /// <summary>
        /// Filter sampling period in seconds
        /// </summary>
        public double SamplingPeriod
        {
            set => samplingPeriod = value >= 0.0 ? value : throw new NotSupportedException("Sampling period must be positive number");
            get => samplingPeriod;
        }

        public IIRFilterType FilterType { set; get; }

        /// <summary>
        /// IIR Digital filter function
        /// Call this function with constant sample period
        /// </summary>
        /// <param name="NewSample">Sample to filter</param>
        /// <returns>Filtered value</returns>
        public double Filter(double sample)
        {
            //shift the old samples
            for (int n = numberCoefficients - 1; n > 0; n--)
            {
                x[n] = x[n - 1];
                y[n] = y[n - 1];
            }
            //Calculate the new output
            x[0] = sample;
            y[0] = aCoef[0] * x[0];
            for (int n = 1; n < numberCoefficients; n++)
                y[0] = y[0] + aCoef[n] * x[n] - bCoef[n] * y[n];

            return y[0];
        }

        /// <summary>
        /// IIR Digital filter function
        /// Call this function with constant sample period
        /// </summary>
        /// <param name="NewSample">Sample to filter</param>
        /// <param name="samplingPeriod">Sampling period</param>
        /// <returns>Filtered value</returns>
        public double Filter(double sample, double samplingPeriod)
        {
            if (samplingPeriod <= 0.0)
                return 0.0;

            switch (FilterType)
            {
                case IIRFilterType.Butterworth:
                    if (samplingPeriod != this.samplingPeriod)
                    {
                        if (1 / samplingPeriod < Frequency.Angular.RadToHz(cuttoffFreqRadpS))
                            //Reset();
                            return sample;
                        ComputeButterworth(cuttoffFreqRadpS, this.samplingPeriod = samplingPeriod);
                    }
                    break;
                default:
                    throw new NotImplementedException("Other filter types are not implemented yet. Try to use constant sampling period and Filter(double sample) version of this method.");
            }
            //shift the old samples
            for (int n = numberCoefficients - 1; n > 0; n--)
            {
                x[n] = x[n - 1];
                y[n] = y[n - 1];
            }
            //Calculate the new output
            x[0] = sample;
            y[0] = aCoef[0] * x[0];
            for (int n = 1; n < numberCoefficients; n++)
                y[0] = y[0] + aCoef[n] * x[n] - bCoef[n] * y[n];

            return y[0];
        }

        /// <summary>
        /// Resets all buffers of the filter
        /// </summary>
        public void Reset()
        {
            for (int i = 0; i < x.Length; i++)
            {
                x[i] = 0.0;
                y[i] = 0.0;
            }
        }
        /// <summary>
        /// Resets all buffers of the filter with given initial value
        /// </summary>
        /// <param name="initValue">Initial value</param>
        public void Reset(double initValue)
        {
            for (double t = 0; t < 10.0 * cuttoffFreqRadpS; t += 0.1)
                Filter(initValue, 0.1f);
        }

        /// <summary>
        /// First-Order IIR Filter — Calculation by Freescale Semiconductor, Inc.
        /// **********************************************************************
        /// In GDFLIB User Reference Manual, 01/2009, Rev.0
        /// 
        /// Butterworth coefficients calculation
        /// The Butterworth first-order low-pass filter prototype is therefore given as:
        ///           w_c
        /// H(s) = ---------
        ///         s + w_c
        /// This is a transfer function of Butterworth low-pass filter in the s-domain with the cutoff frequency given by the w_c
        /// Transformation of an analog filter described by previous equation into a discrete form is done using the bilinear
        /// transformation, resulting in the following transfer function:
        ///         w_cd*Ts           w_cd*Ts      -1
        ///       -------------- + ------------ * z
        ///         2 + w_cd*Ts     2 + w_cd*Ts
        /// H(z)=-------------------------------------
        ///              w_cd*Ts - 2     -1
        ///         1 + ------------- * z
        ///              2 + w_cd*Ts
        /// where w_cd is the cutoff frequency of the filter in the digital domain and Ts
        /// is the sampling period. However, mapping of the analog system into a digital domain using the bilinear
        /// transformation makes the relation between w_c and w_cd non-linear. This introduces a distortion in the frequency
        /// scale of the digital filter relative to that of the analog filter. This is known as warping effect. The warping 
        /// effect can be eliminated by pre-warping the analog filter, and then transforming it into the digital domain,
        /// resulting in this transfer function:
        ///         w_cd_p*Ts_p           w_cd_p*Ts_p      -1
        ///       ------------------ + ---------------- * z
        ///         2 + w_cd_p*Ts_p     2 + w_cd_p*Ts_p
        /// H(z)=-------------------------------------
        ///              w_cd_p*Ts_p - 2     -1
        ///         1 + ----------------- * z
        ///              2 + w_cd_p*Ts_p
        /// where ωcd_p is the pre-warped cutoff frequency of the filter in the digital domain, and Ts_p is the 
        /// pre-warped sampling period. The pre-warped cutoff frequency is calculated as follows:
        ///            2             w_cd*Ts
        /// w_cd_p = ------ * tan ( --------- )
        ///           Ts_p              2
        /// and the pre-warped sampling period is:
        /// Ts_p = 0.5
        /// 
        /// Because the given filter equation is as described, the Butterworth low-pass filter 
        /// coefficients are calculated as follows:
        ///             w_cd_p*Ts_p
        /// a1 = a2 = -----------------
        ///            2 + w_cd_p*Ts_p           
        /// b1 = 1.0
        ///       w_cd_p*Ts_p - 2
        /// b2 = ------------------
        ///       2 + w_cd_p*Ts_p
        /// </summary>
        /// <param name="order">Filter order</param>
        /// <param name="cutoffFrequency">Cuttof frequency in rad/s</param>
        /// <param name="samplingPeriod">Sampling period</param>
#pragma warning disable CA1801 // Review unused parameters
        private void ComputeButterworth(double cutoffFrequency, double samplingPeriod)
#pragma warning restore CA1801 // Review unused parameters
        {
            double Ts_p = 0.5;
            double w_cd_p = 2 / Ts_p * Math.Tan(cutoffFrequency * samplingPeriod / 2.0);

            //a1
            aCoef[0] = w_cd_p * Ts_p / (2.0 + w_cd_p * Ts_p);
            //a2
            aCoef[1] = w_cd_p * Ts_p / (2.0 + w_cd_p * Ts_p);
            //b1 = always 1.0
            bCoef[0] = 1.0;
            //b2
            bCoef[1] = (w_cd_p * Ts_p - 2.0) / (2.0 + w_cd_p * Ts_p);
        }
    }
}
