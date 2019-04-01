#region netDxf library, Copyright (C) 2009-2018 Daniel Carvajal (haplokuon@gmail.com)

//                        netDxf library
// Copyright (C) 2009-2018 Daniel Carvajal (haplokuon@gmail.com)
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#endregion

using System;

namespace netDxf.Units
{
    /// <summary>
    /// Represents the parameters to convert linear and angular units to its string representation.
    /// </summary>
    public class UnitStyleFormat
    {
        #region private fields

        private short linearDecimalPlaces;
        private short angularDecimalPlaces;
        private string decimalSeparator;
        private string feetInchesSeparator;
        private string degreesSymbol;
        private string minutesSymbol;
        private string secondsSymbol;
        private string radiansSymbol;
        private string gradiansSymbol;
        private string feetSymbol;
        private string inchesSymbol;
        private double fractionHeigthScale;
        private FractionFormatType fractionType;
        private bool supressLinearLeadingZeros;
        private bool supressLinearTrailingZeros;
        private bool supressAngularLeadingZeros;
        private bool supressAngularTrailingZeros;
        private bool supressZeroFeet;
        private bool supressZeroInches;

        #endregion

        #region constructors

        /// <summary>
        /// Initializes a new instance of the <c>UnitStyleFormat</c> class.
        /// </summary>
        public UnitStyleFormat()
        {
            linearDecimalPlaces = 2;
            angularDecimalPlaces = 0;
            decimalSeparator = ".";
            feetInchesSeparator = "-";
            degreesSymbol = "°";
            minutesSymbol = "\'";
            secondsSymbol = "\"";
            radiansSymbol = "r";
            gradiansSymbol = "g";
            feetSymbol = "\'";
            inchesSymbol = "\"";
            fractionHeigthScale = 1.0;
            fractionType = FractionFormatType.Horizontal;
            supressLinearLeadingZeros = false;
            supressLinearTrailingZeros = false;
            supressAngularLeadingZeros = false;
            supressAngularTrailingZeros = false;
            supressZeroFeet = true;
            supressZeroInches = true;
        }

        #endregion

        #region public properties

        /// <summary>
        /// Gets or sets the number of decimal places for linear units.
        /// </summary>
        /// <remarks>
        /// For architectural and fractional the precision used for the minimum fraction is 1/2^LinearDecimalPlaces.
        /// </remarks>
        public short LinearDecimalPlaces
        {
            get { return linearDecimalPlaces; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The number of decimal places must be equals or greater than zero.");
                linearDecimalPlaces = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of decimal places for angular units.
        /// </summary>
        public short AngularDecimalPlaces
        {
            get { return angularDecimalPlaces; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The number of decimal places must be equals or greater than zero.");
                angularDecimalPlaces = value;
            }
        }

        /// <summary>
        /// Gets or set the decimal separator.
        /// </summary>
        public string DecimalSeparator
        {
            get { return decimalSeparator; }
            set { decimalSeparator = value; }
        }

        /// <summary>
        /// Gets or sets the separator between feet and inches.
        /// </summary>
        public string FeetInchesSeparator
        {
            get { return feetInchesSeparator; }
            set { feetInchesSeparator = value; }
        }

        /// <summary>
        /// Gets or set the angle degrees symbol.
        /// </summary>
        public string DegreesSymbol
        {
            get { return degreesSymbol; }
            set { degreesSymbol = value; }
        }

        /// <summary>
        /// Gets or set the angle minutes symbol.
        /// </summary>
        public string MinutesSymbol
        {
            get { return minutesSymbol; }
            set { minutesSymbol = value; }
        }

        /// <summary>
        /// Gets or set the angle seconds symbol.
        /// </summary>
        public string SecondsSymbol
        {
            get { return secondsSymbol; }
            set { secondsSymbol = value; }
        }

        /// <summary>
        /// Gets or set the angle radians symbol.
        /// </summary>
        public string RadiansSymbol
        {
            get { return radiansSymbol; }
            set { radiansSymbol = value; }
        }

        /// <summary>
        /// Gets or set the angle gradians symbol.
        /// </summary>
        public string GradiansSymbol
        {
            get { return gradiansSymbol; }
            set { gradiansSymbol = value; }
        }

        /// <summary>
        /// Gets or set the feet symbol.
        /// </summary>
        public string FeetSymbol
        {
            get { return feetSymbol; }
            set { feetSymbol = value; }
        }

        /// <summary>
        /// Gets or set the inches symbol.
        /// </summary>
        public string InchesSymbol
        {
            get { return inchesSymbol; }
            set { inchesSymbol = value; }
        }

        /// <summary>
        /// Gets or sets the scale of fractions relative to dimension text height.
        /// </summary>
        public double FractionHeightScale
        {
            get { return fractionHeigthScale; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The fraction height scale must be greater than zero.");
                fractionHeigthScale = value;
            }
        }

        /// <summary>
        /// Gets or sets the fraction format for architectural or fractional units.
        /// </summary>
        /// <remarks>
        /// Horizontal stacking<br/>
        /// Diagonal stacking<br/>
        /// Not stacked (for example, 1/2)
        /// </remarks>
        public FractionFormatType FractionType
        {
            get { return fractionType; }
            set { fractionType = value; }
        }

        /// <summary>
        /// Suppresses leading zeros in linear decimal dimensions (for example, 0.5000 becomes .5000).
        /// </summary>
        public bool SupressLinearLeadingZeros
        {
            get { return supressLinearLeadingZeros; }
            set { supressLinearLeadingZeros = value; }
        }

        /// <summary>
        /// Suppresses trailing zeros in linear decimal dimensions (for example, 12.5000 becomes 12.5).
        /// </summary>
        public bool SupressLinearTrailingZeros
        {
            get { return supressLinearTrailingZeros; }
            set { supressLinearTrailingZeros = value; }
        }

        /// <summary>
        /// Suppresses leading zeros in angular decimal dimensions (for example, 0.5000 becomes .5000).
        /// </summary>
        public bool SupressAngularLeadingZeros
        {
            get { return supressAngularLeadingZeros; }
            set { supressAngularLeadingZeros = value; }
        }

        /// <summary>
        /// Suppresses trailing zeros in angular decimal dimensions (for example, 12.5000 becomes 12.5).
        /// </summary>
        public bool SupressAngularTrailingZeros
        {
            get { return supressAngularTrailingZeros; }
            set { supressAngularTrailingZeros = value; }
        }

        /// <summary>
        /// Suppresses zero feet in architectural dimensions.
        /// </summary>
        public bool SupressZeroFeet
        {
            get { return supressZeroFeet; }
            set { supressZeroFeet = value; }
        }

        /// <summary>
        /// Suppresses zero inches in architectural dimensions.
        /// </summary>
        public bool SupressZeroInches
        {
            get { return supressZeroInches; }
            set { supressZeroInches = value; }
        }

        #endregion
    }
}