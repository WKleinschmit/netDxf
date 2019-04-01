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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using netDxf.Blocks;
using netDxf.Collections;
using netDxf.Entities;
using netDxf.Header;
using netDxf.Objects;
using netDxf.Tables;
using netDxf.Units;
using Attribute = netDxf.Entities.Attribute;
using Image = netDxf.Entities.Image;
using Point = netDxf.Entities.Point;
using TextAlignment = netDxf.Entities.TextAlignment;
using Trace = netDxf.Entities.Trace;

namespace netDxf.IO
{
    /// <summary>
    /// Low level dxf writer.
    /// </summary>
    internal sealed class DxfWriter
    {
        #region private fields

        private bool isBinary;
        private string activeSection = DxfObjectCode.Unknown;
        private string activeTable = DxfObjectCode.Unknown;
        private ICodeValueWriter chunk;
        private DxfDocument doc;
        // here we will store strings already encoded <string: original, string: encoded>
        private Dictionary<string, string> encodedStrings;

        #endregion

        #region constructors

        #endregion

        #region public methods

        public void Write(Stream stream, DxfDocument document, bool binary)
        {
            doc = document;
            isBinary = binary;
            DxfVersion version = doc.DrawingVariables.AcadVer;
            if (version < DxfVersion.AutoCad2000)
                throw new DxfVersionNotSupportedException(string.Format("DXF file version not supported : {0}.", version), version);

            if (!Vector3.ArePerpendicular(doc.DrawingVariables.UcsXDir, doc.DrawingVariables.UcsYDir))
                throw new ArithmeticException("The drawing variables vectors UcsXDir and UcsYDir must be perpendicular.");

            encodedStrings = new Dictionary<string, string>();

            // create the default PaperSpace layout in case it does not exist. The ModelSpace layout always exists
            if (doc.Layouts.Count == 1)
                doc.Layouts.Add(new Layout("Layout1"));

            // create the application registry AcCmTransparency in case it doesn't exists, it is required by the layer transparency
            doc.ApplicationRegistries.Add(new ApplicationRegistry("AcCmTransparency"));

            // create the application registry GradientColor1ACI and GradientColor2ACI in case they don't exists , they are required by the hatch gradient pattern
            doc.ApplicationRegistries.Add(new ApplicationRegistry("GradientColor1ACI"));
            doc.ApplicationRegistries.Add(new ApplicationRegistry("GradientColor2ACI"));

            // dictionaries
            List<DictionaryObject> dictionaries = new List<DictionaryObject>();

            // Named dictionary it is always the first to appear in the object section
            DictionaryObject namedObjectDictionary = new DictionaryObject(doc);
            doc.NumHandles = namedObjectDictionary.AsignHandle(doc.NumHandles);
            dictionaries.Add(namedObjectDictionary);

            // create the Group dictionary, this dictionary always appear even if there are no groups in the drawing
            DictionaryObject groupDictionary = new DictionaryObject(namedObjectDictionary);
            doc.NumHandles = groupDictionary.AsignHandle(doc.NumHandles);
            foreach (Group group in doc.Groups.Items)
            {
                groupDictionary.Entries.Add(group.Handle, group.Name);
            }
            dictionaries.Add(groupDictionary);
            namedObjectDictionary.Entries.Add(groupDictionary.Handle, DxfObjectCode.GroupDictionary);

            // Layout dictionary
            DictionaryObject layoutDictionary = new DictionaryObject(namedObjectDictionary);
            doc.NumHandles = layoutDictionary.AsignHandle(doc.NumHandles);
            if (doc.Layouts.Count > 0)
            {
                foreach (Layout layout in doc.Layouts.Items)
                {
                    layoutDictionary.Entries.Add(layout.Handle, layout.Name);
                }
                dictionaries.Add(layoutDictionary);
                namedObjectDictionary.Entries.Add(layoutDictionary.Handle, DxfObjectCode.LayoutDictionary);
            }

            // create the Underlay definitions dictionary
            DictionaryObject dgnDefinitionDictionary = new DictionaryObject(namedObjectDictionary);
            doc.NumHandles = dgnDefinitionDictionary.AsignHandle(doc.NumHandles);
            if (doc.UnderlayDgnDefinitions.Count > 0)
            {
                foreach (UnderlayDgnDefinition underlayDef in doc.UnderlayDgnDefinitions.Items)
                {
                    dgnDefinitionDictionary.Entries.Add(underlayDef.Handle, underlayDef.Name);
                    dictionaries.Add(dgnDefinitionDictionary);
                    namedObjectDictionary.Entries.Add(dgnDefinitionDictionary.Handle, DxfObjectCode.UnderlayDgnDefinitionDictionary);
                }
            }
            DictionaryObject dwfDefinitionDictionary = new DictionaryObject(namedObjectDictionary);
            doc.NumHandles = dwfDefinitionDictionary.AsignHandle(doc.NumHandles);
            if (doc.UnderlayDwfDefinitions.Count > 0)
            {
                foreach (UnderlayDwfDefinition underlayDef in doc.UnderlayDwfDefinitions.Items)
                {
                    dwfDefinitionDictionary.Entries.Add(underlayDef.Handle, underlayDef.Name);
                    dictionaries.Add(dwfDefinitionDictionary);
                    namedObjectDictionary.Entries.Add(dwfDefinitionDictionary.Handle, DxfObjectCode.UnderlayDwfDefinitionDictionary);
                }
            }
            DictionaryObject pdfDefinitionDictionary = new DictionaryObject(namedObjectDictionary);
            doc.NumHandles = pdfDefinitionDictionary.AsignHandle(doc.NumHandles);
            if (doc.UnderlayPdfDefinitions.Count > 0)
            {
                foreach (UnderlayPdfDefinition underlayDef in doc.UnderlayPdfDefinitions.Items)
                {
                    pdfDefinitionDictionary.Entries.Add(underlayDef.Handle, underlayDef.Name);
                    dictionaries.Add(pdfDefinitionDictionary);
                    namedObjectDictionary.Entries.Add(pdfDefinitionDictionary.Handle, DxfObjectCode.UnderlayPdfDefinitionDictionary);
                }
            }

            // create the MLine style dictionary
            DictionaryObject mLineStyleDictionary = new DictionaryObject(namedObjectDictionary);
            doc.NumHandles = mLineStyleDictionary.AsignHandle(doc.NumHandles);
            if (doc.MlineStyles.Count > 0)
            {
                foreach (MLineStyle mLineStyle in doc.MlineStyles.Items)
                {
                    mLineStyleDictionary.Entries.Add(mLineStyle.Handle, mLineStyle.Name);
                }
                dictionaries.Add(mLineStyleDictionary);
                namedObjectDictionary.Entries.Add(mLineStyleDictionary.Handle, DxfObjectCode.MLineStyleDictionary);
            }

            // create the image dictionary
            DictionaryObject imageDefDictionary = new DictionaryObject(namedObjectDictionary);
            doc.NumHandles = imageDefDictionary.AsignHandle(doc.NumHandles);
            if (doc.ImageDefinitions.Count > 0)
            {
                foreach (ImageDefinition imageDef in doc.ImageDefinitions.Items)
                {
                    imageDefDictionary.Entries.Add(imageDef.Handle, imageDef.Name);
                }

                dictionaries.Add(imageDefDictionary);

                namedObjectDictionary.Entries.Add(imageDefDictionary.Handle, DxfObjectCode.ImageDefDictionary);
                namedObjectDictionary.Entries.Add(doc.RasterVariables.Handle, DxfObjectCode.ImageVarsDictionary);
            }

            doc.DrawingVariables.HandleSeed = doc.NumHandles.ToString("X");

            Open(stream, doc.DrawingVariables.AcadVer < DxfVersion.AutoCad2007 ? Encoding.ASCII : null);

            //this.Open(stream, this.doc.DrawingVariables.AcadVer < DxfVersion.AutoCad2007 ? Encoding.Default : null);

            // The comment group, 999, is not used in binary DXF files.
            if (!isBinary)
            {
                foreach (string comment in doc.Comments)
                    WriteComment(comment);
            }

            //HEADER SECTION
            BeginSection(DxfObjectCode.HeaderSection);
            foreach (HeaderVariable variable in doc.DrawingVariables.Values)
            {
                WriteSystemVariable(variable);
            }
            // writing a copy of the active dimension style variables in the header section will avoid to be displayed as <style overrides> in AutoCad
            DimensionStyle activeDimStyle;
            if (doc.DimensionStyles.TryGetValue(doc.DrawingVariables.DimStyle, out activeDimStyle))
                WriteActiveDimensionStyleSystemVaribles(activeDimStyle);
            EndSection();

            //CLASSES SECTION
            BeginSection(DxfObjectCode.ClassesSection);
            WriteRasterVariablesClass(1);
            if (doc.ImageDefinitions.Items.Count > 0)
            {
                WriteImageDefClass(doc.ImageDefinitions.Count);
                WriteImageDefRectorClass(doc.Images.Count());
                WriteImageClass(doc.Images.Count());
            }
            EndSection();

            //TABLES SECTION
            BeginSection(DxfObjectCode.TablesSection);

            //registered application tables
            BeginTable(doc.ApplicationRegistries.CodeName, (short) doc.ApplicationRegistries.Count, doc.ApplicationRegistries.Handle);
            foreach (ApplicationRegistry id in doc.ApplicationRegistries.Items)
            {
                WriteApplicationRegistry(id);
            }
            EndTable();

            //viewport tables
            BeginTable(doc.VPorts.CodeName, (short) doc.VPorts.Count, doc.VPorts.Handle);
            foreach (VPort vport in doc.VPorts)
            {
                WriteVPort(vport);
            }
            EndTable();

            //line type tables
            //The LTYPE table always precedes the LAYER table. I guess because the layers reference the line types,
            //why this same rule is not applied to DIMSTYLE tables is a mystery, since they also reference text styles and block records
            BeginTable(doc.Linetypes.CodeName, (short) doc.Linetypes.Count, doc.Linetypes.Handle);
            foreach (Linetype linetype in doc.Linetypes.Items)
            {
                WriteLinetype(linetype);
            }
            EndTable();

            //layer tables
            BeginTable(doc.Layers.CodeName, (short) doc.Layers.Count, doc.Layers.Handle);
            foreach (Layer layer in doc.Layers.Items)
            {
                WriteLayer(layer);
            }
            EndTable();

            //style tables text and shapes
            BeginTable(doc.TextStyles.CodeName, (short) (doc.TextStyles.Count + doc.ShapeStyles.Count), doc.TextStyles.Handle);
            foreach (TextStyle style in doc.TextStyles.Items)
            {
                WriteTextStyle(style);
            }
            foreach (ShapeStyle style in doc.ShapeStyles)
            {
                WriteShapeStyle(style);
            }
            EndTable();

            //dimension style tables
            BeginTable(doc.DimensionStyles.CodeName, (short) doc.DimensionStyles.Count, doc.DimensionStyles.Handle);
            foreach (DimensionStyle style in doc.DimensionStyles.Items)
            {
                WriteDimensionStyle(style);
            }
            EndTable();

            //view
            BeginTable(doc.Views.CodeName, (short) doc.Views.Count, doc.Views.Handle);
            EndTable();

            //UCS
            BeginTable(doc.UCSs.CodeName, (short) doc.UCSs.Count, doc.UCSs.Handle);
            foreach (UCS ucs in doc.UCSs.Items)
            {
                WriteUCS(ucs);
            }
            EndTable();

            //block record table
            BeginTable(doc.Blocks.CodeName, (short) doc.Blocks.Count, doc.Blocks.Handle);
            foreach (Block block in doc.Blocks.Items)
            {
                WriteBlockRecord(block.Record);
            }
            EndTable();

            EndSection(); //End section tables

            //BLOCKS SECTION
            BeginSection(DxfObjectCode.BlocksSection);
            foreach (Block block in doc.Blocks.Items)
            {
                WriteBlock(block);
            }
            EndSection(); //End section blocks

            //ENTITIES SECTION
            BeginSection(DxfObjectCode.EntitiesSection);
            foreach (Layout layout in doc.Layouts)
            {
                if (layout.IsPaperSpace)
                {
                    // only the entities of the layout associated with the block "*Paper_Space" are included in the Entities Section
                    string index = layout.AssociatedBlock.Name.Remove(0, 12);
                    if (string.IsNullOrEmpty(index))
                    {
                        WriteEntity(layout.Viewport, layout);

                        foreach (AttributeDefinition attDef in layout.AssociatedBlock.AttributeDefinitions.Values)
                        {
                            WriteAttributeDefinition(attDef, layout);
                        }

                        foreach (EntityObject entity in layout.AssociatedBlock.Entities)
                        {
                            WriteEntity(entity, layout);
                        }
                    }                  
                }
                else 
                {
                    // ModelSpace
                    foreach (AttributeDefinition attDef in layout.AssociatedBlock.AttributeDefinitions.Values)
                    {
                        WriteAttributeDefinition(attDef, layout);
                    }

                    foreach (EntityObject entity in layout.AssociatedBlock.Entities)
                    {
                        WriteEntity(entity, layout);
                    }
                }
            }
            EndSection(); //End section entities

            //OBJECTS SECTION
            BeginSection(DxfObjectCode.ObjectsSection);

            foreach (DictionaryObject dictionary in dictionaries)
            {
                WriteDictionary(dictionary);
            }

            foreach (Group group in doc.Groups.Items)
            {
                WriteGroup(group, groupDictionary.Handle);
            }

            foreach (Layout layout in doc.Layouts)
            {
                WriteLayout(layout, layoutDictionary.Handle);
            }

            foreach (MLineStyle style in doc.MlineStyles.Items)
            {
                WriteMLineStyle(style, mLineStyleDictionary.Handle);
            }

            foreach (UnderlayDgnDefinition underlayDef in doc.UnderlayDgnDefinitions.Items)
            {
                WriteUnderlayDefinition(underlayDef, dgnDefinitionDictionary.Handle);
            }
            foreach (UnderlayDwfDefinition underlayDef in doc.UnderlayDwfDefinitions.Items)
            {
                WriteUnderlayDefinition(underlayDef, dwfDefinitionDictionary.Handle);
            }
            foreach (UnderlayPdfDefinition underlayDef in doc.UnderlayPdfDefinitions.Items)
            {
                WriteUnderlayDefinition(underlayDef, pdfDefinitionDictionary.Handle);
            }

            // the raster variables dictionary is only needed when the drawing has image entities
            if (doc.ImageDefinitions.Count > 0)
            {
                WriteRasterVariables(doc.RasterVariables, imageDefDictionary.Handle);
                foreach (ImageDefinition imageDef in doc.ImageDefinitions.Items)
                {
                    foreach (ImageDefinitionReactor reactor in imageDef.Reactors.Values)
                    {
                        WriteImageDefReactor(reactor);
                    }
                    WriteImageDef(imageDef, imageDefDictionary.Handle);
                }
            }

            EndSection(); //End section objects

            Close();

        }

        #endregion

        #region private methods

        /// <summary>
        /// Open the dxf writer.
        /// </summary>
        private void Open(Stream stream, Encoding encoding)
        {
            if (isBinary)
                chunk = new BinaryCodeValueWriter(encoding == null ? new BinaryWriter(stream, new UTF8Encoding(false)) : new BinaryWriter(stream, encoding));
            else
                chunk = new TextCodeValueWriter(encoding == null ? new StreamWriter(stream, new UTF8Encoding(false)) : new StreamWriter(stream, encoding));
        }

        /// <summary>
        /// Closes the dxf writer.
        /// </summary>
        private void Close()
        {
            chunk.Write(0, DxfObjectCode.EndOfFile);
            chunk.Flush();
        }

        /// <summary>
        /// Opens a new section.
        /// </summary>
        /// <param name="section">Section type to open.</param>
        /// <remarks>There can be only one type section.</remarks>
        private void BeginSection(string section)
        {
            Debug.Assert(activeSection == DxfObjectCode.Unknown);

            chunk.Write(0, DxfObjectCode.BeginSection);
            chunk.Write(2, section);
            activeSection = section;
        }

        /// <summary>
        /// Closes the active section.
        /// </summary>
        private void EndSection()
        {
            Debug.Assert(activeSection != DxfObjectCode.Unknown);

            chunk.Write(0, DxfObjectCode.EndSection);
            activeSection = DxfObjectCode.Unknown;
        }

        /// <summary>
        /// Opens a new table.
        /// </summary>
        /// <param name="table">Table type to open.</param>
        /// <param name="handle">Handle assigned to this table</param>
        private void BeginTable(string table, short numEntries, string handle)
        {
            Debug.Assert(activeSection == DxfObjectCode.TablesSection);

            chunk.Write(0, DxfObjectCode.Table);
            chunk.Write(2, table);
            chunk.Write(5, handle);
            chunk.Write(330, "0");

            chunk.Write(100, SubclassMarker.Table);
            chunk.Write(70, numEntries);

            if (table == DxfObjectCode.DimensionStyleTable)
                chunk.Write(100, SubclassMarker.DimensionStyleTable);

            activeTable = table;
        }

        /// <summary>
        /// Closes the active table.
        /// </summary>
        private void EndTable()
        {
            Debug.Assert(activeSection != DxfObjectCode.Unknown);

            chunk.Write(0, DxfObjectCode.EndTable);
            activeTable = DxfObjectCode.Unknown;
        }

        #endregion

        #region methods for Header section

        private void WriteComment(string comment)
        {
            if (!string.IsNullOrEmpty(comment))
                chunk.Write(999, comment);
        }

        private void WriteSystemVariable(HeaderVariable variable)
        {
            Debug.Assert(activeSection == DxfObjectCode.HeaderSection);

            string name = variable.Name;
            object value = variable.Value;

            switch (name)
            {
                case HeaderVariableCode.AcadVer:
                    chunk.Write(9, name);
                    chunk.Write(1, StringEnum.GetStringValue((DxfVersion) value));
                    break;
                case HeaderVariableCode.HandleSeed:
                    chunk.Write(9, name);
                    chunk.Write(5, value);
                    break;
                case HeaderVariableCode.Angbase:
                    chunk.Write(9, name);
                    chunk.Write(50, value);
                    break;
                case HeaderVariableCode.Angdir:
                    chunk.Write(9, name);
                    chunk.Write(70, (short) (AngleDirection) value);
                    break;
                case HeaderVariableCode.AttMode:
                    chunk.Write(9, name);
                    chunk.Write(70, (short) (AttMode) value);
                    break;
                case HeaderVariableCode.AUnits:
                    chunk.Write(9, name);
                    chunk.Write(70, (short) (AngleUnitType) value);
                    break;
                case HeaderVariableCode.AUprec:
                    chunk.Write(9, name);
                    chunk.Write(70, value);
                    break;
                case HeaderVariableCode.CeColor:
                    chunk.Write(9, name);
                    chunk.Write(62, ((AciColor) value).Index);
                    break;
                case HeaderVariableCode.CeLtScale:
                    chunk.Write(9, name);
                    chunk.Write(40, value);
                    break;
                case HeaderVariableCode.CeLtype:
                    chunk.Write(9, name);
                    chunk.Write(6, EncodeNonAsciiCharacters((string) value));
                    break;
                case HeaderVariableCode.CeLweight:
                    chunk.Write(9, name);
                    chunk.Write(370, (short) (Lineweight) value);
                    break;
                case HeaderVariableCode.CLayer:
                    chunk.Write(9, name);
                    chunk.Write(8, EncodeNonAsciiCharacters((string) value));
                    break;
                case HeaderVariableCode.CMLJust:
                    chunk.Write(9, name);
                    chunk.Write(70, (short) (MLineJustification) value);
                    break;
                case HeaderVariableCode.CMLScale:
                    chunk.Write(9, name);
                    chunk.Write(40, value);
                    break;
                case HeaderVariableCode.CMLStyle:
                    chunk.Write(9, name);
                    chunk.Write(2, EncodeNonAsciiCharacters((string) value));
                    break;
                case HeaderVariableCode.DimStyle:
                    chunk.Write(9, name);
                    chunk.Write(2, EncodeNonAsciiCharacters((string) value));
                    break;
                case HeaderVariableCode.TextSize:
                    chunk.Write(9, name);
                    chunk.Write(40, value);
                    break;
                case HeaderVariableCode.TextStyle:
                    chunk.Write(9, name);
                    chunk.Write(7, EncodeNonAsciiCharacters((string) value));
                    break;
                case HeaderVariableCode.LastSavedBy:
                    if (doc.DrawingVariables.AcadVer <= DxfVersion.AutoCad2000)
                        break;
                    chunk.Write(9, name);
                    chunk.Write(1, EncodeNonAsciiCharacters((string) value));
                    break;
                case HeaderVariableCode.LUnits:
                    chunk.Write(9, name);
                    chunk.Write(70, (short) (LinearUnitType) value);
                    break;
                case HeaderVariableCode.LUprec:
                    chunk.Write(9, name);
                    chunk.Write(70, value);
                    break;
                case HeaderVariableCode.DwgCodePage:
                    chunk.Write(9, name);
                    chunk.Write(3, value);
                    break;
                case HeaderVariableCode.Extnames:
                    chunk.Write(9, name);
                    chunk.Write(290, value);
                    break;
                case HeaderVariableCode.InsBase:
                    chunk.Write(9, name);
                    Vector3 pos = (Vector3) value;
                    chunk.Write(10, pos.X);
                    chunk.Write(20, pos.Y);
                    chunk.Write(30, pos.Z);
                    break;
                case HeaderVariableCode.InsUnits:
                    chunk.Write(9, name);
                    chunk.Write(70, (short) (DrawingUnits) value);
                    break;
                case HeaderVariableCode.LtScale:
                    chunk.Write(9, name);
                    chunk.Write(40, value);
                    break;
                case HeaderVariableCode.LwDisplay:
                    chunk.Write(9, name);
                    chunk.Write(290, value);
                    break;
                case HeaderVariableCode.PdMode:
                    chunk.Write(9, name);
                    chunk.Write(70, (short) (PointShape) value);
                    break;
                case HeaderVariableCode.PdSize:
                    chunk.Write(9, name);
                    chunk.Write(40, value);
                    break;
                case HeaderVariableCode.PLineGen:
                    chunk.Write(9, name);
                    chunk.Write(70, value);
                    break;
                case HeaderVariableCode.PsLtScale:
                    chunk.Write(9, name);
                    chunk.Write(70, value);
                    break;
                case HeaderVariableCode.TdCreate:
                    chunk.Write(9, name);
                    chunk.Write(40, DrawingTime.ToJulianCalendar((DateTime) value));
                    break;
                case HeaderVariableCode.TduCreate:
                    chunk.Write(9, name);
                    chunk.Write(40, DrawingTime.ToJulianCalendar((DateTime) value));
                    break;
                case HeaderVariableCode.TdUpdate:
                    chunk.Write(9, name);
                    chunk.Write(40, DrawingTime.ToJulianCalendar((DateTime) value));
                    break;
                case HeaderVariableCode.TduUpdate:
                    chunk.Write(9, name);
                    chunk.Write(40, DrawingTime.ToJulianCalendar((DateTime) value));
                    break;
                case HeaderVariableCode.TdinDwg:
                    chunk.Write(9, name);
                    chunk.Write(40, ((TimeSpan) value).TotalDays);
                    break;
                case HeaderVariableCode.UcsOrg:
                    chunk.Write(9, name);
                    Vector3 org = (Vector3)value;
                    chunk.Write(10, org.X);
                    chunk.Write(20, org.Y);
                    chunk.Write(30, org.Z);
                    break;
                case HeaderVariableCode.UcsXDir:
                    chunk.Write(9, name);
                    Vector3 xdir = (Vector3)value;
                    chunk.Write(10, xdir.X);
                    chunk.Write(20, xdir.Y);
                    chunk.Write(30, xdir.Z);
                    break;
                case HeaderVariableCode.UcsYDir:
                    chunk.Write(9, name);
                    Vector3 ydir = (Vector3)value;
                    chunk.Write(10, ydir.X);
                    chunk.Write(20, ydir.Y);
                    chunk.Write(30, ydir.Z);
                    break;
            }
        }

