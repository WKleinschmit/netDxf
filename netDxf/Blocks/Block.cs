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
using System.Collections.Generic;
using netDxf.Collections;
using netDxf.Entities;
using netDxf.Header;
using netDxf.Objects;
using netDxf.Tables;

namespace netDxf.Blocks
{
    /// <summary>
    /// Represents a block definition.
    /// </summary>
    /// <remarks>
    /// Avoid to add any kind of dimensions to the block's entities list, programs loading DXF files with them seems to behave in a weird fashion.
    /// This is not applicable when working in the Model and Paper space blocks.
    /// </remarks>
    public class Block :
        TableObject
    {
        #region delegates and events

        public delegate void LayerChangedEventHandler(Block sender, TableObjectChangedEventArgs<Layer> e);

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

        public delegate void EntityAddedEventHandler(Block sender, BlockEntityChangeEventArgs e);

        public event EntityAddedEventHandler EntityAdded;

        protected virtual void OnEntityAddedEvent(EntityObject item)
        {
            EntityAddedEventHandler ae = EntityAdded;
            if (ae != null)
                ae(this, new BlockEntityChangeEventArgs(item));
        }

        public delegate void EntityRemovedEventHandler(Block sender, BlockEntityChangeEventArgs e);

        public event EntityRemovedEventHandler EntityRemoved;

        protected virtual void OnEntityRemovedEvent(EntityObject item)
        {
            EntityRemovedEventHandler ae = EntityRemoved;
            if (ae != null)
                ae(this, new BlockEntityChangeEventArgs(item));
        }

        public delegate void AttributeDefinitionAddedEventHandler(Block sender, BlockAttributeDefinitionChangeEventArgs e);

        public event AttributeDefinitionAddedEventHandler AttributeDefinitionAdded;

        protected virtual void OnAttributeDefinitionAddedEvent(AttributeDefinition item)
        {
            AttributeDefinitionAddedEventHandler ae = AttributeDefinitionAdded;
            if (ae != null)
                ae(this, new BlockAttributeDefinitionChangeEventArgs(item));
        }

        public delegate void AttributeDefinitionRemovedEventHandler(Block sender, BlockAttributeDefinitionChangeEventArgs e);

        public event AttributeDefinitionRemovedEventHandler AttributeDefinitionRemoved;

        protected virtual void OnAttributeDefinitionRemovedEvent(AttributeDefinition item)
        {
            AttributeDefinitionRemovedEventHandler ae = AttributeDefinitionRemoved;
            if (ae != null)
                ae(this, new BlockAttributeDefinitionChangeEventArgs(item));
        }

        #endregion

        #region private fields

        private readonly EntityCollection entities;
        private readonly AttributeDefinitionDictionary attributes;
        private string description;
        private readonly EndBlock end;
        private BlockTypeFlags flags;
        private Layer layer;
        private Vector3 origin;
        private readonly string xrefFile;
        private bool forInternalUse;

        #endregion

        #region constants

        /// <summary>
        /// Default ModelSpace block name.
        /// </summary>
        public const string DefaultModelSpaceName = "*Model_Space";

        /// <summary>
        /// Default PaperSpace block name.
        /// </summary>
        public const string DefaultPaperSpaceName = "*Paper_Space";

        /// <summary>
        /// Gets the default *Model_Space block.
        /// </summary>
        public static Block ModelSpace
        {
            get { return new Block(DefaultModelSpaceName, null, null, false); }
        }

        /// <summary>
        /// Gets the default *Paper_Space block.
        /// </summary>
        public static Block PaperSpace
        {
            get { return new Block(DefaultPaperSpaceName, null, null, false); }
        }

        #endregion

        #region constructors

        /// <summary>
        /// Initializes a new instance of the <c>Block</c> class as an external reference drawing. 
        /// </summary>
        /// <param name="name">Block name.</param>
        /// <param name="xrefFile">External reference path name.</param>
        /// <remarks>Only DWG files can be used as externally referenced blocks.</remarks>
        public Block(string name, string xrefFile)
            : this(name, xrefFile, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Block</c> class as an external reference drawing. 
        /// </summary>
        /// <param name="name">Block name.</param>
        /// <param name="xrefFile">External reference path name.</param>
        /// <param name="overlay">Specifies if the external reference is an overlay, by default it is set to false.</param>
        /// <remarks>Only DWG files can be used as externally referenced blocks.</remarks>
        public Block(string name, string xrefFile, bool overlay)
            : this(name, null, null, true)
        {
            if (string.IsNullOrEmpty(xrefFile))
                throw new ArgumentNullException(nameof(xrefFile));

            this.xrefFile = xrefFile;
            flags = BlockTypeFlags.XRef | BlockTypeFlags.ResolvedExternalReference;
            if (overlay)
                flags |= BlockTypeFlags.XRefOverlay;
        }

        /// <summary>
        /// Initializes a new instance of the <c>Block</c> class.
        /// </summary>
        /// <param name="name">Block name.</param>
        public Block(string name)
            : this(name, null, null, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Block</c> class.
        /// </summary>
        /// <param name="name">Block name.</param>
        /// <param name="entities">The list of entities that make the block.</param>
        public Block(string name, IEnumerable<EntityObject> entities)
            : this(name, entities, null, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Block</c> class.
        /// </summary>
        /// <param name="name">Block name.</param>
        /// <param name="entities">The list of entities that make the block.</param>
        /// <param name="attributes">The list of attribute definitions that make the block.</param>
        public Block(string name, IEnumerable<EntityObject> entities, IEnumerable<AttributeDefinition> attributes)
            : this(name, entities, attributes, true)
        {
        }

        internal Block(string name, IEnumerable<EntityObject> entities, IEnumerable<AttributeDefinition> attributes, bool checkName)
            : base(name, DxfObjectCode.Block, checkName)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            IsReserved = string.Equals(name, DefaultModelSpaceName, StringComparison.OrdinalIgnoreCase);
            forInternalUse = name.StartsWith("*");
            description = string.Empty;
            origin = Vector3.Zero;
            layer = Layer.Default;
            xrefFile = string.Empty;
            Owner = new BlockRecord(name);
            flags = BlockTypeFlags.None;
            end = new EndBlock(this);

            this.entities = new EntityCollection();
            this.entities.BeforeAddItem += Entities_BeforeAddItem;
            this.entities.AddItem += Entities_AddItem;
            this.entities.BeforeRemoveItem += Entities_BeforeRemoveItem;
            this.entities.RemoveItem += Entities_RemoveItem;
            if (entities != null) this.entities.AddRange(entities);

            this.attributes = new AttributeDefinitionDictionary();
            this.attributes.BeforeAddItem += AttributeDefinitions_BeforeAddItem;
            this.attributes.AddItem += AttributeDefinitions_ItemAdd;
            this.attributes.BeforeRemoveItem += AttributeDefinitions_BeforeRemoveItem;
            this.attributes.RemoveItem += AttributeDefinitions_RemoveItem;
            if (attributes != null) this.attributes.AddRange(attributes);
        }

        #endregion

        #region public properties

        /// <summary>
        /// Gets the name of the table object.
        /// </summary>
        /// <remarks>Table object names are case insensitive.</remarks>
        public new string Name
        {
            get { return base.Name; }
            set
            {
                if (forInternalUse)
                    throw new ArgumentException("Blocks for internal use cannot be renamed.", nameof(value));
                base.Name = value;
                Record.Name = value;
            }
        }

        /// <summary>
        /// Gets or sets the block description.
        /// </summary>
        public string Description
        {
            get { return description; }
            set { description = string.IsNullOrEmpty(value) ? string.Empty : value; }
        }

        /// <summary>
        /// Gets or sets the block origin in world coordinates, it is recommended to always keep this value to the default Vector3.Zero.
        /// </summary>
        public Vector3 Origin
        {
            get { return origin; }
            set { origin = value; }
        }

        /// <summary>
        /// Gets or sets the block <see cref="Layer">layer</see>.
        /// </summary>
        /// <remarks>It seems that the block layer is always the default "0" regardless of what is defined here, so it is pointless to change this value.</remarks>
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
        /// Gets the <see cref="EntityObject">entity</see> list of the block.
        /// </summary>
        /// <remarks>Null entities, attribute definitions or entities already owned by another block or document cannot be added to the list.</remarks>
        public EntityCollection Entities
        {
            get { return entities; }
        }

        /// <summary>
        /// Gets the <see cref="AttributeDefinition">entity</see> list of the block.
        /// </summary>
        /// <remarks>
        /// Null or attribute definitions already owned by another block or document cannot be added to the list.
        /// Additionally Paper Space blocks do not contain attribute definitions.
        /// </remarks>
        public AttributeDefinitionDictionary AttributeDefinitions
        {
            get { return attributes; }
        }

        /// <summary>
        /// Gets the block record associated with this block.
        /// </summary>
        /// <remarks>It returns the same object as the owner property.</remarks>
        public BlockRecord Record
        {
            get { return (BlockRecord) Owner; }
        }

        /// <summary>
        /// Gets the block-type flags (bit-coded values, may be combined).
        /// </summary>
        public BlockTypeFlags Flags
        {
            get { return flags; }
            internal set { flags = value; }
        }

        /// <summary>
        /// Gets the external reference path name.
        /// </summary>
        /// <remarks>
        /// This property is only applicable to externally referenced blocks.
        /// </remarks>
        public string XrefFile
        {
            get { return xrefFile; }
        }

        /// <summary>
        /// Gets if the block is an external reference.
        /// </summary>
        public bool IsXRef
        {
            get { return flags.HasFlag(BlockTypeFlags.XRef); }
        }

        ///// <summary>
        ///// All blocks that starts with "*" are for internal use only.
        ///// </summary>
        //public bool IsForInternalUseOnly
        //{
        //    get { return this.forInternalUse; }
        //}

        #endregion

        #region internal properties

        /// <summary>
        /// Gets or sets the block end object.
        /// </summary>
        internal EndBlock End
        {
            get { return end; }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Creates a block from the content of a <see cref="DxfDocument">document</see>.
        /// </summary>
        /// <param name="doc">A <see cref="DxfDocument">DxfDocument</see> instance.</param>
        /// <param name="name">Name of the new block.</param>
        /// <returns>The block build from the <see cref="DxfDocument">document</see> content.</returns>
        /// <remarks>Only the entities contained in ModelSpace will make part of the block.</remarks>
        public static Block Create(DxfDocument doc, string name)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            Block block = new Block(name) {Origin = doc.DrawingVariables.InsBase};
            block.Record.Units = doc.DrawingVariables.InsUnits;

            foreach (EntityObject entity in doc.Layouts[Layout.ModelSpaceName].AssociatedBlock.Entities)
            {
                // entities with reactors will be handle by the entity that controls it
                // and will not be added to the block automatically
                if (entity.Reactors.Count > 0)
                {
                    // only entities that belong only to groups will be added
                    // blocks cannot contain groups
                    bool add = false;
                    foreach (DxfObject reactor in entity.Reactors)
                    {
                        Group group = reactor as Group;
                        if (group != null)
                        {
                            add = true;
                        }
                        else
                        {
                            add = false; // at least one reactor is not a group, skip the entity
                            break;
                        }
                    }
                    if(!add) continue;
                }

                EntityObject clone = (EntityObject) entity.Clone();
                block.Entities.Add(clone);
            }

            foreach (AttributeDefinition attdef in doc.Layouts[Layout.ModelSpaceName].AssociatedBlock.AttributeDefinitions.Values)
            {
                AttributeDefinition clone = (AttributeDefinition) attdef.Clone();
                block.AttributeDefinitions.Add(clone);
            }

            return block;
        }

        /// <summary>
        /// Creates a block from an external dxf file.
        /// </summary>
        /// <param name="file">Dxf file name.</param>
        /// <returns>The block build from the dxf file content. It will return null if the file has not been able to load.</returns>
        /// <remarks>
        /// The name of the block will be the file name without extension, and
        /// only the entities contained in ModelSpace will make part of the block.
        /// </remarks>
        public static Block Load(string file)
        {
            return Load(file, null);
        }

        /// <summary>
        /// Creates a block from an external dxf file.
        /// </summary>
        /// <param name="file">Dxf file name.</param>
        /// <param name="name">Name of the new block.</param>
        /// <returns>The block build from the dxf file content. It will return null if the file has not been able to load.</returns>
        /// <remarks>Only the entities contained in ModelSpace will make part of the block.</remarks>
        public static Block Load(string file, string name)
        {
#if DEBUG
            DxfDocument dwg = DxfDocument.Load(file);
#else
            DxfDocument dwg;
            try 
            {
                dwg = DxfDocument.Load(file);
            }
            catch
            {
                return null;
            }
#endif

            string blkName = string.IsNullOrEmpty(name) ? dwg.Name : name;
            return Create(dwg, blkName);
        }

        /// <summary>
        /// Saves a block to a text dxf file.
        /// </summary>
        /// <param name="file">Dxf file name.</param>
        /// <param name="version">Version of the dxf database version.</param>
        /// <returns>Return true if the file has been successfully save, false otherwise.</returns>
        public bool Save(string file, DxfVersion version)
        {
            return Save(file, version, false);
        }

        /// <summary>
        /// Saves a block to a dxf file.
        /// </summary>
        /// <param name="file">Dxf file name.</param>
        /// <param name="version">Version of the dxf database version.</param>
        /// <param name="isBinary">Defines if the file will be saved as binary.</param>
        /// <returns>Return true if the file has been successfully save, false otherwise.</returns>
        public bool Save(string file, DxfVersion version, bool isBinary)
        {
            DxfDocument dwg = new DxfDocument(version);
            dwg.DrawingVariables.InsBase = origin;
            dwg.DrawingVariables.InsUnits = Record.Units;

            foreach (AttributeDefinition attdef in attributes.Values)
            {
                if(!dwg.Layouts[Layout.ModelSpaceName].AssociatedBlock.AttributeDefinitions.ContainsTag(attdef.Tag))
                    dwg.Layouts[Layout.ModelSpaceName].AssociatedBlock.AttributeDefinitions.Add((AttributeDefinition) attdef.Clone());
            }

            foreach (EntityObject entity in entities)
                dwg.Layouts[Layout.ModelSpaceName].AssociatedBlock.Entities.Add((EntityObject) entity.Clone());


            return dwg.Save(file, isBinary);
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Hack to change the table name without having to check its name. Some invalid characters are used for internal purposes only.
        /// </summary>
        /// <param name="newName">Table object new name.</param>
        internal new void SetName(string newName, bool checkName)
        {
            base.SetName(newName, checkName);
            Record.Name = newName;
            forInternalUse = newName.StartsWith("*");
        }

        #endregion

        #region overrides

        private static TableObject Clone(Block block, string newName, bool checkName)
        {
            if (block.Record.Layout != null && !IsValidName(newName))
                throw new ArgumentException("*Model_Space and *Paper_Space# blocks can only be cloned with a new valid name.");

            Block copy = new Block(newName, null, null, checkName)
            {
                Description = block.description,
                Flags = block.flags,
                Layer = (Layer)block.Layer.Clone(),
                Origin = block.origin
            };

            // remove anonymous flag for renamed anonymous blocks
            if (checkName)
                copy.Flags &= ~BlockTypeFlags.AnonymousBlock;

            foreach (EntityObject e in block.entities)
                copy.entities.Add((EntityObject)e.Clone());

            foreach (AttributeDefinition a in block.attributes.Values)
                copy.attributes.Add((AttributeDefinition)a.Clone());

            foreach (XData data in block.XData.Values)
                copy.XData.Add((XData)data.Clone());

            foreach (XData data in block.Record.XData.Values)
                copy.Record.XData.Add((XData)data.Clone());

            return copy;
        }

        /// <summary>
        /// Creates a new Block that is a copy of the current instance.
        /// </summary>
        /// <param name="newName">Block name of the copy.</param>
        /// <returns>A new Block that is a copy of this instance.</returns>
        public override TableObject Clone(string newName)
        {
            return Clone(this, newName, true) ;
        }

        /// <summary>
        /// Creates a new Block that is a copy of the current instance.
        /// </summary>
        /// <returns>A new Block that is a copy of this instance.</returns>
        public override object Clone()
        {
            return Clone(this, Name, !flags.HasFlag(BlockTypeFlags.AnonymousBlock));
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
            entityNumber = Owner.AsignHandle(entityNumber);
            entityNumber = end.AsignHandle(entityNumber);
            foreach (AttributeDefinition attdef in attributes.Values)
            {
                entityNumber = attdef.AsignHandle(entityNumber);
            }
            return base.AsignHandle(entityNumber);
        }

        #endregion

        #region Entities collection events

        private void Entities_BeforeAddItem(EntityCollection sender, EntityCollectionEventArgs e)
        {
            // null items, entities already owned by another Block, attribute definitions and attributes are not allowed in the entities list.
            if (e.Item == null)
                e.Cancel = true;
            else if (entities.Contains(e.Item))
                e.Cancel = true;
            else if (Flags.HasFlag(BlockTypeFlags.ExternallyDependent))
                e.Cancel = true;
            else if (e.Item.Owner != null)
            {
                // if the block does not belong to a document, all entities which owner is not null will be rejected
                if (Record.Owner == null)
                    e.Cancel = true;
                // if the block belongs to a document, the entity will be added to the block only if both, the block and the entity document, are the same
                // this is handled by the BlocksRecordCollection
            }
            else
                e.Cancel = false;
        }

        private void Entities_AddItem(EntityCollection sender, EntityCollectionEventArgs e)
        {
            if (e.Item.Type == EntityType.Leader)
            {
                Leader leader = (Leader) e.Item;
                if (leader.Annotation != null)
                {
                    entities.Add(leader.Annotation);
                }
            }
            else if (e.Item.Type == EntityType.Hatch)
            {
                Hatch hatch = (Hatch) e.Item;
                foreach (HatchBoundaryPath path in hatch.BoundaryPaths)
                {
                    foreach (EntityObject entity in path.Entities)
                        entities.Add(entity);
                }
            }

            OnEntityAddedEvent(e.Item);
            e.Item.Owner = this;
        }

        private void Entities_BeforeRemoveItem(EntityCollection sender, EntityCollectionEventArgs e)
        {
            if (e.Item.Reactors.Count > 0)
                e.Cancel = true;
            else
                // only items owned by the actual block can be removed
                e.Cancel = !ReferenceEquals(e.Item.Owner, this);
        }

        private void Entities_RemoveItem(EntityCollection sender, EntityCollectionEventArgs e)
        {
            OnEntityRemovedEvent(e.Item);
            e.Item.Owner = null;
        }

        #endregion

        #region Attributes dictionary events

        private void AttributeDefinitions_BeforeAddItem(AttributeDefinitionDictionary sender, AttributeDefinitionDictionaryEventArgs e)
        {
            // attributes with the same tag, and attribute definitions already owned by another Block are not allowed in the attributes list.
            if (e.Item == null)
                e.Cancel = true;
            else if (Flags.HasFlag(BlockTypeFlags.ExternallyDependent))
                e.Cancel = true;
            else if(Name.StartsWith(DefaultPaperSpaceName)) // paper space blocks do not contain attribute definitions
                e.Cancel = true;
            else if (attributes.ContainsTag(e.Item.Tag))
                e.Cancel = true;
            else if (e.Item.Owner != null)
            {
                // if the block does not belong to a document, all attribute definitions which owner is not null will be rejected
                if (Record.Owner == null)
                    e.Cancel = true;
                // if the block belongs to a document, the entity will be added to the block only if both, the block and the attribute definitions document, are the same
                // this is handled by the BlocksRecordCollection
            }
            else
                e.Cancel = false;
        }

        private void AttributeDefinitions_ItemAdd(AttributeDefinitionDictionary sender, AttributeDefinitionDictionaryEventArgs e)
        {
            OnAttributeDefinitionAddedEvent(e.Item);
            e.Item.Owner = this;
            // the block has attributes
            flags |= BlockTypeFlags.NonConstantAttributeDefinitions;
        }

        private void AttributeDefinitions_BeforeRemoveItem(AttributeDefinitionDictionary sender, AttributeDefinitionDictionaryEventArgs e)
        {
            // only attribute definitions owned by the actual block can be removed
            e.Cancel = !ReferenceEquals(e.Item.Owner, this) ;
        }

        private void AttributeDefinitions_RemoveItem(AttributeDefinitionDictionary sender, AttributeDefinitionDictionaryEventArgs e)
        {
            OnAttributeDefinitionRemovedEvent(e.Item);
            e.Item.Owner = null;
            if (attributes.Count == 0)
                flags &= ~BlockTypeFlags.NonConstantAttributeDefinitions;
        }

        #endregion
    }
}