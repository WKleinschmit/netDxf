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
using netDxf.Collections;
using netDxf.Tables;

namespace netDxf.Entities
{
    /// <summary>
    /// Represents an attribute definition <see cref="EntityObject">entity</see>.
    /// </summary>
    /// <remarks>
    /// AutoCad allows to have duplicate tags in the attribute definitions list, but this library does not.
    /// To have duplicate tags is not recommended in any way, since there will be now way to know which is the definition associated to the insert attribute.
    /// </remarks>
    public class AttributeDefinition :
        DxfObject,
        ICloneable,
        IHasXData
    {
        #region delegates and events

        public delegate void LayerChangedEventHandler(AttributeDefinition sender, TableObjectChangedEventArgs<Layer> e);
        public event LayerChangedEventHandler LayerChanged;
        protected virtual Layer OnLayerChangedEvent(Layer oldLayer, Layer newLayer)
        {
            LayerChangedEventHandler ae = LayerChanged;
            if (ae != null)
            {
                TableObjectChangedEventArgs<Layer> eventArgs = new TableObjectChangedEventArgs<Layer>(oldLayer, newLayer);
                ae(this, eventArgs);
                return eventArgs.NewValue;
            }
            return newLayer;
        }

        public delegate void LinetypeChangedEventHandler(AttributeDefinition sender, TableObjectChangedEventArgs<Linetype> e);
        public event LinetypeChangedEventHandler LinetypeChanged;
        protected virtual Linetype OnLinetypeChangedEvent(Linetype oldLinetype, Linetype newLinetype)
        {
            LinetypeChangedEventHandler ae = LinetypeChanged;
            if (ae != null)
            {
                TableObjectChangedEventArgs<Linetype> eventArgs = new TableObjectChangedEventArgs<Linetype>(oldLinetype, newLinetype);
                ae(this, eventArgs);
                return eventArgs.NewValue;
            }
            return newLinetype;
        }

        public delegate void TextStyleChangedEventHandler(AttributeDefinition sender, TableObjectChangedEventArgs<TextStyle> e);
        public event TextStyleChangedEventHandler TextStyleChange;
        protected virtual TextStyle OnTextStyleChangedEvent(TextStyle oldTextStyle, TextStyle newTextStyle)
        {
            TextStyleChangedEventHandler ae = TextStyleChange;
            if (ae != null)
            {
                TableObjectChangedEventArgs<TextStyle> eventArgs = new TableObjectChangedEventArgs<TextStyle>(oldTextStyle, newTextStyle);
                ae(this, eventArgs);
                return eventArgs.NewValue;
            }
            return newTextStyle;
        }

        public event XDataAddAppRegEventHandler XDataAddAppReg;
        protected virtual void OnXDataAddAppRegEvent(ApplicationRegistry item)
        {
            XDataAddAppRegEventHandler ae = XDataAddAppReg;
            if (ae != null)
                ae(this, new ObservableCollectionEventArgs<ApplicationRegistry>(item));
        }

        public event XDataRemoveAppRegEventHandler XDataRemoveAppReg;
        protected virtual void OnXDataRemoveAppRegEvent(ApplicationRegistry item)
        {
            XDataRemoveAppRegEventHandler ae = XDataRemoveAppReg;
            if (ae != null)
                ae(this, new ObservableCollectionEventArgs<ApplicationRegistry>(item));
        }

        #endregion

        #region private fields

        private AciColor color;
        private Layer layer;
        private Linetype linetype;
        private Lineweight lineweight;
        private Transparency transparency;
        private double linetypeScale;
        private bool isVisible;
        private Vector3 normal;

        private readonly string tag;
        private string prompt;
        private object attValue;
        private TextStyle style;
        private Vector3 position;
        private AttributeFlags flags;
        private double height;
        private double widthFactor;
        private double obliqueAngle;
        private double rotation;
        private TextAlignment alignment;

        private readonly XDataDictionary xData;

        #endregion

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <c>AttributeDefiniton</c> class.
        /// </summary>
        /// <param name="tag">Attribute identifier, the parameter <c>id</c> string cannot contain spaces.</param>
        public AttributeDefinition(string tag)
            : this(tag, TextStyle.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>AttributeDefiniton</c> class.
        /// </summary>
        /// <param name="tag">Attribute identifier.</param>
        /// <param name="style">Attribute <see cref="TextStyle">text style</see>.</param>
        public AttributeDefinition(string tag, TextStyle style)
            : this(tag, MathHelper.IsZero(style.Height) ? 1.0 : style.Height, style)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>AttributeDefiniton</c> class.
        /// </summary>
        /// <param name="tag">Attribute identifier, the parameter <c>id</c> string cannot contain spaces.</param>
        /// <param name="textHeight">Height of the attribute definition text.</param>
        /// <param name="style">Attribute <see cref="TextStyle">text style</see>.</param>
        public AttributeDefinition(string tag, double textHeight, TextStyle style)
            : base(DxfObjectCode.AttributeDefinition)
        {
            if (string.IsNullOrEmpty(tag))
                throw new ArgumentNullException(nameof(tag));

            if (tag.Contains(" "))
                throw new ArgumentException("The tag string cannot contain spaces.", nameof(tag));
            this.tag = tag;
            flags = AttributeFlags.Visible;
            prompt = string.Empty;
            attValue = null;
            position = Vector3.Zero;
            if (style == null)
                throw new ArgumentNullException(nameof(style));
            this.style = style;
            if (textHeight <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(textHeight), attValue, "The attribute definition text height must be greater than zero.");
            height = textHeight;
            widthFactor = style.WidthFactor;
            obliqueAngle = style.ObliqueAngle;
            rotation = 0.0;
            alignment = TextAlignment.BaselineLeft;

            color = AciColor.ByLayer;
            layer = Layer.Default;
            linetype = Linetype.ByLayer;
            lineweight = Lineweight.ByLayer;
            transparency = Transparency.ByLayer;
            linetypeScale = 1.0;
            isVisible = true;
            normal = Vector3.UnitZ;

            xData = new XDataDictionary();
            xData.AddAppReg += XData_AddAppReg;
            xData.RemoveAppReg += XData_RemoveAppReg;

        }

        #endregion

        #region public property

        /// <summary>
        /// Gets or sets the entity <see cref="AciColor">color</see>.
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
        /// Gets or sets the entity <see cref="Layer">layer</see>.
        /// </summary>
        public Layer Layer
        {
            get { return layer; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                layer = OnLayerChangedEvent(layer, value);
            }
        }

        /// <summary>
        /// Gets or sets the entity <see cref="Linetype">line type</see>.
        /// </summary>
        public Linetype Linetype
        {
            get { return linetype; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                linetype = OnLinetypeChangedEvent(linetype, value);
            }
        }

        /// <summary>
        /// Gets or sets the entity line weight, one unit is always 1/100 mm (default = ByLayer).
        /// </summary>
        public Lineweight Lineweight
        {
            get { return lineweight; }
            set { lineweight = value; }
        }

        /// <summary>
        /// Gets or sets layer transparency (default: ByLayer).
        /// </summary>
        public Transparency Transparency
        {
            get { return transparency; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                transparency = value;
            }
        }

        /// <summary>
        /// Gets or sets the entity line type scale.
        /// </summary>
        public double LinetypeScale
        {
            get { return linetypeScale; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The line type scale must be greater than zero.");
                linetypeScale = value;
            }
        }

        /// <summary>
        /// Gets or set the entity visibility.
        /// </summary>
        public bool IsVisible
        {
            get { return isVisible; }
            set { isVisible = value; }
        }

        /// <summary>
        /// Gets or sets the entity <see cref="Vector3">normal</see>.
        /// </summary>
        public Vector3 Normal
        {
            get { return normal; }
            set
            {
                normal = Vector3.Normalize(value);
                if (Vector3.IsNaN(normal))
                    throw new ArgumentException("The normal can not be the zero vector.", nameof(value));
            }
        }

        /// <summary>
        /// Gets the attribute identifier.
        /// </summary>
        public string Tag
        {
            get { return tag; }
        }

        /// <summary>
        /// Gets or sets the attribute information text.
        /// </summary>
        /// <remarks>This is the text prompt shown to introduce the attribute value when new Insert entities are inserted into the drawing.</remarks>
        public string Prompt
        {
            get { return prompt; }
            set { prompt = value; }
        }

        /// <summary>
        /// Gets or sets the attribute text height.
        /// </summary>
        public double Height
        {
            get { return height; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The height should be greater than zero.");
                height = value;
            }
        }

        /// <summary>
        /// Gets or sets the attribute text width factor.
        /// </summary>
        public double WidthFactor
        {
            get { return widthFactor; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The width factor should be greater than zero.");
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
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The oblique angle valid values range from -85 to 85.");
                obliqueAngle = value;
            }
        }

        /// <summary>
        /// Gets or sets the attribute default value.
        /// </summary>
        public object Value
        {
            get { return attValue; }
            set { attValue = value; }
        }

        /// <summary>
        /// Gets or sets  the attribute text style.
        /// </summary>
        /// <remarks>
        /// The <see cref="TextStyle">text style</see> defines the basic properties of the information text.
        /// </remarks>
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
        /// Gets or sets the attribute <see cref="Vector3">position</see> in object coordinates.
        /// </summary>
        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }

        /// <summary>
        /// Gets or sets the attribute flags.
        /// </summary>
        public AttributeFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        /// <summary>
        /// Gets or sets the attribute text rotation in degrees.
        /// </summary>
        public double Rotation
        {
            get { return rotation; }
            set { rotation = MathHelper.NormalizeAngle(value); }
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
        /// Gets the owner of the actual dxf object.
        /// </summary>
        public new Block Owner
        {
            get { return (Block)base.Owner; }
            internal set { base.Owner = value; }
        }

        /// <summary>
        /// Gets the entity <see cref="XDataDictionary">extended data</see>.
        /// </summary>
        public XDataDictionary XData
        {
            get { return xData; }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Moves, scales, and/or rotates the current attribute definition given a 3x3 transformation matrix and a translation vector.
        /// </summary>
        /// <param name="transformation">Transformation matrix.</param>
        /// <param name="translation">Translation vector.</param>
        public void TransformBy(Matrix3 transformation, Vector3 translation)
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
            if (newWidthFactor < 0.01)
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

        #endregion

        #region overrides

        /// <summary>
        /// Creates a new AttributeDefinition that is a copy of the current instance.
        /// </summary>
        /// <returns>A new AttributeDefinition that is a copy of this instance.</returns>
        public object Clone()
        {
            AttributeDefinition entity = new AttributeDefinition(tag)
            {
                //Attribute definition properties
                Layer = (Layer) Layer.Clone(),
                Linetype = (Linetype) Linetype.Clone(),
                Color = (AciColor) Color.Clone(),
                Lineweight = Lineweight,
                Transparency = (Transparency) Transparency.Clone(),
                LinetypeScale = LinetypeScale,
                Normal = Normal,
                IsVisible = IsVisible,
                Prompt = prompt,
                Value = attValue,
                Height = height,
                WidthFactor = widthFactor,
                ObliqueAngle = obliqueAngle,
                Style = style,
                Position = position,
                Flags = flags,
                Rotation = rotation,
                Alignment = alignment
            };

            foreach (XData data in XData.Values)
                entity.XData.Add((XData) data.Clone());

            return entity;
        }

        #endregion

        #region XData events

        private void XData_AddAppReg(XDataDictionary sender, ObservableCollectionEventArgs<ApplicationRegistry> e)
        {
            OnXDataAddAppRegEvent(e.Item);
        }

        private void XData_RemoveAppReg(XDataDictionary sender, ObservableCollectionEventArgs<ApplicationRegistry> e)
        {
            OnXDataRemoveAppRegEvent(e.Item);
        }

        #endregion
    }
}