        private void WriteActiveDimensionStyleSystemVaribles(DimensionStyle style)
        {
            chunk.Write(9, "$DIMADEC");
            chunk.Write(70, style.AngularPrecision);

            chunk.Write(9, "$DIMALT");
            chunk.Write(70, style.AlternateUnits.Enabled ? (short) 1 : (short) 0);

            chunk.Write(9, "$DIMALTD");
            chunk.Write(70, style.AlternateUnits.LengthPrecision);

            chunk.Write(9, "$DIMALTF");
            chunk.Write(40, style.AlternateUnits.Multiplier);

            chunk.Write(9, "$DIMALTRND");
            chunk.Write(40, style.AlternateUnits.Roundoff);

            chunk.Write(9, "$DIMALTTD");
            chunk.Write(70, style.Tolerances.AlternatePrecision);

            chunk.Write(9, "$DIMALTTZ");
            chunk.Write(70, GetSupressZeroesValue(
                    style.Tolerances.AlternateSuppressLinearLeadingZeros,
                    style.Tolerances.AlternateSuppressLinearTrailingZeros,
                    style.Tolerances.AlternateSuppressZeroFeet,
                    style.Tolerances.AlternateSuppressZeroInches));

            chunk.Write(9, "$DIMALTU");
            switch (style.AlternateUnits.LengthUnits)
            {
                case LinearUnitType.Scientific:
                    chunk.Write(70, (short) 1);
                    break;
                case LinearUnitType.Decimal:
                    chunk.Write(70, (short) 2);
                    break;
                case LinearUnitType.Engineering:
                    chunk.Write(70, (short) 3);
                    break;
                case LinearUnitType.Architectural:
                    chunk.Write(70, style.AlternateUnits.StackUnits ? (short) 4 : (short) 6);
                    break;
                case LinearUnitType.Fractional:
                    chunk.Write(70, style.AlternateUnits.StackUnits ? (short) 5 : (short) 7);
                    break;
            }

            chunk.Write(9, "$DIMALTZ");
            chunk.Write(70, GetSupressZeroesValue(
                    style.AlternateUnits.SuppressLinearLeadingZeros,
                    style.AlternateUnits.SuppressLinearTrailingZeros,
                    style.AlternateUnits.SuppressZeroFeet,
                    style.AlternateUnits.SuppressZeroInches));

            chunk.Write(9, "$DIMAPOST");
            chunk.Write(1, EncodeNonAsciiCharacters(string.Format("{0}[]{1}", style.AlternateUnits.Prefix, style.AlternateUnits.Suffix)));

            chunk.Write(9, "$DIMATFIT");
            chunk.Write(70, (short) style.FitOptions);

            chunk.Write(9, "$DIMAUNIT");
            chunk.Write(70, (short) style.DimAngularUnits);

            chunk.Write(9, "$DIMASZ");
            chunk.Write(40, style.ArrowSize);

            short angSupress;
            if (style.SuppressAngularLeadingZeros && style.SuppressAngularTrailingZeros)
                angSupress = 3;
            else if (!style.SuppressAngularLeadingZeros && !style.SuppressAngularTrailingZeros)
                angSupress = 0;
            else if (!style.SuppressAngularLeadingZeros && style.SuppressAngularTrailingZeros)
                angSupress = 2;
            else if (style.SuppressAngularLeadingZeros && !style.SuppressAngularTrailingZeros)
                angSupress = 1;
            else
                angSupress = 3;

            chunk.Write(9, "$DIMAZIN");
            chunk.Write(70, angSupress);

            if (style.DimArrow1 == null && style.DimArrow2 == null)
            {
                chunk.Write(9, "$DIMSAH");
                chunk.Write(70, (short) 0);

                chunk.Write(9, "$DIMBLK");
                chunk.Write(1, "");
            }
            else if (style.DimArrow1 == null)
            {
                chunk.Write(9, "$DIMSAH");
                chunk.Write(70, (short) 1);

                chunk.Write(9, "$DIMBLK1");
                chunk.Write(1, "");

                chunk.Write(9, "$DIMBLK2");
                chunk.Write(1, EncodeNonAsciiCharacters(style.DimArrow2.Name));
            }
            else if (style.DimArrow2 == null)
            {
                chunk.Write(9, "$DIMSAH");
                chunk.Write(70, (short) 1);

                chunk.Write(9, "$DIMBLK1");
                chunk.Write(1, EncodeNonAsciiCharacters(style.DimArrow1.Name));

                chunk.Write(9, "$DIMBLK2");
                chunk.Write(1, "");
            }
            else if (string.Equals(style.DimArrow1.Name, style.DimArrow2.Name, StringComparison.OrdinalIgnoreCase))
            {
                chunk.Write(9, "$DIMSAH");
                chunk.Write(70, (short) 0);

                chunk.Write(9, "$DIMBLK");
                chunk.Write(1, EncodeNonAsciiCharacters(style.DimArrow1.Name));
            }
            else
            {
                chunk.Write(9, "$DIMSAH");
                chunk.Write(70, (short) 1);

                chunk.Write(9, "$DIMBLK1");
                chunk.Write(1, EncodeNonAsciiCharacters(style.DimArrow1.Name));

                chunk.Write(9, "$DIMBLK2");
                chunk.Write(1, EncodeNonAsciiCharacters(style.DimArrow2.Name));
            }

            chunk.Write(9, "$DIMLDRBLK");
            chunk.Write(1, style.LeaderArrow == null ? "" : EncodeNonAsciiCharacters(style.LeaderArrow.Name));

            chunk.Write(9, "$DIMCEN");
            chunk.Write(40, style.CenterMarkSize);

            chunk.Write(9, "$DIMCLRD");
            chunk.Write(70, style.DimLineColor.Index);

            chunk.Write(9, "$DIMCLRE");
            chunk.Write(70, style.ExtLineColor.Index);

            chunk.Write(9, "$DIMCLRT");
            chunk.Write(70, style.TextColor.Index);

            chunk.Write(9, "$DIMDEC");
            chunk.Write(70, style.LengthPrecision);

            chunk.Write(9, "$DIMDLE");
            chunk.Write(40, style.DimLineExtend);

            chunk.Write(9, "$DIMDLI");
            chunk.Write(40, style.DimBaselineSpacing);

            chunk.Write(9, "$DIMDSEP");
            chunk.Write(70, (short) style.DecimalSeparator);

            chunk.Write(9, "$DIMEXE");
            chunk.Write(40, style.ExtLineExtend);

            chunk.Write(9, "$DIMEXO");
            chunk.Write(40, style.ExtLineOffset);

            chunk.Write(9, "$DIMFXLON");
            chunk.Write(70, style.ExtLineFixed ? (short) 1 : (short) 0);

            chunk.Write(9, "$DIMFXL");
            chunk.Write(40, style.ExtLineFixedLength);

            chunk.Write(9, "$DIMGAP");
            chunk.Write(40, style.TextOffset);

            chunk.Write(9, "$DIMJUST");
            chunk.Write(70, (short) style.TextHorizontalPlacement);

            chunk.Write(9, "$DIMLFAC");
            chunk.Write(40, style.DimScaleLinear);

            chunk.Write(9, "$DIMLUNIT");
            chunk.Write(70, (short) style.DimLengthUnits);

            chunk.Write(9, "$DIMLWD");
            chunk.Write(70, (short) style.DimLineLineweight);

            chunk.Write(9, "$DIMLWE");
            chunk.Write(70, (short) style.ExtLineLineweight);

            chunk.Write(9, "$DIMPOST");
            chunk.Write(1, EncodeNonAsciiCharacters(string.Format("{0}<>{1}", style.DimPrefix, style.DimSuffix)));

            chunk.Write(9, "$DIMRND");
            chunk.Write(40, style.DimRoundoff);

            chunk.Write(9, "$DIMSCALE");
            chunk.Write(40, style.DimScaleOverall);

            chunk.Write(9, "$DIMSD1");
            chunk.Write(70, style.DimLine1Off ? (short) 1 : (short) 0);

            chunk.Write(9, "$DIMSD2");
            chunk.Write(70, style.DimLine2Off ? (short) 1 : (short) 0);

            chunk.Write(9, "$DIMSE1");
            chunk.Write(70, style.ExtLine1Off ? (short) 1 : (short) 0);

            chunk.Write(9, "$DIMSE2");
            chunk.Write(70, style.ExtLine2Off ? (short) 1 : (short) 0);

            chunk.Write(9, "$DIMSOXD");
            chunk.Write(70, style.FitDimLineInside ? (short) 1 : (short) 0);

            chunk.Write(9, "$DIMTAD");
            chunk.Write(70, (short) style.TextVerticalPlacement);

            chunk.Write(9, "$DIMTDEC");
            chunk.Write(70, style.Tolerances.Precision);

            chunk.Write(9, "$DIMTFAC");
            chunk.Write(40, style.TextFractionHeightScale);

            if (style.TextFillColor != null)
            {
                chunk.Write(9, "$DIMTFILL");
                chunk.Write(70, (short) 2);

                chunk.Write(9, "$DIMTFILLCLR");
                chunk.Write(70, style.TextFillColor.Index);
            }

            chunk.Write(9, "$DIMTIH");
            chunk.Write(70, style.TextInsideAlign ? (short) 1 : (short) 0);

            chunk.Write(9, "$DIMTIX");
            chunk.Write(70, style.FitTextInside ? (short)1 : (short)0);

            if (style.Tolerances.DisplayMethod == DimensionStyleTolerancesDisplayMethod.Deviation)
            {
                chunk.Write(9, "$DIMTM");
                chunk.Write(40, MathHelper.IsZero(style.Tolerances.LowerLimit) ? MathHelper.Epsilon : style.Tolerances.LowerLimit);
            }
            else
            {
                chunk.Write(9, "$DIMTM");
                chunk.Write(40, style.Tolerances.LowerLimit);
            }

            chunk.Write(9, "$DIMTMOVE");
            chunk.Write(70, (short) style.FitTextMove);

            chunk.Write(9, "$DIMTOFL");
            chunk.Write(70, style.FitDimLineForce ? (short)1 : (short)0);

            chunk.Write(9, "$DIMTOH");
            chunk.Write(70, style.TextOutsideAlign ? (short) 1 : (short) 0);

            switch (style.Tolerances.DisplayMethod)
            {
                case DimensionStyleTolerancesDisplayMethod.None:
                    chunk.Write(9, "$DIMTOL");
                    chunk.Write(70, (short) 0);
                    chunk.Write(9, "$DIMLIM");
                    chunk.Write(70, (short) 0);
                    break;
                case DimensionStyleTolerancesDisplayMethod.Symmetrical:
                    chunk.Write(9, "$DIMTOL");
                    chunk.Write(70, (short) 1);
                    chunk.Write(9, "$DIMLIM");
                    chunk.Write(70, (short) 0);
                    break;
                case DimensionStyleTolerancesDisplayMethod.Deviation:
                    chunk.Write(9, "$DIMTOL");
                    chunk.Write(70, (short) 1);
                    chunk.Write(9, "$DIMLIM");
                    chunk.Write(70, (short) 0);
                    break;
                case DimensionStyleTolerancesDisplayMethod.Limits:
                    chunk.Write(9, "$DIMTOL");
                    chunk.Write(70, (short) 0);
                    chunk.Write(9, "$DIMLIM");
                    chunk.Write(70, (short) 1);
                    break;
            }

            chunk.Write(9, "$DIMTOLJ");
            chunk.Write(70, (short) style.Tolerances.VerticalPlacement);

            chunk.Write(9, "$DIMTP");
            chunk.Write(40, style.Tolerances.UpperLimit);

            chunk.Write(9, "$DIMTXT");
            chunk.Write(40, style.TextHeight);

            chunk.Write(9, "$DIMTXTDIRECTION");
            chunk.Write(70, (short) style.TextDirection);

            chunk.Write(9, "$DIMTZIN");
            chunk.Write(70, GetSupressZeroesValue(
                    style.Tolerances.SuppressLinearLeadingZeros,
                    style.Tolerances.SuppressLinearTrailingZeros,
                    style.Tolerances.SuppressZeroFeet,
                    style.Tolerances.SuppressZeroInches));

            chunk.Write(9, "$DIMZIN");
            chunk.Write(70, GetSupressZeroesValue(
                    style.SuppressLinearLeadingZeros,
                    style.SuppressLinearTrailingZeros,
                    style.SuppressZeroFeet,
                    style.SuppressZeroInches));

            // CAUTION: The next four codes are not documented in the official dxf docs
            chunk.Write(9, "$DIMFRAC");
            chunk.Write(70, (short) style.FractionType);

            chunk.Write(9, "$DIMLTYPE");
            chunk.Write(6, EncodeNonAsciiCharacters(style.DimLineLinetype.Name));

            chunk.Write(9, "$DIMLTEX1");
            chunk.Write(6, EncodeNonAsciiCharacters(style.ExtLine1Linetype.Name));

            chunk.Write(9, "$DIMLTEX2");
            chunk.Write(6, EncodeNonAsciiCharacters(style.ExtLine2Linetype.Name));
    }

        #endregion

        #region methods for Classes section

        private void WriteImageClass(int count)
        {
            chunk.Write(0, DxfObjectCode.Class);
            chunk.Write(1, DxfObjectCode.Image);
            chunk.Write(2, SubclassMarker.RasterImage);
            chunk.Write(3, "ISM");

            // default codes as shown in the dxf documentation
            chunk.Write(90, 127);
            if (doc.DrawingVariables.AcadVer > DxfVersion.AutoCad2000)
                chunk.Write(91, count);
            chunk.Write(280, (short) 0);
            chunk.Write(281, (short) 1);
        }

        private void WriteImageDefClass(int count)
        {
            chunk.Write(0, DxfObjectCode.Class);
            chunk.Write(1, DxfObjectCode.ImageDef);
            chunk.Write(2, SubclassMarker.RasterImageDef);
            chunk.Write(3, "ISM");

            // default codes as shown in the dxf documentation
            chunk.Write(90, 0);
            if (doc.DrawingVariables.AcadVer > DxfVersion.AutoCad2000)
                chunk.Write(91, count);
            chunk.Write(280, (short) 0);
            chunk.Write(281, (short) 0);
        }

        private void WriteImageDefRectorClass(int count)
        {
            chunk.Write(0, DxfObjectCode.Class);
            chunk.Write(1, DxfObjectCode.ImageDefReactor);
            chunk.Write(2, SubclassMarker.RasterImageDefReactor);
            chunk.Write(3, "ISM");

            // default codes as shown in the dxf documentation
            chunk.Write(90, 1);
            if (doc.DrawingVariables.AcadVer > DxfVersion.AutoCad2000)
                chunk.Write(91, count);
            chunk.Write(280, (short) 0);
            chunk.Write(281, (short) 0);
        }

        private void WriteRasterVariablesClass(int count)
        {
            chunk.Write(0, DxfObjectCode.Class);
            chunk.Write(1, DxfObjectCode.RasterVariables);
            chunk.Write(2, SubclassMarker.RasterVariables);
            chunk.Write(3, "ISM");

            // default codes as shown in the dxf documentation
            chunk.Write(90, 0);
            if (doc.DrawingVariables.AcadVer > DxfVersion.AutoCad2000)
                chunk.Write(91, count);
            chunk.Write(280, (short) 0);
            chunk.Write(281, (short) 0);
        }

        #endregion

        #region methods for Table section

        /// <summary>
        /// Writes a new extended data application registry to the table section.
        /// </summary>
        /// <param name="appReg">Name of the application registry.</param>
        private void WriteApplicationRegistry(ApplicationRegistry appReg)
        {
            Debug.Assert(activeTable == DxfObjectCode.ApplicationIdTable);

            chunk.Write(0, DxfObjectCode.ApplicationIdTable);
            chunk.Write(5, appReg.Handle);
            chunk.Write(330, appReg.Owner.Handle);

            chunk.Write(100, SubclassMarker.TableRecord);
            chunk.Write(100, SubclassMarker.ApplicationId);

            chunk.Write(2, EncodeNonAsciiCharacters(appReg.Name));

            chunk.Write(70, (short) 0);

            WriteXData(appReg.XData);
        }

        /// <summary>
        /// Writes a new viewport to the table section.
        /// </summary>
        /// <param name="vp">viewport.</param>
        private void WriteVPort(VPort vp)
        {
            Debug.Assert(activeTable == DxfObjectCode.VportTable);

            chunk.Write(0, vp.CodeName);
            chunk.Write(5, vp.Handle);
            chunk.Write(330, vp.Owner.Handle);

            chunk.Write(100, SubclassMarker.TableRecord);

            chunk.Write(100, SubclassMarker.VPort);

            chunk.Write(2, EncodeNonAsciiCharacters(vp.Name));

            chunk.Write(70, (short) 0);

            chunk.Write(10, 0.0);
            chunk.Write(20, 0.0);

            chunk.Write(11, 1.0);
            chunk.Write(21, 1.0);

            chunk.Write(12, vp.ViewCenter.X);
            chunk.Write(22, vp.ViewCenter.Y);

            chunk.Write(13, vp.SnapBasePoint.X);
            chunk.Write(23, vp.SnapBasePoint.Y);

            chunk.Write(14, vp.SnapSpacing.X);
            chunk.Write(24, vp.SnapSpacing.Y);

            chunk.Write(15, vp.GridSpacing.X);
            chunk.Write(25, vp.GridSpacing.Y);

            chunk.Write(16, vp.ViewDirection.X);
            chunk.Write(26, vp.ViewDirection.Y);
            chunk.Write(36, vp.ViewDirection.Z);

            chunk.Write(17, vp.ViewTarget.X);
            chunk.Write(27, vp.ViewTarget.Y);
            chunk.Write(37, vp.ViewTarget.Z);

            chunk.Write(40, vp.ViewHeight);
            chunk.Write(41, vp.ViewAspectRatio);

            chunk.Write(75, vp.SnapMode ? (short) 1 : (short) 0);
            chunk.Write(76, vp.ShowGrid ? (short) 1 : (short) 0);

            WriteXData(vp.XData);
        }

