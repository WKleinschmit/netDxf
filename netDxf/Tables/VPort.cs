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
using netDxf.Collections;

namespace netDxf.Tables
{
    /// <summary>
    /// Represents a document viewport.
    /// </summary>
    public class VPort :
        TableObject
    {
        #region private fields

        private Vector2 center;
        private Vector2 snapBasePoint;
        private Vector2 snapSpacing;
        private Vector2 gridSpacing;
        private Vector3 direction;
        private Vector3 target;
        private double height;
        private double aspectRatio;
        private bool showGrid;
        private bool snapMode;

        #endregion

        #region constants

        /// <summary>
        /// Default VPort name.
        /// </summary>
        public const string DefaultName = "*Active";

        /// <summary>
        /// Gets the active viewport.
        /// </summary>
        public static VPort Active
        {
            get { return new VPort(DefaultName, false); }
        }

        #endregion

        #region constructors

        /// <summary>
        /// Initializes a new instance of the <c>VPort</c> class.
        /// </summary>
        public VPort(string name)
            : this(name, true)
        {
        }

        internal VPort(string name, bool checkName)
            : base(name, DxfObjectCode.VPort, checkName)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name), "The viewport name should be at least one character long.");

            IsReserved = name.Equals("*Active", StringComparison.OrdinalIgnoreCase);
            center = Vector2.Zero;
            snapBasePoint = Vector2.Zero;
            snapSpacing = new Vector2(0.5);
            gridSpacing = new Vector2(10.0);
            target = Vector3.Zero;
            direction = Vector3.UnitZ;
            height = 10;
            aspectRatio = 1.0;
            showGrid = true;
            snapMode = false;
        }

        #endregion

        #region public properties

        /// <summary>
        /// Gets or sets the view center point in DCS (Display Coordinate System)
        /// </summary>
        public Vector2 ViewCenter
        {
            get { return center; }
            set { center = value; }
        }

        /// <summary>
        /// Gets or sets the snap base point in DCS (Display Coordinate System)
        /// </summary>
        public Vector2 SnapBasePoint
        {
            get { return snapBasePoint; }
            set { snapBasePoint = value; }
        }

        /// <summary>
        /// Gets or sets the snap spacing X and Y.
        /// </summary>
        public Vector2 SnapSpacing
        {
            get { return snapSpacing; }
            set { snapSpacing = value; }
        }

        /// <summary>
        /// Gets or sets the grid spacing X and Y.
        /// </summary>
        public Vector2 GridSpacing
        {
            get { return gridSpacing; }
            set { gridSpacing = value; }
        }

        /// <summary>
        /// Gets or sets the view direction from target point in WCS (World Coordinate System).
        /// </summary>
        public Vector3 ViewDirection
        {
            get { return direction; }
            set
            {
                direction = Vector3.Normalize(value);
                if (Vector3.IsNaN(direction))
                    throw new ArgumentException("The direction can not be the zero vector.", nameof(value));
            }
        }

        /// <summary>
        /// Gets or sets the view target point in WCS (World Coordinate System).
        /// </summary>
        public Vector3 ViewTarget
        {
            get { return target; }
            set { target = value; }
        }

        /// <summary>
        /// Gets or sets the view height.
        /// </summary>
        public double ViewHeight
        {
            get { return height; }
            set { height = value; }
        }

        /// <summary>
        /// Gets or sets the view aspect ratio (view width/view height).
        /// </summary>
        public double ViewAspectRatio
        {
            get { return aspectRatio; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        "The VPort aspect ratio must be greater than zero.");
                aspectRatio = value;
            }
        }

        /// <summary>
        /// Gets or sets the grid on/off.
        /// </summary>
        public bool ShowGrid
        {
            get { return showGrid; }
            set { showGrid = value; }
        }

        /// <summary>
        /// Gets or sets the snap mode on/off.
        /// </summary>
        public bool SnapMode
        {
            get { return snapMode; }
            set { snapMode = value; }
        }

        /// <summary>
        /// Gets the owner of the actual viewport.
        /// </summary>
        public new VPorts Owner
        {
            get { return (VPorts) base.Owner; }
            internal set { base.Owner = value; }
        }

        #endregion

        #region overrides

        /// <summary>
        /// Creates a new VPort that is a copy of the current instance.
        /// </summary>
        /// <param name="newName">VPort name of the copy.</param>
        /// <returns>A new VPort that is a copy of this instance.</returns>
        public override TableObject Clone(string newName)
        {
            VPort copy = new VPort(newName)
            {
                ViewCenter = center,
                SnapBasePoint = snapBasePoint,
                SnapSpacing = snapSpacing,
                GridSpacing = gridSpacing,
                ViewTarget = target,
                ViewDirection = direction,
                ViewHeight = height,
                ViewAspectRatio = aspectRatio,
                ShowGrid = showGrid
            };

            foreach (XData data in XData.Values)
                copy.XData.Add((XData)data.Clone());

            return copy;
        }

        /// <summary>
        /// Creates a new VPort that is a copy of the current instance.
        /// </summary>
        /// <returns>A new VPort that is a copy of this instance.</returns>
        public override object Clone()
        {
            return Clone(Name);
        }

        #endregion
    }
}