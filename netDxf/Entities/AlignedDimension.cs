#region netDxf library, Copyright (C) 2009-2019 Daniel Carvajal (haplokuon@gmail.com)

//                        netDxf library
// Copyright (C) 2009-2019 Daniel Carvajal (haplokuon@gmail.com)
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
using System.Collections.Generic;
using netDxf.Blocks;
using netDxf.Tables;

namespace netDxf.Entities
{
    /// <summary>
    /// Represents a dimension <see cref="EntityObject">entity</see> that is aligned the reference line.
    /// </summary>
    public class AlignedDimension :
        Dimension
    {
        #region private fields

        private Vector2 firstRefPoint;
        private Vector2 secondRefPoint;
        private double offset;

        #endregion

        #region constructors

        /// <summary>
        /// Initializes a new instance of the <c>AlignedDimension</c> class.
        /// </summary>
        public AlignedDimension()
            : this(Vector2.Zero, Vector2.UnitX, 0.1)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>AlignedDimension</c> class.
        /// </summary>
        /// <param name="referenceLine">Reference <see cref="Line">line</see> of the dimension.</param>
        /// <param name="offset">Distance between the reference line and the dimension line.</param>
        /// <remarks>The reference points define the distance to be measure.</remarks>
        public AlignedDimension(Line referenceLine, double offset)
            : this(referenceLine, offset, Vector3.UnitZ, DimensionStyle.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>AlignedDimension</c> class.
        /// </summary>
        /// <param name="referenceLine">Reference <see cref="Line">line</see> of the dimension.</param>
        /// <param name="offset">Distance between the reference line and the dimension line.</param>
        /// <param name="style">The <see cref="DimensionStyle">style</see> to use with the dimension.</param>
        /// <remarks>The reference points define the distance to be measure.</remarks>
        public AlignedDimension(Line referenceLine, double offset, DimensionStyle style)
            : this(referenceLine, offset, Vector3.UnitZ, style)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>AlignedDimension</c> class.
        /// </summary>
        /// <param name="referenceLine">Reference <see cref="Line">line</see> of the dimension.</param>
        /// <param name="offset">Distance between the reference line and the dimension line.</param>
        /// <param name="normal">Normal vector of the plane where the dimension is defined.</param>
        /// <remarks>The reference points define the distance to be measure.</remarks>
        public AlignedDimension(Line referenceLine, double offset, Vector3 normal)
            : this(referenceLine, offset, normal, DimensionStyle.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>AlignedDimension</c> class.
        /// </summary>
        /// <param name="referenceLine">Reference <see cref="Line">line</see> of the dimension.</param>
        /// <param name="offset">Distance between the reference line and the dimension line.</param>
        /// <param name="normal">Normal vector of the plane where the dimension is defined.</param>
        /// <param name="style">The <see cref="DimensionStyle">style</see> to use with the dimension.</param>
        /// <remarks>The reference line define the distance to be measure.</remarks>
        public AlignedDimension(Line referenceLine, double offset, Vector3 normal, DimensionStyle style)
            : base(DimensionType.Aligned)
        {
            if (referenceLine == null)
                throw new ArgumentNullException(nameof(referenceLine));

            IList<Vector3> ocsPoints = MathHelper.Transform(
                new List<Vector3> { referenceLine.StartPoint, referenceLine.EndPoint }, normal, CoordinateSystem.World, CoordinateSystem.Object);
            firstRefPoint = new Vector2(ocsPoints[0].X, ocsPoints[0].Y);
            secondRefPoint = new Vector2(ocsPoints[1].X, ocsPoints[1].Y);

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "The offset value must be equal or greater than zero.");
            this.offset = offset;
            if (style == null)
                throw new ArgumentNullException(nameof(style));
            Style = style;
            Normal = normal;
            Elevation = ocsPoints[0].Z;
            Update();
        }

        /// <summary>
        /// Initializes a new instance of the <c>AlignedDimension</c> class.
        /// </summary>
        /// <param name="firstPoint">First reference <see cref="Vector2">point</see> of the dimension.</param>
        /// <param name="secondPoint">Second reference <see cref="Vector2">point</see> of the dimension.</param>
        /// <param name="offset">Distance between the reference line and the dimension line.</param>
        /// <remarks>The reference points define the distance to be measure.</remarks>
        public AlignedDimension(Vector2 firstPoint, Vector2 secondPoint, double offset)
            : this(firstPoint, secondPoint, offset, DimensionStyle.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>AlignedDimension</c> class.
        /// </summary>
        /// <param name="firstPoint">First reference <see cref="Vector2">point</see> of the dimension.</param>
        /// <param name="secondPoint">Second reference <see cref="Vector2">point</see> of the dimension.</param>
        /// <param name="offset">Distance between the reference line and the dimension line.</param>
        /// <param name="style">The <see cref="DimensionStyle">style</see> to use with the dimension.</param>
        /// <remarks>The reference points define the distance to be measure.</remarks>
        public AlignedDimension(Vector2 firstPoint, Vector2 secondPoint, double offset, DimensionStyle style)
            : base(DimensionType.Aligned)
        {
            firstRefPoint = firstPoint;
            secondRefPoint = secondPoint;
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "The offset value must be equal or greater than zero.");
            this.offset = offset;
            if (style == null)
                throw new ArgumentNullException(nameof(style));
            Style = style;
            Update();
        }

        #endregion

        #region public properties

        /// <summary>
        /// Gets or sets the first definition point of the dimension in OCS (object coordinate system).
        /// </summary>
        public Vector2 FirstReferencePoint
        {
            get { return firstRefPoint; }
            set { firstRefPoint = value; }
        }

        /// <summary>
        /// Gets or sets the second definition point of the dimension in OCS (object coordinate system).
        /// </summary>
        public Vector2 SecondReferencePoint
        {
            get { return secondRefPoint; }
            set { secondRefPoint = value; }
        }

        /// <summary>
        /// Gets the location of the dimension line.
        /// </summary>
        public Vector2 DimLinePosition
        {
            get { return defPoint; }
        }

        /// <summary>
        /// Gets or sets the distance between the reference line and the dimension line.
        /// </summary>
        /// <remarks>
        /// The offset value must be equal or greater than zero.<br />
        /// The side at which the dimension line is drawn depends of the direction of its reference line.
        /// </remarks>
        public double Offset
        {
            get { return offset; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "The offset value must be equal or greater than zero.");
                offset = value;
            }
        }

        /// <summary>
        /// Actual measurement.
        /// </summary>
        /// <remarks>The dimension is always measured in the plane defined by the normal.</remarks>
        public override double Measurement
        {
            get { return Vector2.Distance(firstRefPoint, secondRefPoint); }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Calculates the dimension offset from a point along the dimension line.
        /// </summary>
        /// <param name="point">Point along the dimension line.</param>
        public void SetDimensionLinePosition(Vector2 point)
        {
            Vector2 refDir = Vector2.Normalize(secondRefPoint - firstRefPoint);
            Vector2 offsetDir = point - firstRefPoint;

            double cross = Vector2.CrossProduct(refDir, offsetDir);
            if (cross < 0)
            {
                Vector2 tmp = firstRefPoint;
                firstRefPoint = secondRefPoint;
                secondRefPoint = tmp;
                refDir *= -1;
            }

            Vector2 vec = Vector2.Perpendicular(refDir);
            offset = MathHelper.PointLineDistance(point, firstRefPoint, refDir);
            defPoint = secondRefPoint + offset * vec;

            if (!TextPositionManuallySet)
            {
                DimensionStyleOverride styleOverride;
                double textGap = Style.TextOffset;
                if (StyleOverrides.TryGetValue(DimensionStyleOverrideType.TextOffset, out styleOverride))
                {
                    textGap = (double)styleOverride.Value;
                }
                double scale = Style.DimScaleOverall;
                if (StyleOverrides.TryGetValue(DimensionStyleOverrideType.DimScaleOverall, out styleOverride))
                {
                    scale = (double)styleOverride.Value;
                }

                double gap = offset + textGap * scale;
                textRefPoint = Vector2.MidPoint(firstRefPoint, secondRefPoint) + gap * vec;
            }
        }

        #endregion

        #region overrides

        /// <summary>
        /// Moves, scales, and/or rotates the current entity given a 3x3 transformation matrix and a translation vector.
        /// </summary>
        /// <param name="transformation">Transformation matrix.</param>
        /// <param name="translation">Translation vector.</param>
        public override void TransformBy(Matrix3 transformation, Vector3 translation)
        {
            Vector2 newStart;
            Vector2 newEnd;
            Vector3 newNormal;
            double newElevation;
            double newScale;

            newNormal = transformation * Normal;
            newScale = newNormal.Modulus();

            Matrix3 transOW = MathHelper.ArbitraryAxis(Normal);
            Matrix3 transWO = MathHelper.ArbitraryAxis(newNormal).Transpose();

            Vector3 v = transOW * new Vector3(FirstReferencePoint.X, FirstReferencePoint.Y, Elevation);
            v = transformation * v + translation;
            v = transWO * v;
            newStart = new Vector2(v.X, v.Y);
            newElevation = v.Z;

            v = transOW * new Vector3(SecondReferencePoint.X, SecondReferencePoint.Y, Elevation);
            v = transformation * v + translation;
            v = transWO * v;
            newEnd = new Vector2(v.X, v.Y);

            v = transOW * new Vector3(textRefPoint.X, textRefPoint.Y, Elevation);
            v = transformation * v + translation;
            v = transWO * v;
            textRefPoint = new Vector2(v.X, v.Y);

            v = transOW * new Vector3(defPoint.X, defPoint.Y, Elevation);
            v = transformation * v + translation;
            v = transWO * v;
            defPoint = new Vector2(v.X, v.Y);

            Offset *= newScale;
            FirstReferencePoint = newStart;
            SecondReferencePoint = newEnd;
            Elevation = newElevation;
            Normal = newNormal;
        }

        /// <summary>
        /// Calculate the dimension reference points.
        /// </summary>
        protected override void CalculteReferencePoints()
        {
            DimensionStyleOverride styleOverride;

            Vector2 ref1 = FirstReferencePoint;
            Vector2 ref2 = SecondReferencePoint;
            Vector2 dirRef = ref2 - ref1;
            Vector2 dirDesp = Vector2.Normalize(Vector2.Perpendicular(dirRef));
            Vector2 vec = offset* dirDesp;
            Vector2 dimRef1 = ref1 + vec;
            Vector2 dimRef2 = ref2 + vec;

            defPoint = dimRef2;

            if (TextPositionManuallySet)
            {
                DimensionStyleFitTextMove moveText = Style.FitTextMove;
                if (StyleOverrides.TryGetValue(DimensionStyleOverrideType.FitTextMove, out styleOverride))
                {
                    moveText = (DimensionStyleFitTextMove) styleOverride.Value;
                }

                if (moveText == DimensionStyleFitTextMove.BesideDimLine)
                {
                    SetDimensionLinePosition(textRefPoint);
                }
            }
            else
            {
                double textGap = Style.TextOffset;
                if (StyleOverrides.TryGetValue(DimensionStyleOverrideType.TextOffset, out styleOverride))
                {
                    textGap = (double) styleOverride.Value;
                }
                double scale = Style.DimScaleOverall;
                if (StyleOverrides.TryGetValue(DimensionStyleOverrideType.DimScaleOverall, out styleOverride))
                {
                    scale = (double) styleOverride.Value;
                }

                double gap = textGap*scale;
                textRefPoint = Vector2.MidPoint(dimRef1, dimRef2) + gap*dirDesp;
            }
        }

        /// <summary>
        /// Gets the block that contains the entities that make up the dimension picture.
        /// </summary>
        /// <param name="name">Name to be assigned to the generated block.</param>
        /// <returns>The block that represents the actual dimension.</returns>
        protected override Block BuildBlock(string name)
        {
            return DimensionBlock.Build(this, name);
        }

        /// <summary>
        /// Creates a new AlignedDimension that is a copy of the current instance.
        /// </summary>
        /// <returns>A new AlignedDimension that is a copy of this instance.</returns>
        public override object Clone()
        {
            AlignedDimension entity = new AlignedDimension
            {
                //EntityObject properties
                Layer = (Layer) Layer.Clone(),
                Linetype = (Linetype) Linetype.Clone(),
                Color = (AciColor) Color.Clone(),
                Lineweight = Lineweight,
                Transparency = (Transparency) Transparency.Clone(),
                LinetypeScale = LinetypeScale,
                Normal = Normal,
                IsVisible = IsVisible,
                //Dimension properties
                Style = (DimensionStyle) Style.Clone(),
                DefinitionPoint = DefinitionPoint,
                TextReferencePoint = TextReferencePoint,
                TextPositionManuallySet = TextPositionManuallySet,
                TextRotation = TextRotation,
                AttachmentPoint = AttachmentPoint,
                LineSpacingStyle = LineSpacingStyle,
                LineSpacingFactor = LineSpacingFactor,
                UserText = UserText,
                Elevation = Elevation,
                //AlignedDimension properties
                FirstReferencePoint = firstRefPoint,
                SecondReferencePoint = secondRefPoint,
                Offset = offset
            };

            foreach (DimensionStyleOverride styleOverride in StyleOverrides.Values)
            {
                ICloneable value = styleOverride.Value as ICloneable;
                object copy = value != null ? value.Clone() : styleOverride.Value;

                entity.StyleOverrides.Add(new DimensionStyleOverride(styleOverride.Type, copy));
            }

            foreach (XData data in XData.Values)
                entity.XData.Add((XData) data.Clone());

            return entity;
        }

        #endregion
    }
}