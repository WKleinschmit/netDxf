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
using netDxf.Tables;

namespace netDxf.Collections
{
    /// <summary>
    /// Represents a collection of views.
    /// </summary>
    public sealed class Views :
        TableObjects<View>
    {
        #region constructor

        internal Views(DxfDocument document)
            : this(document, null)
        {
        }

        internal Views(DxfDocument document, string handle)
            : base(document, DxfObjectCode.ViewTable, handle)
        {
            MaxCapacity = short.MaxValue;
        }

        #endregion

        #region override methods

        /// <summary>
        /// Adds a view to the list.
        /// </summary>
        /// <param name="view"><see cref="View">View</see> to add to the list.</param>
        /// <param name="assignHandle">Specifies if a handle needs to be generated for the view parameter.</param>
        /// <returns>
        /// If a view already exists with the same name as the instance that is being added the method returns the existing view,
        /// if not it will return the new view.
        /// </returns>
        internal override View Add(View view, bool assignHandle)
        {
            if (list.Count >= MaxCapacity)
                throw new OverflowException(string.Format("Table overflow. The maximum number of elements the table {0} can have is {1}", CodeName, MaxCapacity));
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            View add;
            if (list.TryGetValue(view.Name, out add))
                return add;

            if (assignHandle || string.IsNullOrEmpty(view.Handle))
                Owner.NumHandles = view.AsignHandle(Owner.NumHandles);

            list.Add(view.Name, view);
            references.Add(view.Name, new List<DxfObject>());

            view.Owner = this;

            view.NameChanged += Item_NameChanged;

            Owner.AddedObjects.Add(view.Handle, view);

            return view;
        }

        /// <summary>
        /// Removes view.
        /// </summary>
        /// <param name="name"><see cref="View">View</see> name to remove from the document.</param>
        /// <returns>True if the view has been successfully removed, or false otherwise.</returns>
        /// <remarks>Reserved views or any other referenced by objects cannot be removed.</remarks>
        public override bool Remove(string name)
        {
            return Remove(this[name]);
        }

        /// <summary>
        /// Removes a view.
        /// </summary>
        /// <param name="item"><see cref="View">View</see> to remove from the document.</param>
        /// <returns>True if the view has been successfully removed, or false otherwise.</returns>
        /// <remarks>Reserved views or any other referenced by objects cannot be removed.</remarks>
        public override bool Remove(View item)
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

        #region UCS events

        private void Item_NameChanged(TableObject sender, TableObjectChangedEventArgs<string> e)
        {
            if (Contains(e.NewValue))
                throw new ArgumentException("There is already another View with the same name.");

            list.Remove(sender.Name);
            list.Add(e.NewValue, (View) sender);

            List<DxfObject> refs = references[sender.Name];
            references.Remove(sender.Name);
            references.Add(e.NewValue, refs);
        }

        #endregion
    }
}