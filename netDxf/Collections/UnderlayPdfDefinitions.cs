﻿#region netDxf library, Copyright (C) 2009-2018 Daniel Carvajal (haplokuon@gmail.com)

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
using netDxf.Objects;
using netDxf.Tables;

namespace netDxf.Collections
{
    /// <summary>
    /// Represents a collection of PDF underlay definitions.
    /// </summary>
    public sealed class UnderlayPdfDefinitions :
        TableObjects<UnderlayPdfDefinition>
    {
        #region constructor

        internal UnderlayPdfDefinitions(DxfDocument document)
            : this(document, null)
        {
        }

        internal UnderlayPdfDefinitions(DxfDocument document, string handle)
            : base(document, DxfObjectCode.UnderlayPdfDefinitionDictionary, handle)
        {
            MaxCapacity = int.MaxValue;
        }

        #endregion

        #region override methods

        /// <summary>
        /// Adds a PDF underlay definition to the list.
        /// </summary>
        /// <param name="underlayPdfDefinition"><see cref="UnderlayPdfDefinition">UnderlayPdfDefinition</see> to add to the list.</param>
        /// <param name="assignHandle">Specifies if a handle needs to be generated for the underlay definition parameter.</param>
        /// <returns>
        /// If an underlay definition already exists with the same name as the instance that is being added the method returns the existing underlay definition,
        /// if not it will return the new underlay definition.
        /// </returns>
        internal override UnderlayPdfDefinition Add(UnderlayPdfDefinition underlayPdfDefinition, bool assignHandle)
        {
            if (list.Count >= MaxCapacity)
                throw new OverflowException(string.Format("Table overflow. The maximum number of elements the table {0} can have is {1}", CodeName, MaxCapacity));
            if (underlayPdfDefinition == null)
                throw new ArgumentNullException(nameof(underlayPdfDefinition));

            UnderlayPdfDefinition add;
            if (list.TryGetValue(underlayPdfDefinition.Name, out add))
                return add;

            if (assignHandle || string.IsNullOrEmpty(underlayPdfDefinition.Handle))
                Owner.NumHandles = underlayPdfDefinition.AsignHandle(Owner.NumHandles);

            list.Add(underlayPdfDefinition.Name, underlayPdfDefinition);
            references.Add(underlayPdfDefinition.Name, new List<DxfObject>());

            underlayPdfDefinition.Owner = this;

            underlayPdfDefinition.NameChanged += Item_NameChanged;

            Owner.AddedObjects.Add(underlayPdfDefinition.Handle, underlayPdfDefinition);

            return underlayPdfDefinition;
        }

        /// <summary>
        /// Removes a PDF underlay definition.
        /// </summary>
        /// <param name="name"><see cref="UnderlayPdfDefinition">UnderlayPdfDefinition</see> name to remove from the document.</param>
        /// <returns>True if the underlay definition has been successfully removed, or false otherwise.</returns>
        /// <remarks>Any underlay definition referenced by objects cannot be removed.</remarks>
        public override bool Remove(string name)
        {
            return Remove(this[name]);
        }

        /// <summary>
        /// Removes a PDF underlay definition.
        /// </summary>
        /// <param name="item"><see cref="UnderlayPdfDefinition">UnderlayPdfDefinition</see> to remove from the document.</param>
        /// <returns>True if the underlay definition has been successfully removed, or false otherwise.</returns>
        /// <remarks>Any underlay definition referenced by objects cannot be removed.</remarks>
        public override bool Remove(UnderlayPdfDefinition item)
        {
            if (item == null)
                return false;

            if (!Contains(item))
                return false;

            if (item.IsReserved)
                return false;

            if (references[item.Name].Count != 0)
                return false;

            Owner.AddedObjects.Remove(item.Handle);
            references.Remove(item.Name);
            list.Remove(item.Name);

            item.Handle = null;
            item.Owner = null;

            item.NameChanged -= Item_NameChanged;

            return true;
        }

        #endregion

        #region TableObject events

        private void Item_NameChanged(TableObject sender, TableObjectChangedEventArgs<string> e)
        {
            if (Contains(e.NewValue))
                throw new ArgumentException("There is already another PDF underlay definition with the same name.");

            list.Remove(sender.Name);
            list.Add(e.NewValue, (UnderlayPdfDefinition) sender);

            List<DxfObject> refs = references[sender.Name];
            references.Remove(sender.Name);
            references.Add(e.NewValue, refs);
        }

        #endregion
    }
}