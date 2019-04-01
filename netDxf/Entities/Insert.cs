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
using netDxf.Units;

namespace netDxf.Entities
{
    /// <summary>
    /// Represents a block insertion <see cref="EntityObject">entity</see>.
    /// </summary>
    public class Insert :
        EntityObject
    {
        #region delegates and events

        public delegate void AttributeAddedEventHandler(Insert sender, AttributeChangeEventArgs e);

        public event AttributeAddedEventHandler AttributeAdded;

        protected virtual void OnAttributeAddedEvent(Attribute item)
        {
            AttributeAddedEventHandler ae = AttributeAdded;
            if (ae != null)
                ae(this, new AttributeChangeEventArgs(item));
        }

        public delegate void AttributeRemovedEventHandler(Insert sender, AttributeChangeEventArgs e);

        public event AttributeRemovedEventHandler AttributeRemoved;

        protected virtual void OnAttributeRemovedEvent(Attribute item)
        {
            AttributeRemovedEventHandler ae = AttributeRemoved;
            if (ae != null)
                ae(this, new AttributeChangeEventArgs(item));
        }

        #endregion

        #region private fields

        private readonly EndSequence endSequence;
        private Block block;
        private Vector3 position;
        private Vector3 scale;
        private double rotation;
        private AttributeCollection attributes;

        #endregion

        #region constructors

        internal Insert(List<Attribute> attributes)
            : base(EntityType.Insert, DxfObjectCode.Insert)
        {
            if(attributes == null)
                throw new ArgumentNullException(nameof(attributes));
            this.attributes = new AttributeCollection(attributes);
            foreach (Attribute att in this.attributes)
            {
                if(att.Owner!=null)
                    throw new ArgumentException("The attributes list contains at least an attribute that already has an owner.", nameof(attributes));
                att.Owner = this;
            }

            block = null;
            position = Vector3.Zero;
            scale = new Vector3(1.0);
            rotation = 0.0;
            endSequence = new EndSequence(this);

        }

        /// <summary>
        /// Initializes a new instance of the <c>Insert</c> class.
        /// </summary>
        /// <param name="block">Insert <see cref="Block">block definition</see>.</param>
        public Insert(Block block)
            : this(block, Vector3.Zero)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Insert</c> class.
        /// </summary>
        /// <param name="block">Insert block definition.</param>
        /// <param name="position">Insert <see cref="Vector2">position</see> in world coordinates.</param>
        public Insert(Block block, Vector2 position)
            : this(block, new Vector3(position.X, position.Y, 0.0))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Insert</c> class.
        /// </summary>
        /// <param name="block">Insert block definition.</param>
        /// <param name="position">Insert <see cref="Vector3">point</see> in world coordinates.</param>
        public Insert(Block block, Vector3 position)
            : this(block, position, 1.0)
        {           
        }

        /// <summary>
        /// Initializes a new instance of the <c>Insert</c> class.
        /// </summary>
        /// <param name="block">Insert block definition.</param>
        /// <param name="position">Insert <see cref="Vector3">point</see> in world coordinates.</param>
        public Insert(Block block, Vector3 position, double scale)
            : base(EntityType.Insert, DxfObjectCode.Insert)
        {
            if (block == null)
                throw new ArgumentNullException(nameof(block));

            this.block = block;
            this.position = position;
            if (scale <= 0)
                throw new ArgumentOutOfRangeException(nameof(scale), scale, "The Insert scale must be greater than zero.");
            this.scale = new Vector3(scale);
            rotation = 0.0;
            endSequence = new EndSequence(this);

            List<Attribute> atts = new List<Attribute>(block.AttributeDefinitions.Count);
            foreach (AttributeDefinition attdef in block.AttributeDefinitions.Values)
            {
                Attribute att = new Attribute(attdef)
                {
                    Position = attdef.Position + this.position - this.block.Origin,
                    Owner = this
                };
                atts.Add(att);
            }

            attributes = new AttributeCollection(atts);
        }

        #endregion

        #region public properties

        /// <summary>
        /// Gets the insert list of <see cref="Attribute">attributes</see>.
        /// </summary>
        public AttributeCollection Attributes
        {
            get { return attributes; }
        }

        /// <summary>
        /// Gets the insert <see cref="Block">block definition</see>.
        /// </summary>
        public Block Block
        {
            get { return block; }
            internal set { block = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="Vector3">position</see> in world coordinates.
        /// </summary>
        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }

        /// <summary>
        /// Gets or sets the insert <see cref="Vector3">scale</see>.
        /// </summary>
        /// <remarks>Any of the vector scale components cannot be zero.</remarks>
        public Vector3 Scale
        {
            get { return scale; }
            set
            {
                if (MathHelper.IsZero(value.X) || MathHelper.IsZero(value.Y) || MathHelper.IsZero(value.Z))
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Any of the vector scale components cannot be zero.");
                scale = value;
            }
        }

        /// <summary>
        /// Gets or sets the insert rotation along the normal vector in degrees.
        /// </summary>
        public double Rotation
        {
            get { return rotation; }
            set { rotation = MathHelper.NormalizeAngle(value); }
        }

        #endregion

        #region internal properties

        internal EndSequence EndSequence
        {
            get { return endSequence; }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Updates the actual insert with the attribute properties currently defined in the block. This does not affect any values assigned to attributes in each block.
        /// </summary>
        /// <remarks>This method will automatically call the TransformAttributes method, to keep all attributes position and orientation up to date.</remarks>
        /// <remarks></remarks>
        public void Sync()
        {
            List<Attribute> atts = new List<Attribute>(block.AttributeDefinitions.Count);

            // remove all attributes in the actual insert
            foreach (Attribute att in attributes)
            {
                OnAttributeRemovedEvent(att);
                att.Handle = null;
                att.Owner = null;
            }

            // add any new attributes from the attribute definitions of the block
            foreach (AttributeDefinition attdef in block.AttributeDefinitions.Values)
            {
                Attribute att = new Attribute(attdef)
                {
                    Owner = this
                };

                atts.Add(att);
                OnAttributeAddedEvent(att);
            }
            attributes = new AttributeCollection(atts);

            TransformAttributes();
        }

        /// <summary>
        /// Calculates the insertion rotation matrix.
        /// </summary>
        /// <param name="insertionUnits">The insertion units.</param>
        /// <returns>The insert rotation matrix.</returns>
        public Matrix3 GetTransformation(DrawingUnits insertionUnits)
        {
            double docScale = UnitHelper.ConversionFactor(Block.Record.Units, insertionUnits);
            Matrix3 trans = MathHelper.ArbitraryAxis(Normal);
            trans *= Matrix3.RotationZ(rotation * MathHelper.DegToRad);
            trans *= Matrix3.Scale(scale * docScale);

            return trans;
        }

        /// <summary>
        /// Recalculate the attributes position, normal, rotation, text height, width factor, and oblique angle from the values applied to the insertion.
        /// </summary>
        /// <remarks>
        /// Changes to the insert, the block, or the document insertion units will require this method to be called manually.<br />
        /// The attributes position, normal, rotation, text height, width factor, and oblique angle values includes the transformations applied to the insertion,
        /// if required this method will calculate the proper values according to the ones defined by the attribute definition.<br />
        /// All the attribute values can be changed manually independently to its definition,
        /// but, usually, you will want them to be transformed with the insert based on the local values defined by the attribute definition.<br />
        /// This method only applies to attributes that have a definition, some DXF files might generate attributes that have no definition in the block.<br />
        /// At the moment the attribute width factor and oblique angle are not calculated, this is applied to inserts with non uniform scaling.
        /// </remarks>
        public void TransformAttributes()
        {
            // if the insert does not contain attributes there is nothing to do
            if (attributes.Count == 0)
                return;

            DrawingUnits insUnits;

            if (Owner == null)
                insUnits = DrawingUnits.Unitless;
            else
                // if the insert belongs to a block the units to use are the ones defined in the BlockRecord
                // if the insert belongs to a layout the units to use are the ones defined in the Document
                insUnits = Owner.Record.Layout == null ? Owner.Record.Units : Owner.Record.Owner.Owner.DrawingVariables.InsUnits;

            Matrix3 transformation = GetTransformation(insUnits);
            Vector3 translation = Position - transformation * block.Origin;

            foreach (Attribute att in attributes)
            {
                AttributeDefinition attdef = att.Definition;
                if (attdef == null)
                    continue;

                // reset the attribute to its default values
                att.Position = attdef.Position;
                att.Height = attdef.Height;
                att.WidthFactor = attdef.WidthFactor;
                att.ObliqueAngle = attdef.ObliqueAngle;
                att.Rotation = attdef.Rotation;
                att.Normal = attdef.Normal;

                att.TransformBy(transformation, translation);
            }
        }

        /// <summary>
        /// Explodes the current insert.
        /// </summary>
        /// <returns>A list of entities.</returns>
        /// <remarks>
        /// Non-uniform scaling is not supported by all entities. Read the documentation of the entities TranformBy method.
        /// </remarks>
        public List<EntityObject> Explode()
        {
            bool isUniformScale = MathHelper.IsEqual(scale.X, scale.Y) &&
                                  MathHelper.IsEqual(scale.Y, scale.Z);

            List<EntityObject> entities = new List<EntityObject>();
            Matrix3 transformation = GetTransformation(Owner == null ? DrawingUnits.Unitless : Block.Record.Owner.Owner.DrawingVariables.InsUnits);
            Vector3 translation = Position - transformation * block.Origin;

            foreach (EntityObject entity in block.Entities)
            {
                // TODO: entities with no implemented TransformBy method
                if (entity.Type == EntityType.Viewport) continue;

                // entities with reactors are associated with other entities they will handle the transformation
                if (entity.Reactors.Count > 0)
                    continue;

                if(!isUniformScale)
                {
                    switch (entity.Type)
                    {
                        case EntityType.Circle:
                        {
                            Circle circle = (Circle) entity;

                            Ellipse ellipse = new Ellipse
                            {
                                //EntityObject properties
                                Layer = (Layer) entity.Layer.Clone(),
                                Linetype = (Linetype) entity.Linetype.Clone(),
                                Color = (AciColor) entity.Color.Clone(),
                                Lineweight = entity.Lineweight,
                                Transparency = (Transparency) entity.Transparency.Clone(),
                                LinetypeScale = entity.LinetypeScale,
                                Normal = entity.Normal,
                                IsVisible = entity.IsVisible,
                                //Ellipse properties
                                Center = circle.Center,
                                MajorAxis = 2 * circle.Radius,
                                MinorAxis = 2 * circle.Radius,
                                Thickness = circle.Thickness
                            };
                            foreach (XData data in XData.Values)
                                entity.XData.Add((XData) data.Clone());

                            ellipse.TransformBy(transformation, translation);
                            entities.Add(ellipse);
                            break;
                        }
                        case EntityType.Arc:
                        {
                            Arc arc = (Arc) entity;
                            Ellipse ellipse = new Ellipse
                            {
                                //EntityObject properties
                                Layer = (Layer) entity.Layer.Clone(),
                                Linetype = (Linetype) entity.Linetype.Clone(),
                                Color = (AciColor) entity.Color.Clone(),
                                Lineweight = entity.Lineweight,
                                Transparency = (Transparency) entity.Transparency.Clone(),
                                LinetypeScale = entity.LinetypeScale,
                                Normal = entity.Normal,
                                IsVisible = entity.IsVisible,
                                //Ellipse properties
                                Center = arc.Center,
                                MajorAxis = 2 * arc.Radius,
                                MinorAxis = 2 * arc.Radius,
                                StartAngle = arc.StartAngle,
                                EndAngle = arc.EndAngle,
                                Thickness = arc.Thickness
                            };
                            ellipse.TransformBy(transformation, translation);
                            entities.Add(ellipse);
                            break;
                        }
                        case EntityType.LwPolyline:
                        {
                            List<EntityObject> newEntities = ((LwPolyline) entity).Explode();
                            foreach (EntityObject newEntity in newEntities)
                            {
                                newEntity.TransformBy(transformation, translation);
                                entities.Add(newEntity);
                            }
                            break;
                        }
                        case EntityType.MLine:
                        {
                            List<EntityObject> newEntities = ((MLine)entity).Explode();
                            foreach (EntityObject newEntity in newEntities)
                            {
                                newEntity.TransformBy(transformation, translation);
                                entities.Add(newEntity);
                            }
                            break;
                        }
                        default:
                        {
                            EntityObject newEntity = (EntityObject) entity.Clone();
                            newEntity.TransformBy(transformation, translation);
                            entities.Add(newEntity);
                            break;
                        }
                    }
                }
                else
                {
                    EntityObject newEntity = (EntityObject) entity.Clone();
                    newEntity.TransformBy(transformation, translation);
                    entities.Add(newEntity);
                }
            }

            return entities;
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
            Vector3 newScale;
            double newRotation;

            newPosition = transformation * Position + translation;
            newNormal = transformation * Normal;

            Matrix3 transOW = MathHelper.ArbitraryAxis(Normal);
            transOW *= Matrix3.RotationZ(Rotation * MathHelper.DegToRad);

            Matrix3 transWO = MathHelper.ArbitraryAxis(newNormal);
            transWO = transWO.Transpose();

            Vector3 v = transOW * Vector3.UnitX;
            v = transformation * v;
            v = transWO * v;
            double angle = Vector2.Angle(new Vector2(v.X, v.Y));

            newRotation = angle * MathHelper.RadToDeg;

            transWO = Matrix3.RotationZ(newRotation * MathHelper.DegToRad).Transpose() * transWO;

            Vector3 s = transOW * Scale;
            s = transformation * s;
            s = transWO * s;
            newScale = s;

            Normal = newNormal;
            Position = newPosition;
            Scale = newScale;
            Rotation = newRotation;

            TransformAttributes();
        }

        /// <summary>
        /// Assigns a handle to the object based in a integer counter.
        /// </summary>
        /// <param name="entityNumber">Number to assign.</param>
        /// <returns>Next available entity number.</returns>
        /// <remarks>
        /// Some objects might consume more than one, is, for example, the case of polylines that will assign
        /// automatically a handle to its vertexes. The entity number will be converted to an hexadecimal number.
        /// </remarks>
        internal override long AsignHandle(long entityNumber)
        {
            entityNumber = endSequence.AsignHandle(entityNumber);
            foreach (Attribute attrib in attributes)
            {
                entityNumber = attrib.AsignHandle(entityNumber);
            }
            return base.AsignHandle(entityNumber);
        }


        /// <summary>
        /// Creates a new Insert that is a copy of the current instance.
        /// </summary>
        /// <returns>A new Insert that is a copy of this instance.</returns>
        public override object Clone()
        {
            // copy attributes
            List<Attribute> copyAttributes = new List<Attribute>();
            foreach (Attribute att in attributes)
                copyAttributes.Add((Attribute)att.Clone());

            Insert entity = new Insert(copyAttributes)
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
                //Insert properties
                Position = position,
                Block = (Block) block.Clone(),
                Scale = scale,
                Rotation = rotation,
            };

            // copy extended data
            foreach (XData data in XData.Values)
                entity.XData.Add((XData) data.Clone());

            return entity;
        }

        #endregion
    }
}