        /// <summary>
        /// Writes a new dimension style to the table section.
        /// </summary>
        /// <param name="style">DimensionStyle.</param>
        private void WriteDimensionStyle(DimensionStyle style)
        {
            Debug.Assert(activeTable == DxfObjectCode.DimensionStyleTable);

            chunk.Write(0, style.CodeName);
            chunk.Write(105, style.Handle);
            chunk.Write(330, style.Owner.Handle);

            chunk.Write(100, SubclassMarker.TableRecord);

            chunk.Write(100, SubclassMarker.DimensionStyle);

            chunk.Write(2, EncodeNonAsciiCharacters(style.Name));
           
            chunk.Write(3, EncodeNonAsciiCharacters(string.Format("{0}<>{1}", style.DimPrefix, style.DimSuffix)));
            chunk.Write(4, EncodeNonAsciiCharacters(string.Format("{0}[]{1}", style.AlternateUnits.Prefix, style.AlternateUnits.Suffix)));
            chunk.Write(40, style.DimScaleOverall);
            chunk.Write(41, style.ArrowSize);
            chunk.Write(42, style.ExtLineOffset);
            chunk.Write(43, style.DimBaselineSpacing);
            chunk.Write(44, style.ExtLineExtend);
            chunk.Write(45, style.DimRoundoff);
            chunk.Write(46, style.DimLineExtend);
            chunk.Write(47, style.Tolerances.UpperLimit);
            // code 48 is written later
            chunk.Write(49, style.ExtLineFixedLength);

            if (style.TextFillColor != null)
            {
                chunk.Write(69, (short) 2);
                chunk.Write(70, style.TextFillColor.Index);
            }
            else
            {
                chunk.Write(70, (short) 0);
            }

            switch (style.Tolerances.DisplayMethod)
            {
                case DimensionStyleTolerancesDisplayMethod.None:
                    chunk.Write(71, (short) 0);
                    chunk.Write(72, (short) 0);
                    break;
                case DimensionStyleTolerancesDisplayMethod.Symmetrical:
                    chunk.Write(71, (short) 1);
                    chunk.Write(72, (short) 0);
                    break;
                case DimensionStyleTolerancesDisplayMethod.Deviation:
                    chunk.Write(48, MathHelper.IsZero(style.Tolerances.LowerLimit) ? MathHelper.Epsilon : style.Tolerances.LowerLimit);
                    chunk.Write(71, (short) 1);
                    chunk.Write(72, (short) 0);
                    break;
                case DimensionStyleTolerancesDisplayMethod.Limits:
                    chunk.Write(48, style.Tolerances.LowerLimit);
                    chunk.Write(71, (short) 0);
                    chunk.Write(72, (short) 1);
                    break;
            }

            chunk.Write(73, style.TextInsideAlign ? (short) 1 : (short) 0);
            chunk.Write(74, style.TextOutsideAlign ? (short) 1 : (short) 0);
            chunk.Write(75, style.ExtLine1Off ? (short) 1 : (short) 0);
            chunk.Write(76, style.ExtLine2Off ? (short) 1 : (short) 0);

            chunk.Write(77, (short) style.TextVerticalPlacement);
            chunk.Write(78, GetSupressZeroesValue(
                    style.SuppressLinearLeadingZeros,
                    style.SuppressLinearTrailingZeros,
                    style.SuppressZeroFeet,
                    style.SuppressZeroInches));

            short angSupress = 3;
            if (style.SuppressAngularLeadingZeros && style.SuppressAngularTrailingZeros)
                angSupress = 3;
            else if (!style.SuppressAngularLeadingZeros && !style.SuppressAngularTrailingZeros)
                angSupress = 0;
            else if (!style.SuppressAngularLeadingZeros && style.SuppressAngularTrailingZeros)
                angSupress = 2;
            else if (style.SuppressAngularLeadingZeros && !style.SuppressAngularTrailingZeros)
                angSupress = 1;

            chunk.Write(79, angSupress);

            chunk.Write(140, style.TextHeight);
            chunk.Write(141, style.CenterMarkSize);
            chunk.Write(143, style.AlternateUnits.Multiplier);
            chunk.Write(144, style.DimScaleLinear);
            chunk.Write(146, style.TextFractionHeightScale);
            chunk.Write(147, style.TextOffset);
            chunk.Write(148, style.AlternateUnits.Roundoff);
            chunk.Write(170, style.AlternateUnits.Enabled ? (short) 1 : (short) 0);
            chunk.Write(171, style.AlternateUnits.LengthPrecision);
            chunk.Write(172, style.FitDimLineForce ? (short) 1 : (short) 0);
            // code 173 is written later
            chunk.Write(174, style.FitTextInside ? (short) 1 : (short) 0);
            chunk.Write(175, style.FitDimLineInside ? (short) 1 : (short) 0);
            chunk.Write(176, style.DimLineColor.Index);
            chunk.Write(177, style.ExtLineColor.Index);
            chunk.Write(178, style.TextColor.Index);
            chunk.Write(179, style.AngularPrecision);
            chunk.Write(271, style.LengthPrecision);
            chunk.Write(272, style.Tolerances.Precision);
            switch (style.AlternateUnits.LengthUnits)
            {
                case LinearUnitType.Scientific:
                    chunk.Write(273, (short) 1);
                    break;
                case LinearUnitType.Decimal:
                    chunk.Write(273, (short) 2);
                    break;
                case LinearUnitType.Engineering:
                    chunk.Write(273, (short) 3);
                    break;
                case LinearUnitType.Architectural:
                    chunk.Write(273, style.AlternateUnits.StackUnits ? (short) 4 : (short) 6);
                    break;
                case LinearUnitType.Fractional:
                    chunk.Write(273, style.AlternateUnits.StackUnits ? (short) 5 : (short) 7);
                    break;
            }       
            chunk.Write(274, style.Tolerances.AlternatePrecision);              
            chunk.Write(275, (short) style.DimAngularUnits);
            chunk.Write(276, (short) style.FractionType);
            chunk.Write(277, (short) style.DimLengthUnits);
            chunk.Write(278, (short) style.DecimalSeparator);
            chunk.Write(279, (short) style.FitTextMove);
            chunk.Write(280, (short) style.TextHorizontalPlacement);
            chunk.Write(281, style.DimLine1Off ? (short) 1 : (short) 0);
            chunk.Write(282, style.DimLine2Off ? (short) 1 : (short) 0);
            chunk.Write(283, (short) style.Tolerances.VerticalPlacement);
            chunk.Write(284, GetSupressZeroesValue(
                    style.Tolerances.SuppressLinearLeadingZeros,
                    style.Tolerances.SuppressLinearTrailingZeros,
                    style.Tolerances.SuppressZeroFeet,
                    style.Tolerances.SuppressZeroInches));
            chunk.Write(285, GetSupressZeroesValue(
                    style.AlternateUnits.SuppressLinearLeadingZeros,
                    style.AlternateUnits.SuppressLinearTrailingZeros,
                    style.AlternateUnits.SuppressZeroFeet,
                    style.AlternateUnits.SuppressZeroInches));
            chunk.Write(286, GetSupressZeroesValue(
                    style.Tolerances.AlternateSuppressLinearLeadingZeros,
                    style.Tolerances.AlternateSuppressLinearTrailingZeros,
                    style.Tolerances.AlternateSuppressZeroFeet,
                    style.Tolerances.AlternateSuppressZeroInches));
            chunk.Write(289, (short) style.FitOptions);
            chunk.Write(290, style.ExtLineFixed);
            chunk.Write(294, style.TextDirection == DimensionStyleTextDirection.RightToLeft);
            chunk.Write(340, style.TextStyle.Handle);

            // CAUTION: The documentation says that the next values are the handles of referenced BLOCK,
            // but they are the handles of referenced BLOCK_RECORD
            if (style.LeaderArrow != null)
                chunk.Write(341, style.LeaderArrow.Record.Handle);

            if (style.DimArrow1 == null && style.DimArrow2 == null)
            {
                chunk.Write(173, (short) 0);
            }
            else if (style.DimArrow1 == null)
            {
                chunk.Write(173, (short) 1);
                if (style.DimArrow2 != null)
                    chunk.Write(344, style.DimArrow2.Record.Handle);
            }
            else if (style.DimArrow2 == null)
            {
                chunk.Write(173, (short) 1);
                if (style.DimArrow1 != null)
                    chunk.Write(344, style.DimArrow1.Record.Handle);
            }
            else if (string.Equals(style.DimArrow1.Name, style.DimArrow2.Name, StringComparison.OrdinalIgnoreCase))
            {
                chunk.Write(173, (short) 0);
                chunk.Write(342, style.DimArrow1.Record.Handle);
            }
            else
            {
                chunk.Write(173, (short) 1);
                chunk.Write(343, style.DimArrow1.Record.Handle);
                chunk.Write(344, style.DimArrow2.Record.Handle);
            }

            // CAUTION: The next three codes are undocumented in the official dxf docs
            chunk.Write(345, style.DimLineLinetype.Handle);
            chunk.Write(346, style.ExtLine1Linetype.Handle);
            chunk.Write(347, style.ExtLine2Linetype.Handle);

            chunk.Write(371, (short) style.DimLineLineweight);
            chunk.Write(372, (short) style.ExtLineLineweight);

            WriteXData(style.XData);
        }      

        /// <summary>
        /// Writes a new block record to the table section.
        /// </summary>
        /// <param name="blockRecord">Block.</param>
        private void WriteBlockRecord(BlockRecord blockRecord)
        {
            Debug.Assert(activeTable == DxfObjectCode.BlockRecordTable);

            chunk.Write(0, blockRecord.CodeName);
            chunk.Write(5, blockRecord.Handle);
            chunk.Write(330, blockRecord.Owner.Handle);

            chunk.Write(100, SubclassMarker.TableRecord);

            chunk.Write(100, SubclassMarker.BlockRecord);

            chunk.Write(2, EncodeNonAsciiCharacters(blockRecord.Name));

            // Hard-pointer ID/handle to associated LAYOUT object
            chunk.Write(340, blockRecord.Layout == null ? "0" : blockRecord.Layout.Handle);

            // internal blocks do not need more information
            if (blockRecord.IsForInternalUseOnly)
                return;

            // The next three values will only work for dxf version AutoCad2007 and upwards
            chunk.Write(70, (short) blockRecord.Units);
            chunk.Write(280, blockRecord.AllowExploding ? (short) 1 : (short) 0);
            chunk.Write(281, blockRecord.ScaleUniformly ? (short) 1 : (short) 0);

            AddBlockRecordUnitsXData(blockRecord);

            WriteXData(blockRecord.XData);
        }

