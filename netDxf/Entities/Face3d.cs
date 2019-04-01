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

using netDxf.Tables;

namespace netDxf.Entities
{
    /// <summary>
    /// Represents a 3dFace <see cref="EntityObject">entity</see>.
    /// </summary>
    public class Face3d :
        EntityObject
    {
        #region private fields

        private Vector3 firstVertex;
        private Vector3 secondVertex;
        private Vector3 thirdVertex;
        private Vector3 fourthVertex;
        private Face3dEdgeFlags edgeFlags;

        #endregion

        #region constructors

        /// <summary>
        /// Initializes a new instance of the <c>Face3d</c> class.
        /// </summary>
        public Face3d()
            : this(Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Face3d</c> class.
        /// </summary>
        /// <param name="firstVertex">3d face <see cref="Vector2">first vertex</see>.</param>
        /// <param name="secondVertex">3d face <see cref="Vector2">second vertex</see>.</param>
        /// <param name="thirdVertex">3d face <see cref="Vector2">third vertex</see>.</param>
        public Face3d(Vector2 firstVertex, Vector2 secondVertex, Vector2 thirdVertex)
            : this(new Vector3(firstVertex.X, firstVertex.Y, 0.0),
                new Vector3(secondVertex.X, secondVertex.Y, 0.0),
                new Vector3(thirdVertex.X, thirdVertex.Y, 0.0),
                new Vector3(thirdVertex.X, thirdVertex.Y, 0.0))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Face3d</c> class.
        /// </summary>
        /// <param name="firstVertex">3d face <see cref="Vector2">first vertex</see>.</param>
        /// <param name="secondVertex">3d face <see cref="Vector2">second vertex</see>.</param>
        /// <param name="thirdVertex">3d face <see cref="Vector2">third vertex</see>.</param>
        /// <param name="fourthVertex">3d face <see cref="Vector2">fourth vertex</see>.</param>
        public Face3d(Vector2 firstVertex, Vector2 secondVertex, Vector2 thirdVertex, Vector2 fourthVertex)
            : this(new Vector3(firstVertex.X, firstVertex.Y, 0.0),
                new Vector3(secondVertex.X, secondVertex.Y, 0.0),
                new Vector3(thirdVertex.X, thirdVertex.Y, 0.0),
                new Vector3(fourthVertex.X, fourthVertex.Y, 0.0))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Face3d</c> class.
        /// </summary>
        /// <param name="firstVertex">3d face <see cref="Vector3">first vertex</see>.</param>
        /// <param name="secondVertex">3d face <see cref="Vector3">second vertex</see>.</param>
        /// <param name="thirdVertex">3d face <see cref="Vector3">third vertex</see>.</param>
        public Face3d(Vector3 firstVertex, Vector3 secondVertex, Vector3 thirdVertex)
            : this(firstVertex, secondVertex, thirdVertex, thirdVertex)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Face3d</c> class.
        /// </summary>
        /// <param name="firstVertex">3d face <see cref="Vector3">first vertex</see>.</param>
        /// <param name="secondVertex">3d face <see cref="Vector3">second vertex</see>.</param>
        /// <param name="thirdVertex">3d face <see cref="Vector3">third vertex</see>.</param>
        /// <param name="fourthVertex">3d face <see cref="Vector3">fourth vertex</see>.</param>
        public Face3d(Vector3 firstVertex, Vector3 secondVertex, Vector3 thirdVertex, Vector3 fourthVertex)
            : base(EntityType.Face3D, DxfObjectCode.Face3d)
        {
            this.firstVertex = firstVertex;
            this.secondVertex = secondVertex;
            this.thirdVertex = thirdVertex;
            this.fourthVertex = fourthVertex;
            edgeFlags = Face3dEdgeFlags.Visibles;
        }

        #endregion

        #region public properties

        /// <summary>
        /// Gets or sets the first 3d face <see cref="Vector3">vertex</see>.
        /// </summary>
        public Vector3 FirstVertex
        {
            get { return firstVertex; }
            set { firstVertex = value; }
        }

        /// <summary>
        /// Gets or sets the second 3d face <see cref="Vector3">vertex</see>.
        /// </summary>
        public Vector3 SecondVertex
        {
            get { return secondVertex; }
            set { secondVertex = value; }
        }

        /// <summary>
        /// Gets or sets the third 3d face <see cref="Vector3">vertex</see>.
        /// </summary>
        public Vector3 ThirdVertex
        {
            get { return thirdVertex; }
            set { thirdVertex = value; }
        }

        /// <summary>
        /// Gets or sets the fourth 3d face <see cref="Vector3">vertex</see>.
        /// </summary>
        public Vector3 FourthVertex
        {
            get { return fourthVertex; }
            set { fourthVertex = value; }
        }

        /// <summary>
        /// Gets or sets the 3d face edge visibility.
        /// </summary>
        public Face3dEdgeFlags EdgeFlags
        {
            get { return edgeFlags; }
            set { edgeFlags = value; }
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
            FirstVertex = transformation * FirstVertex + translation;
            SecondVertex = transformation * SecondVertex + translation;
            ThirdVertex = transformation * ThirdVertex + translation;
            FourthVertex = transformation * FourthVertex + translation;
        }

        /// <summary>
        /// Creates a new Face3d that is a copy of the current instance.
        /// </summary>
        /// <returns>A new Face3d that is a copy of this instance.</returns>
        public override object Clone()
        {
            Face3d entity = new Face3d
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
                //Face3d properties
                FirstVertex = firstVertex,
                SecondVertex = secondVertex,
                ThirdVertex = thirdVertex,
                FourthVertex = fourthVertex,
                EdgeFlags = edgeFlags
            };

            foreach (XData data in XData.Values)
                entity.XData.Add((XData) data.Clone());

            return entity;
        }

        #endregion
    }
}