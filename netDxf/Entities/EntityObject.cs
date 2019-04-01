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
using netDxf.Blocks;
using netDxf.Collections;
using netDxf.Tables;

namespace netDxf.Entities
{
    /// <summary>
    /// Represents a generic entity.
    /// </summary>
    public abstract class EntityObject :
        DxfObject,
        IHasXData,
        ICloneable
    {
        #region delegates and events

        public delegate void LayerChangedEventHandler(EntityObject sender, TableObjectChangedEventArgs<Layer> e);
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

        public delegate void LinetypeChangedEventHandler(EntityObject sender, TableObjectChangedEventArgs<Linetype> e);
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

        private readonly EntityType type;
        private AciColor color;
        private Layer layer;
        private Linetype linetype;
        private Lineweight lineweight;
        private Transparency transparency;
        private double linetypeScale;
        private bool isVisible;
        private Vector3 normal;
        private readonly XDataDictionary xData;
        private readonly List<DxfObject> reactors;

        #endregion

        #region constructors

        protected EntityObject(EntityType type, string dxfCode)
            : base(dxfCode)
        {
            this.type = type;
            color = AciColor.ByLayer;
            layer = Layer.Default;
            linetype = Linetype.ByLayer;
            lineweight = Lineweight.ByLayer;
            transparency = Transparency.ByLayer;
            linetypeScale = 1.0;
            isVisible = true;
            normal = Vector3.UnitZ;
            reactors = new List<DxfObject>();
            xData = new XDataDictionary();
            xData.AddAppReg += XData_AddAppReg;
            xData.RemoveAppReg += XData_RemoveAppReg;
        }

        #endregion

        #region public properties

        /// <summary>
        /// Gets the list of dxf objects that has been attached to this entity.
        /// </summary>
        public IReadOnlyList<DxfObject> Reactors
        {
            get { return reactors; }
        }

        /// <summary>
        /// Gets the entity <see cref="EntityType">type</see>.
        /// </summary>
        public EntityType Type
        {
            get { return type; }
        }

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
        /// Gets or sets the entity <see cref="Lineweight">line weight</see>, one unit is always 1/100 mm (default = ByLayer).
        /// </summary>
        public Lineweight Lineweight
        {
            get { return lineweight; }
            set { lineweight = value; }
        }

        /// <summary>
        /// Gets or sets layer <see cref="Transparency">transparency</see> (default: ByLayer).
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
        /// Gets the owner of the actual dxf object.
        /// </summary>
        public new Block Owner
        {
            get { return (Block) base.Owner; }
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

        #region internal methods

        internal void AddReactor(DxfObject o)
        {
            reactors.Add(o);
        }

        internal bool RemoveReactor(DxfObject o)
        {
            return reactors.Remove(o);
        }

        #endregion

        #region public methods

        /// <summary>
        /// Moves, scales, and/or rotates the current entity given a 3x3 transformation matrix and a translation vector.
        /// </summary>
        /// <param name="transformation">Transformation matrix.</param>
        /// <param name="translation">Translation vector.</param>
        public abstract void TransformBy(Matrix3 transformation, Vector3 translation);

        #endregion

        #region overrides

        /// <summary>
        /// Converts the value of this instance to its equivalent string representation.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            return type.ToString();
        }

        #endregion

        #region ICloneable

        /// <summary>
        /// Creates a new entity that is a copy of the current instance.
        /// </summary>
        /// <returns>A new entity that is a copy of this instance.</returns>
        public abstract object Clone();

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