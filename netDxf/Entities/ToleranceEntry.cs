﻿#region netDxf library, Copyright (C) 2009-2016 Daniel Carvajal (haplokuon@gmail.com)

//                        netDxf library
// Copyright (C) 2009-2016 Daniel Carvajal (haplokuon@gmail.com)
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

namespace netDxf.Entities
{
    /// <summary>
    /// Represents an entry in a tolerance entity.
    /// </summary>
    /// <remarks>
    /// Each entry can be made of up to two tolerance values and three datum references, plus a symbol that represents the geometric characteristics.
    /// </remarks>
    public class ToleranceEntry :
        ICloneable
    {
        #region private fields

        private ToleranceGeometricSymbol geometricSymbol;
        private ToleranceValue tolerance1;
        private ToleranceValue tolerance2;
        private DatumReferenceValue datum1;
        private DatumReferenceValue datum2;
        private DatumReferenceValue datum3;

        #endregion

        #region constructors

        /// <summary>
        /// Initializes a new instance of the <c>ToleranceEntry</c> class.
        /// </summary>
        public ToleranceEntry()
        {
            geometricSymbol = ToleranceGeometricSymbol.None;
            tolerance1 = null;
            tolerance2 = null;
            datum1 = null;
            datum2 = null;
            datum3 = null;
        }

        #endregion

        #region public properties

        /// <summary>
        /// Gets or sets the geometric characteristics symbol.
        /// </summary>
        public ToleranceGeometricSymbol GeometricSymbol
        {
            get { return geometricSymbol; }
            set { geometricSymbol = value; }
        }

        /// <summary>
        /// Gets or sets the first tolerance value.
        /// </summary>
        public ToleranceValue Tolerance1
        {
            get { return tolerance1; }
            set { tolerance1 = value; }
        }

        /// <summary>
        /// Gets or sets the second tolerance value.
        /// </summary>
        public ToleranceValue Tolerance2
        {
            get { return tolerance2; }
            set { tolerance2 = value; }
        }

        /// <summary>
        /// Gets or sets the first datum reference value.
        /// </summary>
        public DatumReferenceValue Datum1
        {
            get { return datum1; }
            set { datum1 = value; }
        }

        /// <summary>
        /// Gets or sets the second datum reference value.
        /// </summary>
        public DatumReferenceValue Datum2
        {
            get { return datum2; }
            set { datum2 = value; }
        }

        /// <summary>
        /// Gets or sets the third datum reference value.
        /// </summary>
        public DatumReferenceValue Datum3
        {
            get { return datum3; }
            set { datum3 = value; }
        }

        #endregion

        #region ICloneable

        /// <summary>
        /// Creates a new ToleranceEntry that is a copy of the current instance.
        /// </summary>
        /// <returns>A new ToleranceEntry that is a copy of this instance.</returns>
        public object Clone()
        {
            return new ToleranceEntry
            {
                GeometricSymbol = geometricSymbol,
                Tolerance1 = (ToleranceValue) tolerance1.Clone(),
                Tolerance2 = (ToleranceValue) tolerance1.Clone(),
                Datum1 = (DatumReferenceValue) datum1.Clone(),
                Datum2 = (DatumReferenceValue) datum1.Clone(),
                Datum3 = (DatumReferenceValue) datum1.Clone(),
            };
        }

        #endregion
    }
}