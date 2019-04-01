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
using netDxf.Objects;
using netDxf.Tables;

namespace netDxf.Entities
{
    /// <summary>
    /// Represents a raster image <see cref="EntityObject">entity</see>.
    /// </summary>
    public class Image :
        EntityObject
    {
        #region private fields

        private Vector3 position;
        private Vector2 uvector;
        private Vector2 vvector;
        private double width;
        private double height;
        //private double rotation;
        private ImageDefinition imageDefinition;
        private bool clipping;
        private short brightness;
        private short contrast;
        private short fade;
        private ImageDisplayFlags displayOptions;
        private ClippingBoundary clippingBoundary;

        #endregion

        #region constructors

        internal Image()
            : base(EntityType.Image, DxfObjectCode.Image)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Image</c> class.
        /// </summary>
        /// <param name="imageDefinition">Image definition.</param>
        /// <param name="position">Image <see cref="Vector2">position</see> in world coordinates.</param>
        /// <param name="size">Image <see cref="Vector2">size</see> in world coordinates.</param>
        public Image(ImageDefinition imageDefinition, Vector2 position, Vector2 size)
            : this(imageDefinition, new Vector3(position.X, position.Y, 0.0), size.X, size.Y)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Image</c> class.
        /// </summary>
        /// <param name="imageDefinition">Image definition.</param>
        /// <param name="position">Image <see cref="Vector3">position</see> in world coordinates.</param>
        /// <param name="size">Image <see cref="Vector2">size</see> in world coordinates.</param>
        public Image(ImageDefinition imageDefinition, Vector3 position, Vector2 size)
            : this(imageDefinition, position, size.X, size.Y)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Image</c> class.
        /// </summary>
        /// <param name="imageDefinition">Image definition.</param>
        /// <param name="position">Image <see cref="Vector2">position</see> in world coordinates.</param>
        /// <param name="width">Image width in world coordinates.</param>
        /// <param name="height">Image height in world coordinates.</param>
        public Image(ImageDefinition imageDefinition, Vector2 position, double width, double height)
            : this(imageDefinition, new Vector3(position.X, position.Y, 0.0), width, height)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Image</c> class.
        /// </summary>
        /// <param name="imageDefinition">Image definition.</param>
        /// <param name="position">Image <see cref="Vector3">position</see> in world coordinates.</param>
        /// <param name="width">Image width in world coordinates.</param>
        /// <param name="height">Image height in world coordinates.</param>
        public Image(ImageDefinition imageDefinition, Vector3 position, double width, double height)
            : base(EntityType.Image, DxfObjectCode.Image)
        {
            if (imageDefinition == null)
                throw new ArgumentNullException(nameof(imageDefinition));

            this.imageDefinition = imageDefinition;
            this.position = position;
            uvector = Vector2.UnitX;
            vvector = Vector2.UnitY;
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), width, "The Image width must be greater than zero.");
            this.width = width;
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), height, "The Image height must be greater than zero.");
            this.height = height;
            //this.rotation = 0;
            clipping = false;
            brightness = 50;
            contrast = 50;
            fade = 0;
            displayOptions = ImageDisplayFlags.ShowImage | ImageDisplayFlags.ShowImageWhenNotAlignedWithScreen | ImageDisplayFlags.UseClippingBoundary;
            clippingBoundary = new ClippingBoundary(0, 0, imageDefinition.Width, imageDefinition.Height);
        }

        #endregion

        #region public properties

        /// <summary>
        /// Gets or sets the image <see cref="Vector3">position</see> in world coordinates.
        /// </summary>
        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }

        /// <summary>
        /// Gets or sets the image <see cref="Vector2">U-vector</see>.
        /// </summary>
        public Vector2 Uvector
        {
            get { return uvector; }
            set
            {
                uvector = Vector2.Normalize(value);
                if (Vector2.IsNaN(uvector))
                    throw new ArgumentException("The U vector can not be the zero vector.", nameof(value));
            }
        }

        /// <summary>
        /// Gets or sets the image <see cref="Vector2">V-vector</see>.
        /// </summary>
        public Vector2 Vvector
        {
            get { return vvector; }
            set
            {
                vvector = Vector2.Normalize(value);
                if (Vector2.IsNaN(vvector))
                    throw new ArgumentException("The V-vector can not be the zero vector.", nameof(value));
            }
        }

        /// <summary>
        /// Gets or sets the height of the image in drawing units.
        /// </summary>
        public double Height
        {
            get { return height; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The Image height must be greater than zero.");
                height = value;
            }
        }

        /// <summary>
        /// Gets or sets the width of the image in drawing units.
        /// </summary>
        public double Width
        {
            get { return width; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The Image width must be greater than zero.");
                width = value;
            }
        }

        /// <summary>
        /// Gets or sets the image rotation in degrees.
        /// </summary>
        /// <remarks>The image rotation is the angle of the U-vector.</remarks>
        public double Rotation
        {
            get
            {
                return Vector2.Angle(uvector) * MathHelper.RadToDeg;
                //return this.rotation;
            }
            set
            {
                //this.rotation = MathHelper.NormalizeAngle(value);

                IList<Vector2> uv = MathHelper.Transform(new List<Vector2> { uvector, vvector },
                    MathHelper.NormalizeAngle(value) * MathHelper.DegToRad,
                    CoordinateSystem.Object, CoordinateSystem.World);
                uvector = uv[0];
                vvector = uv[1];
            }
        }

        /// <summary>
        /// Gets the <see cref="ImageDefinition">image definition</see>.
        /// </summary>
        public ImageDefinition Definition
        {
            get { return imageDefinition; }
            internal set { imageDefinition = value; }
        }

        /// <summary>
        /// Gets or sets the clipping state: false = off, true = on.
        /// </summary>
        public bool Clipping
        {
            get { return clipping; }
            set { clipping = value; }
        }

        /// <summary>
        /// Gets or sets the brightness value (0-100; default = 50)
        /// </summary>
        public short Brightness
        {
            get { return brightness; }
            set
            {
                if (value < 0 && value > 100)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Accepted brightness values range from 0 to 100.");
                brightness = value;
            }
        }

        /// <summary>
        /// Gets or sets the contrast value (0-100; default = 50)
        /// </summary>
        public short Contrast
        {
            get { return contrast; }
            set
            {
                if (value < 0 && value > 100)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Accepted contrast values range from 0 to 100.");
                contrast = value;
            }
        }

        /// <summary>
        /// Gets or sets the fade value (0-100; default = 0)
        /// </summary>
        public short Fade
        {
            get { return fade; }
            set
            {
                if (value < 0 && value > 100)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Accepted fade values range from 0 to 100.");
                fade = value;
            }
        }

        /// <summary>
        /// Gets or sets the image display options.
        /// </summary>
        public ImageDisplayFlags DisplayOptions
        {
            get { return displayOptions; }
            set { displayOptions = value; }
        }

        /// <summary>
        /// Gets or sets the image clipping boundary.
        /// </summary>
        /// <remarks>
        /// The vertexes coordinates of the clipping boundary are expressed in local coordinates of the image in pixels.
        /// Set as null to restore the default clipping boundary, full image.
        /// </remarks>
        public ClippingBoundary ClippingBoundary
        {
            get { return clippingBoundary; }
            set { clippingBoundary = value ?? new ClippingBoundary(0, 0, Definition.Width, Definition.Height); }
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
            Vector3 newPosition;
            Vector3 newNormal;
            Vector2 newUvector;
            Vector2 newVvector;
            double newWidth;
            double newHeight;

            newPosition = transformation * Position + translation;
            newNormal = transformation * Normal;

            Matrix3 transOW = MathHelper.ArbitraryAxis(Normal);

            Matrix3 transWO = MathHelper.ArbitraryAxis(newNormal);
            transWO = transWO.Transpose();

            Vector3 v;
            v = transOW * new Vector3(Uvector.X * Width, Uvector.Y * Width, 0.0);
            v = transformation * v;
            v = transWO * v;
            newUvector = new Vector2(v.X, v.Y);

            v = transOW * new Vector3(Vvector.X * Height, Vvector.Y * Height, 0.0);
            v = transformation * v;
            v = transWO * v;
            newVvector = new Vector2(v.X, v.Y);

            newWidth = newUvector.Modulus();
            newWidth = MathHelper.IsZero(newWidth) ? MathHelper.Epsilon : newWidth;
            newHeight = newVvector.Modulus();
            newHeight = MathHelper.IsZero(newHeight) ? MathHelper.Epsilon : newHeight;

            Position = newPosition;
            Normal = newNormal;
            Uvector = newUvector;
            Vvector = newVvector;
            Width = newWidth;
            Height = newHeight;
        }

        /// <summary>
        /// Creates a new Image that is a copy of the current instance.
        /// </summary>
        /// <returns>A new Image that is a copy of this instance.</returns>
        public override object Clone()
        {
            Image entity = new Image
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
                //Image properties
                Position = position,
                Height = height,
                Width = width,
                Uvector = uvector,
                Vvector = vvector,
                //Rotation = this.rotation,
                Definition = (ImageDefinition) imageDefinition.Clone(),
                Clipping = clipping,
                Brightness = brightness,
                Contrast = contrast,
                Fade = fade,
                DisplayOptions = displayOptions,
                ClippingBoundary = (ClippingBoundary) clippingBoundary.Clone()
            };

            foreach (XData data in XData.Values)
                entity.XData.Add((XData) data.Clone());

            return entity;
        }

        #endregion
    }
}