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
using netDxf.Blocks;
using netDxf.Tables;

namespace netDxf.Collections
{
    /// <summary>
    /// Represents a collection of dimension styles.
    /// </summary>
    public sealed class DimensionStyles :
        TableObjects<DimensionStyle>
    {
        #region constructor

        internal DimensionStyles(DxfDocument document)
            : this(document, null)
        {
        }

        internal DimensionStyles(DxfDocument document, string handle)
            : base(document, DxfObjectCode.DimensionStyleTable, handle)
        {
            MaxCapacity = short.MaxValue;
        }

        #endregion

        #region override methods

        /// <summary>
        /// Adds a dimension style to the list.
        /// </summary>
        /// <param name="style"><see cref="DimensionStyle">DimensionStyle</see> to add to the list.</param>
        /// <param name="assignHandle">Specifies if a handle needs to be generated for the dimension style parameter.</param>
        /// <returns>
        /// If a dimension style already exists with the same name as the instance that is being added the method returns the existing dimension style,
        /// if not it will return the new dimension style.
        /// </returns>
        internal override DimensionStyle Add(DimensionStyle style, bool assignHandle)
        {
            if (list.Count >= MaxCapacity)
                throw new OverflowException(string.Format("Table overflow. The maximum number of elements the table {0} can have is {1}", CodeName, MaxCapacity));
            if (style == null)
                throw new ArgumentNullException(nameof(style));

            DimensionStyle add;
            if (list.TryGetValue(style.Name, out add))
                return add;

            if (assignHandle || string.IsNullOrEmpty(style.Handle))
                Owner.NumHandles = style.AsignHandle(Owner.NumHandles);

            list.Add(style.Name, style);
            references.Add(style.Name, new List<DxfObject>());

            // add referenced text style
            style.TextStyle = Owner.TextStyles.Add(style.TextStyle, assignHandle);
            Owner.TextStyles.References[style.TextStyle.Name].Add(style);

            // add referenced blocks
            if (style.LeaderArrow != null)
            {
                style.LeaderArrow = Owner.Blocks.Add(style.LeaderArrow, assignHandle);
                Owner.Blocks.References[style.LeaderArrow.Name].Add(style);
            }

            if (style.DimArrow1 != null)
            {
                style.DimArrow1 = Owner.Blocks.Add(style.DimArrow1, assignHandle);
                Owner.Blocks.References[style.DimArrow1.Name].Add(style);
            }
            if (style.DimArrow2 != null)
            {
                style.DimArrow2 = Owner.Blocks.Add(style.DimArrow2, assignHandle);
                Owner.Blocks.References[style.DimArrow2.Name].Add(style);
            }

            // add referenced line types
            style.DimLineLinetype = Owner.Linetypes.Add(style.DimLineLinetype, assignHandle);
            Owner.Linetypes.References[style.DimLineLinetype.Name].Add(style);

            style.ExtLine1Linetype = Owner.Linetypes.Add(style.ExtLine1Linetype, assignHandle);
            Owner.Linetypes.References[style.ExtLine1Linetype.Name].Add(style);

            style.ExtLine2Linetype = Owner.Linetypes.Add(style.ExtLine2Linetype, assignHandle);
            Owner.Linetypes.References[style.ExtLine2Linetype.Name].Add(style);

            style.Owner = this;

            style.NameChanged += Item_NameChanged;
            style.LinetypeChanged += DimensionStyleLinetypeChanged;
            style.TextStyleChanged += DimensionStyleTextStyleChanged;
            style.BlockChanged += DimensionStyleBlockChanged;

            Owner.AddedObjects.Add(style.Handle, style);

            return style;
        }

        /// <summary>
        /// Removes a dimension style.
        /// </summary>
        /// <param name="name"><see cref="DimensionStyle">DimensionStyle</see> name to remove from the document.</param>
        /// <returns>True if the dimension style has been successfully removed, or false otherwise.</returns>
        /// <remarks>Reserved dimension styles or any other referenced by objects cannot be removed.</remarks>
        public override bool Remove(string name)
        {
            return Remove(this[name]);
        }

        /// <summary>
        /// Removes a dimension style.
        /// </summary>
        /// <param name="item"><see cref="DimensionStyle">DimensionStyle</see> to remove from the document.</param>
        /// <returns>True if the dimension style has been successfully removed, or false otherwise.</returns>
        /// <remarks>Reserved dimension styles or any other referenced by objects cannot be removed.</remarks>
        public override bool Remove(DimensionStyle item)
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


            // remove referenced text style
            Owner.TextStyles.References[item.TextStyle.Name].Remove(item);

            // remove referenced blocks
            if (item.DimArrow1 != null)
                Owner.Blocks.References[item.DimArrow1.Name].Remove(item);
            if (item.DimArrow2 != null)
                Owner.Blocks.References[item.DimArrow2.Name].Remove(item);

            // remove referenced line types
            Owner.Linetypes.References[item.DimLineLinetype.Name].Remove(item);
            Owner.Linetypes.References[item.ExtLine1Linetype.Name].Remove(item);
            Owner.Linetypes.References[item.ExtLine2Linetype.Name].Remove(item);

            references.Remove(item.Name);
            list.Remove(item.Name);

            item.Handle = null;
            item.Owner = null;

            item.NameChanged -= Item_NameChanged;
            item.LinetypeChanged -= DimensionStyleLinetypeChanged;
            item.TextStyleChanged -= DimensionStyleTextStyleChanged;
            item.BlockChanged -= DimensionStyleBlockChanged;

            return true;
        }

        #endregion

        #region TableObject events

        private void Item_NameChanged(TableObject sender, TableObjectChangedEventArgs<string> e)
        {
            if (Contains(e.NewValue))
                throw new ArgumentException("There is already another dimension style with the same name.");

            list.Remove(sender.Name);
            list.Add(e.NewValue, (DimensionStyle) sender);

            List<DxfObject> refs = references[sender.Name];
            references.Remove(sender.Name);
            references.Add(e.NewValue, refs);
        }

        private void DimensionStyleLinetypeChanged(TableObject sender, TableObjectChangedEventArgs<Linetype> e)
        {
            Owner.Linetypes.References[e.OldValue.Name].Remove(sender);

            e.NewValue = Owner.Linetypes.Add(e.NewValue);
            Owner.Linetypes.References[e.NewValue.Name].Add(sender);
        }

        private void DimensionStyleTextStyleChanged(TableObject sender, TableObjectChangedEventArgs<TextStyle> e)
        {
            Owner.TextStyles.References[e.OldValue.Name].Remove(sender);

            e.NewValue = Owner.TextStyles.Add(e.NewValue);
            Owner.TextStyles.References[e.NewValue.Name].Add(sender);
        }

        private void DimensionStyleBlockChanged(TableObject sender, TableObjectChangedEventArgs<Block> e)
        {
            if (e.OldValue != null)
                Owner.Blocks.References[e.OldValue.Name].Remove(sender);

            e.NewValue = Owner.Blocks.Add(e.NewValue);
            if (e.NewValue != null)
                Owner.Blocks.References[e.NewValue.Name].Add(sender);
        }

        #endregion
    }
}