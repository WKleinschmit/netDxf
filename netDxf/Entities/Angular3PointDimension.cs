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
using netDxf.Blocks;
using netDxf.Tables;

namespace netDxf.Entities
{
    /// <summary>
    /// Represents a 3 point angular dimension <see cref="EntityObject">entity</see>.
    /// </summary>
    public class Angular3PointDimension :
        Dimension
    {
        #region private fields

        private double offset;
        private Vector2 center;
        private Vector2 start;
        private Vector2 end;

        #endregion

        #region constructors

        /// <summary>
        /// Initializes a new instance of the <c>Angular3PointDimension</c> class.
        /// </summary>
        public Angular3PointDimension()
            : this(Vector2.Zero, Vector2.UnitX, Vector2.UnitY, 0.1)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Angular3PointDimension</c> class.
        /// </summary>
        /// <param name="arc"><see cref="Arc">Arc</see> to measure.</param>
        /// <param name="offset">Distance between the center of the arc and the dimension line.</param>
        public Angular3PointDimension(Arc arc, double offset)
            : this(arc, offset, DimensionStyle.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Angular3PointDimension</c> class.
        /// </summary>
        /// <param name="arc">Angle <see cref="Arc">arc</see> to measure.</param>
        /// <param name="offset">Distance between the center of the arc and the dimension line.</param>
        /// <param name="style">The <see cref="DimensionStyle">style</see> to use with the dimension.</param>
        public Angular3PointDimension(Arc arc, double offset, DimensionStyle style)
            : base(DimensionType.Angular3Point)
        {
            if (arc == null)
                throw new ArgumentNullException(nameof(arc));

            Vector3 refPoint = MathHelper.Transform(arc.Center, arc.Normal, CoordinateSystem.World, CoordinateSystem.Object);
            center = new Vector2(refPoint.X, refPoint.Y);
            start = Vector2.Polar(center, arc.Radius, arc.StartAngle*MathHelper.DegToRad);
            end = Vector2.Polar(center, arc.Radius, arc.EndAngle*MathHelper.DegToRad);

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "The offset value must be equal or greater than zero.");
            this.offset = offset;

            if (style == null)
                throw new ArgumentNullException(nameof(style));
            Style = style;
            Normal = arc.Normal;
            Elevation = refPoint.Z;
            Update();
        }

        /// <summary>
        /// Initializes a new instance of the <c>Angular3PointDimension</c> class.
        /// </summary>
        /// <param name="centerPoint">Center of the angle arc to measure.</param>
        /// <param name="startPoint">Angle start point.</param>
        /// <param name="endPoint">Angle end point.</param>
        /// <param name="offset">Distance between the center point and the dimension line.</param>
        public Angular3PointDimension(Vector2 centerPoint, Vector2 startPoint, Vector2 endPoint, double offset)
            : this(centerPoint, startPoint, endPoint, offset, DimensionStyle.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Angular3PointDimension</c> class.
        /// </summary>
        /// <param name="centerPoint">Center of the angle arc to measure.</param>
        /// <param name="startPoint">Angle start point.</param>
        /// <param name="endPoint">Angle end point.</param>
        /// <param name="offset">Distance between the center point and the dimension line.</param>
        /// <param name="style">The <see cref="DimensionStyle">style</see> to use with the dimension.</param>
        public Angular3PointDimension(Vector2 centerPoint, Vector2 startPoint, Vector2 endPoint, double offset, DimensionStyle style)
            : base(DimensionType.Angular3Point)
        {
            center = centerPoint;
            start = startPoint;
            end = endPoint;
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
        /// Gets or sets the center <see cref="Vector2">point</see> of the arc in OCS (object coordinate system).
        /// </summary>
        public Vector2 CenterPoint
        {
            get { return center; }
            set { center = value; }
        }

        /// <summary>
        /// Gets or sets the angle start <see cref="Vector2">point</see> of the dimension in OCS (object coordinate system).
        /// </summary>
        public Vector2 StartPoint
        {
            get { return start; }
            set { start = value; }
        }

        /// <summary>
        /// Gets or sets the angle end <see cref="Vector2">point</see> of the dimension in OCS (object coordinate system).
        /// </summary>
        public Vector2 EndPoint
        {
            get { return end; }
            set { end = value; }
        }

        /// <summary>
        /// Gets the location of the dimension line arc.
        /// </summary>
        public Vector2 ArcDefinitionPoint
        {
            get { return defPoint; }
        }

        /// <summary>
        /// Gets or sets the distance between the center point and the dimension line.
        /// </summary>
        /// <remarks>
        /// Offset values cannot be negative and, even thought, zero values are allowed, they are not recommended.
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
            get
            {
                double angle = (Vector2.Angle(center, end) - Vector2.Angle(center, start))*MathHelper.RadToDeg;
                return MathHelper.NormalizeAngle(angle);
            }
        }

        #endregion

        #region public method

        /// <summary>
        /// Calculates the dimension offset from a point along the dimension line.
        /// </summary>
        /// <param name="point">Point along the dimension line.</param>
        /// <remarks>
        /// The start and end points of the reference lines will be modified,
        /// the angle measurement is always made from the direction of the center-first point line to the direction of the center-second point line.
        /// </remarks>
        public void SetDimensionLinePosition(Vector2 point)
        {
            SetDimensionLinePosition(point, true);
        }

        #endregion

        #region private methods

        public void SetDimensionLinePosition(Vector2 point, bool updateRefs)
        {
            if (updateRefs)
            {
                Vector2 refStart = start;
                Vector2 refEnd = end;
                Vector2 dirStart = refStart - center;
                Vector2 dirEnd = refEnd - center;
                Vector2 dirPoint = point - center;
                double cross = Vector2.CrossProduct(dirStart, dirEnd);
                double cross1 = Vector2.CrossProduct(dirStart, dirPoint);
                double cross2 = Vector2.CrossProduct(dirEnd, dirPoint);
                if (cross >= 0)
                {
                    if (!(cross1 >= 0 && cross2 < 0))
                    {
                        start = refEnd;
                        end = refStart;
                    }
                }              
                else if (cross1 < 0 && cross2 >= 0)
                {
                    start = refEnd;
                    end = refStart;
                }
            }
            
            offset = Vector2.Distance(center, point);

            double startAngle = Vector2.Angle(center, start);
            double midRot = startAngle + Measurement * MathHelper.DegToRad * 0.5;
            Vector2 midDim = Vector2.Polar(center, offset, midRot);
            defPoint = midDim;

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

                double gap = textGap * scale;
                textRefPoint = midDim + gap * Vector2.Normalize(midDim - center);
            }
        }

        #endregion

        #region overrides

        /// <summary>
        /// Moves, scales, and/or rotates the current entity given a 3x3 transformation matrix and a translation vector.
        /// </summary>
        /// <param name="transformation">Transformation matrix.</param>
        /// <param name="translation">Translation vector.</param>
        /// <remarks>
        /// Non-uniform scaling is not supported.
        /// </remarks>
        public override void TransformBy(Matrix3 transformation, Vector3 translation)
        {
            Vector2 newStart;
            Vector2 newEnd;
            Vector2 newCenter;
            Vector3 newNormal;
            double newElevation;
            double newScale;

            newNormal = transformation * Normal;
            newScale = newNormal.Modulus();

            Matrix3 transOW = MathHelper.ArbitraryAxis(Normal);
            Matrix3 transWO = MathHelper.ArbitraryAxis(newNormal).Transpose();

            Vector3 v = transOW * new Vector3(StartPoint.X, StartPoint.Y, Elevation);
            v = transformation * v + translation;
            v = transWO * v;
            newStart = new Vector2(v.X, v.Y);
            newElevation = v.Z;

            v = transOW * new Vector3(EndPoint.X, EndPoint.Y, Elevation);
            v = transformation * v + translation;
            v = transWO * v;
            newEnd = new Vector2(v.X, v.Y);

            v = transOW * new Vector3(CenterPoint.X, CenterPoint.Y, Elevation);
            v = transformation * v + translation;
            v = transWO * v;
            newCenter = new Vector2(v.X, v.Y);

            v = transOW * new Vector3(textRefPoint.X, textRefPoint.Y, Elevation);
            v = transformation * v + translation;
            v = transWO * v;
            textRefPoint = new Vector2(v.X, v.Y);

            v = transOW * new Vector3(defPoint.X, defPoint.Y, Elevation);
            v = transformation * v + translation;
            v = transWO * v;
            defPoint = new Vector2(v.X, v.Y);

            Offset *= newScale;
            StartPoint = newStart;
            EndPoint = newEnd;
            CenterPoint = newCenter;
            Elevation = newElevation;
            Normal = newNormal;
        }

        /// <summary>
        /// Calculate the dimension reference points.
        /// </summary>
        protected override void CalculteReferencePoints()
        {
            DimensionStyleOverride styleOverride;

            double measure = Measurement;
            double startAngle = Vector2.Angle(center, start);
            double midRot = startAngle + measure * MathHelper.DegToRad * 0.5;
            Vector2 midDim = Vector2.Polar(center, offset, midRot);

            defPoint = midDim;

            if (TextPositionManuallySet)
            {
                DimensionStyleFitTextMove moveText = Style.FitTextMove;
                if (StyleOverrides.TryGetValue(DimensionStyleOverrideType.FitTextMove, out styleOverride))
                {
                    moveText = (DimensionStyleFitTextMove)styleOverride.Value;
                }

                if (moveText == DimensionStyleFitTextMove.BesideDimLine)
                {
                    SetDimensionLinePosition(textRefPoint, false);
                }
            }
            else
            {
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

                double gap = textGap * scale;
                textRefPoint = midDim + gap * Vector2.Normalize(midDim - center);
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
        /// Creates a new Angular3PointDimension that is a copy of the current instance.
        /// </summary>
        /// <returns>A new Angular3PointDimension that is a copy of this instance.</returns>
        public override object Clone()
        {
            Angular3PointDimension entity = new Angular3PointDimension
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
                //Angular3PointDimension properties
                CenterPoint = center,
                StartPoint = start,
                EndPoint = end,
                Offset = offset
            };

            foreach (DimensionStyleOverride styleOverride in StyleOverrides.Values)
            {
                object copy;
                ICloneable value = styleOverride.Value as ICloneable;
                copy = value != null ? value.Clone() : styleOverride.Value;

                entity.StyleOverrides.Add(new DimensionStyleOverride(styleOverride.Type, copy));
            }

            foreach (XData data in XData.Values)
                entity.XData.Add((XData) data.Clone());

            return entity;
        }

        #endregion
    }
}