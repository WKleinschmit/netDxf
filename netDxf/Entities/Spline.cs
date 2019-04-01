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
    /// Represents a spline curve <see cref="EntityObject">entity</see> (NURBS Non-Uniform Rational B-Splines).
    /// </summary>
    public class Spline :
        EntityObject
    {
        #region private fields

        private readonly List<Vector3> fitPoints;
        private readonly SplineCreationMethod creationMethod;
        private Vector3? startTangent;
        private Vector3? endTangent;
        private SplineKnotParameterization knotParameterization;
        private double knotTolerance = 0.0000001;
        private double ctrlPointTolerance = 0.0000001;
        private double fitTolerance = 0.0000000001;
        private List<SplineVertex> controlPoints;
        private List<double> knots;
        private readonly SplinetypeFlags flags;
        private readonly short degree;
        private readonly bool isClosed;
        private readonly bool isPeriodic;

        #endregion

        #region constructors

        /// <summary>
        /// Initializes a new instance of the <c>Spline</c> class.
        /// </summary>
        /// <param name="fitPoints">Spline fit points.</param>
        /// <remarks>Spline entities created with a list of fit points cannot be used as a boundary path in a hatch.</remarks>
        public Spline(IEnumerable<Vector3> fitPoints)
            : base(EntityType.Spline, DxfObjectCode.Spline)
        {
            degree = 3;
            isPeriodic = false;
            controlPoints = new List<SplineVertex>();
            knots = new List<double>();
            if (fitPoints == null)
                throw new ArgumentNullException(nameof(fitPoints));
            this.fitPoints = new List<Vector3>(fitPoints);
            creationMethod = SplineCreationMethod.FitPoints;
            isClosed = this.fitPoints[0].Equals(this.fitPoints[this.fitPoints.Count - 1]);
            flags = isClosed ? SplinetypeFlags.Closed | SplinetypeFlags.Rational : SplinetypeFlags.Rational;
        }

        /// <summary>
        /// Initializes a new instance of the <c>Spline</c> class.
        /// </summary>
        /// <param name="controlPoints">Spline control points.</param>
        /// <param name="knots">Spline knot vector.</param>
        /// <param name="degree">Degree of the spline curve.  Valid values are 1 (linear), degree 2 (quadratic), degree 3 (cubic), and so on up to degree 10.</param>
        public Spline(List<SplineVertex> controlPoints, List<double> knots, short degree)
            : this(controlPoints, knots, degree, new List<Vector3>(), SplineCreationMethod.ControlPoints, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Spline</c> class.
        /// </summary>
        /// <param name="controlPoints">Spline control points.</param>
        /// <remarks>By default the degree of the spline is equal three.</remarks>
        public Spline(List<SplineVertex> controlPoints)
            : this(controlPoints, 3, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Spline</c> class.
        /// </summary>
        /// <param name="controlPoints">Spline control points.</param>
        /// <param name="periodic">Sets if the spline as periodic closed (default false).</param>
        /// <remarks>By default the degree of the spline is equal three.</remarks>
        public Spline(List<SplineVertex> controlPoints, bool periodic)
            : this(controlPoints, 3, periodic)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Spline</c> class.
        /// </summary>
        /// <param name="controlPoints">Spline control points.</param>
        /// <param name="degree">Degree of the spline curve.  Valid values are 1 (linear), degree 2 (quadratic), degree 3 (cubic), and so on up to degree 10.</param>
        public Spline(List<SplineVertex> controlPoints, short degree)
            : this(controlPoints, degree, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Spline</c> class.
        /// </summary>
        /// <param name="controlPoints">Spline control points.</param>
        /// <param name="degree">Degree of the spline curve.  Valid values are 1 (linear), degree 2 (quadratic), degree 3 (cubic), and so on up to degree 10.</param>
        /// <param name="periodic">Sets if the spline as periodic closed (default false).</param>
        public Spline(List<SplineVertex> controlPoints, short degree, bool periodic)
            : base(EntityType.Spline, DxfObjectCode.Spline)
        {
            if (degree < 1 || degree > 10)
                throw new ArgumentOutOfRangeException(nameof(degree), degree, "The spline degree valid values range from 1 to 10.");
            if (controlPoints == null)
                throw new ArgumentNullException(nameof(controlPoints));
            if (controlPoints.Count < 2)
                throw new ArgumentException("The number of control points must be equal or greater than 2.");
            if (controlPoints.Count < degree + 1)
                throw new ArgumentException("The number of control points must be equal or greater than the spline degree + 1.");

            fitPoints = new List<Vector3>();
            this.degree = degree;
            creationMethod = SplineCreationMethod.ControlPoints;

            isPeriodic = periodic;
            if (isPeriodic)
            {
                isClosed = true;
                flags = SplinetypeFlags.Closed | SplinetypeFlags.Periodic | SplinetypeFlags.Rational;
            }
            else
            {
                isClosed = controlPoints[0].Position.Equals(controlPoints[controlPoints.Count - 1].Position);
                flags = isClosed ? SplinetypeFlags.Closed | SplinetypeFlags.Rational : SplinetypeFlags.Rational;
            }
            Create(controlPoints);
        }

        /// <summary>
        /// Initializes a new instance of the <c>Spline</c> class.
        /// </summary>
        /// <param name="controlPoints">Spline control points.</param>
        /// <param name="knots">Spline knot vector.</param>
        /// <param name="degree">Degree of the spline curve.  Valid values are 1 (linear), degree 2 (quadratic), degree 3 (cubic), and so on up to degree 10.</param>
        internal Spline(List<SplineVertex> controlPoints, List<double> knots, short degree, List<Vector3> fitPoints, SplineCreationMethod method, bool isPeriodic)
            : base(EntityType.Spline, DxfObjectCode.Spline)
        {
            if (degree < 1 || degree > 10)
                throw new ArgumentOutOfRangeException(nameof(degree), degree, "The spline degree valid values range from 1 to 10.");
            if (controlPoints == null)
                throw new ArgumentNullException(nameof(controlPoints));
            if (controlPoints.Count < 2)
                throw new ArgumentException("The number of control points must be equal or greater than 2.");
            if (controlPoints.Count < degree + 1)
                throw new ArgumentException("The number of control points must be equal or greater than the spline degree + 1.");
            if (knots == null)
                throw new ArgumentNullException(nameof(knots));
            if (knots.Count != controlPoints.Count + degree + 1)
                throw new ArgumentException("The number of knots must be equals to the number of control points + spline degree + 1.");

            this.fitPoints = fitPoints;
            this.controlPoints = controlPoints;
            this.knots = knots;
            this.degree = degree;
            creationMethod = method;

            this.isPeriodic = isPeriodic;
            if (this.isPeriodic)
            {
                isClosed = true;
                flags = SplinetypeFlags.Closed | SplinetypeFlags.Periodic | SplinetypeFlags.Rational;
            }
            else
            {
                isClosed = controlPoints[0].Position.Equals(controlPoints[controlPoints.Count - 1].Position);
                flags = isClosed ? SplinetypeFlags.Closed | SplinetypeFlags.Rational : SplinetypeFlags.Rational;
            }
        }

        #endregion

        #region public properties

        /// <summary>
        /// Gets the spline <see cref="Vector3">fit points</see> list.
        /// </summary>
        public List<Vector3> FitPoints
        {
            get { return fitPoints; }
        }

        /// <summary>
        /// Gets or sets the spline curve start tangent.
        /// </summary>
        /// <remarks>Only applicable to splines created with fit points.</remarks>
        public Vector3? StartTangent
        {
            get { return startTangent; }
            set { startTangent = value; }
        }

        /// <summary>
        /// Gets or sets the spline curve end tangent.
        /// </summary>
        /// <remarks>Only applicable to splines created with fit points.</remarks>
        public Vector3? EndTangent
        {
            get { return endTangent; }
            set { endTangent = value; }
        }

        /// <summary>
        /// Gets or set the knot parameterization computational method.
        /// </summary>
        /// <remarks>Only applicable to splines created with fit points.</remarks>
        public SplineKnotParameterization KnotParameterization
        {
            get { return knotParameterization; }
            set { knotParameterization = value; }
        }

        /// <summary>
        /// Gets the spline creation method.
        /// </summary>
        public SplineCreationMethod CreationMethod
        {
            get { return creationMethod; }
        }

        /// <summary>
        /// Gets or sets the knot tolerance.
        /// </summary>
        public double KnotTolerance
        {
            get { return knotTolerance; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The knot tolerance must be greater than zero.");
                knotTolerance = value;
            }
        }

        /// <summary>
        /// Gets or sets the control point tolerance.
        /// </summary>
        public double CtrlPointTolerance
        {
            get { return ctrlPointTolerance; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The control point tolerance must be greater than zero.");
                ctrlPointTolerance = value;
            }
        }

        /// <summary>
        /// Gets or sets the fit point tolerance.
        /// </summary>
        public double FitTolerance
        {
            get { return fitTolerance; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The fit tolerance must be greater than zero.");
                fitTolerance = value;
            }
        }

        /// <summary>
        /// Gets or sets the polynomial degree of the resulting spline.
        /// </summary>
        /// <remarks>
        /// Valid values are 1 (linear), degree 2 (quadratic), degree 3 (cubic), and so on up to degree 10.
        /// </remarks>
        public short Degree
        {
            get { return degree; }
        }

        /// <summary>
        /// Gets if the spline is closed.
        /// </summary>
        public bool IsClosed
        {
            get { return isClosed; }
        }

        /// <summary>
        /// Gets if the spline is periodic.
        /// </summary>
        public bool IsPeriodic
        {
            get { return isPeriodic; }
        }

        /// <summary>
        /// Gets the spline <see cref="SplineVertex">control points</see> list.
        /// </summary>
        public IReadOnlyList<SplineVertex> ControlPoints
        {
            get { return controlPoints; }
        }

        /// <summary>
        /// Gets the spline knot vector.
        /// </summary>
        /// <remarks>By default a uniform knot vector is created.</remarks>
        public IReadOnlyList<double> Knots
        {
            get { return knots; }
        }

        #endregion

        #region internal properties

        /// <summary>
        /// Gets the spline type.
        /// </summary>
        internal SplinetypeFlags Flags
        {
            get { return flags; }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Switch the polyline direction.
        /// </summary>
        public void Reverse()
        {
            fitPoints.Reverse();
            controlPoints.Reverse();
            Vector3? tmp = startTangent;
            startTangent = -endTangent;
            endTangent = -tmp;
        }

        /// <summary>
        /// Sets all control point weights to the specified number.
        /// </summary>
        /// <param name="weight">Control point weight.</param>
        public void SetUniformWeights(double weight)
        {
            foreach (SplineVertex controlPoint in controlPoints)
            {
                controlPoint.Weight = weight;
            }
        }

        #endregion

        #region private methods

        private void Create(List<SplineVertex> points)
        {
            controlPoints = new List<SplineVertex>();

            int replicate = isPeriodic ? degree : 0;
            int numControlPoints = points.Count + replicate;

            foreach (SplineVertex controlPoint in points)
            {
                SplineVertex vertex = new SplineVertex(controlPoint.Position, controlPoint.Weight);
                controlPoints.Add(vertex);
            }

            for (int i = 0; i < replicate; i++)
            {
                SplineVertex vertex = new SplineVertex(points[i].Position, points[i].Weight);
                controlPoints.Add(vertex);
            }

            int numKnots = numControlPoints + degree + 1;
            knots = new List<double>(numKnots);

            double factor = 1.0/(numControlPoints - degree);
            if (!isPeriodic)
            {
                int i;
                for (i = 0; i <= degree; i++)
                    knots.Add(0.0);

                for (; i < numControlPoints; i++)
                    knots.Add(i - degree);

                for (; i < numKnots; i++)
                    knots.Add(numControlPoints - degree);
            }
            else
            {
                for (int i = 0; i < numKnots; i++)
                    knots.Add((i - degree)*factor);
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
            foreach (SplineVertex vertex in ControlPoints)
            {
                vertex.Position = transformation * vertex.Position + translation;
            }

            for (int i = 0; i < FitPoints.Count; i++)
            {
                FitPoints[i] = transformation * FitPoints[i] + translation;
            }

            Normal = transformation * Normal;

        }

        /// <summary>
        /// Creates a new Spline that is a copy of the current instance.
        /// </summary>
        /// <returns>A new Spline that is a copy of this instance.</returns>
        public override object Clone()
        {
            Spline entity;
            if (creationMethod == SplineCreationMethod.FitPoints)
            {
                entity = new Spline(new List<Vector3>(fitPoints))
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
                    //Spline properties
                    KnotParameterization = KnotParameterization,
                    StartTangent = startTangent,
                    EndTangent = endTangent
                };
            }
            else
            {
                List<SplineVertex> copyControlPoints = new List<SplineVertex>(controlPoints.Count);
                foreach (SplineVertex vertex in controlPoints)
                {
                    copyControlPoints.Add((SplineVertex) vertex.Clone());
                }
                List<double> copyKnots = new List<double>(knots);

                entity = new Spline(copyControlPoints, copyKnots, degree)
                {
                    //EntityObject properties
                    Layer = (Layer) Layer.Clone(),
                    Linetype = (Linetype) Linetype.Clone(),
                    Color = (AciColor) Color.Clone(),
                    Lineweight = Lineweight,
                    Transparency = (Transparency) Transparency.Clone(),
                    LinetypeScale = LinetypeScale,
                    Normal = Normal
                    //Spline properties
                };
            }


            foreach (XData data in XData.Values)
                entity.XData.Add((XData) data.Clone());

            return entity;
        }

        #endregion

        #region NURBS evaluator provided by mikau16 based on Michael V. implementation, roughly follows the notation of http://cs.mtu.edu/~shene/PUBLICATIONS/2004/NURBS.pdf

        /// <summary>
        /// Converts the spline in a list of vertexes.
        /// </summary>
        /// <param name="precision">Number of vertexes generated.</param>
        /// <returns>A list vertexes that represents the spline.</returns>
        public List<Vector3> PolygonalVertexes(int precision)
        {
            if (controlPoints.Count == 0)
                throw new NotSupportedException("A spline entity with control points is required.");

            double u_start;
            double u_end;
            List<Vector3> vertexes = new List<Vector3>();

            // added a few fixes to make it work for open, closed, and periodic closed splines.
            if (!isClosed)
            {
                precision -= 1;
                u_start = knots[0];
                u_end = knots[knots.Count - 1];
            }
            else if (isPeriodic)
            {
                u_start = knots[degree];
                u_end = knots[knots.Count - degree - 1];
            }
            else
            {
                u_start = knots[0];
                u_end = knots[knots.Count - 1];
            }

            double u_delta = (u_end - u_start)/precision;

            for (int i = 0; i < precision; i++)
            {
                double u = u_start + u_delta*i;
                vertexes.Add(C(u));
            }

            if (!isClosed)
                vertexes.Add(controlPoints[controlPoints.Count - 1].Position);

            return vertexes;
        }

        /// <summary>
        /// Converts the spline in a Polyline.
        /// </summary>
        /// <param name="precision">Number of vertexes generated.</param>
        /// <returns>A new instance of <see cref="Polyline">Polyline</see> that represents the spline.</returns>
        public Polyline ToPolyline(int precision)
        {
            IEnumerable<Vector3> vertexes = PolygonalVertexes(precision);

            Polyline poly = new Polyline
            {
                Layer = (Layer) Layer.Clone(),
                Linetype = (Linetype) Linetype.Clone(),
                Color = (AciColor) Color.Clone(),
                Lineweight = Lineweight,
                Transparency = (Transparency) Transparency.Clone(),
                LinetypeScale = LinetypeScale,
                Normal = Normal,
                IsClosed = isClosed
            };
            foreach (Vector3 v in vertexes)
            {
                poly.Vertexes.Add(new PolylineVertex(v));
            }
            return poly;
        }

        private Vector3 C(double u)
        {
            Vector3 vectorSum = Vector3.Zero;
            double denominatorSum = 0.0;

            // optimization suggested by ThVoss
            for (int i = 0; i < controlPoints.Count; i++)
            {
                double n = N(i, degree, u);
                denominatorSum += n*controlPoints[i].Weight;
                vectorSum += controlPoints[i].Weight*n*controlPoints[i].Position;
            }

            // avoid possible divided by zero error, this should never happen
            if (Math.Abs(denominatorSum) < double.Epsilon)
                return Vector3.Zero;

            return (1.0/denominatorSum)*vectorSum;
        }

        private double N(int i, int p, double u)
        {
            if (p <= 0)
            {
                if (knots[i] <= u && u < knots[i + 1])
                    return 1;
                return 0.0;
            }

            double leftCoefficient = 0.0;
            if (!(Math.Abs(knots[i + p] - knots[i]) < double.Epsilon))
                leftCoefficient = (u - knots[i])/(knots[i + p] - knots[i]);

            double rightCoefficient = 0.0; // article contains error here, denominator is Knots[i + p + 1] - Knots[i + 1]
            if (!(Math.Abs(knots[i + p + 1] - knots[i + 1]) < double.Epsilon))
                rightCoefficient = (knots[i + p + 1] - u)/(knots[i + p + 1] - knots[i + 1]);

            return leftCoefficient*N(i, p - 1, u) + rightCoefficient*N(i + 1, p - 1, u);
        }

        #endregion
    }
}