        private static void AddBlockRecordUnitsXData(BlockRecord record)
        {
            // for dxf versions prior to AutoCad2007 the block record units is stored in an extended data block
            XData xdataEntry;
            if (record.XData.ContainsAppId(ApplicationRegistry.DefaultName))
            {
                xdataEntry = record.XData[ApplicationRegistry.DefaultName];
                xdataEntry.XDataRecord.Clear();
            }
            else
            {
                xdataEntry = new XData(new ApplicationRegistry(ApplicationRegistry.DefaultName));
                record.XData.Add(xdataEntry);
            }

            xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.String, "DesignCenter Data"));
            xdataEntry.XDataRecord.Add(XDataRecord.OpenControlString);
            xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 1));
            xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) record.Units));
            xdataEntry.XDataRecord.Add(XDataRecord.CloseControlString);
        }

        /// <summary>
        /// Writes a new line type to the table section.
        /// </summary>
        /// <param name="linetype">Line type.</param>
        private void WriteLinetype(Linetype linetype)
        {
            Debug.Assert(activeTable == DxfObjectCode.LinetypeTable);

            chunk.Write(0, linetype.CodeName);
            chunk.Write(5, linetype.Handle);
            chunk.Write(330, linetype.Owner.Handle);

            chunk.Write(100, SubclassMarker.TableRecord);

            chunk.Write(100, SubclassMarker.Linetype);

            chunk.Write(2, EncodeNonAsciiCharacters(linetype.Name));

            chunk.Write(70, (short) 0);

            chunk.Write(3, EncodeNonAsciiCharacters(linetype.Description));

            chunk.Write(72, (short) 65);
            chunk.Write(73, (short) linetype.Segments.Count);
            chunk.Write(40, linetype.Length());

            foreach (LinetypeSegment s in linetype.Segments)
            {
                chunk.Write(49, s.Length);
                switch (s.Type)
                {
                    case LinetypeSegmentType.Simple:
                        chunk.Write(74, (short)0);
                        break;

                    case LinetypeSegmentType.Text:
                        LinetypeTextSegment textSegment = (LinetypeTextSegment)s;
                        if (textSegment.RotationType == LinetypeSegmentRotationType.Absolute)
                            chunk.Write(74, (short)3);
                        else
                            chunk.Write(74, (short)2);

                        chunk.Write(75, (short)0);
                        chunk.Write(340, textSegment.Style.Handle);
                        chunk.Write(46, textSegment.Scale);
                        chunk.Write(50, textSegment.Rotation); // the dxf documentation is wrong the rotation value is stored in degrees not radians
                        chunk.Write(44, textSegment.Offset.X);
                        chunk.Write(45, textSegment.Offset.Y);
                        chunk.Write(9, EncodeNonAsciiCharacters(textSegment.Text));

                        break;
                    case LinetypeSegmentType.Shape:
                        LinetypeShapeSegment shapeSegment = (LinetypeShapeSegment) s;
                        if(shapeSegment.RotationType == LinetypeSegmentRotationType.Absolute)
                            chunk.Write(74, (short)5);
                        else
                            chunk.Write(74, (short)4);

                        chunk.Write(75, shapeSegment.Style.ShapeNumber(shapeSegment.Name)); // this.ShapeNumberFromSHPfile(shapeSegment.Name, shapeSegment.Style.File));
                        chunk.Write(340, shapeSegment.Style.Handle);
                        chunk.Write(46, shapeSegment.Scale);
                        chunk.Write(50, shapeSegment.Rotation); // the dxf documentation is wrong the rotation value is stored in degrees not radians
                        chunk.Write(44, shapeSegment.Offset.X);
                        chunk.Write(45, shapeSegment.Offset.Y);

                        break;
                }
            }

            WriteXData(linetype.XData);
        }

        /// <summary>
        /// Writes a new layer to the table section.
        /// </summary>
        /// <param name="layer">Layer.</param>
        private void WriteLayer(Layer layer)
        {
            Debug.Assert(activeTable == DxfObjectCode.LayerTable);

            chunk.Write(0, layer.CodeName);
            chunk.Write(5, layer.Handle);
            chunk.Write(330, layer.Owner.Handle);

            chunk.Write(100, SubclassMarker.TableRecord);

            chunk.Write(100, SubclassMarker.Layer);

            chunk.Write(2, EncodeNonAsciiCharacters(layer.Name));

            LayerFlags flags = LayerFlags.None;
            if (layer.IsFrozen)
                flags = flags | LayerFlags.Frozen;
            if (layer.IsLocked)
                flags = flags | LayerFlags.Locked;
            chunk.Write(70, (short) flags);

            //a negative color represents a hidden layer.
            if (layer.IsVisible)
                chunk.Write(62, layer.Color.Index);
            else
                chunk.Write(62, (short) -layer.Color.Index);
            if (layer.Color.UseTrueColor)
                chunk.Write(420, AciColor.ToTrueColor(layer.Color));

            chunk.Write(6, EncodeNonAsciiCharacters(layer.Linetype.Name));

            chunk.Write(290, layer.Plot);
            chunk.Write(370, (short) layer.Lineweight);
            // Hard pointer ID/handle of PlotStyleName object
            chunk.Write(390, "0");

            // transparency is stored in XData
            if (layer.Transparency.Value > 0)
            {
                AddLayerTransparencyXData(layer);
            }

            WriteXData(layer.XData);
        }

        private static void AddLayerTransparencyXData(Layer layer)
        {
            // for dxf versions prior to AutoCad2007 the block record units is stored in an extended data block
            XData xdataEntry;
            if (layer.XData.ContainsAppId("AcCmTransparency"))
            {
                xdataEntry = layer.XData["AcCmTransparency"];
                xdataEntry.XDataRecord.Clear();
            }
            else
            {
                xdataEntry = new XData(new ApplicationRegistry(ApplicationRegistry.DefaultName));
                layer.XData.Add(xdataEntry);
            }

            int alpha = Transparency.ToAlphaValue(layer.Transparency);
            xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.String, "DesignCenter Data"));
            xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int32, alpha));
        }

        /// <summary>
        /// Writes a new text style to the table section.
        /// </summary>
        /// <param name="style">TextStyle.</param>
        private void WriteTextStyle(TextStyle style)
        {
            Debug.Assert(activeTable == DxfObjectCode.TextStyleTable);

            chunk.Write(0, style.CodeName);
            chunk.Write(5, style.Handle);
            chunk.Write(330, style.Owner.Handle);

            chunk.Write(100, SubclassMarker.TableRecord);

            chunk.Write(100, SubclassMarker.TextStyle);

            chunk.Write(2, EncodeNonAsciiCharacters(style.Name));

            chunk.Write(3, EncodeNonAsciiCharacters(style.FontFile));

            if(!string.IsNullOrEmpty(style.BigFont))
                chunk.Write(4, EncodeNonAsciiCharacters(style.BigFont));

            chunk.Write(70, style.IsVertical ? (short) 4 : (short) 0);

            if (style.IsBackward && style.IsUpsideDown)
                chunk.Write(71, (short) 6);
            else if (style.IsBackward)
                chunk.Write(71, (short) 2);
            else if (style.IsUpsideDown)
                chunk.Write(71, (short) 4);
            else
                chunk.Write(71, (short) 0);

            chunk.Write(40, style.Height);
            chunk.Write(41, style.WidthFactor);
            chunk.Write(42, style.Height);
            chunk.Write(50, style.ObliqueAngle);

            // when a true type font file is present the font information is defined by the file and this information is not needed
            if (style.IsTrueType && string.IsNullOrEmpty(style.FontFile))
            {
               AddTextStyleFontXData(style);
            }
            WriteXData(style.XData);
        }

        private void AddTextStyleFontXData(TextStyle style)
        {
            // for dxf versions prior to AutoCad2007 the block record units is stored in an extended data block
            XData xdataEntry;
            if (style.XData.ContainsAppId(ApplicationRegistry.DefaultName))
            {
                xdataEntry = style.XData[ApplicationRegistry.DefaultName];
                xdataEntry.XDataRecord.Clear();
            }
            else
            {
                xdataEntry = new XData(new ApplicationRegistry(ApplicationRegistry.DefaultName));
                style.XData.Add(xdataEntry);
            }
            byte[] st = new byte[4];
            st[3] = (byte)style.FontStyle;
            xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.String, EncodeNonAsciiCharacters(style.FontFamilyName)));
            xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int32, BitConverter.ToInt32(st, 0)));
        }

        /// <summary>
        /// Writes a new shape style to the table section.
        /// </summary>
        /// <param name="style">ShapeStyle.</param>
        private void WriteShapeStyle(ShapeStyle style)
        {
            Debug.Assert(activeTable == DxfObjectCode.TextStyleTable);

            chunk.Write(0, style.CodeName);
            chunk.Write(5, style.Handle);
            chunk.Write(330, style.Owner.Handle);

            chunk.Write(100, SubclassMarker.TableRecord);

            chunk.Write(100, SubclassMarker.TextStyle);

            chunk.Write(2, string.Empty);

            chunk.Write(3, EncodeNonAsciiCharacters(style.File));
            chunk.Write(70, (short)1);
            chunk.Write(40, style.Size);
            chunk.Write(41, style.WidthFactor);
            chunk.Write(42, style.Size);
            chunk.Write(50, style.ObliqueAngle);

            WriteXData(style.XData);
        }

        /// <summary>
        /// Writes a new user coordinate system to the table section.
        /// </summary>
        /// <param name="ucs">UCS.</param>
        private void WriteUCS(UCS ucs)
        {
            Debug.Assert(activeTable == DxfObjectCode.UcsTable);

            chunk.Write(0, ucs.CodeName);
            chunk.Write(5, ucs.Handle);
            chunk.Write(330, ucs.Owner.Handle);

            chunk.Write(100, SubclassMarker.TableRecord);

            chunk.Write(100, SubclassMarker.Ucs);

            chunk.Write(2, EncodeNonAsciiCharacters(ucs.Name));

            chunk.Write(70, (short) 0);

            chunk.Write(10, ucs.Origin.X);
            chunk.Write(20, ucs.Origin.Y);
            chunk.Write(30, ucs.Origin.Z);

            chunk.Write(11, ucs.XAxis.X);
            chunk.Write(21, ucs.XAxis.Y);
            chunk.Write(31, ucs.XAxis.Z);

            chunk.Write(12, ucs.YAxis.X);
            chunk.Write(22, ucs.YAxis.Y);
            chunk.Write(32, ucs.YAxis.Z);

            chunk.Write(79, (short) 0);

            chunk.Write(146, ucs.Elevation);

            WriteXData(ucs.XData);
        }

        #endregion

        #region methods for Block section

        private void WriteBlock(Block block)
        {
            Debug.Assert(activeSection == DxfObjectCode.BlocksSection);

            string name = EncodeNonAsciiCharacters(block.Name);
            string blockLayer = EncodeNonAsciiCharacters(block.Layer.Name);
            Layout layout = block.Record.Layout;

            chunk.Write(0, block.CodeName);
            chunk.Write(5, block.Handle);
            chunk.Write(330, block.Owner.Handle);

            chunk.Write(100, SubclassMarker.Entity);

            if (layout != null)
                chunk.Write(67, layout.IsPaperSpace ? (short) 1 : (short) 0);

            chunk.Write(8, blockLayer);

            chunk.Write(100, SubclassMarker.BlockBegin);
            if (block.IsXRef)
                chunk.Write(1, EncodeNonAsciiCharacters(block.XrefFile));
            chunk.Write(2, name);
            chunk.Write(70, (short) block.Flags);
            chunk.Write(10, block.Origin.X);
            chunk.Write(20, block.Origin.Y);
            chunk.Write(30, block.Origin.Z);
            chunk.Write(3, name);

            if (layout == null)
            {
                foreach (AttributeDefinition attdef in block.AttributeDefinitions.Values)
                {
                    WriteAttributeDefinition(attdef, null);
                }

                foreach (EntityObject entity in block.Entities)
                {
                    WriteEntity(entity, null);
                }
            }
            else
            {
                // the entities of the model space and the first paper space are written in the entities section
                if (!(string.Equals(layout.AssociatedBlock.Name, Block.DefaultModelSpaceName, StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(layout.AssociatedBlock.Name, Block.DefaultPaperSpaceName, StringComparison.OrdinalIgnoreCase)))
                {
                    WriteEntity(layout.Viewport, layout);

                    foreach (AttributeDefinition attdef in layout.AssociatedBlock.AttributeDefinitions.Values)
                    {
                        WriteAttributeDefinition(attdef, layout);
                    }

                    foreach (EntityObject entity in layout.AssociatedBlock.Entities)
                    {
                        WriteEntity(entity, layout);
                    }
                }               
            }

            // EndBlock entity
            chunk.Write(0, block.End.CodeName);
            chunk.Write(5, block.End.Handle);
            chunk.Write(330, block.Owner.Handle);
            chunk.Write(100, SubclassMarker.Entity);
            chunk.Write(8, blockLayer);
            chunk.Write(100, SubclassMarker.BlockEnd);

            WriteXData(block.XData);
        }

        #endregion

        #region methods for Entity section

        private void WriteEntity(EntityObject entity, Layout layout)
        {
            Debug.Assert(activeSection == DxfObjectCode.EntitiesSection || activeSection == DxfObjectCode.BlocksSection);
            Debug.Assert(entity != null);

            // hatches with zero boundaries are not allowed
            if (entity.Type == EntityType.Hatch && ((Hatch) entity).BoundaryPaths.Count == 0)
                return;
            // leader entities with less than two vertexes are not allowed
            if (entity.Type == EntityType.Leader && ((Leader) entity).Vertexes.Count < 2)
                return;
            // polyline entities with less than two vertexes are not allowed
            if (entity.Type == EntityType.Polyline && ((Polyline) entity).Vertexes.Count < 2)
                return;
            // lwPolyline entities with less than two vertexes are not allowed
            if (entity.Type == EntityType.LwPolyline && ((LwPolyline) entity).Vertexes.Count < 2)
                return;

            WriteEntityCommonCodes(entity, layout);

            switch (entity.Type)
            {
                case EntityType.Arc:
                    WriteArc((Arc) entity);
                    break;
                case EntityType.Circle:
                    WriteCircle((Circle) entity);
                    break;
                case EntityType.Dimension:
                    WriteDimension((Dimension) entity);
                    break;
                case EntityType.Ellipse:
                    WriteEllipse((Ellipse) entity);
                    break;
                case EntityType.Face3D:
                    WriteFace3D((Face3d) entity);
                    break;
                case EntityType.Hatch:
                    WriteHatch((Hatch) entity);
                    break;
                case EntityType.Image:
                    WriteImage((Image) entity);
                    break;
                case EntityType.Insert:
                    WriteInsert((Insert) entity);
                    break;
                case EntityType.Leader:
                    WriteLeader((Leader) entity);
                    break;
                case EntityType.LwPolyline:
                    WriteLightWeightPolyline((LwPolyline) entity);
                    break;
                case EntityType.Line:
                    WriteLine((Line) entity);
                    break;
                case EntityType.Mesh:
                    WriteMesh((Mesh) entity);
                    break;
                case EntityType.MLine:
                    WriteMLine((MLine) entity);
                    break;
                case EntityType.MText:
                    WriteMText((MText) entity);
                    break;
                case EntityType.Point:
                    WritePoint((Point) entity);
                    break;
                case EntityType.PolyfaceMesh:
                    WritePolyfaceMesh((PolyfaceMesh) entity);
                    break;
                case EntityType.Polyline:
                    WritePolyline((Polyline) entity);
                    break;
                case EntityType.Ray:
                    WriteRay((Ray) entity);
                    break;
                case EntityType.Shape:
                    WriteShape((Shape) entity);
                    break;
                case EntityType.Solid:
                    WriteSolid((Solid) entity);
                    break;
                case EntityType.Spline:
                    WriteSpline((Spline) entity);
                    break;
                case EntityType.Text:
                    WriteText((Text) entity);
                    break;
                case EntityType.Tolerance:
                    WriteTolerance((Tolerance) entity);
                    break;
                case EntityType.Trace:
                    WriteTrace((Trace) entity);
                    break;
                case EntityType.Underlay:
                    WriteUnderlay((Underlay) entity);
                    break;
                case EntityType.Viewport:
                    WriteViewport((Viewport) entity);
                    break;
                case EntityType.Wipeout:
                    WriteWipeout((Wipeout) entity);
                    break;
                case EntityType.XLine:
                    WriteXLine((XLine) entity);
                    break;
                default:
                    throw new ArgumentException("Entity unknown.", nameof(entity));
            }
        }

        private void WriteEntityCommonCodes(EntityObject entity, Layout layout)
        {
            chunk.Write(0, entity.CodeName);
            chunk.Write(5, entity.Handle);

            if (entity.Reactors.Count > 0)
            {
                chunk.Write(102, "{ACAD_REACTORS");
                foreach (DxfObject o in entity.Reactors)
                {
                    if(!string.IsNullOrEmpty(o.Handle)) chunk.Write(330, o.Handle);
                }
                chunk.Write(102, "}");
            }

            chunk.Write(330, entity.Owner.Record.Handle);

            chunk.Write(100, SubclassMarker.Entity);

            if (layout != null)
                chunk.Write(67, layout.IsPaperSpace ? (short) 1 : (short) 0);

            chunk.Write(8, EncodeNonAsciiCharacters(entity.Layer.Name));

            chunk.Write(62, entity.Color.Index);
            if (entity.Color.UseTrueColor)
                chunk.Write(420, AciColor.ToTrueColor(entity.Color));

            if (entity.Transparency.Value >= 0)
                chunk.Write(440, Transparency.ToAlphaValue(entity.Transparency));

            chunk.Write(6, EncodeNonAsciiCharacters(entity.Linetype.Name));

            chunk.Write(370, (short) entity.Lineweight);
            chunk.Write(48, entity.LinetypeScale);
            chunk.Write(60, entity.IsVisible ? (short) 0 : (short) 1);
        }

        private void WriteWipeout(Wipeout wipeout)
        {
            chunk.Write(100, SubclassMarker.Wipeout);

            BoundingRectangle br = new BoundingRectangle(wipeout.ClippingBoundary.Vertexes);

            Vector3 ocsInsPoint = new Vector3(br.Min.X, br.Min.Y, wipeout.Elevation);
            double w = br.Width;
            double h = br.Height;
            double max = w >= h ? w : h;
            Vector3 ocsUx = new Vector3(max, 0.0, 0.0);
            Vector3 ocsUy = new Vector3(0.0, max, 0.0);

            IList<Vector3> wcsPoints = MathHelper.Transform(new List<Vector3> {ocsInsPoint, ocsUx, ocsUy}, wipeout.Normal, CoordinateSystem.Object, CoordinateSystem.World);

            // Insertion point in WCS
            chunk.Write(10, wcsPoints[0].X);
            chunk.Write(20, wcsPoints[0].Y);
            chunk.Write(30, wcsPoints[0].Z);

            // U vector in WCS
            chunk.Write(11, wcsPoints[1].X);
            chunk.Write(21, wcsPoints[1].Y);
            chunk.Write(31, wcsPoints[1].Z);

            // V vector in WCS
            chunk.Write(12, wcsPoints[2].X);
            chunk.Write(22, wcsPoints[2].Y);
            chunk.Write(32, wcsPoints[2].Z);

            chunk.Write(13, 1.0);
            chunk.Write(23, 1.0);

            //this.chunk.Write(280, wipeout.ShowClippingFrame ? (short) 1 : (short) 0);
            chunk.Write(280, (short) 1);
            chunk.Write(281, (short) 50);
            chunk.Write(282, (short) 50);
            chunk.Write(283, (short) 0);

            chunk.Write(71, (short) wipeout.ClippingBoundary.Type);

            // for unknown reasons the wipeout with a polygonal clipping boundary requires to repeat the first vertex
            if (wipeout.ClippingBoundary.Type == ClippingBoundaryType.Polygonal)
            {
                chunk.Write(91, wipeout.ClippingBoundary.Vertexes.Count + 1);
                foreach (Vector2 vertex in wipeout.ClippingBoundary.Vertexes)
                {
                    double x = (vertex.X - ocsInsPoint.X)/max - 0.5;
                    double y = -((vertex.Y - ocsInsPoint.Y)/max - 0.5);
                    chunk.Write(14, x);
                    chunk.Write(24, y);
                }
                chunk.Write(14, (wipeout.ClippingBoundary.Vertexes[0].X - ocsInsPoint.X)/max - 0.5);
                chunk.Write(24, -((wipeout.ClippingBoundary.Vertexes[0].Y - ocsInsPoint.Y)/max - 0.5));
            }
            else
            {
                chunk.Write(91, wipeout.ClippingBoundary.Vertexes.Count);
                foreach (Vector2 vertex in wipeout.ClippingBoundary.Vertexes)
                {
                    double x = (vertex.X - ocsInsPoint.X)/max - 0.5;
                    double y = -((vertex.Y - ocsInsPoint.Y)/max - 0.5);
                    chunk.Write(14, x);
                    chunk.Write(24, y);
                }
            }
            WriteXData(wipeout.XData);
        }

        private void WriteUnderlay(Underlay underlay)
        {
            chunk.Write(100, SubclassMarker.Underlay);

            chunk.Write(340, underlay.Definition.Handle);

            Vector3 ocsPosition = MathHelper.Transform(underlay.Position, underlay.Normal, CoordinateSystem.World, CoordinateSystem.Object);
            chunk.Write(10, ocsPosition.X);
            chunk.Write(20, ocsPosition.Y);
            chunk.Write(30, ocsPosition.Z);

            chunk.Write(41, underlay.Scale.X);
            chunk.Write(42, underlay.Scale.Y);
            chunk.Write(43, 1.0);

            chunk.Write(50, underlay.Rotation);

            chunk.Write(210, underlay.Normal.X);
            chunk.Write(220, underlay.Normal.Y);
            chunk.Write(230, underlay.Normal.Z);

            chunk.Write(280, (short) underlay.DisplayOptions);

            chunk.Write(281, underlay.Contrast);
            chunk.Write(282, underlay.Fade);

            if (underlay.ClippingBoundary != null)
            {
                foreach (Vector2 vertex in underlay.ClippingBoundary.Vertexes)
                {
                    chunk.Write(11, vertex.X);
                    chunk.Write(21, vertex.Y);
                }
            }
        }

        private void WriteTolerance(Tolerance tolerance)
        {
            chunk.Write(100, SubclassMarker.Tolerance);

            chunk.Write(3, EncodeNonAsciiCharacters(tolerance.Style.Name));

            chunk.Write(10, tolerance.Position.X);
            chunk.Write(20, tolerance.Position.Y);
            chunk.Write(30, tolerance.Position.Z);

            string rep = tolerance.ToStringRepresentation();
            chunk.Write(1, EncodeNonAsciiCharacters(rep));

            chunk.Write(210, tolerance.Normal.X);
            chunk.Write(220, tolerance.Normal.Y);
            chunk.Write(230, tolerance.Normal.Z);

            double angle = tolerance.Rotation*MathHelper.DegToRad;
            Vector3 xAxis = new Vector3(Math.Cos(angle), Math.Sin(angle), 0.0);
            xAxis = MathHelper.Transform(xAxis, tolerance.Normal, CoordinateSystem.Object, CoordinateSystem.World);

            chunk.Write(11, xAxis.X);
            chunk.Write(21, xAxis.Y);
            chunk.Write(31, xAxis.Z);
        }

        private void WriteLeader(Leader leader)
        {
            chunk.Write(100, SubclassMarker.Leader);

            chunk.Write(3, leader.Style.Name);

            if (leader.ShowArrowhead)
                chunk.Write(71, (short) 1);
            else
                chunk.Write(71, (short) 0);

            chunk.Write(72, (short) leader.PathType);

            if (leader.Annotation != null)
            {
                switch (leader.Annotation.Type)
                {
                    case EntityType.MText:
                        chunk.Write(73, (short) 0);
                        break;
                    case EntityType.Insert:
                        chunk.Write(73, (short) 2);
                        break;
                    default:
                        chunk.Write(73, (short) 3);
                        break;
                }
            }
            else
            {
                chunk.Write(73, (short) 3);
            }

            chunk.Write(74, (short) 0);
            chunk.Write(75, leader.HasHookline ? (short) 1 : (short) 0);

            //this.chunk.Write(40, 0.0);
            //this.chunk.Write(41, 0.0);

            List<Vector3> ocsVertexes = new List<Vector3>();
            foreach (Vector2 vector in leader.Vertexes)
                ocsVertexes.Add(new Vector3(vector.X, vector.Y, leader.Elevation));

            IList<Vector3> wcsVertexes = MathHelper.Transform(ocsVertexes, leader.Normal, CoordinateSystem.Object, CoordinateSystem.World);
            chunk.Write(76, (short) wcsVertexes.Count);
            foreach (Vector3 vertex in wcsVertexes)
            {
                chunk.Write(10, vertex.X);
                chunk.Write(20, vertex.Y);
                chunk.Write(30, vertex.Z);
            }

            chunk.Write(77, leader.LineColor.Index);

            if (leader.Annotation != null)
                chunk.Write(340, leader.Annotation.Handle);

            chunk.Write(210, leader.Normal.X);
            chunk.Write(220, leader.Normal.Y);
            chunk.Write(230, leader.Normal.Z);

            Vector3 dir = ocsVertexes[ocsVertexes.Count-1] - ocsVertexes[ocsVertexes.Count - 2];

            Vector3 xDir = MathHelper.Transform(new Vector3(dir.X, dir.Y, 0.0), leader.Normal, CoordinateSystem.Object, CoordinateSystem.World);
            xDir.Normalize();
            chunk.Write(211, xDir.X);
            chunk.Write(221, xDir.Y);
            chunk.Write(231, xDir.Z);

            Vector3 wcsOffset = MathHelper.Transform(new Vector3(leader.Offset.X, leader.Offset.Y, leader.Elevation), leader.Normal, CoordinateSystem.Object, CoordinateSystem.World);
            chunk.Write(212, wcsOffset.X);
            chunk.Write(222, wcsOffset.Y);
            chunk.Write(232, wcsOffset.Z);

            chunk.Write(213, wcsOffset.X);
            chunk.Write(223, wcsOffset.Y);
            chunk.Write(233, wcsOffset.Z);

            // dimension style overrides info
            if (leader.StyleOverrides.Count > 0)
                AddDimensionStyleOverridesXData(leader.XData, leader.StyleOverrides);

            WriteXData(leader.XData);
        }

        private void WriteMesh(Mesh mesh)
        {
            chunk.Write(100, SubclassMarker.Mesh);

            chunk.Write(71, (short) 2);
            chunk.Write(72, (short) 0);

            chunk.Write(91, (int) mesh.SubdivisionLevel);

            //vertexes
            chunk.Write(92, mesh.Vertexes.Count);
            foreach (Vector3 vertex in mesh.Vertexes)
            {
                chunk.Write(10, vertex.X);
                chunk.Write(20, vertex.Y);
                chunk.Write(30, vertex.Z);
            }

            //faces
            int sizeFaceList = mesh.Faces.Count;
            foreach (int[] face in mesh.Faces)
            {
                sizeFaceList += face.Length;
            }
            chunk.Write(93, sizeFaceList);
            foreach (int[] face in mesh.Faces)
            {
                chunk.Write(90, face.Length);
                foreach (int index in face)
                {
                    chunk.Write(90, index);
                }
            }

            // the edges information is optional, and only really useful when it is required to assign creases values to edges
            if (mesh.Edges != null)
            {
                //edges
                chunk.Write(94, mesh.Edges.Count);
                foreach (MeshEdge edge in mesh.Edges)
                {
                    chunk.Write(90, edge.StartVertexIndex);
                    chunk.Write(90, edge.EndVertexIndex);
                }

                //creases
                chunk.Write(95, mesh.Edges.Count);
                foreach (MeshEdge edge in mesh.Edges)
                {
                    chunk.Write(140, edge.Crease);
                }
            }

            chunk.Write(90, 0);

            WriteXData(mesh.XData);
        }

        private void WriteShape(Shape shape)
        {
            chunk.Write(100, SubclassMarker.Shape);

            chunk.Write(39, shape.Thickness);
            chunk.Write(10, shape.Position.X);
            chunk.Write(20, shape.Position.Y);
            chunk.Write(30, shape.Position.Z);
            chunk.Write(40, shape.Size);
            chunk.Write(2, shape.Name);
            chunk.Write(50, shape.Rotation);
            chunk.Write(41, shape.WidthFactor);
            chunk.Write(51, shape.ObliqueAngle);

            chunk.Write(210, shape.Normal.X);
            chunk.Write(220, shape.Normal.Y);
            chunk.Write(230, shape.Normal.Z);

            WriteXData(shape.XData);
        }

        private void WriteArc(Arc arc)
        {
            chunk.Write(100, SubclassMarker.Circle);

            chunk.Write(39, arc.Thickness);

            // this is just an example of the weird Autodesk dxf way of doing things, while an ellipse the center is given in world coordinates,
            // the center of an arc is given in object coordinates (different rules for the same concept).
            // It is a lot more intuitive to give the center in world coordinates and then define the orientation with the normal..
            Vector3 ocsCenter = MathHelper.Transform(arc.Center, arc.Normal, CoordinateSystem.World, CoordinateSystem.Object);

            chunk.Write(10, ocsCenter.X);
            chunk.Write(20, ocsCenter.Y);
            chunk.Write(30, ocsCenter.Z);

            chunk.Write(40, arc.Radius);

            chunk.Write(210, arc.Normal.X);
            chunk.Write(220, arc.Normal.Y);
            chunk.Write(230, arc.Normal.Z);

            chunk.Write(100, SubclassMarker.Arc);
            chunk.Write(50, arc.StartAngle);
            chunk.Write(51, arc.EndAngle);

            WriteXData(arc.XData);
        }

        private void WriteCircle(Circle circle)
        {
            chunk.Write(100, SubclassMarker.Circle);

            // this is just an example of the stupid autodesk dxf way of doing things, while an ellipse the center is given in world coordinates,
            // the center of a circle is given in object coordinates (different rules for the same concept).
            // It is a lot more intuitive to give the center in world coordinates and then define the orientation with the normal..
            Vector3 ocsCenter = MathHelper.Transform(circle.Center, circle.Normal, CoordinateSystem.World, CoordinateSystem.Object);

            chunk.Write(10, ocsCenter.X);
            chunk.Write(20, ocsCenter.Y);
            chunk.Write(30, ocsCenter.Z);

            chunk.Write(40, circle.Radius);

            chunk.Write(39, circle.Thickness);

            chunk.Write(210, circle.Normal.X);
            chunk.Write(220, circle.Normal.Y);
            chunk.Write(230, circle.Normal.Z);

            WriteXData(circle.XData);
        }

        private void WriteEllipse(Ellipse ellipse)
        {
            chunk.Write(100, SubclassMarker.Ellipse);

            chunk.Write(10, ellipse.Center.X);
            chunk.Write(20, ellipse.Center.Y);
            chunk.Write(30, ellipse.Center.Z);

            Vector2 axis = Vector2.Rotate(new Vector2(0.5*ellipse.MajorAxis, 0.0), ellipse.Rotation * MathHelper.DegToRad);
            Vector3 axisPoint = MathHelper.Transform(new Vector3(axis.X, axis.Y, 0.0), ellipse.Normal, CoordinateSystem.Object, CoordinateSystem.World);

            chunk.Write(11, axisPoint.X);
            chunk.Write(21, axisPoint.Y);
            chunk.Write(31, axisPoint.Z);

            chunk.Write(210, ellipse.Normal.X);
            chunk.Write(220, ellipse.Normal.Y);
            chunk.Write(230, ellipse.Normal.Z);

            chunk.Write(40, ellipse.MinorAxis/ellipse.MajorAxis);

            double[] paramaters = GetEllipseParameters(ellipse);
            chunk.Write(41, paramaters[0]);
            chunk.Write(42, paramaters[1]);

            WriteXData(ellipse.XData);
        }

        private static double[] GetEllipseParameters(Ellipse ellipse)
        {
            double atan1;
            double atan2;
            if (ellipse.IsFullEllipse)
            {
                atan1 = 0.0;
                atan2 = MathHelper.TwoPI;
            }
            else
            {
                Vector2 startPoint = new Vector2(ellipse.Center.X, ellipse.Center.Y) + ellipse.PolarCoordinateRelativeToCenter(ellipse.StartAngle);
                Vector2 endPoint = new Vector2(ellipse.Center.X, ellipse.Center.Y) + ellipse.PolarCoordinateRelativeToCenter(ellipse.EndAngle);
                double a = ellipse.MajorAxis*0.5;
                double b = ellipse.MinorAxis*0.5;
                double px1 = (startPoint.X - ellipse.Center.X)/a;
                double py1 = (startPoint.Y - ellipse.Center.Y)/b;
                double px2 = (endPoint.X - ellipse.Center.X)/a;
                double py2 = (endPoint.Y - ellipse.Center.Y)/b;

                atan1 = Math.Atan2(py1, px1);
                atan2 = Math.Atan2(py2, px2);
            }
            return new[] {atan1, atan2};
        }

        private void WriteSolid(Solid solid)
        {
            chunk.Write(100, SubclassMarker.Solid);

            // the vertexes are stored in OCS
            chunk.Write(10, solid.FirstVertex.X);
            chunk.Write(20, solid.FirstVertex.Y);
            chunk.Write(30, solid.Elevation);

            chunk.Write(11, solid.SecondVertex.X);
            chunk.Write(21, solid.SecondVertex.Y);
            chunk.Write(31, solid.Elevation);

            chunk.Write(12, solid.ThirdVertex.X);
            chunk.Write(22, solid.ThirdVertex.Y);
            chunk.Write(32, solid.Elevation);

            chunk.Write(13, solid.FourthVertex.X);
            chunk.Write(23, solid.FourthVertex.Y);
            chunk.Write(33, solid.Elevation);

            chunk.Write(39, solid.Thickness);

            chunk.Write(210, solid.Normal.X);
            chunk.Write(220, solid.Normal.Y);
            chunk.Write(230, solid.Normal.Z);

            WriteXData(solid.XData);
        }

        private void WriteTrace(Trace trace)
        {
            chunk.Write(100, SubclassMarker.Trace);

            // the vertexes are stored in OCS
            chunk.Write(10, trace.FirstVertex.X);
            chunk.Write(20, trace.FirstVertex.Y);
            chunk.Write(30, trace.Elevation);

            chunk.Write(11, trace.SecondVertex.X);
            chunk.Write(21, trace.SecondVertex.Y);
            chunk.Write(31, trace.Elevation);

            chunk.Write(12, trace.ThirdVertex.X);
            chunk.Write(22, trace.ThirdVertex.Y);
            chunk.Write(32, trace.Elevation);

            chunk.Write(13, trace.FourthVertex.X);
            chunk.Write(23, trace.FourthVertex.Y);
            chunk.Write(33, trace.Elevation);

            chunk.Write(39, trace.Thickness);

            chunk.Write(210, trace.Normal.X);
            chunk.Write(220, trace.Normal.Y);
            chunk.Write(230, trace.Normal.Z);

            WriteXData(trace.XData);
        }

        private void WriteFace3D(Face3d face)
        {
            chunk.Write(100, SubclassMarker.Face3d);

            chunk.Write(10, face.FirstVertex.X);
            chunk.Write(20, face.FirstVertex.Y);
            chunk.Write(30, face.FirstVertex.Z);

            chunk.Write(11, face.SecondVertex.X);
            chunk.Write(21, face.SecondVertex.Y);
            chunk.Write(31, face.SecondVertex.Z);

            chunk.Write(12, face.ThirdVertex.X);
            chunk.Write(22, face.ThirdVertex.Y);
            chunk.Write(32, face.ThirdVertex.Z);

            chunk.Write(13, face.FourthVertex.X);
            chunk.Write(23, face.FourthVertex.Y);
            chunk.Write(33, face.FourthVertex.Z);

            chunk.Write(70, (short) face.EdgeFlags);

            WriteXData(face.XData);
        }

        private void WriteSpline(Spline spline)
        {
            chunk.Write(100, SubclassMarker.Spline);

            short flags = (short) spline.Flags;

            if (spline.CreationMethod == SplineCreationMethod.FitPoints)
            {
                flags += (short) SplinetypeFlags.FitPointCreationMethod;
                flags += (short) spline.KnotParameterization;
            }

            if (spline.IsPeriodic)
                flags += (short) SplinetypeFlags.ClosedPeriodicSpline;

            chunk.Write(70, flags);
            chunk.Write(71, spline.Degree);

            // the next two codes are purely cosmetic and writing them causes more bad than good.
            // internally AutoCad allows for an INT number of knots and control points,
            // but for some weird decision they decided to define them in the dxf with codes 72 and 73 (16-bit integer value), this is a SHORT in net.
            // I guess this is the result of legacy code, nevertheless AutoCad do not use those values when importing Spline entities
            //this.chunk.Write(72, (short)spline.Knots.Length);
            //this.chunk.Write(73, (short)spline.ControlPoints.Count);
            //this.chunk.Write(74, (short)spline.FitPoints.Count);

            chunk.Write(42, spline.KnotTolerance);
            chunk.Write(43, spline.CtrlPointTolerance);
            chunk.Write(44, spline.FitTolerance);

            if (spline.StartTangent != null)
            {
                chunk.Write(12, spline.StartTangent.Value.X);
                chunk.Write(22, spline.StartTangent.Value.Y);
                chunk.Write(32, spline.StartTangent.Value.Z);
            }

            if (spline.EndTangent != null)
            {
                chunk.Write(13, spline.EndTangent.Value.X);
                chunk.Write(23, spline.EndTangent.Value.Y);
                chunk.Write(33, spline.EndTangent.Value.Z);
            }

            foreach (double knot in spline.Knots)
                chunk.Write(40, knot);

            foreach (SplineVertex point in spline.ControlPoints)
            {
                chunk.Write(41, point.Weight);
                chunk.Write(10, point.Position.X);
                chunk.Write(20, point.Position.Y);
                chunk.Write(30, point.Position.Z);
            }

            foreach (Vector3 point in spline.FitPoints)
            {
                chunk.Write(11, point.X);
                chunk.Write(21, point.Y);
                chunk.Write(31, point.Z);
            }


            WriteXData(spline.XData);
        }

        private void WriteInsert(Insert insert)
        {
            chunk.Write(100, SubclassMarker.Insert);

            chunk.Write(2, EncodeNonAsciiCharacters(insert.Block.Name));

            // It is a lot more intuitive to give the center in world coordinates and then define the orientation with the normal.
            Vector3 ocsInsertion = MathHelper.Transform(insert.Position, insert.Normal, CoordinateSystem.World, CoordinateSystem.Object);

            chunk.Write(10, ocsInsertion.X);
            chunk.Write(20, ocsInsertion.Y);
            chunk.Write(30, ocsInsertion.Z);

            // we need to apply the scaling factor between the block and the document or the block that owns it in case of nested blocks
            double scale = UnitHelper.ConversionFactor(insert.Block.Record.Units, insert.Owner.Record.IsForInternalUseOnly ? doc.DrawingVariables.InsUnits : insert.Owner.Record.Units);

            chunk.Write(41, insert.Scale.X*scale);
            chunk.Write(42, insert.Scale.Y*scale);
            chunk.Write(43, insert.Scale.Z*scale);

            chunk.Write(50, insert.Rotation);

            chunk.Write(210, insert.Normal.X);
            chunk.Write(220, insert.Normal.Y);
            chunk.Write(230, insert.Normal.Z);

            if (insert.Attributes.Count > 0)
            {
                //Obsolete; formerly an entities follow flag (optional; ignore if present)
                //AutoCAD will fail loading the file if it is not there, more dxf voodoo
                chunk.Write(66, (short) 1);

                WriteXData(insert.XData);

                foreach (Attribute attrib in insert.Attributes)
                    WriteAttribute(attrib);

                chunk.Write(0, insert.EndSequence.CodeName);
                chunk.Write(5, insert.EndSequence.Handle);
                chunk.Write(100, SubclassMarker.Entity);
                chunk.Write(8, EncodeNonAsciiCharacters(insert.Layer.Name));
            }
            else
            {
                WriteXData(insert.XData);
            }
        }

        private void WriteLine(Line line)
        {
            chunk.Write(100, SubclassMarker.Line);

            chunk.Write(10, line.StartPoint.X);
            chunk.Write(20, line.StartPoint.Y);
            chunk.Write(30, line.StartPoint.Z);

            chunk.Write(11, line.EndPoint.X);
            chunk.Write(21, line.EndPoint.Y);
            chunk.Write(31, line.EndPoint.Z);

            chunk.Write(39, line.Thickness);

            chunk.Write(210, line.Normal.X);
            chunk.Write(220, line.Normal.Y);
            chunk.Write(230, line.Normal.Z);

            WriteXData(line.XData);
        }

        private void WriteRay(Ray ray)
        {
            chunk.Write(100, SubclassMarker.Ray);

            chunk.Write(10, ray.Origin.X);
            chunk.Write(20, ray.Origin.Y);
            chunk.Write(30, ray.Origin.Z);

            chunk.Write(11, ray.Direction.X);
            chunk.Write(21, ray.Direction.Y);
            chunk.Write(31, ray.Direction.Z);

            WriteXData(ray.XData);
        }

        private void WriteXLine(XLine xline)
        {
            chunk.Write(100, SubclassMarker.XLine);

            chunk.Write(10, xline.Origin.X);
            chunk.Write(20, xline.Origin.Y);
            chunk.Write(30, xline.Origin.Z);

            chunk.Write(11, xline.Direction.X);
            chunk.Write(21, xline.Direction.Y);
            chunk.Write(31, xline.Direction.Z);

            WriteXData(xline.XData);
        }

        private void WriteLightWeightPolyline(LwPolyline polyline)
        {
            chunk.Write(100, SubclassMarker.LightWeightPolyline);
            chunk.Write(90, polyline.Vertexes.Count);
            chunk.Write(70, (short) polyline.Flags);

            chunk.Write(38, polyline.Elevation);
            chunk.Write(39, polyline.Thickness);


            foreach (LwPolylineVertex v in polyline.Vertexes)
            {
                chunk.Write(10, v.Position.X);
                chunk.Write(20, v.Position.Y);
                chunk.Write(40, v.StartWidth);
                chunk.Write(41, v.EndWidth);
                chunk.Write(42, v.Bulge);
            }

            chunk.Write(210, polyline.Normal.X);
            chunk.Write(220, polyline.Normal.Y);
            chunk.Write(230, polyline.Normal.Z);

            WriteXData(polyline.XData);
        }

        private void WritePolyline(Polyline polyline)
        {
            chunk.Write(100, SubclassMarker.Polyline3d);

            //dummy point
            chunk.Write(10, 0.0);
            chunk.Write(20, 0.0);
            chunk.Write(30, 0.0);

            chunk.Write(70, (short) polyline.Flags);
            chunk.Write(75, (short) polyline.SmoothType);

            chunk.Write(210, polyline.Normal.X);
            chunk.Write(220, polyline.Normal.Y);
            chunk.Write(230, polyline.Normal.Z);

            WriteXData(polyline.XData);

            string layerName = EncodeNonAsciiCharacters(polyline.Layer.Name);

            foreach (PolylineVertex v in polyline.Vertexes)
            {
                chunk.Write(0, v.CodeName);
                chunk.Write(5, v.Handle);
                chunk.Write(100, SubclassMarker.Entity);

                chunk.Write(8, layerName); // the vertex layer should be the same as the polyline layer

                chunk.Write(62, polyline.Color.Index); // the vertex color should be the same as the polyline color
                if (polyline.Color.UseTrueColor)
                    chunk.Write(420, AciColor.ToTrueColor(polyline.Color));
                chunk.Write(100, SubclassMarker.Vertex);
                chunk.Write(100, SubclassMarker.Polyline3dVertex);
                chunk.Write(10, v.Position.X);
                chunk.Write(20, v.Position.Y);
                chunk.Write(30, v.Position.Z);
                chunk.Write(70, (short) v.Flags);
            }

            chunk.Write(0, polyline.EndSequence.CodeName);
            chunk.Write(5, polyline.EndSequence.Handle);
            chunk.Write(100, SubclassMarker.Entity);
            chunk.Write(8, layerName); // the polyline EndSequence layer should be the same as the polyline layer
        }

        private void WritePolyfaceMesh(PolyfaceMesh mesh)
        {
            chunk.Write(100, SubclassMarker.PolyfaceMesh);
            chunk.Write(70, (short) mesh.Flags);

            chunk.Write(71, (short) mesh.Vertexes.Count);
            chunk.Write(72, (short) mesh.Faces.Count);

            //dummy point
            chunk.Write(10, 0.0);
            chunk.Write(20, 0.0);
            chunk.Write(30, 0.0);

            chunk.Write(210, mesh.Normal.X);
            chunk.Write(220, mesh.Normal.Y);
            chunk.Write(230, mesh.Normal.Z);

            if (mesh.XData != null)
                WriteXData(mesh.XData);

            string layerName = EncodeNonAsciiCharacters(mesh.Layer.Name);

            foreach (PolyfaceMeshVertex v in mesh.Vertexes)
            {
                chunk.Write(0, v.CodeName);
                chunk.Write(5, v.Handle);
                chunk.Write(100, SubclassMarker.Entity);

                chunk.Write(8, layerName); // the polyface mesh vertex layer should be the same as the polyface mesh layer

                chunk.Write(62, mesh.Color.Index); // the polyface mesh vertex color should be the same as the polyface mesh color
                if (mesh.Color.UseTrueColor)
                    chunk.Write(420, AciColor.ToTrueColor(mesh.Color));
                chunk.Write(100, SubclassMarker.Vertex);
                chunk.Write(100, SubclassMarker.PolyfaceMeshVertex);
                chunk.Write(70, (short) v.Flags);
                chunk.Write(10, v.Position.X);
                chunk.Write(20, v.Position.Y);
                chunk.Write(30, v.Position.Z);
            }

            foreach (PolyfaceMeshFace face in mesh.Faces)
            {
                chunk.Write(0, face.CodeName);
                chunk.Write(5, face.Handle);
                chunk.Write(100, SubclassMarker.Entity);

                chunk.Write(8, layerName); // the polyface mesh face layer should be the same as the polyface mesh layer
                chunk.Write(62, mesh.Color.Index); // the polyface mesh face color should be the same as the polyface mesh color
                if (mesh.Color.UseTrueColor)
                    chunk.Write(420, AciColor.ToTrueColor(mesh.Color));
                chunk.Write(100, SubclassMarker.PolyfaceMeshFace);
                chunk.Write(70, (short) VertexTypeFlags.PolyfaceMeshVertex);
                chunk.Write(10, 0.0);
                chunk.Write(20, 0.0);
                chunk.Write(30, 0.0);

                chunk.Write(71, face.VertexIndexes[0]);
                if (face.VertexIndexes.Count > 1)
                    chunk.Write(72, face.VertexIndexes[1]);
                if (face.VertexIndexes.Count > 2)
                    chunk.Write(73, face.VertexIndexes[2]);
                if (face.VertexIndexes.Count > 3)
                    chunk.Write(74, face.VertexIndexes[3]);
            }

            chunk.Write(0, mesh.EndSequence.CodeName);
            chunk.Write(5, mesh.EndSequence.Handle);
            chunk.Write(100, SubclassMarker.Entity);
            chunk.Write(8, layerName); // the polyface mesh EndSequence layer should be the same as the polyface mesh layer
        }

        private void WritePoint(Point point)
        {
            chunk.Write(100, SubclassMarker.Point);

            chunk.Write(10, point.Position.X);
            chunk.Write(20, point.Position.Y);
            chunk.Write(30, point.Position.Z);

            chunk.Write(39, point.Thickness);

            chunk.Write(210, point.Normal.X);
            chunk.Write(220, point.Normal.Y);
            chunk.Write(230, point.Normal.Z);

            // for unknown reasons the dxf likes the point rotation inverted
            chunk.Write(50, 360.0 - point.Rotation);

            WriteXData(point.XData);
        }

        private void WriteText(Text text)
        {
            chunk.Write(100, SubclassMarker.Text);

            chunk.Write(1, EncodeNonAsciiCharacters(text.Value));

            // another example of this OCS vs WCS non sense.
            // while the MText position is written in WCS the position of the Text is written in OCS (different rules for the same concept).
            Vector3 ocsBasePoint = MathHelper.Transform(text.Position, text.Normal, CoordinateSystem.World, CoordinateSystem.Object);

            chunk.Write(10, ocsBasePoint.X);
            chunk.Write(20, ocsBasePoint.Y);
            chunk.Write(30, ocsBasePoint.Z);

            chunk.Write(40, text.Height);

            chunk.Write(41, text.WidthFactor);

            chunk.Write(50, text.Rotation);

            chunk.Write(51, text.ObliqueAngle);

            chunk.Write(7, EncodeNonAsciiCharacters(text.Style.Name));

            chunk.Write(11, ocsBasePoint.X);
            chunk.Write(21, ocsBasePoint.Y);
            chunk.Write(31, ocsBasePoint.Z);

            chunk.Write(210, text.Normal.X);
            chunk.Write(220, text.Normal.Y);
            chunk.Write(230, text.Normal.Z);

            switch (text.Alignment)
            {
                case TextAlignment.TopLeft:

                    chunk.Write(72, (short) 0);
                    chunk.Write(100, SubclassMarker.Text);
                    chunk.Write(73, (short) 3);
                    break;

                case TextAlignment.TopCenter:

                    chunk.Write(72, (short) 1);
                    chunk.Write(100, SubclassMarker.Text);
                    chunk.Write(73, (short) 3);
                    break;

                case TextAlignment.TopRight:

                    chunk.Write(72, (short) 2);
                    chunk.Write(100, SubclassMarker.Text);
                    chunk.Write(73, (short) 3);
                    break;

                case TextAlignment.MiddleLeft:

                    chunk.Write(72, (short) 0);
                    chunk.Write(100, SubclassMarker.Text);
                    chunk.Write(73, (short) 2);
                    break;

                case TextAlignment.MiddleCenter:

                    chunk.Write(72, (short) 1);
                    chunk.Write(100, SubclassMarker.Text);
                    chunk.Write(73, (short) 2);
                    break;

                case TextAlignment.MiddleRight:

                    chunk.Write(72, (short) 2);
                    chunk.Write(100, SubclassMarker.Text);
                    chunk.Write(73, (short) 2);
                    break;

                case TextAlignment.BottomLeft:

                    chunk.Write(72, (short) 0);
                    chunk.Write(100, SubclassMarker.Text);
                    chunk.Write(73, (short) 1);
                    break;
                case TextAlignment.BottomCenter:

                    chunk.Write(72, (short) 1);
                    chunk.Write(100, SubclassMarker.Text);
                    chunk.Write(73, (short) 1);
                    break;

                case TextAlignment.BottomRight:

                    chunk.Write(72, (short) 2);
                    chunk.Write(100, SubclassMarker.Text);
                    chunk.Write(73, (short) 1);
                    break;

                case TextAlignment.BaselineLeft:
                    chunk.Write(72, (short) 0);
                    chunk.Write(100, SubclassMarker.Text);
                    chunk.Write(73, (short) 0);
                    break;

                case TextAlignment.BaselineCenter:
                    chunk.Write(72, (short) 1);
                    chunk.Write(100, SubclassMarker.Text);
                    chunk.Write(73, (short) 0);
                    break;

                case TextAlignment.BaselineRight:
                    chunk.Write(72, (short) 2);
                    chunk.Write(100, SubclassMarker.Text);
                    chunk.Write(73, (short) 0);
                    break;

                case TextAlignment.Aligned:
                    chunk.Write(72, (short) 3);
                    chunk.Write(100, SubclassMarker.Text);
                    chunk.Write(73, (short) 0);
                    break;

                case TextAlignment.Middle:
                    chunk.Write(72, (short) 4);
                    chunk.Write(100, SubclassMarker.Text);
                    chunk.Write(73, (short) 0);
                    break;

                case TextAlignment.Fit:
                    chunk.Write(72, (short) 5);
                    chunk.Write(100, SubclassMarker.Text);
                    chunk.Write(73, (short) 0);
                    break;
            }

            WriteXData(text.XData);
        }

        private void WriteMText(MText mText)
        {
            chunk.Write(100, SubclassMarker.MText);

            chunk.Write(10, mText.Position.X);
            chunk.Write(20, mText.Position.Y);
            chunk.Write(30, mText.Position.Z);

            chunk.Write(210, mText.Normal.X);
            chunk.Write(220, mText.Normal.Y);
            chunk.Write(230, mText.Normal.Z);

            WriteMTextChunks(EncodeNonAsciiCharacters(mText.Value));

            chunk.Write(40, mText.Height);
            chunk.Write(41, mText.RectangleWidth);
            chunk.Write(44, mText.LineSpacingFactor);

            //even if the AutoCAD dxf documentation says that the rotation is in radians, this is wrong this value must be saved in degrees
            //this.chunk.Write(50, mText.Rotation);

            //the other option for the rotation is to store the horizontal vector of the text
            //it will be used just in case other programs read the rotation as radians, QCAD seems to do that.
            Vector2 direction = Vector2.Rotate(Vector2.UnitX, mText.Rotation * MathHelper.DegToRad);
            direction.Normalize();
            Vector3 ocsDirection = MathHelper.Transform(new Vector3(direction.X, direction.Y, 0.0), mText.Normal, CoordinateSystem.Object, CoordinateSystem.World);

            chunk.Write(11, ocsDirection.X);
            chunk.Write(21, ocsDirection.Y);
            chunk.Write(31, ocsDirection.Z);

            chunk.Write(71, (short) mText.AttachmentPoint);

            chunk.Write(72, (short) mText.DrawingDirection);

            chunk.Write(73, (short) mText.LineSpacingStyle);

            chunk.Write(7, EncodeNonAsciiCharacters(mText.Style.Name));

            WriteXData(mText.XData);
        }

        private void WriteMTextChunks(string text)
        {
            //Text string. If the text string is less than 250 characters, all characters
            //appear in group 1. If the text string is greater than 250 characters, the
            //string is divided into 250 character chunks, which appear in one or
            //more group 3 codes. If group 3 codes are used, the last group is a
            //group 1 and has fewer than 250 characters
            while (text.Length > 250)
            {
                string part = text.Substring(0, 250);
                chunk.Write(3, part);
                text = text.Remove(0, 250);
            }
            chunk.Write(1, text);
        }

        private void WriteHatch(Hatch hatch)
        {
            chunk.Write(100, SubclassMarker.Hatch);

            chunk.Write(10, 0.0);
            chunk.Write(20, 0.0);
            chunk.Write(30, hatch.Elevation);

            chunk.Write(210, hatch.Normal.X);
            chunk.Write(220, hatch.Normal.Y);
            chunk.Write(230, hatch.Normal.Z);

            chunk.Write(2, EncodeNonAsciiCharacters(hatch.Pattern.Name));

            chunk.Write(70, (short) hatch.Pattern.Fill);

            if (hatch.Associative)
                chunk.Write(71, (short) 1);
            else
                chunk.Write(71, (short) 0);

            // boundary paths info
            WriteHatchBoundaryPaths(hatch.BoundaryPaths);

            // pattern info
            WriteHatchPattern(hatch.Pattern);

            // add the required extended data entries to the hatch XData
            AddHatchPatternXData(hatch);

            WriteXData(hatch.XData);
        }

        private static void AddHatchPatternXData(Hatch hatch)
        {
            XData xdataEntry;
            if (hatch.XData.ContainsAppId(ApplicationRegistry.DefaultName))
            {
                xdataEntry = hatch.XData[ApplicationRegistry.DefaultName];
                xdataEntry.XDataRecord.Clear();
            }
            else
            {
                xdataEntry = new XData(new ApplicationRegistry(ApplicationRegistry.DefaultName));
                hatch.XData.Add(xdataEntry);
            }
            xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.RealX, hatch.Pattern.Origin.X));
            xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.RealY, hatch.Pattern.Origin.Y));
            xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.RealZ, 0.0));

            HatchGradientPattern grad = hatch.Pattern as HatchGradientPattern;

            if (grad == null) return;

            if (hatch.XData.ContainsAppId("GradientColor1ACI"))
            {
                xdataEntry = hatch.XData["GradientColor1ACI"];
                xdataEntry.XDataRecord.Clear();
            }
            else
            {
                xdataEntry = new XData(new ApplicationRegistry("GradientColor1ACI"));
                hatch.XData.Add(xdataEntry);
            }
            xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, grad.Color1.Index));


            if (hatch.XData.ContainsAppId("GradientColor2ACI"))
            {
                xdataEntry = hatch.XData["GradientColor2ACI"];
                xdataEntry.XDataRecord.Clear();
            }
            else
            {
                xdataEntry = new XData(new ApplicationRegistry("GradientColor2ACI"));
                hatch.XData.Add(xdataEntry);
            }
            xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, grad.Color2.Index));
        }

        private void WriteHatchBoundaryPaths(ObservableCollection<HatchBoundaryPath> boundaryPaths)
        {
            chunk.Write(91, boundaryPaths.Count);

            // each hatch boundary paths are made of multiple closed loops
            foreach (HatchBoundaryPath path in boundaryPaths)
            {
                chunk.Write(92, (int) path.PathType);

                if (!path.PathType.HasFlag(HatchBoundaryPathTypeFlags.Polyline))
                    chunk.Write(93, path.Edges.Count);

                foreach (HatchBoundaryPath.Edge entity in path.Edges)
                    WriteHatchBoundaryPathData(entity);

                chunk.Write(97, path.Entities.Count);
                foreach (EntityObject entity in path.Entities)
                {
                    chunk.Write(330, entity.Handle);
                }
            }
        }

        private void WriteHatchBoundaryPathData(HatchBoundaryPath.Edge entity)
        {
            if (entity.Type == HatchBoundaryPath.EdgeType.Arc)
            {
                chunk.Write(72, (short) 2); // Edge type (only if boundary is not a polyline): 1 = Line; 2 = Circular arc; 3 = Elliptic arc; 4 = Spline

                HatchBoundaryPath.Arc arc = (HatchBoundaryPath.Arc) entity;

                chunk.Write(10, arc.Center.X);
                chunk.Write(20, arc.Center.Y);
                chunk.Write(40, arc.Radius);
                chunk.Write(50, arc.StartAngle);
                chunk.Write(51, arc.EndAngle);
                chunk.Write(73, arc.IsCounterclockwise ? (short) 1 : (short) 0);
            }
            else if (entity.Type == HatchBoundaryPath.EdgeType.Ellipse)
            {
                chunk.Write(72, (short) 3); // Edge type (only if boundary is not a polyline): 1 = Line; 2 = Circular arc; 3 = Elliptic arc; 4 = Spline

                HatchBoundaryPath.Ellipse ellipse = (HatchBoundaryPath.Ellipse) entity;

                chunk.Write(10, ellipse.Center.X);
                chunk.Write(20, ellipse.Center.Y);
                chunk.Write(11, ellipse.EndMajorAxis.X);
                chunk.Write(21, ellipse.EndMajorAxis.Y);
                chunk.Write(40, ellipse.MinorRatio);
                chunk.Write(50, ellipse.StartAngle);
                chunk.Write(51, ellipse.EndAngle);
                chunk.Write(73, ellipse.IsCounterclockwise ? (short) 1 : (short) 0);
            }
            else if (entity.Type == HatchBoundaryPath.EdgeType.Line)
            {
                chunk.Write(72, (short) 1); // Edge type (only if boundary is not a polyline): 1 = Line; 2 = Circular arc; 3 = Elliptic arc; 4 = Spline

                HatchBoundaryPath.Line line = (HatchBoundaryPath.Line) entity;

                chunk.Write(10, line.Start.X);
                chunk.Write(20, line.Start.Y);
                chunk.Write(11, line.End.X);
                chunk.Write(21, line.End.Y);
            }
            else if (entity.Type == HatchBoundaryPath.EdgeType.Polyline)
            {
                HatchBoundaryPath.Polyline poly = (HatchBoundaryPath.Polyline) entity;
                chunk.Write(72, (short) 1); // Has bulge flag
                chunk.Write(73, poly.IsClosed ? (short) 1 : (short) 0);
                chunk.Write(93, poly.Vertexes.Length);

                foreach (Vector3 vertex in poly.Vertexes)
                {
                    chunk.Write(10, vertex.X);
                    chunk.Write(20, vertex.Y);
                    chunk.Write(42, vertex.Z);
                }
            }
            else if (entity.Type == HatchBoundaryPath.EdgeType.Spline)
            {
                chunk.Write(72, (short) 4); // Edge type (only if boundary is not a polyline): 1 = Line; 2 = Circular arc; 3 = Elliptic arc; 4 = Spline

                HatchBoundaryPath.Spline spline = (HatchBoundaryPath.Spline) entity;

                // another dxf inconsistency!; while the Spline entity degree is written as a short (code 71)
                // the degree of a hatch boundary path spline is written as an int (code 94)
                chunk.Write(94, (int) spline.Degree);
                chunk.Write(73, spline.IsRational ? (short) 1 : (short) 0);
                chunk.Write(74, spline.IsPeriodic ? (short) 1 : (short) 0);

                // now the number of knots and control points of a spline are written as an int, as it should be.
                // but in the Spline entities they are defined as shorts. Guess what, while you can avoid writing these two codes for the Spline entity, now they are required.
                chunk.Write(95, spline.Knots.Length);
                chunk.Write(96, spline.ControlPoints.Length);

                foreach (double knot in spline.Knots)
                    chunk.Write(40, knot);
                foreach (Vector3 point in spline.ControlPoints)
                {
                    chunk.Write(10, point.X);
                    chunk.Write(20, point.Y);
                    if (spline.IsRational)
                        chunk.Write(42, point.Z);
                }

                // this information is only required for AutoCAD version 2010
                // stores information about spline fit points (the spline entity has no fit points and no tangent info)
                // another dxf inconsistency!; while the number of fit points of Spline entity is written as a short (code 74)
                // the number of fit points of a hatch boundary path spline is written as an int (code 97)
                if (doc.DrawingVariables.AcadVer >= DxfVersion.AutoCad2010)
                    chunk.Write(97, 0);
            }
        }

        private void WriteHatchPattern(HatchPattern pattern)
        {
            chunk.Write(75, (short) pattern.Style);
            chunk.Write(76, (short) pattern.Type);

            if (pattern.Fill == HatchFillType.PatternFill)
            {
                chunk.Write(52, pattern.Angle);
                chunk.Write(41, pattern.Scale);
                chunk.Write(77, (short) 0); // Hatch pattern double flag
                chunk.Write(78, (short) pattern.LineDefinitions.Count); // Number of pattern definition lines  
                WriteHatchPatternDefinitonLines(pattern);
            }

            // I don't know what is the purpose of these codes, it seems that it doesn't change anything but they are needed
            chunk.Write(47, 0.0);
            chunk.Write(98, 1);
            chunk.Write(10, 0.0);
            chunk.Write(20, 0.0);

            // dxf AutoCad2000 does not support hatch gradient patterns
            if (doc.DrawingVariables.AcadVer <= DxfVersion.AutoCad2000)
                return;

            HatchGradientPattern gradientPattern = pattern as HatchGradientPattern;
            if (gradientPattern != null)
                WriteGradientHatchPattern(gradientPattern);
        }

        private void WriteGradientHatchPattern(HatchGradientPattern pattern)
        {
            // again the order of codes shown in the documentation will not work
            chunk.Write(450, 1);
            chunk.Write(451, 0);
            chunk.Write(460, pattern.Angle*MathHelper.DegToRad);
            chunk.Write(461, pattern.Centered ? 0.0 : 1.0);
            chunk.Write(452, pattern.SingleColor ? 1 : 0);
            chunk.Write(462, pattern.Tint);
            chunk.Write(453, 2);
            chunk.Write(463, 0.0);
            chunk.Write(63, pattern.Color1.Index);
            chunk.Write(421, AciColor.ToTrueColor(pattern.Color1));
            chunk.Write(463, 1.0);
            chunk.Write(63, pattern.Color2.Index);
            chunk.Write(421, AciColor.ToTrueColor(pattern.Color2));
            chunk.Write(470, StringEnum.GetStringValue(pattern.GradientType));
        }

        private void WriteHatchPatternDefinitonLines(HatchPattern pattern)
        {
            foreach (HatchPatternLineDefinition line in pattern.LineDefinitions)
            {
                double scale = pattern.Scale;
                double angle = line.Angle + pattern.Angle;
                // Pattern fill data.
                // In theory this should hold the same information as the pat file but for unknown reason the dxf requires global data instead of local,
                // it's a guess the documentation is kinda obscure.
                // This means we have to apply the pattern rotation and scale to the line definitions
                chunk.Write(53, angle);

                double sinOrigin = Math.Sin(pattern.Angle*MathHelper.DegToRad);
                double cosOrigin = Math.Cos(pattern.Angle*MathHelper.DegToRad);
                Vector2 origin = new Vector2(cosOrigin*line.Origin.X*scale - sinOrigin*line.Origin.Y*scale, sinOrigin*line.Origin.X*scale + cosOrigin*line.Origin.Y*scale);
                chunk.Write(43, origin.X);
                chunk.Write(44, origin.Y);

                double sinDelta = Math.Sin(angle*MathHelper.DegToRad);
                double cosDelta = Math.Cos(angle*MathHelper.DegToRad);
                Vector2 delta = new Vector2(cosDelta*line.Delta.X*scale - sinDelta*line.Delta.Y*scale, sinDelta*line.Delta.X*scale + cosDelta*line.Delta.Y*scale);
                chunk.Write(45, delta.X);
                chunk.Write(46, delta.Y);

                chunk.Write(79, (short) line.DashPattern.Count);
                foreach (double dash in line.DashPattern)
                {
                    chunk.Write(49, dash*scale);
                }
            }
        }

        private void WriteDimension(Dimension dim)
        {
            chunk.Write(100, SubclassMarker.Dimension);

            if(dim.Block != null)
                chunk.Write(2, EncodeNonAsciiCharacters(dim.Block.Name));

            Vector3 ocsDef = new Vector3(dim.DefinitionPoint.X, dim.DefinitionPoint.Y, dim.Elevation);
            Vector3 wcsDef = MathHelper.Transform(ocsDef, dim.Normal, CoordinateSystem.Object, CoordinateSystem.World);
            chunk.Write(10, wcsDef.X);
            chunk.Write(20, wcsDef.Y);
            chunk.Write(30, wcsDef.Z);
            chunk.Write(11, dim.TextReferencePoint.X);
            chunk.Write(21, dim.TextReferencePoint.Y);
            chunk.Write(31, dim.Elevation);

            DimensionTypeFlags flags = (DimensionTypeFlags) dim.DimensionType;
            flags |= DimensionTypeFlags.BlockReference;
            if (dim.TextPositionManuallySet) flags |= DimensionTypeFlags.UserTextPosition;

            OrdinateDimension ordinateDim = dim as OrdinateDimension;
            if (ordinateDim != null)
            {
                // even if the documentation says that code 51 is optional, rotated ordinate dimensions will not work correctly if this value is not provided
                chunk.Write(51, 360.0 - ordinateDim.Rotation);
                if (ordinateDim.Axis == OrdinateDimensionAxis.X) flags |= DimensionTypeFlags.OrdinateType;
            }
            chunk.Write(53, dim.TextRotation);
            chunk.Write(70, (short) flags);
            chunk.Write(71, (short) dim.AttachmentPoint);
            chunk.Write(72, (short) dim.LineSpacingStyle);
            chunk.Write(41, dim.LineSpacingFactor);
            if (dim.UserText != null)
                chunk.Write(1, EncodeNonAsciiCharacters(dim.UserText));
            chunk.Write(210, dim.Normal.X);
            chunk.Write(220, dim.Normal.Y);
            chunk.Write(230, dim.Normal.Z);

            chunk.Write(3, EncodeNonAsciiCharacters(dim.Style.Name));

            // add dimension style overrides info
            if (dim.StyleOverrides.Count > 0)
                AddDimensionStyleOverridesXData(dim.XData, dim.StyleOverrides);

            switch (dim.DimensionType)
            {
                case DimensionType.Aligned:
                    WriteAlignedDimension((AlignedDimension) dim);
                    break;
                case DimensionType.Linear:
                    WriteLinearDimension((LinearDimension) dim);
                    break;
                case DimensionType.Radius:
                    WriteRadialDimension((RadialDimension) dim);
                    break;
                case DimensionType.Diameter:
                    WriteDiametricDimension((DiametricDimension) dim);
                    break;
                case DimensionType.Angular3Point:
                    WriteAngular3PointDimension((Angular3PointDimension) dim);
                    break;
                case DimensionType.Angular:
                    WriteAngular2LineDimension((Angular2LineDimension) dim);
                    break;
                case DimensionType.Ordinate:
                    WriteOrdinateDimension((OrdinateDimension) dim);
                    break;
            }
        }

        private void AddDimensionStyleOverridesXData(XDataDictionary xdata, DimensionStyleOverrideDictionary overrides)
        {
            bool writeDIMPOST = false;
            string prefix = string.Empty;
            string suffix = string.Empty;
            bool writeDIMSAH = false;
            bool writeDIMZIN = false;
            bool writeDIMAZIN = false;
            bool suppressLinearLeadingZeros = false;
            bool suppressLinearTrailingZeros = false;
            bool suppressAngularLeadingZeros = false;
            bool suppressAngularTrailingZeros = false;
            bool suppressZeroFeet = true;
            bool suppressZeroInches = true;

            bool writeDIMALTU = false;
            LinearUnitType altLinearUnitType = LinearUnitType.Decimal;
            bool altStackedUnits = false;
            bool writeDIMAPOST = false;
            string altPrefix = string.Empty;
            string altSuffix = string.Empty;
            bool writeDIMALTZ = false;
            bool altSuppressLinearLeadingZeros = false;
            bool altSuppressLinearTrailingZeros = false;
            bool altSuppressZeroFeet = true;
            bool altSuppressZeroInches = true;

            bool writeDIMTOL = false;
            double dimtm = 0;
            DimensionStyleTolerancesDisplayMethod dimtol = DimensionStyleTolerancesDisplayMethod.None;

            bool writeDIMTZIN = false;
            bool tolSuppressLinearLeadingZeros = false;
            bool tolSuppressLinearTrailingZeros = false;
            bool tolSuppressZeroFeet = true;
            bool tolSuppressZeroInches = true;

            bool writeDIMALTTZ = false;
            bool tolAltSuppressLinearLeadingZeros = false;
            bool tolAltSuppressLinearTrailingZeros = false;
            bool tolAltSuppressZeroFeet = true;
            bool tolAltSuppressZeroInches = true;

            XData xdataEntry;
            if (xdata.ContainsAppId(ApplicationRegistry.DefaultName))
            {
                xdataEntry = xdata[ApplicationRegistry.DefaultName];
                xdataEntry.XDataRecord.Clear();
            }
            else
            {
                xdataEntry = new XData(new ApplicationRegistry(ApplicationRegistry.DefaultName));
                xdata.Add(xdataEntry);
            }

            xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.String, "DSTYLE"));
            xdataEntry.XDataRecord.Add(XDataRecord.OpenControlString);

            foreach (DimensionStyleOverride styleOverride in overrides.Values)
            {
                switch (styleOverride.Type)
                {
                    case DimensionStyleOverrideType.DimLineColor:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 176));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, ((AciColor) styleOverride.Value).Index));
                        break;
                    case DimensionStyleOverrideType.DimLineLinetype:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 345));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.DatabaseHandle, ((Linetype) styleOverride.Value).Handle));
                        break;
                    case DimensionStyleOverrideType.DimLineLineweight:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 371));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) (Lineweight) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.DimLine1Off:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 281));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (bool) styleOverride.Value ? (short) 1 : (short) 0));
                        break;
                    case DimensionStyleOverrideType.DimLine2Off:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 282));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (bool) styleOverride.Value ? (short) 1 : (short) 0));
                        break;
                    case DimensionStyleOverrideType.DimLineExtend:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 46));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Real, (double) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.ExtLineColor:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 177));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, ((AciColor) styleOverride.Value).Index));
                        break;
                    case DimensionStyleOverrideType.ExtLine1Linetype:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 346));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.DatabaseHandle, ((Linetype) styleOverride.Value).Handle));
                        break;
                    case DimensionStyleOverrideType.ExtLine2Linetype:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 347));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.DatabaseHandle, ((Linetype) styleOverride.Value).Handle));
                        break;
                    case DimensionStyleOverrideType.ExtLineLineweight:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 372));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) (Lineweight) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.ExtLine1Off:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 75));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (bool) styleOverride.Value ? (short) 1 : (short) 0));
                        break;
                    case DimensionStyleOverrideType.ExtLine2Off:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 76));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16,(bool) styleOverride.Value ? (short) 1 : (short) 0));
                        break;
                    case DimensionStyleOverrideType.ExtLineOffset:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 42));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Real, (double) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.ExtLineExtend:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 44));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Real, (double) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.ExtLineFixed:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 290));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (bool) styleOverride.Value ? (short) 1 : (short) 0));
                        break;
                    case DimensionStyleOverrideType.ExtLineFixedLength:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 49));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Real, (double) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.ArrowSize:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 41));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Real, (double) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.CenterMarkSize:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 141));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Real, (double) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.LeaderArrow:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 341));
                        xdataEntry.XDataRecord.Add(styleOverride.Value != null
                            ? new XDataRecord(XDataCode.DatabaseHandle, ((Block) styleOverride.Value).Record.Handle)
                            : new XDataRecord(XDataCode.DatabaseHandle, "0"));
                        break;
                    case DimensionStyleOverrideType.DimArrow1:
                        writeDIMSAH = true;
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 343));
                        xdataEntry.XDataRecord.Add(styleOverride.Value != null
                            ? new XDataRecord(XDataCode.DatabaseHandle, ((Block) styleOverride.Value).Record.Handle)
                            : new XDataRecord(XDataCode.DatabaseHandle, "0"));
                        break;
                    case DimensionStyleOverrideType.DimArrow2:
                        writeDIMSAH = true;
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 344));
                        xdataEntry.XDataRecord.Add(styleOverride.Value != null
                            ? new XDataRecord(XDataCode.DatabaseHandle, ((Block) styleOverride.Value).Record.Handle)
                            : new XDataRecord(XDataCode.DatabaseHandle, "0"));
                        break;
                    case DimensionStyleOverrideType.TextStyle:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 340));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.DatabaseHandle, ((TextStyle) styleOverride.Value).Handle));
                        break;
                    case DimensionStyleOverrideType.TextColor:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 178));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, ((AciColor) styleOverride.Value).Index));
                        break;
                    case DimensionStyleOverrideType.TextFillColor:
                        if (styleOverride.Value != null)
                        {
                            xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 70));
                            xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, ((AciColor) styleOverride.Value).Index));

                            xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 69));
                            xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 2));
                        }
                        else
                        {
                            xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 69));
                            xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 0));
                        }
                        break;
                    case DimensionStyleOverrideType.TextHeight:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 140));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Real, (double) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.TextOffset:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 147));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Real, (double) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.TextVerticalPlacement:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 77));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short)(DimensionStyleTextVerticalPlacement) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.TextHorizontalPlacement:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 280));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) (DimensionStyleTextHorizontalPlacement) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.TextInsideAlign:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 73));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (bool) styleOverride.Value ? (short) 1 : (short) 0));
                        break;
                    case DimensionStyleOverrideType.TextOutsideAlign:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 74));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (bool) styleOverride.Value ? (short) 1 : (short) 0));
                        break;
                    case DimensionStyleOverrideType.TextDirection:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 294));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) (DimensionStyleTextDirection) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.FitDimLineForce:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 172));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (bool) styleOverride.Value ? (short) 1 : (short) 0));
                        break;
                    case DimensionStyleOverrideType.FitDimLineInside:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 175));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (bool) styleOverride.Value ? (short) 1 : (short) 0));
                        break;
                    case DimensionStyleOverrideType.DimScaleOverall:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 40));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Real, (double) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.FitOptions:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 289));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.FitTextInside:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 174));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16,
                            (bool) styleOverride.Value ? (short) 1 : (short) 0));
                        break;
                    case DimensionStyleOverrideType.FitTextMove:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 279));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) (DimensionStyleFitOptions) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.AngularPrecision:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 179));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.LengthPrecision:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 271));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.DimPrefix:
                        writeDIMPOST = true;
                        prefix = (string) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.DimSuffix:
                        writeDIMPOST = true;
                        suffix = (string) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.DecimalSeparator:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 278));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) (char) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.DimScaleLinear:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 144));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Real, (double) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.DimLengthUnits:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 277));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) (LinearUnitType) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.DimAngularUnits:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 275));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) (AngleUnitType) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.FractionalType:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 276));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) (FractionFormatType) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.SuppressZeroFeet:
                        writeDIMZIN = true;
                        suppressZeroFeet = (bool) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.SuppressZeroInches:
                        writeDIMZIN = true;
                        suppressZeroInches = (bool) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.SuppressLinearLeadingZeros:
                        writeDIMZIN = true;
                        suppressLinearLeadingZeros = (bool) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.SuppressLinearTrailingZeros:
                        writeDIMZIN = true;
                        suppressLinearTrailingZeros = (bool) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.SuppressAngularLeadingZeros:
                        writeDIMAZIN = true;
                        suppressAngularLeadingZeros = (bool) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.SuppressAngularTrailingZeros:
                        writeDIMAZIN = true;
                        suppressAngularTrailingZeros = (bool) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.DimRoundoff:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 45));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Real, (double) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.AltUnitsEnabled:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 170));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.AltUnitsLengthUnits:
                        writeDIMALTU = true;
                        altLinearUnitType = (LinearUnitType) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.AltUnitsStackedUnits:
                        altStackedUnits = (bool) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.AltUnitsLengthPrecision:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 171));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.AltUnitsMultiplier:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 143));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Real, (double) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.AltUnitsRoundoff:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 148));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Real, (double) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.AltUnitsPrefix:
                        writeDIMAPOST = true;
                        altPrefix = (string) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.AltUnitsSuffix:
                        writeDIMPOST = true;
                        altSuffix = (string) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.AltUnitsSuppressLinearLeadingZeros:
                        writeDIMALTZ = true;
                        altSuppressLinearLeadingZeros = (bool) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.AltUnitsSuppressLinearTrailingZeros:
                        writeDIMALTZ = true;
                        altSuppressLinearTrailingZeros = (bool) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.AltUnitsSuppressZeroFeet:
                        writeDIMALTZ = true;
                        altSuppressZeroFeet = (bool) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.AltUnitsSuppressZeroInches:
                        writeDIMALTZ = true;
                        altSuppressZeroInches = (bool) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.TolerancesDisplayMethod:
                        dimtol = (DimensionStyleTolerancesDisplayMethod) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.TolerancesUpperLimit:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 47));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Real, (double) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.TolerancesLowerLimit:
                        dimtm = (double) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.TolerancesVerticalPlacement:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 283));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) (DimensionStyleTolerancesVerticalPlacement) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.TolerancesPrecision:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 272));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.TolerancesSuppressLinearLeadingZeros:
                        writeDIMTZIN = true;
                        tolSuppressLinearLeadingZeros = (bool) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.TolerancesSuppressLinearTrailingZeros:
                        writeDIMTZIN = true;
                        tolSuppressLinearTrailingZeros = (bool) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.TolerancesSuppressZeroFeet:
                        writeDIMTZIN = true;
                        tolSuppressZeroFeet = (bool) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.TolerancesSuppressZeroInches:
                        writeDIMTZIN = true;
                        tolSuppressZeroInches = (bool) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.TextFractionHeightScale:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 146));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Real, (double) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.TolerancesAlternatePrecision:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 274));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) styleOverride.Value));
                        break;
                    case DimensionStyleOverrideType.TolerancesAltSuppressLinearLeadingZeros:
                        writeDIMALTTZ = true;
                        tolAltSuppressLinearLeadingZeros = (bool) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.TolerancesAltSuppressLinearTrailingZeros:
                        writeDIMALTTZ = true;
                        tolAltSuppressLinearTrailingZeros = (bool) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.TolerancesAltSuppressZeroFeet:
                        writeDIMALTTZ = true;
                        tolAltSuppressZeroFeet = (bool) styleOverride.Value;
                        break;
                    case DimensionStyleOverrideType.TolerancesAltSuppressZeroInches:
                        writeDIMALTTZ = true;
                        tolAltSuppressZeroInches = (bool) styleOverride.Value;
                        break;
                }
            }

            if (writeDIMSAH)
            {
                xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 173));
                xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 1));
            }

            if (writeDIMPOST)
            {
                xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 3));
                xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.String,
                    EncodeNonAsciiCharacters(string.Format("{0}<>{1}", prefix, suffix))));
            }

            if (writeDIMZIN)
            {
                xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 78));
                xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16,
                    GetSupressZeroesValue(suppressLinearLeadingZeros, suppressLinearTrailingZeros, suppressZeroFeet,
                        suppressZeroInches)));
            }

            if (writeDIMAZIN)
            {
                short angSupress = 3;
                if (suppressAngularLeadingZeros && suppressAngularTrailingZeros)
                    angSupress = 3;
                else if (!suppressAngularLeadingZeros && !suppressAngularTrailingZeros)
                    angSupress = 0;
                else if (!suppressAngularLeadingZeros && suppressAngularTrailingZeros)
                    angSupress = 2;
                else if (suppressAngularLeadingZeros && !suppressAngularTrailingZeros)
                    angSupress = 1;

                xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 79));
                xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, angSupress));
            }

            // alternate units
            if (writeDIMAPOST)
            {
                xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 4));
                xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.String,
                    EncodeNonAsciiCharacters(string.Format("{0}[]{1}", altPrefix, altSuffix))));
            }

            if (writeDIMALTU)
            {
                xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 273));
                switch (altLinearUnitType)
                {
                    case LinearUnitType.Scientific:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 1));
                        break;
                    case LinearUnitType.Decimal:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 2));
                        break;
                    case LinearUnitType.Engineering:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 3));
                        break;
                    case LinearUnitType.Architectural:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16,
                            altStackedUnits ? (short) 4 : (short) 6));
                        break;
                    case LinearUnitType.Fractional:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16,
                            altStackedUnits ? (short) 5 : (short) 7));
                        break;
                }
            }

            if (writeDIMALTZ)
            {
                xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 285));
                xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16,
                    GetSupressZeroesValue(altSuppressLinearLeadingZeros, altSuppressLinearTrailingZeros,
                        altSuppressZeroFeet, altSuppressZeroInches)));
            }

            // tolerances
            if (writeDIMTOL)
            { 
                switch (dimtol)
                {
                    case DimensionStyleTolerancesDisplayMethod.None:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 71));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (double) 0));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 72));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (double) 0));
                        break;
                    case DimensionStyleTolerancesDisplayMethod.Symmetrical:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 71));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (double) 1));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 72));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (double) 0));
                        break;
                    case DimensionStyleTolerancesDisplayMethod.Deviation:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 48));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Real, MathHelper.IsZero(dimtm) ? MathHelper.Epsilon : dimtm));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 71));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (double) 1));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 72));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (double) 0));
                        break;
                    case DimensionStyleTolerancesDisplayMethod.Limits:
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 48));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Real, dimtm));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 71));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (double) 0));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 72));
                        xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (double) 1));
                        break;
                }
            }

            if (writeDIMTZIN)
            {
                xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 284));
                xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16,
                    GetSupressZeroesValue(tolSuppressLinearLeadingZeros, tolSuppressLinearTrailingZeros, tolSuppressZeroFeet, tolSuppressZeroInches)));
            }

            if (writeDIMALTTZ)
            {
                xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16, (short) 286));
                xdataEntry.XDataRecord.Add(new XDataRecord(XDataCode.Int16,
                    GetSupressZeroesValue(tolAltSuppressLinearLeadingZeros, tolAltSuppressLinearTrailingZeros, tolAltSuppressZeroFeet, tolAltSuppressZeroInches)));
            }

            xdataEntry.XDataRecord.Add(XDataRecord.CloseControlString);
        }

        private void WriteAlignedDimension(AlignedDimension dim)
        {
            chunk.Write(100, SubclassMarker.AlignedDimension);

            IList<Vector3> wcsPoints = MathHelper.Transform(
                new[]
                {
                    new Vector3(dim.FirstReferencePoint.X, dim.FirstReferencePoint.Y, dim.Elevation),
                    new Vector3(dim.SecondReferencePoint.X, dim.SecondReferencePoint.Y, dim.Elevation)
                },
                dim.Normal, CoordinateSystem.Object, CoordinateSystem.World);

            chunk.Write(13, wcsPoints[0].X);
            chunk.Write(23, wcsPoints[0].Y);
            chunk.Write(33, wcsPoints[0].Z);

            chunk.Write(14, wcsPoints[1].X);
            chunk.Write(24, wcsPoints[1].Y);
            chunk.Write(34, wcsPoints[1].Z);

            WriteXData(dim.XData);
        }

        private void WriteLinearDimension(LinearDimension dim)
        {
            chunk.Write(100, SubclassMarker.AlignedDimension);

            IList<Vector3> wcsPoints = MathHelper.Transform(
                new[]
                {
                    new Vector3(dim.FirstReferencePoint.X, dim.FirstReferencePoint.Y, dim.Elevation),
                    new Vector3(dim.SecondReferencePoint.X, dim.SecondReferencePoint.Y, dim.Elevation)
                },
                dim.Normal, CoordinateSystem.Object, CoordinateSystem.World);

            chunk.Write(13, wcsPoints[0].X);
            chunk.Write(23, wcsPoints[0].Y);
            chunk.Write(33, wcsPoints[0].Z);

            chunk.Write(14, wcsPoints[1].X);
            chunk.Write(24, wcsPoints[1].Y);
            chunk.Write(34, wcsPoints[1].Z);

            chunk.Write(50, dim.Rotation);

            // AutoCAD is unable to recognized code 52 for oblique dimension line even though it appears as valid in the dxf documentation
            // this.chunk.Write(52, dim.ObliqueAngle);

            chunk.Write(100, SubclassMarker.LinearDimension);

            WriteXData(dim.XData);
        }

        private void WriteRadialDimension(RadialDimension dim)
        {
            chunk.Write(100, SubclassMarker.RadialDimension);

            Vector3 wcsPoint = MathHelper.Transform(new Vector3(dim.ReferencePoint.X, dim.ReferencePoint.Y, dim.Elevation), dim.Normal, CoordinateSystem.Object, CoordinateSystem.World);
            chunk.Write(15, wcsPoint.X);
            chunk.Write(25, wcsPoint.Y);
            chunk.Write(35, wcsPoint.Z);

            chunk.Write(40, 0.0);

            WriteXData(dim.XData);
        }

        private void WriteDiametricDimension(DiametricDimension dim)
        {
            chunk.Write(100, SubclassMarker.DiametricDimension);

            Vector3 wcsPoint = MathHelper.Transform(new Vector3(dim.ReferencePoint.X, dim.ReferencePoint.Y, dim.Elevation), dim.Normal, CoordinateSystem.Object, CoordinateSystem.World);
            chunk.Write(15, wcsPoint.X);
            chunk.Write(25, wcsPoint.Y);
            chunk.Write(35, wcsPoint.Z);

            chunk.Write(40, 0.0);

            WriteXData(dim.XData);
        }

        private void WriteAngular3PointDimension(Angular3PointDimension dim)
        {
            chunk.Write(100, SubclassMarker.Angular3PointDimension);

            IList<Vector3> wcsPoints = MathHelper.Transform(
                new[]
                {
                    new Vector3(dim.StartPoint.X, dim.StartPoint.Y, dim.Elevation),
                    new Vector3(dim.EndPoint.X, dim.EndPoint.Y, dim.Elevation),
                    new Vector3(dim.CenterPoint.X, dim.CenterPoint.Y, dim.Elevation)
                },
                dim.Normal, CoordinateSystem.Object, CoordinateSystem.World);

            chunk.Write(13, wcsPoints[0].X);
            chunk.Write(23, wcsPoints[0].Y);
            chunk.Write(33, wcsPoints[0].Z);

            chunk.Write(14, wcsPoints[1].X);
            chunk.Write(24, wcsPoints[1].Y);
            chunk.Write(34, wcsPoints[1].Z);

            chunk.Write(15, wcsPoints[2].X);
            chunk.Write(25, wcsPoints[2].Y);
            chunk.Write(35, wcsPoints[2].Z);

            chunk.Write(40, 0.0);

            WriteXData(dim.XData);
        }

        private void WriteAngular2LineDimension(Angular2LineDimension dim)
        {
            chunk.Write(100, SubclassMarker.Angular2LineDimension);

            IList<Vector3> wcsPoints = MathHelper.Transform(
                new[]
                {
                    new Vector3(dim.StartFirstLine.X, dim.StartFirstLine.Y, dim.Elevation),
                    new Vector3(dim.EndFirstLine.X, dim.EndFirstLine.Y, dim.Elevation),
                    new Vector3(dim.StartSecondLine.X, dim.StartSecondLine.Y, dim.Elevation)
                },
                dim.Normal, CoordinateSystem.Object, CoordinateSystem.World);

            chunk.Write(13, wcsPoints[0].X);
            chunk.Write(23, wcsPoints[0].Y);
            chunk.Write(33, wcsPoints[0].Z);

            chunk.Write(14, wcsPoints[1].X);
            chunk.Write(24, wcsPoints[1].Y);
            chunk.Write(34, wcsPoints[1].Z);

            chunk.Write(15, wcsPoints[2].X);
            chunk.Write(25, wcsPoints[2].Y);
            chunk.Write(35, wcsPoints[2].Z);

            chunk.Write(16, dim.ArcDefinitionPoint.X);
            chunk.Write(26, dim.ArcDefinitionPoint.Y);
            chunk.Write(36, dim.Elevation);

            chunk.Write(40, 0.0);

            WriteXData(dim.XData);
        }

        private void WriteOrdinateDimension(OrdinateDimension dim)
        {
            chunk.Write(100, SubclassMarker.OrdinateDimension);

            IList<Vector3> wcsPoints = MathHelper.Transform(
                new[]
                {
                    new Vector3(dim.FeaturePoint.X, dim.FeaturePoint.Y, dim.Elevation),
                    new Vector3(dim.LeaderEndPoint.X, dim.LeaderEndPoint.Y, dim.Elevation)
                },
                dim.Normal, CoordinateSystem.Object, CoordinateSystem.World);

            chunk.Write(13, wcsPoints[0].X);
            chunk.Write(23, wcsPoints[0].Y);
            chunk.Write(33, wcsPoints[0].Z);

            chunk.Write(14, wcsPoints[1].X);
            chunk.Write(24, wcsPoints[1].Y);
            chunk.Write(34, wcsPoints[1].Z);

            WriteXData(dim.XData);
        }

        private void WriteImage(Image image)
        {
            chunk.Write(100, SubclassMarker.RasterImage);

            chunk.Write(10, image.Position.X);
            chunk.Write(20, image.Position.Y);
            chunk.Write(30, image.Position.Z);

            //Vector2 u = new Vector2(image.Width/image.Definition.Width, 0.0);
            //Vector2 v = new Vector2(0.0, image.Height/image.Definition.Height);
            //IList<Vector2> ocsUV = MathHelper.Transform(new List<Vector2> {u, v}, image.Rotation*MathHelper.DegToRad, CoordinateSystem.Object, CoordinateSystem.World);

            //Vector2 u = image.Uvector * (image.Width / image.Definition.Width);
            //Vector2 v = image.Vvector * (image.Height / image.Definition.Height);
            //IList<Vector2> ocsUV = MathHelper.Transform(new List<Vector2> { u, v }, image.Rotation * MathHelper.DegToRad, CoordinateSystem.Object, CoordinateSystem.World);

            //Vector3 ocsU = new Vector3(ocsUV[0].X, ocsUV[0].Y, 0.0);
            //Vector3 ocsV = new Vector3(ocsUV[1].X, ocsUV[1].Y, 0.0);
            //IList<Vector3> wcsUV = MathHelper.Transform(new List<Vector3> {ocsU, ocsV}, image.Normal, CoordinateSystem.Object, CoordinateSystem.World);

            Vector2 u = image.Uvector * (image.Width / image.Definition.Width);
            Vector2 v = image.Vvector * (image.Height / image.Definition.Height);

            Vector3 ocsU = new Vector3(u.X, u.Y, 0.0);
            Vector3 ocsV = new Vector3(v.X, v.Y, 0.0);
            IList<Vector3> wcsUV = MathHelper.Transform(new List<Vector3> { ocsU, ocsV },
                image.Normal,
                CoordinateSystem.Object,
                CoordinateSystem.World);

            double factor = UnitHelper.ConversionFactor(doc.RasterVariables.Units, doc.DrawingVariables.InsUnits);

            Vector3 wcsU = wcsUV[0]*factor;
            chunk.Write(11, wcsU.X);
            chunk.Write(21, wcsU.Y);
            chunk.Write(31, wcsU.Z);

            Vector3 wcsV = wcsUV[1]*factor;
            chunk.Write(12, wcsV.X);
            chunk.Write(22, wcsV.Y);
            chunk.Write(32, wcsV.Z);

            chunk.Write(13, (double) image.Definition.Width);
            chunk.Write(23, (double) image.Definition.Height);

            chunk.Write(340, image.Definition.Handle);

            chunk.Write(70, (short) image.DisplayOptions);
            chunk.Write(280, image.Clipping ? (short) 1 : (short) 0);
            chunk.Write(281, image.Brightness);
            chunk.Write(282, image.Contrast);
            chunk.Write(283, image.Fade);
            chunk.Write(360, image.Definition.Reactors[image.Handle].Handle);

            chunk.Write(71, (short) image.ClippingBoundary.Type);
            if (image.ClippingBoundary.Type == ClippingBoundaryType.Rectangular)
            {
                chunk.Write(91, image.ClippingBoundary.Vertexes.Count);
                foreach (Vector2 vertex in image.ClippingBoundary.Vertexes)
                {
                    chunk.Write(14, vertex.X-0.5);
                    chunk.Write(24, vertex.Y-0.5);
                }
            }
            else
            {
                // for polygonal clipping boundaries the last vertex must be duplicated
                chunk.Write(91, image.ClippingBoundary.Vertexes.Count+1);
                foreach (Vector2 vertex in image.ClippingBoundary.Vertexes)
                {
                    chunk.Write(14, vertex.X - 0.5);
                    chunk.Write(24, vertex.Y - 0.5);
                }
                chunk.Write(14, image.ClippingBoundary.Vertexes[0].X - 0.5);
                chunk.Write(24, image.ClippingBoundary.Vertexes[0].Y - 0.5);
            }

            WriteXData(image.XData);
        }

        private void WriteMLine(MLine mLine)
        {
            chunk.Write(100, SubclassMarker.MLine);

            chunk.Write(2, EncodeNonAsciiCharacters(mLine.Style.Name));

            chunk.Write(340, mLine.Style.Handle);

            chunk.Write(40, mLine.Scale);
            chunk.Write(70, (short) mLine.Justification);
            chunk.Write(71, (short) mLine.Flags);
            chunk.Write(72, (short) mLine.Vertexes.Count);
            chunk.Write(73, (short) mLine.Style.Elements.Count);

            // the MLine information is in OCS we need to save it in WCS
            // this behavior is similar to the LWPolyline, the info is in OCS because these entities are strictly 2d. Normally they are used in the XY plane whose
            // normal is (0, 0, 1) so no transformation is needed, OCS are equal to WCS
            List<Vector3> ocsVertexes = new List<Vector3>();
            foreach (MLineVertex segment in mLine.Vertexes)
            {
                ocsVertexes.Add(new Vector3(segment.Position.X, segment.Position.Y, mLine.Elevation));
            }
            IList<Vector3> vertexes = MathHelper.Transform(ocsVertexes, mLine.Normal, CoordinateSystem.Object, CoordinateSystem.World);

            Vector3[] wcsVertexes = new Vector3[vertexes.Count];
            vertexes.CopyTo(wcsVertexes, 0);

            // Although it is not recommended the vertex list might have 0 entries
            if (wcsVertexes.Length == 0)
            {
                chunk.Write(10, 0.0);
                chunk.Write(20, 0.0);
                chunk.Write(30, 0.0);
            }
            else
            {
                chunk.Write(10, wcsVertexes[0].X);
                chunk.Write(20, wcsVertexes[0].Y);
                chunk.Write(30, wcsVertexes[0].Z);
            }

            chunk.Write(210, mLine.Normal.X);
            chunk.Write(220, mLine.Normal.Y);
            chunk.Write(230, mLine.Normal.Z);

            for (int i = 0; i < wcsVertexes.Length; i++)
            {
                chunk.Write(11, wcsVertexes[i].X);
                chunk.Write(21, wcsVertexes[i].Y);
                chunk.Write(31, wcsVertexes[i].Z);

                // the directions are written in world coordinates
                Vector2 dir = mLine.Vertexes[i].Direction;
                Vector3 wcsDir = MathHelper.Transform(new Vector3(dir.X, dir.Y, mLine.Elevation), mLine.Normal, CoordinateSystem.Object, CoordinateSystem.World);
                chunk.Write(12, wcsDir.X);
                chunk.Write(22, wcsDir.Y);
                chunk.Write(32, wcsDir.Z);
                Vector2 mitter = mLine.Vertexes[i].Miter;
                Vector3 wcsMitter = MathHelper.Transform(new Vector3(mitter.X, mitter.Y, mLine.Elevation), mLine.Normal, CoordinateSystem.Object, CoordinateSystem.World);
                chunk.Write(13, wcsMitter.X);
                chunk.Write(23, wcsMitter.Y);
                chunk.Write(33, wcsMitter.Z);

                foreach (List<double> distances in mLine.Vertexes[i].Distances)
                {
                    chunk.Write(74, (short) distances.Count);
                    foreach (double distance in distances)
                    {
                        chunk.Write(41, distance);
                    }
                    chunk.Write(75, (short) 0);
                }
            }

            WriteXData(mLine.XData);
        }

        private void WriteAttributeDefinition(AttributeDefinition def, Layout layout)
        {
            chunk.Write(0, def.CodeName);
            chunk.Write(5, def.Handle);

            //if (def.Reactors.Count > 0)
            //{
            //    this.chunk.Write(102, "{ACAD_REACTORS");
            //    foreach (DxfObject o in def.Reactors)
            //    {
            //        if (!string.IsNullOrEmpty(o.Handle)) this.chunk.Write(330, o.Handle);
            //    }
            //    this.chunk.Write(102, "}");
            //}

            chunk.Write(330, def.Owner.Record.Handle);

            chunk.Write(100, SubclassMarker.Entity);

            if (layout != null)
                chunk.Write(67, layout.IsPaperSpace ? (short)1 : (short)0);

            chunk.Write(8, EncodeNonAsciiCharacters(def.Layer.Name));

            chunk.Write(62, def.Color.Index);
            if (def.Color.UseTrueColor)
                chunk.Write(420, AciColor.ToTrueColor(def.Color));

            if (def.Transparency.Value >= 0)
                chunk.Write(440, Transparency.ToAlphaValue(def.Transparency));

            chunk.Write(6, EncodeNonAsciiCharacters(def.Linetype.Name));

            chunk.Write(370, (short)def.Lineweight);
            chunk.Write(48, def.LinetypeScale);
            chunk.Write(60, def.IsVisible ? (short)0 : (short)1);

            chunk.Write(100, SubclassMarker.Text);

            Vector3 ocsInsertion = MathHelper.Transform(def.Position, def.Normal, CoordinateSystem.World, CoordinateSystem.Object);

            chunk.Write(10, ocsInsertion.X);
            chunk.Write(20, ocsInsertion.Y);
            chunk.Write(30, ocsInsertion.Z);

            chunk.Write(40, def.Height);

            object value = def.Value;
            if (value == null)
                chunk.Write(1, string.Empty);
            else if (value is string)
                chunk.Write(1, EncodeNonAsciiCharacters((string) value));
            else
                chunk.Write(1, value.ToString());

            switch (def.Alignment)
            {
                case TextAlignment.TopLeft:
                    chunk.Write(72, (short) 0);
                    break;
                case TextAlignment.TopCenter:
                    chunk.Write(72, (short) 1);
                    break;
                case TextAlignment.TopRight:
                    chunk.Write(72, (short) 2);
                    break;
                case TextAlignment.MiddleLeft:
                    chunk.Write(72, (short) 0);
                    break;
                case TextAlignment.MiddleCenter:
                    chunk.Write(72, (short) 1);
                    break;
                case TextAlignment.MiddleRight:
                    chunk.Write(72, (short) 2);
                    break;
                case TextAlignment.BottomLeft:
                    chunk.Write(72, (short) 0);
                    break;
                case TextAlignment.BottomCenter:
                    chunk.Write(72, (short) 1);
                    break;
                case TextAlignment.BottomRight:
                    chunk.Write(72, (short) 2);
                    break;
                case TextAlignment.BaselineLeft:
                    chunk.Write(72, (short) 0);
                    break;
                case TextAlignment.BaselineCenter:
                    chunk.Write(72, (short) 1);
                    break;
                case TextAlignment.BaselineRight:
                    chunk.Write(72, (short) 2);
                    break;
                case TextAlignment.Aligned:
                    chunk.Write(72, (short) 3);
                    break;
                case TextAlignment.Middle:
                    chunk.Write(72, (short) 4);
                    break;
                case TextAlignment.Fit:
                    chunk.Write(72, (short) 5);
                    break;
            }

            chunk.Write(50, def.Rotation);
            chunk.Write(51, def.ObliqueAngle);
            chunk.Write(41, def.WidthFactor);

            chunk.Write(7, EncodeNonAsciiCharacters(def.Style.Name));

            chunk.Write(11, def.Position.X);
            chunk.Write(21, def.Position.Y);
            chunk.Write(31, def.Position.Z);

            chunk.Write(210, def.Normal.X);
            chunk.Write(220, def.Normal.Y);
            chunk.Write(230, def.Normal.Z);

            chunk.Write(100, SubclassMarker.AttributeDefinition);

            chunk.Write(3, EncodeNonAsciiCharacters(def.Prompt));

            chunk.Write(2, EncodeNonAsciiCharacters(def.Tag));

            chunk.Write(70, (short) def.Flags);

            switch (def.Alignment)
            {
                case TextAlignment.TopLeft:
                    chunk.Write(74, (short) 3);
                    break;
                case TextAlignment.TopCenter:
                    chunk.Write(74, (short) 3);
                    break;
                case TextAlignment.TopRight:
                    chunk.Write(74, (short) 3);
                    break;
                case TextAlignment.MiddleLeft:
                    chunk.Write(74, (short) 2);
                    break;
                case TextAlignment.MiddleCenter:
                    chunk.Write(74, (short) 2);
                    break;
                case TextAlignment.MiddleRight:
                    chunk.Write(74, (short) 2);
                    break;
                case TextAlignment.BottomLeft:
                    chunk.Write(74, (short) 1);
                    break;
                case TextAlignment.BottomCenter:
                    chunk.Write(74, (short) 1);
                    break;
                case TextAlignment.BottomRight:
                    chunk.Write(74, (short) 1);
                    break;
                case TextAlignment.BaselineLeft:
                    chunk.Write(74, (short) 0);
                    break;
                case TextAlignment.BaselineCenter:
                    chunk.Write(74, (short) 0);
                    break;
                case TextAlignment.BaselineRight:
                    chunk.Write(74, (short) 0);
                    break;
                case TextAlignment.Aligned:
                    chunk.Write(74, (short) 0);
                    break;
                case TextAlignment.Middle:
                    chunk.Write(74, (short) 0);
                    break;
                case TextAlignment.Fit:
                    chunk.Write(74, (short) 0);
                    break;
            }

            WriteXData(def.XData);
        }

        private void WriteAttribute(Attribute attrib)
        {
            chunk.Write(0, attrib.CodeName);
            chunk.Write(5, attrib.Handle);

            chunk.Write(330, attrib.Owner.Handle);

            chunk.Write(100, SubclassMarker.Entity);

            chunk.Write(8, EncodeNonAsciiCharacters(attrib.Layer.Name));

            chunk.Write(62, attrib.Color.Index);
            if (attrib.Color.UseTrueColor)
                chunk.Write(420, AciColor.ToTrueColor(attrib.Color));

            if (attrib.Transparency.Value >= 0)
                chunk.Write(440, Transparency.ToAlphaValue(attrib.Transparency));

            chunk.Write(6, EncodeNonAsciiCharacters(attrib.Linetype.Name));

            chunk.Write(370, (short) attrib.Lineweight);
            chunk.Write(48, attrib.LinetypeScale);
            chunk.Write(60, attrib.IsVisible ? (short) 0 : (short) 1);

            chunk.Write(100, SubclassMarker.Text);

            Vector3 ocsInsertion = MathHelper.Transform(attrib.Position, attrib.Normal, CoordinateSystem.World, CoordinateSystem.Object);

            chunk.Write(10, ocsInsertion.X);
            chunk.Write(20, ocsInsertion.Y);
            chunk.Write(30, ocsInsertion.Z);

            chunk.Write(40, attrib.Height);
            chunk.Write(41, attrib.WidthFactor);

            chunk.Write(7, EncodeNonAsciiCharacters(attrib.Style.Name));

            object value = attrib.Value;
            if (value == null)
                chunk.Write(1, string.Empty);
            else if (value is string)
                chunk.Write(1, EncodeNonAsciiCharacters((string) value));
            else
                chunk.Write(1, value.ToString());

            switch (attrib.Alignment)
            {
                case TextAlignment.TopLeft:
                    chunk.Write(72, (short) 0);
                    break;
                case TextAlignment.TopCenter:
                    chunk.Write(72, (short) 1);
                    break;
                case TextAlignment.TopRight:
                    chunk.Write(72, (short) 2);
                    break;
                case TextAlignment.MiddleLeft:
                    chunk.Write(72, (short) 0);
                    break;
                case TextAlignment.MiddleCenter:
                    chunk.Write(72, (short) 1);
                    break;
                case TextAlignment.MiddleRight:
                    chunk.Write(72, (short) 2);
                    break;
                case TextAlignment.BottomLeft:
                    chunk.Write(72, (short) 0);
                    break;
                case TextAlignment.BottomCenter:
                    chunk.Write(72, (short) 1);
                    break;
                case TextAlignment.BottomRight:
                    chunk.Write(72, (short) 2);
                    break;
                case TextAlignment.BaselineLeft:
                    chunk.Write(72, (short) 0);
                    break;
                case TextAlignment.BaselineCenter:
                    chunk.Write(72, (short) 1);
                    break;
                case TextAlignment.BaselineRight:
                    chunk.Write(72, (short) 2);
                    break;
                case TextAlignment.Aligned:
                    chunk.Write(72, (short) 3);
                    break;
                case TextAlignment.Middle:
                    chunk.Write(72, (short) 4);
                    break;
                case TextAlignment.Fit:
                    chunk.Write(72, (short) 5);
                    break;
            }

            chunk.Write(11, ocsInsertion.X);
            chunk.Write(21, ocsInsertion.Y);
            chunk.Write(31, ocsInsertion.Z);

            chunk.Write(50, attrib.Rotation);
            chunk.Write(51, attrib.ObliqueAngle);

            chunk.Write(210, attrib.Normal.X);
            chunk.Write(220, attrib.Normal.Y);
            chunk.Write(230, attrib.Normal.Z);

            chunk.Write(100, SubclassMarker.Attribute);

            chunk.Write(2, EncodeNonAsciiCharacters(attrib.Tag));

            chunk.Write(70, (short) attrib.Flags);

            switch (attrib.Alignment)
            {
                case TextAlignment.TopLeft:
                    chunk.Write(74, (short) 3);
                    break;
                case TextAlignment.TopCenter:
                    chunk.Write(74, (short) 3);
                    break;
                case TextAlignment.TopRight:
                    chunk.Write(74, (short) 3);
                    break;
                case TextAlignment.MiddleLeft:
                    chunk.Write(74, (short) 2);
                    break;
                case TextAlignment.MiddleCenter:
                    chunk.Write(74, (short) 2);
                    break;
                case TextAlignment.MiddleRight:
                    chunk.Write(74, (short) 2);
                    break;
                case TextAlignment.BottomLeft:
                    chunk.Write(74, (short) 1);
                    break;
                case TextAlignment.BottomCenter:
                    chunk.Write(74, (short) 1);
                    break;
                case TextAlignment.BottomRight:
                    chunk.Write(74, (short) 1);
                    break;
                case TextAlignment.BaselineLeft:
                    chunk.Write(74, (short) 0);
                    break;
                case TextAlignment.BaselineCenter:
                    chunk.Write(74, (short) 0);
                    break;
                case TextAlignment.BaselineRight:
                    chunk.Write(74, (short) 0);
                    break;
                case TextAlignment.Aligned:
                    chunk.Write(74, (short) 0);
                    break;
                case TextAlignment.Middle:
                    chunk.Write(74, (short) 0);
                    break;
                case TextAlignment.Fit:
                    chunk.Write(74, (short) 0);
                    break;
            }
        }

        private void WriteViewport(Viewport vp)
        {
            chunk.Write(100, SubclassMarker.Viewport);

            chunk.Write(10, vp.Center.X);
            chunk.Write(20, vp.Center.Y);
            chunk.Write(30, vp.Center.Z);

            chunk.Write(40, vp.Width);
            chunk.Write(41, vp.Height);
            chunk.Write(68, vp.Stacking);
            chunk.Write(69, vp.Id);

            chunk.Write(12, vp.ViewCenter.X);
            chunk.Write(22, vp.ViewCenter.Y);

            chunk.Write(13, vp.SnapBase.X);
            chunk.Write(23, vp.SnapBase.Y);

            chunk.Write(14, vp.SnapSpacing.X);
            chunk.Write(24, vp.SnapSpacing.Y);

            chunk.Write(15, vp.GridSpacing.X);
            chunk.Write(25, vp.GridSpacing.Y);

            chunk.Write(16, vp.ViewDirection.X);
            chunk.Write(26, vp.ViewDirection.Y);
            chunk.Write(36, vp.ViewDirection.Z);

            chunk.Write(17, vp.ViewTarget.X);
            chunk.Write(27, vp.ViewTarget.Y);
            chunk.Write(37, vp.ViewTarget.Z);

            chunk.Write(42, vp.LensLength);

            chunk.Write(43, vp.FrontClipPlane);
            chunk.Write(44, vp.BackClipPlane);
            chunk.Write(45, vp.ViewHeight);

            chunk.Write(50, vp.SnapAngle);
            chunk.Write(51, vp.TwistAngle);
            chunk.Write(72, vp.CircleZoomPercent);

            foreach (Layer layer in vp.FrozenLayers)
                chunk.Write(331, layer.Handle);

            chunk.Write(90, (int) vp.Status);

            if (vp.ClippingBoundary != null)
                chunk.Write(340, vp.ClippingBoundary.Handle);

            chunk.Write(110, vp.UcsOrigin.X);
            chunk.Write(120, vp.UcsOrigin.Y);
            chunk.Write(130, vp.UcsOrigin.Z);

            chunk.Write(111, vp.UcsXAxis.X);
            chunk.Write(121, vp.UcsXAxis.Y);
            chunk.Write(131, vp.UcsXAxis.Z);

            chunk.Write(112, vp.UcsYAxis.X);
            chunk.Write(122, vp.UcsYAxis.Y);
            chunk.Write(132, vp.UcsYAxis.Z);

            WriteXData(vp.XData);
        }

        #endregion

        #region methods for Object section

        private void WriteDictionary(DictionaryObject dictionary)
        {
            chunk.Write(0, DxfObjectCode.Dictionary);
            chunk.Write(5, dictionary.Handle);
            chunk.Write(330, dictionary.Owner.Handle);

            chunk.Write(100, SubclassMarker.Dictionary);
            chunk.Write(280, dictionary.IsHardOwner ? (short) 1 : (short) 0);
            chunk.Write(281, (short) dictionary.Cloning);

            if (dictionary.Entries == null)
                return;

            foreach (KeyValuePair<string, string> entry in dictionary.Entries)
            {
                chunk.Write(3, EncodeNonAsciiCharacters(entry.Value));
                chunk.Write(350, entry.Key);
            }
        }

        private void WriteUnderlayDefinition(UnderlayDefinition underlayDef, string ownerHandle)
        {
            chunk.Write(0, underlayDef.CodeName);
            chunk.Write(5, underlayDef.Handle);
            chunk.Write(102, "{ACAD_REACTORS");
            List<DxfObject> objects = null;
            switch (underlayDef.Type)
            {
                case UnderlayType.DGN:
                    objects = doc.UnderlayDgnDefinitions.References[underlayDef.Name];
                    break;
                case UnderlayType.DWF:
                    objects = doc.UnderlayDwfDefinitions.References[underlayDef.Name];
                    break;
                case UnderlayType.PDF:
                    objects = doc.UnderlayPdfDefinitions.References[underlayDef.Name];
                    break;
            }
            if (objects == null)
                throw new NullReferenceException("Underlay references list cannot be null");
            foreach (DxfObject o in objects)
            {
                Underlay underlay = o as Underlay;
                if (underlay != null)
                    chunk.Write(330, underlay.Handle);
            }
            chunk.Write(102, "}");
            chunk.Write(330, ownerHandle);

            chunk.Write(100, SubclassMarker.UnderlayDefinition);
            chunk.Write(1, EncodeNonAsciiCharacters(underlayDef.File));
            switch (underlayDef.Type)
            {
                case UnderlayType.DGN:
                    chunk.Write(2, EncodeNonAsciiCharacters(((UnderlayDgnDefinition) underlayDef).Layout));
                    break;
                case UnderlayType.DWF:
                    chunk.Write(2, string.Empty);
                    break;
                case UnderlayType.PDF:
                    chunk.Write(2, EncodeNonAsciiCharacters(((UnderlayPdfDefinition) underlayDef).Page));
                    break;
            }
        }

        private void WriteImageDefReactor(ImageDefinitionReactor reactor)
        {
            chunk.Write(0, reactor.CodeName);
            chunk.Write(5, reactor.Handle);
            chunk.Write(330, reactor.ImageHandle);

            chunk.Write(100, SubclassMarker.RasterImageDefReactor);
            chunk.Write(90, 2);
            chunk.Write(330, reactor.ImageHandle);
        }

        private void WriteImageDef(ImageDefinition imageDefinition, string ownerHandle)
        {
            chunk.Write(0, imageDefinition.CodeName);
            chunk.Write(5, imageDefinition.Handle);

            chunk.Write(102, "{ACAD_REACTORS");
            chunk.Write(330, ownerHandle);
            foreach (ImageDefinitionReactor reactor in imageDefinition.Reactors.Values)
            {
                chunk.Write(330, reactor.Handle);
            }
            chunk.Write(102, "}");

            chunk.Write(330, ownerHandle);

            chunk.Write(100, SubclassMarker.RasterImageDef);
            chunk.Write(1, imageDefinition.File);

            chunk.Write(10, (double) imageDefinition.Width);
            chunk.Write(20, (double) imageDefinition.Height);

            // The documentation says that this is the size of one pixel in AutoCAD units, but it seems that this is always the size of one pixel in millimeters
            // this value is used to calculate the image resolution in PPI or PPC, and the default image size.
            double factor = UnitHelper.ConversionFactor((ImageUnits) imageDefinition.ResolutionUnits, DrawingUnits.Millimeters);
            chunk.Write(11, factor/imageDefinition.HorizontalResolution);
            chunk.Write(21, factor/imageDefinition.VerticalResolution);

            chunk.Write(280, (short) 1);
            chunk.Write(281, (short) imageDefinition.ResolutionUnits);

            WriteXData(imageDefinition.XData);
        }

        private void WriteRasterVariables(RasterVariables variables, string ownerHandle)
        {
            chunk.Write(0, variables.CodeName);
            chunk.Write(5, variables.Handle);
            chunk.Write(330, ownerHandle);

            chunk.Write(100, SubclassMarker.RasterVariables);
            chunk.Write(90, 0);
            chunk.Write(70, variables.DisplayFrame ? (short) 1 : (short) 0);
            chunk.Write(71, (short) variables.DisplayQuality);
            chunk.Write(72, (short) variables.Units);
        }

        private void WriteMLineStyle(MLineStyle style, string ownerHandle)
        {
            chunk.Write(0, style.CodeName);
            chunk.Write(5, style.Handle);
            chunk.Write(330, ownerHandle);

            chunk.Write(100, SubclassMarker.MLineStyle);

            chunk.Write(2, EncodeNonAsciiCharacters(style.Name));

            chunk.Write(70, (short) style.Flags);

            chunk.Write(3, EncodeNonAsciiCharacters(style.Description));

            chunk.Write(62, style.FillColor.Index);
            if (style.FillColor.UseTrueColor) // && this.doc.DrawingVariables.AcadVer > DxfVersion.AutoCad2000)
                chunk.Write(420, AciColor.ToTrueColor(style.FillColor));
            chunk.Write(51, style.StartAngle);
            chunk.Write(52, style.EndAngle);
            chunk.Write(71, (short) style.Elements.Count);
            foreach (MLineStyleElement element in style.Elements)
            {
                chunk.Write(49, element.Offset);
                chunk.Write(62, element.Color.Index);
                if (element.Color.UseTrueColor) // && this.doc.DrawingVariables.AcadVer > DxfVersion.AutoCad2000)
                    chunk.Write(420, AciColor.ToTrueColor(element.Color));

                chunk.Write(6, EncodeNonAsciiCharacters(element.Linetype.Name));
            }

            WriteXData(style.XData);
        }

        private void WriteGroup(Group group, string ownerHandle)
        {
            chunk.Write(0, group.CodeName);
            chunk.Write(5, group.Handle);
            chunk.Write(330, ownerHandle);

            chunk.Write(100, SubclassMarker.Group);

            chunk.Write(300, EncodeNonAsciiCharacters(group.Description));
            chunk.Write(70, group.IsUnnamed ? (short) 1 : (short) 0);
            chunk.Write(71, group.IsSelectable ? (short) 1 : (short) 0);

            foreach (EntityObject entity in group.Entities)
            {
                chunk.Write(340, entity.Handle);
            }

            WriteXData(group.XData);
        }

        private void WriteLayout(Layout layout, string ownerHandle)
        {
            chunk.Write(0, layout.CodeName);
            chunk.Write(5, layout.Handle);
            chunk.Write(330, ownerHandle);

            WritePlotSettings(layout.PlotSettings);

            chunk.Write(100, SubclassMarker.Layout);
            chunk.Write(1, EncodeNonAsciiCharacters(layout.Name));
            chunk.Write(70, (short) 1);
            chunk.Write(71, layout.TabOrder);

            chunk.Write(10, layout.MinLimit.X);
            chunk.Write(20, layout.MinLimit.Y);
            chunk.Write(11, layout.MaxLimit.X);
            chunk.Write(21, layout.MaxLimit.Y);

            chunk.Write(12, layout.BasePoint.X);
            chunk.Write(22, layout.BasePoint.Y);
            chunk.Write(32, layout.BasePoint.Z);

            chunk.Write(14, layout.MinExtents.X);
            chunk.Write(24, layout.MinExtents.Y);
            chunk.Write(34, layout.MinExtents.Z);

            chunk.Write(15, layout.MaxExtents.X);
            chunk.Write(25, layout.MaxExtents.Y);
            chunk.Write(35, layout.MaxExtents.Z);

            chunk.Write(146, layout.Elevation);

            chunk.Write(13, layout.UcsOrigin.X);
            chunk.Write(23, layout.UcsOrigin.Y);
            chunk.Write(33, layout.UcsOrigin.Z);


            chunk.Write(16, layout.UcsXAxis.X);
            chunk.Write(26, layout.UcsXAxis.Y);
            chunk.Write(36, layout.UcsXAxis.Z);

            chunk.Write(17, layout.UcsYAxis.X);
            chunk.Write(27, layout.UcsYAxis.Y);
            chunk.Write(37, layout.UcsYAxis.Z);

            chunk.Write(76, (short) 0);

            chunk.Write(330, layout.AssociatedBlock.Owner.Handle);

            WriteXData(layout.XData);
        }

        private void WritePlotSettings( PlotSettings plot)
        {
            chunk.Write(100, SubclassMarker.PlotSettings);
            chunk.Write(1, EncodeNonAsciiCharacters(plot.PageSetupName));
            chunk.Write(2, EncodeNonAsciiCharacters(plot.PlotterName));
            chunk.Write(4, EncodeNonAsciiCharacters(plot.PaperSizeName));
            chunk.Write(6, EncodeNonAsciiCharacters(plot.ViewName));

            chunk.Write(40, plot.PaperMargin.Left);
            chunk.Write(41, plot.PaperMargin.Bottom);
            chunk.Write(42, plot.PaperMargin.Right);
            chunk.Write(43, plot.PaperMargin.Top);
            chunk.Write(44, plot.PaperSize.X);
            chunk.Write(45, plot.PaperSize.Y);
            chunk.Write(46, plot.Origin.X);
            chunk.Write(47, plot.Origin.Y);
            chunk.Write(48, plot.WindowBottomLeft.X);
            chunk.Write(49, plot.WindowUpRight.X);
            chunk.Write(140, plot.WindowBottomLeft.Y);
            chunk.Write(141, plot.WindowUpRight.Y);
            chunk.Write(142, plot.PrintScaleNumerator);
            chunk.Write(143, plot.PrintScaleDenominator);

            chunk.Write(70, (short) plot.Flags);
            chunk.Write(72, (short) plot.PaperUnits);
            chunk.Write(73, (short) plot.PaperRotation);
            chunk.Write(74, (short) plot.PlotType);

            chunk.Write(7, EncodeNonAsciiCharacters(plot.CurrentStyleSheet));
            chunk.Write(75, plot.ScaleToFit ? (short) 0 : (short) 16);

            chunk.Write(76, (short) plot.ShadePlotMode);
            chunk.Write(77, (short) plot.ShadePlotResolutionMode);
            chunk.Write(78, plot.ShadePlotDPI);
            chunk.Write(147, plot.PrintScale);

            chunk.Write(148, plot.PaperImageOrigin.X);
            chunk.Write(149, plot.PaperImageOrigin.Y);
        }

        #endregion

        #region private methods

        private static short GetSupressZeroesValue(bool leading, bool trailing, bool feet, bool inches)
        {
            short rtn = 0;
            if (feet && inches)
                rtn = 0;
            if (!feet && !inches)
                rtn += 1;
            if (!feet && inches)
                rtn += 2;
            if (feet && !inches)
                rtn += 3;

            if (!leading && !trailing)
                rtn += 0;
            if (leading && !trailing)
                rtn += 4;
            if (!leading && trailing)
                rtn += 8;
            if (leading && trailing)
                rtn += 12;

            return rtn;
        }

        private string EncodeNonAsciiCharacters(string text)
        {
            // for dxf database version prior to AutoCad 2007 non ASCII characters must be encoded to the template \U+####,
            // where #### is the for digits hexadecimal number that represent that character.
            if (doc.DrawingVariables.AcadVer >= DxfVersion.AutoCad2007)
                return text;

            if (string.IsNullOrEmpty(text))
                return string.Empty;

            string encoded;
            if (encodedStrings.TryGetValue(text, out encoded))
                return encoded;

            StringBuilder sb = new StringBuilder();
            foreach (char c in text)
            {
                if (c > 127)
                    sb.Append(string.Concat("\\U+", string.Format("{0:X4}", Convert.ToInt32(c))));
                else
                    sb.Append(c);
            }

            encoded = sb.ToString();
            encodedStrings.Add(text, encoded);
            return encoded;

            // encoding of non ASCII characters, including the extended chart, using regular expressions, this code is slower
            //return Regex.Replace(
            //    text,
            //    @"(?<char>[^\u0000-\u00ff]{1})",
            //    m => "\\U+" + string.Format("{0:X4}", Convert.ToInt32(m.Groups["char"].Value[0])));
        }

        private void WriteXData(XDataDictionary xData)
        {
            foreach (string appReg in xData.AppIds)
            {
                chunk.Write((short) XDataCode.AppReg, EncodeNonAsciiCharacters(appReg));

                foreach (XDataRecord x in xData[appReg].XDataRecord)
                {
                    short code = (short) x.Code;
                    object value = x.Value;
                    if (code == 1000 || code == 1003)
                    {
                        chunk.Write(code, EncodeNonAsciiCharacters((string) value));
                    }
                    else if (code == 1004) // binary extended data is written in chunks of 127 bytes
                    {
                        byte[] bytes = (byte[]) value;
                        byte[] data;
                        int count = bytes.Length;
                        int index = 0;
                        while (count > 127)
                        {
                            data = new byte[127];
                            Array.Copy(bytes, index, data, 0, 127);
                            chunk.Write(code, data);
                            count -= 127;
                            index += 127;
                        }
                        data = new byte[bytes.Length - index];
                        Array.Copy(bytes, index, data, 0, bytes.Length - index);
                        chunk.Write(code, data);
                    }
                    else
                        chunk.Write(code, value);
                }
            }
        }

        #endregion
    }
}