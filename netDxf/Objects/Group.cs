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

using System.Collections.Generic;
using netDxf.Collections;
using netDxf.Entities;
using netDxf.Tables;

namespace netDxf.Objects
{
    /// <summary>
    /// Represents a group of entities.
    /// </summary>
    public class Group :
        TableObject
    {
        #region delegates and events

        public delegate void EntityAddedEventHandler(Group sender, GroupEntityChangeEventArgs e);

        public event EntityAddedEventHandler EntityAdded;

        protected virtual void OnEntityAddedEvent(EntityObject item)
        {
            EntityAddedEventHandler ae = EntityAdded;
            if (ae != null)
                ae(this, new GroupEntityChangeEventArgs(item));
        }

        public delegate void EntityRemovedEventHandler(Group sender, GroupEntityChangeEventArgs e);

        public event EntityRemovedEventHandler EntityRemoved;

        protected virtual void OnEntityRemovedEvent(EntityObject item)
        {
            EntityRemovedEventHandler ae = EntityRemoved;
            if (ae != null)
                ae(this, new GroupEntityChangeEventArgs(item));
        }

        #endregion

        #region private fields

        private string description;
        private bool isSelectable;
        private bool isUnnamed;
        private readonly EntityCollection entities;

        #endregion

        #region constructor

        /// <summary>
        /// Initialized a new unnamed empty group.
        /// </summary>
        /// <remarks>
        /// A unique name will be generated when the group is added to the document.
        /// </remarks>
        public Group()
            : this(string.Empty)
        {
        }

        /// <summary>
        /// Initialized a new empty group.
        /// </summary>
        /// <param name="name">Group name.</param>
        /// <remarks>
        /// If the name is set to null or empty, a unique name will be generated when the group is added to the document.
        /// </remarks>
        public Group(string name)
            : this(name, null)
        {
        }

        /// <summary>
        /// Initialized a new group with the specified entities.
        /// </summary>
        /// <param name="entities">The list of entities contained in the group.</param>
        /// <remarks>
        /// A unique name will be generated when the group is added to the document.
        /// </remarks>
        public Group(IEnumerable<EntityObject> entities)
            : this(string.Empty, entities)
        {
        }

        /// <summary>
        /// Initialized a new group with the specified entities.
        /// </summary>
        /// <param name="name">Group name (optional).</param>
        /// <param name="entities">The list of entities contained in the group.</param>
        /// <remarks>
        /// If the name is set to null or empty, a unique name will be generated when the group is added to the document.
        /// </remarks>
        public Group(string name, IEnumerable<EntityObject> entities)
            : base(name, DxfObjectCode.Group, !string.IsNullOrEmpty(name))
        {
            isUnnamed = string.IsNullOrEmpty(name);
            description = string.Empty;
            isSelectable = true;
            this.entities = new EntityCollection();
            this.entities.BeforeAddItem += Entities_BeforeAddItem;
            this.entities.AddItem += Entities_AddItem;
            this.entities.BeforeRemoveItem += Entities_BeforeRemoveItem;
            this.entities.RemoveItem += Entities_RemoveItem;
            if(entities != null)
                this.entities.AddRange(entities);
        }

        internal Group(string name, bool checkName)
            : base(name, DxfObjectCode.Group, checkName)
        {
            isUnnamed = string.IsNullOrEmpty(name) || name.StartsWith("*");
            description = string.Empty;
            isSelectable = true;
            entities = new EntityCollection();
            entities.BeforeAddItem += Entities_BeforeAddItem;
            entities.AddItem += Entities_AddItem;
            entities.BeforeRemoveItem += Entities_BeforeRemoveItem;
            entities.RemoveItem += Entities_RemoveItem;
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
                base.Name = value;
                isUnnamed = false;
            }
        }

        /// <summary>
        /// Gets or sets the description of the group.
        /// </summary>
        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        /// <summary>
        /// Gets if the group has an automatic generated name.
        /// </summary>
        public bool IsUnnamed
        {
            get { return isUnnamed; }
            internal set { isUnnamed = value; }
        }

        /// <summary>
        /// Gets or sets if the group is selectable.
        /// </summary>
        public bool IsSelectable
        {
            get { return isSelectable; }
            set { isSelectable = value; }
        }

        /// <summary>
        /// Gets the list of entities contained in the group.
        /// </summary>
        /// <remarks>
        /// When the group is added to the document the entities in it will be automatically added too.<br/>
        /// An entity may be contained in different groups.
        /// </remarks>
        public EntityCollection Entities
        {
            get { return entities; }
        }

        /// <summary>
        /// Gets the owner of the actual dxf object.
        /// </summary>
        public new Groups Owner
        {
            get { return (Groups) base.Owner; }
            internal set { base.Owner = value; }
        }

        #endregion

        #region overrides

        /// <summary>
        /// Creates a new Group that is a copy of the current instance.
        /// </summary>
        /// <param name="newName">Group name of the copy.</param>
        /// <returns>A new Group that is a copy of this instance.</returns>
        /// <remarks>The entities that belong to the group will also be cloned.</remarks>
        public override TableObject Clone(string newName)
        {
            EntityObject[] refs = new EntityObject[entities.Count];
            for (int i = 0; i < entities.Count; i++)
            {
                refs[i] = (EntityObject) entities[i].Clone();
            }

            Group copy = new Group(newName, refs)
            {
                Description = description,
                IsSelectable = isSelectable
            };

            foreach (XData data in XData.Values)
                copy.XData.Add((XData)data.Clone());

            return copy;
        }

        /// <summary>
        /// Creates a new Group that is a copy of the current instance.
        /// </summary>
        /// <returns>A new Group that is a copy of this instance.</returns>
        public override object Clone()
        {
            return Clone(IsUnnamed ? string.Empty : Name);
        }

        #endregion

        #region Entities collection events

        private void Entities_BeforeAddItem(EntityCollection sender, EntityCollectionEventArgs e)
        {
            // null or duplicate items are not allowed in the entities list.
            if (e.Item == null)
                e.Cancel = true;
            else if (entities.Contains(e.Item))
                e.Cancel = true;
            else
                e.Cancel = false;
        }

        private void Entities_AddItem(EntityCollection sender, EntityCollectionEventArgs e)
        {
            e.Item.AddReactor(this);
            OnEntityAddedEvent(e.Item);
        }

        private void Entities_BeforeRemoveItem(EntityCollection sender, EntityCollectionEventArgs e)
        {
        }

        private void Entities_RemoveItem(EntityCollection sender, EntityCollectionEventArgs e)
        {
            e.Item.RemoveReactor(this);
            OnEntityRemovedEvent(e.Item);
        }

        #endregion
    }
}