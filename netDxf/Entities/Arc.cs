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
using netDxf.Tables;

namespace netDxf.Entities
{
    /// <summary>
    /// Represents a circular arc <see cref="EntityObject">entity</see>.
    /// </summary>
    public class Arc :
        EntityObject
    {
        #region private fields

        private Vector3 center;
        private double radius;
        private double startAngle;
        private double endAngle;
        private double thickness;

        #endregion

        #region constructors

        /// <summary>
        /// Initializes a new instance of the <c>Arc</c> class.
        /// </summary>
        public Arc()
            : this(Vector3.Zero, 1.0, 0.0, 180.0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Arc</c> class.
        /// </summary>
        /// <param name="center">Arc <see cref="Vector2">center</see> in world coordinates.</param>
        /// <param name="radius">Arc radius.</param>
        /// <param name="startAngle">Arc start angle in degrees.</param>
        /// <param name="endAngle">Arc end angle in degrees.</param>
        public Arc(Vector2 center, double radius, double startAngle, double endAngle)
            : this(new Vector3(center.X, center.Y, 0.0), radius, startAngle, endAngle)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Arc</c> class.
        /// </summary>
        /// <param name="center">Arc <see cref="Vector3">center</see> in world coordinates.</param>
        /// <param name="radius">Arc radius.</param>
        /// <param name="startAngle">Arc start angle in degrees.</param>
        /// <param name="endAngle">Arc end angle in degrees.</param>
        public Arc(Vector3 center, double radius, double startAngle, double endAngle)
            : base(EntityType.Arc, DxfObjectCode.Arc)
        {
            this.center = center;
            if (radius <= 0)
                throw new ArgumentOutOfRangeException(nameof(radius), radius, "The circle radius must be greater than zero.");
            this.radius = radius;
            this.startAngle = MathHelper.NormalizeAngle(startAngle);
            this.endAngle = MathHelper.NormalizeAngle(endAngle);
            thickness = 0.0;
        }

        #endregion

        #region public properties

        /// <summary>
        /// Gets or sets the arc <see cref="Vector3">center</see> in world coordinates.
        /// </summary>
        public Vector3 Center
        {
            get { return center; }
            set { center = value; }
        }

        /// <summary>
        /// Gets or sets the arc radius.
        /// </summary>
        public double Radius
        {
            get { return radius; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The arc radius must be greater than zero.");
                radius = value;
            }
        }

        /// <summary>
        /// Gets or sets the arc start angle in degrees.
        /// </summary>
        public double StartAngle
        {
            get { return startAngle; }
            set { startAngle = MathHelper.NormalizeAngle(value); }
        }

        /// <summary>
        /// Gets or sets the arc end angle in degrees.
        /// </summary>
        public double EndAngle
        {
            get { return endAngle; }
            set { endAngle = MathHelper.NormalizeAngle(value); }
        }

        /// <summary>
        /// Gets or sets the arc thickness.
        /// </summary>
        public double Thickness
        {
            get { return thickness; }
            set { thickness = value; }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Converts the arc in a list of vertexes.
        /// </summary>
        /// <param name="precision">Number of divisions.</param>
        /// <returns>A list vertexes that represents the arc expressed in object coordinate system.</returns>
        public List<Vector2> PolygonalVertexes(int precision)
        {
            if (precision < 2)
                throw new ArgumentOutOfRangeException(nameof(precision), precision, "The arc precision must be greater or equal to three");

            List<Vector2> ocsVertexes = new List<Vector2>();
            double start = startAngle*MathHelper.DegToRad;
            double end = endAngle*MathHelper.DegToRad;
            if (end < start) end += MathHelper.TwoPI;
            double delta = (end - start)/precision;
            for (int i = 0; i <= precision; i++)
            {
                double angle = start + delta*i;
                double sine = radius*Math.Sin(angle);
                double cosine = radius*Math.Cos(angle);
                ocsVertexes.Add(new Vector2(cosine, sine));
            }

            return ocsVertexes;
        }

        /// <summary>
        /// Converts the arc in a Polyline.
        /// </summary>
        /// <param name="precision">Number of divisions.</param>
        /// <returns>A new instance of <see cref="LwPolyline">LightWeightPolyline</see> that represents the arc.</returns>
        public LwPolyline ToPolyline(int precision)
        {
            IEnumerable<Vector2> vertexes = PolygonalVertexes(precision);
            Vector3 ocsCenter = MathHelper.Transform(center, Normal, CoordinateSystem.World, CoordinateSystem.Object);

            LwPolyline poly = new LwPolyline
            {
                Layer = (Layer) Layer.Clone(),
                Linetype = (Linetype) Linetype.Clone(),
                Color = (AciColor) Color.Clone(),
                Lineweight = Lineweight,
                Transparency = (Transparency) Transparency.Clone(),
                LinetypeScale = LinetypeScale,
                Normal = Normal,
                Elevation = ocsCenter.Z,
                Thickness = Thickness,
                IsClosed = false
            };
            foreach (Vector2 v in vertexes)
            {
                poly.Vertexes.Add(new LwPolylineVertex(v.X + ocsCenter.X, v.Y + ocsCenter.Y));
            }
            return poly;
        }

        #endregion

        #region overrides

        /// <summary>
        /// Moves, scales, and/or rotates the current entity given a 3x3 transformation matrix and a translation vector.
        /// </summary>
        /// <param name="transformation">Transformation matrix.</param>
        /// <param name="translation">Translation vector.</param>
        /// <remarks>Non-uniform scaling is not supported, create an ellipse arc from the arc data and transform that instead.</remarks>
        public override void TransformBy(Matrix3 transformation, Vector3 translation)
        {
            Vector3 newCenter;
            Vector3 newNormal;
            double newRadius;
            double newStartAngle;
            double newEndAngle;
            double newScale;

            newCenter = transformation * Center + translation;
            newNormal = transformation * Normal;

            Matrix3 transOW = MathHelper.ArbitraryAxis(Normal);
            Matrix3 transWO = MathHelper.ArbitraryAxis(newNormal).Transpose();

            //Vector2 axis = new Vector2(this.Radius, 0.0);
            //Vector3 v = transOW * new Vector3(axis.X, axis.Y, 0.0);
            //v = transformation * v;
            //v = transWO * v;
            //Vector2 axisPoint = new Vector2(v.X, v.Y);
            //newRadius = axisPoint.Modulus();
            //newRadius = newRadius <= 0 ? MathHelper.Epsilon : newRadius;

            newScale = newNormal.Modulus();
            newRadius = Radius * newScale;
            newRadius = MathHelper.IsZero(newRadius) ? MathHelper.Epsilon : newRadius;

            Vector2 start = Vector2.Rotate(new Vector2(Radius, 0.0), StartAngle * MathHelper.DegToRad);
            Vector2 end = Vector2.Rotate(new Vector2(Radius, 0.0), EndAngle * MathHelper.DegToRad);

            Vector3 vStart = transOW * new Vector3(start.X, start.Y, 0.0);
            vStart = transformation * vStart;
            vStart = transWO * vStart;

            Vector3 vEnd = transOW * new Vector3(end.X, end.Y, 0.0);
            vEnd = transformation * vEnd;
            vEnd = transWO * vEnd;

            Vector2 startPoint = new Vector2(vStart.X, vStart.Y);
            Vector2 endPoint = new Vector2(vEnd.X, vEnd.Y);

            double sign = Math.Sign(transformation.M11 * transformation.M22 * transformation.M33) < 0 ? 180 : 0;
            newStartAngle = sign + Vector2.Angle(startPoint) * MathHelper.RadToDeg;
            newEndAngle = sign + Vector2.Angle(endPoint) * MathHelper.RadToDeg;

            Normal = newNormal;
            Center = newCenter;
            Radius = newRadius;
            StartAngle = newStartAngle;
            EndAngle = newEndAngle;
        }

        /// <summary>
        /// Creates a new Arc that is a copy of the current instance.
        /// </summary>
        /// <returns>A new Arc that is a copy of this instance.</returns>
        public override object Clone()
        {
            Arc entity = new Arc
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
                //Arc properties
                Center = center,
                Radius = radius,
                StartAngle = startAngle,
                EndAngle = endAngle,
                Thickness = thickness
            };

            foreach (XData data in XData.Values)
                entity.XData.Add((XData) data.Clone());

            return entity;
        }

        #endregion
    }
}