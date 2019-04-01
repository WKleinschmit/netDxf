﻿#region netDxf library, Copyright (C) 2009-2019 Daniel Carvajal (haplokuon@gmail.com)

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
    /// Represents a Text <see cref="EntityObject">entity</see>.
    /// </summary>
    public class Text :
        EntityObject
    {
        #region delegates and events

        public delegate void TextStyleChangedEventHandler(Text sender, TableObjectChangedEventArgs<TextStyle> e);

        public event TextStyleChangedEventHandler TextStyleChanged;

        protected virtual TextStyle OnTextStyleChangedEvent(TextStyle oldTextStyle, TextStyle newTextStyle)
        {
            TextStyleChangedEventHandler ae = TextStyleChanged;
            if (ae != null)
            {
                TableObjectChangedEventArgs<TextStyle> eventArgs = new TableObjectChangedEventArgs<TextStyle>(oldTextStyle, newTextStyle);
                ae(this, eventArgs);
                return eventArgs.NewValue;
            }
            return newTextStyle;
        }

        #endregion

        #region private fields

        private TextAlignment alignment;
        private Vector3 position;
        private double obliqueAngle;
        private TextStyle style;
        private string text;
        private double height;
        private double widthFactor;
        private double rotation;

        #endregion

        #region constructors

        /// <summary>
        /// Initializes a new instance of the <c>Text</c> class.
        /// </summary>
        public Text()
            : this(string.Empty, Vector3.Zero, 1.0, TextStyle.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Text</c> class.
        /// </summary>
        /// <param name="text">Text string.</param>
        /// <param name="position">Text <see cref="Vector2">position</see> in world coordinates.</param>
        /// <param name="height">Text height.</param>
        public Text(string text, Vector2 position, double height)
            : this(text, new Vector3(position.X, position.Y, 0.0), height, TextStyle.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Text</c> class.
        /// </summary>
        /// <param name="text">Text string.</param>
        /// <param name="position">Text <see cref="Vector3">position</see> in world coordinates.</param>
        /// <param name="height">Text height.</param>
        public Text(string text, Vector3 position, double height)
            : this(text, position, height, TextStyle.Default)
        {
        }


        /// <summary>
        /// Initializes a new instance of the <c>Text</c> class.
        /// </summary>
        /// <param name="text">Text string.</param>
        /// <param name="position">Text <see cref="Vector2">position</see> in world coordinates.</param>
        /// <param name="height">Text height.</param>
        /// <param name="style">Text <see cref="TextStyle">style</see>.</param>
        public Text(string text, Vector2 position, double height, TextStyle style)
            : this(text, new Vector3(position.X, position.Y, 0.0), height, style)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Text</c> class.
        /// </summary>
        /// <param name="text">Text string.</param>
        /// <param name="position">Text <see cref="Vector3">position</see> in world coordinates.</param>
        /// <param name="height">Text height.</param>
        /// <param name="style">Text <see cref="TextStyle">style</see>.</param>
        public Text(string text, Vector3 position, double height, TextStyle style)
            : base(EntityType.Text, DxfObjectCode.Text)
        {
            this.text = text;
            this.position = position;
            alignment = TextAlignment.BaselineLeft;
            Normal = Vector3.UnitZ;
            if (style == null)
                throw new ArgumentNullException(nameof(style));
            this.style = style;
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), this.text, "The Text height must be greater than zero.");
            this.height = height;
            widthFactor = style.WidthFactor;
            obliqueAngle = style.ObliqueAngle;
            rotation = 0.0;
        }

        #endregion

        #region public properties

        /// <summary>
        /// Gets or sets Text <see cref="Vector3">position</see> in world coordinates.
        /// </summary>
        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }

        /// <summary>
        /// Gets or sets the text rotation in degrees.
        /// </summary>
        public double Rotation
        {
            get { return rotation; }
            set { rotation = MathHelper.NormalizeAngle(value); }
        }

        /// <summary>
        /// Gets or sets the text height.
        /// </summary>
        /// <remarks>Valid values must be greater than zero. Default: 1.0.</remarks>
        public double Height
        {
            get { return height; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The Text height must be greater than zero.");
                height = value;
            }
        }

        /// <summary>
        /// Gets or sets the width factor.
        /// </summary>
        /// <remarks>Valid values range from 0.01 to 100. Default: 1.0.</remarks>
        public double WidthFactor
        {
            get { return widthFactor; }
            set
            {
                if (value < 0.01 || value > 100.0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The Text width factor valid values range from 0.01 to 100.");
                widthFactor = value;
            }
        }

        /// <summary>
        /// Gets or sets the font oblique angle.
        /// </summary>
        /// <remarks>Valid values range from -85 to 85. Default: 0.0.</remarks>
        public double ObliqueAngle
        {
            get { return obliqueAngle; }
            set
            {
                if (value < -85.0 || value > 85.0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The Text oblique angle valid values range from -85 to 85.");
                obliqueAngle = value;
            }
        }

        /// <summary>
        /// Gets or sets the text alignment.
        /// </summary>
        public TextAlignment Alignment
        {
            get { return alignment; }
            set { alignment = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="TextStyle">text style</see>.
        /// </summary>
        public TextStyle Style
        {
            get { return style; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                style = OnTextStyleChangedEvent(style, value);
            }
        }

        /// <summary>
        /// Gets or sets the text string.
        /// </summary>
        public string Value
        {
            get { return text; }
            set { text = value; }
        }

        #endregion

        #region overrides

        /// <summary>
        /// Moves, scales, and/or rotates the current entity given a 3x3 transformation matrix and a translation vector.
        /// </summary>
        /// <param name="transformation">Transformation matrix.</param>
        /// <param name="translation">Translation vector.</param>
        /// <remarks>
        /// Negative scaling is not supported.
        /// </remarks>
        public override void TransformBy(Matrix3 transformation, Vector3 translation)
        {
            Vector3 newPosition;
            Vector3 newNormal;
            Vector2 newUvector;
            Vector2 newVvector;
            double newWidthFactor;
            double newHeight;
            double newRotation;
            double newObliqueAngle;

            newPosition = transformation * Position + translation;
            newNormal = transformation * Normal;

            Matrix3 transOW = MathHelper.ArbitraryAxis(Normal);

            Matrix3 transWO = MathHelper.ArbitraryAxis(newNormal);
            transWO = transWO.Transpose();

            IList<Vector2> uv = MathHelper.Transform(new List<Vector2> { WidthFactor * Height * Vector2.UnitX, Height * Vector2.UnitY },
                Rotation * MathHelper.DegToRad,
                CoordinateSystem.Object, CoordinateSystem.World);


            Vector3 v;
            v = transOW * new Vector3(uv[0].X, uv[0].Y, 0.0);
            v = transformation * v;
            v = transWO * v;
            newUvector = new Vector2(v.X, v.Y);

            v = transOW * new Vector3(uv[1].X, uv[1].Y, 0.0);
            v = transformation * v;
            v = transWO * v;
            newVvector = new Vector2(v.X, v.Y);

            newRotation = Vector2.Angle(newUvector) * MathHelper.RadToDeg;

            // the oblique angle is defined between -85 nad 85 degrees
            newObliqueAngle = Vector2.Angle(newVvector) * MathHelper.RadToDeg;
            newObliqueAngle = (newRotation + 90.0) - newObliqueAngle;
            if (newObliqueAngle > 180)
                newObliqueAngle = 180 - newObliqueAngle;
            if (newObliqueAngle < -85)
                newObliqueAngle = -85;
            else if (newObliqueAngle > 85)
                newObliqueAngle = 85;

            // the height must be greater than zero, the cos is always positive between -85 and 85
            newHeight = newVvector.Modulus() * Math.Cos(newObliqueAngle * MathHelper.DegToRad);
            newHeight = MathHelper.IsZero(newHeight) ? MathHelper.Epsilon : newHeight;

            // the width factor is defined between 0.01 nad 100
            newWidthFactor = newUvector.Modulus() / newHeight;
            if(newWidthFactor<0.01)
                newWidthFactor = 0.01;
            else if (newWidthFactor > 100)
                newWidthFactor = 100;

            Position = newPosition;
            Normal = newNormal;
            Rotation = newRotation;
            Height = newHeight;
            WidthFactor = newWidthFactor;
            ObliqueAngle = newObliqueAngle;         
        }

        /// <summary>
        /// Creates a new Text that is a copy of the current instance.
        /// </summary>
        /// <returns>A new Text that is a copy of this instance.</returns>
        public override object Clone()
        {
            Text entity = new Text
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
                //Text properties
                Position = position,
                Rotation = rotation,
                Height = height,
                WidthFactor = widthFactor,
                ObliqueAngle = obliqueAngle,
                Alignment = alignment,
                Style = (TextStyle) style.Clone(),
                Value = text
            };

            foreach (XData data in XData.Values)
                entity.XData.Add((XData) data.Clone());

            return entity;
        }

        #endregion
    }
}