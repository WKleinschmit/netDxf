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
using netDxf.Tables;

namespace netDxf.Entities
{
    /// <summary>
    /// Represents a dxf Vertex.
    /// </summary>
    /// <remarks>
    /// The Vertex class holds all the information read from the dxf file even if its needed or not. For internal use only.
    /// </remarks>
    internal class Vertex :
        DxfObject
    {
        #region private fields

        private VertexTypeFlags flags;
        private Vector3 position;
        private short[] vertexIndexes;
        private double startWidth;
        private double endWidth;
        private double bulge;
        private AciColor color;
        private Layer layer;
        private Linetype linetype;

        #endregion

        #region constructors

        /// <summary>
        /// Initializes a new instance of the <c>Vertex</c> class.
        /// </summary>
        public Vertex()
            : this(Vector3.Zero)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Vertex</c> class.
        /// </summary>
        /// <param name="position">Vertex <see cref="Vector2">location</see>.</param>
        public Vertex(Vector2 position)
            : this(new Vector3(position.X, position.Y, 0.0))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Vertex</c> class.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="z">Z coordinate.</param>
        public Vertex(double x, double y, double z)
            : this(new Vector3(x, y, z))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Vertex</c> class.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        public Vertex(double x, double y)
            : this(new Vector3(x, y, 0.0))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Vertex</c> class.
        /// </summary>
        /// <param name="position">Vertex <see cref="Vector3">location</see>.</param>
        public Vertex(Vector3 position)
            : base(DxfObjectCode.Vertex)
        {
            flags = VertexTypeFlags.PolylineVertex;
            this.position = position;
            layer = Layer.Default;
            color = AciColor.ByLayer;
            linetype = Linetype.ByLayer;
            bulge = 0.0;
            startWidth = 0.0;
            endWidth = 0.0;
        }

        #endregion

        #region public properties

        /// <summary>
        /// Gets or sets the polyline vertex <see cref="Vector3">location</see>.
        /// </summary>
        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }

        public short[] VertexIndexes
        {
            get { return vertexIndexes; }
            set { vertexIndexes = value; }
        }

        /// <summary>
        /// Gets or sets the light weight polyline start segment width.
        /// </summary>
        public double StartWidth
        {
            get { return startWidth; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The Vertex width must be equals or greater than zero.");
                startWidth = value;
            }
        }

        /// <summary>
        /// Gets or sets the light weight polyline end segment width.
        /// </summary>
        public double EndWidth
        {
            get { return endWidth; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The Vertex width must be equals or greater than zero.");
                endWidth = value;
            }
        }

        /// <summary>
        /// Gets or set the light weight polyline bulge.Accepted values range from 0 to 1.
        /// </summary>
        /// <remarks>
        /// The bulge is the tangent of one fourth the included angle for an arc segment, 
        /// made negative if the arc goes clockwise from the start point to the endpoint. 
        /// A bulge of 0 indicates a straight segment, and a bulge of 1 is a semicircle.
        /// </remarks>
        public double Bulge
        {
            get { return bulge; }
            set
            {
                if (bulge < 0.0 || bulge > 1.0f)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The bulge must be a value between zero and one");
                bulge = value;
            }
        }

        /// <summary>
        /// Gets or sets the vertex type.
        /// </summary>
        public VertexTypeFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        /// <summary>
        /// Gets or sets the entity color.
        /// </summary>
        public AciColor Color
        {
            get { return color; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                color = value;
            }
        }

        /// <summary>
        /// Gets or sets the entity layer.
        /// </summary>
        public Layer Layer
        {
            get { return layer; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                layer = value;
            }
        }

        /// <summary>
        /// Gets or sets the entity line type.
        /// </summary>
        public Linetype Linetype
        {
            get { return linetype; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                linetype = value;
            }
        }

        #endregion

        #region overrides

        public override string ToString()
        {
            return CodeName;
        }

        #endregion
    }
}