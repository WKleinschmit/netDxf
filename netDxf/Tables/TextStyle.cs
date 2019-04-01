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
using System.IO;
using netDxf.Collections;
using System.Drawing.Text;

namespace netDxf.Tables
{
    /// <summary>
    /// Represents a text style.
    /// </summary>
    public class TextStyle :
        TableObject
    {
        #region private fields

        private string file;
        private string bigFont;
        private double height;
        private bool isBackward;
        private bool isUpsideDown;
        private bool isVertical;
        private double obliqueAngle;
        private double widthFactor;
        private FontStyle fontStyle;
        private string fontFamilyName;

        #endregion

        #region constants

        /// <summary>
        /// Default text style name.
        /// </summary>
        public const string DefaultName = "Standard";

        /// <summary>
        /// Gets the default text style.
        /// </summary>
        public static TextStyle Default
        {
            get { return new TextStyle(DefaultName, "simplex.shx"); }
        }

        #endregion

        #region constructors

        /// <summary>
        /// Initializes a new instance of the <c>TextStyle</c> class. The font file name, without the extension, will be used as the TextStyle name.
        /// </summary>
        /// <param name="font">Text style font file name with full or relative path.</param>
        /// <remarks>If the font file is a true type and is not found in the specified path, the constructor will try to find it in the system font folder.</remarks>
        public TextStyle(string font)
            : this(Path.GetFileNameWithoutExtension(font), font)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>TextStyle</c> class.
        /// </summary>
        /// <param name="name">Text style name.</param>
        /// <param name="font">Text style font file name with full or relative path.</param>
        /// <remarks>If the font file is a true type and is not found in the specified path, the constructor will try to find it in the system font folder.</remarks>
        public TextStyle(string name, string font)
            : this(name, font, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>TextStyle</c> class.
        /// </summary>
        /// <param name="name">Text style name.</param>
        /// <param name="font">Text style font file name with full or relative path.</param>
        /// <param name="checkName">Specifies if the style name has to be checked.</param>
        /// <remarks>If the font file is a true type and is not found in the specified path, the constructor will try to find it in the system font folder.</remarks>
        internal TextStyle(string name, string font, bool checkName)
            : base(name, DxfObjectCode.TextStyle, checkName)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name), "The text style name should be at least one character long.");
            IsReserved = name.Equals(DefaultName, StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(font))
                throw new ArgumentNullException(nameof(font));

            if (!Path.GetExtension(font).Equals(".TTF", StringComparison.InvariantCultureIgnoreCase) &&
                !Path.GetExtension(font).Equals(".SHX", StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("Only true type TTF fonts and ACAD compiled shape SHX fonts are allowed.");

            file = font;
            bigFont = string.Empty;
            widthFactor = 1.0;
            obliqueAngle = 0.0;
            height = 0.0;
            isVertical = false;
            isBackward = false;
            isUpsideDown = false;
            fontFamilyName = TrueTypeFontFamilyName(font);
            fontStyle = FontStyle.Regular;
        }

        /// <summary>
        /// Initializes a new instance of the <c>TextStyle</c> class exclusively to be used with true type fonts.
        /// </summary>
        /// <param name="fontFamily">True type font family name.</param>
        /// <param name="fontStyle">True type font style</param>
        /// <remarks>
        /// This constructor is to be use only with true type fonts.
        /// The fontFamily value will also be used as the name of the text style.
        /// </remarks>
        public TextStyle(string fontFamily, FontStyle fontStyle)
            : this(fontFamily, fontFamily, fontStyle, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>TextStyle</c> class exclusively to be used with true type fonts.
        /// </summary>
        /// <param name="name">Text style name.</param>
        /// <param name="fontFamily">True type font family name.</param>
        /// <param name="fontStyle">True type font style</param>
        /// <remarks>This constructor is to be use only with true type fonts.</remarks>
        public TextStyle(string name, string fontFamily, FontStyle fontStyle)
            : this(name, fontFamily, fontStyle, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>TextStyle</c> class exclusively to be used with true type fonts.
        /// </summary>
        /// <param name="name">Text style name.</param>
        /// <param name="fontFamily">True type font family name.</param>
        /// <param name="fontStyle">True type font style</param>
        /// <param name="checkName">Specifies if the style name has to be checked.</param>
        /// <remarks>This constructor is to be use only with true type fonts.</remarks>
        internal TextStyle(string name, string fontFamily, FontStyle fontStyle, bool checkName)
            : base(name, DxfObjectCode.TextStyle, checkName)
        {
            file = string.Empty;
            bigFont = string.Empty;
            widthFactor = 1.0;
            obliqueAngle = 0.0;
            height = 0.0;
            isVertical = false;
            isBackward = false;
            isUpsideDown = false;
            if(string.IsNullOrEmpty(fontFamily))
                throw new ArgumentNullException(nameof(fontFamily));
            fontFamilyName = fontFamily;
            this.fontStyle = fontStyle;
        }

        #endregion

        #region public properties

        /// <summary>
        /// Gets or sets the text style font file name.
        /// </summary>
        /// <remarks>
        /// When this value is used for true type fonts should be present in the Font system folder.<br />
        /// When the style does not contain any information for the file the font information will be saved in the extended data when saved to a DXF,
        /// this is only applicable for true type fonts.
        /// </remarks>
        public string FontFile
        {
            get { return file; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException(nameof(value));

                if (!Path.GetExtension(value).Equals(".TTF", StringComparison.InvariantCultureIgnoreCase) &&
                    !Path.GetExtension(value).Equals(".SHX", StringComparison.InvariantCultureIgnoreCase))
                    throw new ArgumentException("Only true type TTF fonts and ACAD compiled shape SHX fonts are allowed.");

                fontFamilyName = TrueTypeFontFamilyName(value);
                bigFont = string.Empty;
                file = value;
            }
        }

        /// <summary>
        /// Gets or sets an Asian-language Big Font file.
        /// </summary>
        /// <remarks>Only ACAD compiled shape SHX fonts are valid for creating Big Fonts.</remarks>
        public string BigFont
        {
            get { return bigFont; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    bigFont = string.Empty;
                else
                {
                    if (string.IsNullOrEmpty(file))
                        throw new NullReferenceException("The Big Font is only applicable for SHX Asian fonts.");
                    if (!Path.GetExtension(file).Equals(".SHX", StringComparison.InvariantCultureIgnoreCase))
                        throw new NullReferenceException("The Big Font is only applicable for SHX Asian fonts.");
                    if(!Path.GetExtension(value).Equals(".SHX", StringComparison.InvariantCultureIgnoreCase))
                        throw new ArgumentException("The Big Font is only applicable for SHX Asian fonts.", nameof(value));
                    bigFont = value;
                }               
            }
        }

        /// <summary>
        /// Gets or sets the true type font family name.
        /// </summary>
        /// <remarks>
        /// When the font family name is manually specified the file font will not be used and it will be set to empty.
        /// In this case the font information will be stored in the style extended data when saved to a DXF.
        /// This value is only applicable for true type fonts.
        /// </remarks>
        public string FontFamilyName
        {
            get { return fontFamilyName; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException(nameof(value));
                file = string.Empty;
                bigFont = string.Empty;
                fontFamilyName = value;
            }
        }

        /// <summary>
        /// Gets or sets the true type font style.
        /// </summary>
        /// <remarks>
        /// The font style value is ignored when a font file has been specified.<br />
        /// All styles might or might not be available for the current font family.
        /// </remarks>
        public FontStyle FontStyle
        {
            get { return fontStyle; }
            set { fontStyle = value; }
        }

        /// <summary>
        /// Gets if the font used is a true type.
        /// </summary>
        /// <remarks>
        /// It will not only return false for SHX fonts but also if the font file has not been found,
        /// this is applicable to true type fonts not registered in the system.
        /// </remarks>
        public bool IsTrueType
        {
            get { return !string.IsNullOrEmpty(FontFamilyName); }
        }

        /// <summary>
        /// Gets or sets the text height.
        /// </summary>
        /// <remarks>Fixed text height; 0 if not fixed.</remarks>
        public double Height
        {
            get { return height; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The TextStyle height must be equals or greater than zero.");
                height = value;
            }
        }

        /// <summary>
        /// Gets or sets the text width factor.
        /// </summary>
        /// <remarks>Valid values range from 0.01 to 100. Default: 1.0.</remarks>
        public double WidthFactor
        {
            get { return widthFactor; }
            set
            {
                if (value < 0.01 || value > 100.0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The TextStyle width factor valid values range from 0.01 to 100.");
                widthFactor = value;
            }
        }

        /// <summary>
        /// Gets or sets the font oblique angle in degrees.
        /// </summary>
        /// <remarks>Valid values range from -85 to 85. Default: 0.0.</remarks>
        public double ObliqueAngle
        {
            get { return obliqueAngle; }
            set
            {
                if (value < -85.0 || value > 85.0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The TextStyle oblique angle valid values range from -85 to 85.");
                obliqueAngle = value;
            }
        }

        /// <summary>
        /// Gets or sets the text is vertical.
        /// </summary>
        public bool IsVertical
        {
            get { return isVertical; }
            set { isVertical = value; }
        }

        /// <summary>
        /// Gets or sets if the text is backward (mirrored in X).
        /// </summary>
        public bool IsBackward
        {
            get { return isBackward; }
            set { isBackward = value; }
        }

        /// <summary>
        /// Gets or sets if the text is upside down (mirrored in Y).
        /// </summary>
        public bool IsUpsideDown
        {
            get { return isUpsideDown; }
            set { isUpsideDown = value; }
        }

        /// <summary>
        /// Gets the owner of the actual text style.
        /// </summary>
        public new TextStyles Owner
        {
            get { return (TextStyles) base.Owner; }
            internal set { base.Owner = value; }
        }

        #endregion

        #region private methods

        private static string TrueTypeFontFamilyName(string ttfFont)
        {
            if (string.IsNullOrEmpty(ttfFont)) throw new ArgumentNullException(nameof(ttfFont));

            // the following information is only applied to TTF not SHX fonts
            if (!Path.GetExtension(ttfFont).Equals(".TTF", StringComparison.InvariantCultureIgnoreCase))
                return string.Empty;

            // try to find the file in the specified directory, if not try it in the fonts system folder
            string fontFile;
            if (File.Exists(ttfFont))
                fontFile = Path.GetFullPath(ttfFont);
            else
                fontFile = string.Format("{0}{1}{2}", Environment.GetFolderPath(Environment.SpecialFolder.Fonts), Path.DirectorySeparatorChar, Path.GetFileName(ttfFont));

            try
            {
                PrivateFontCollection fontCollection = new PrivateFontCollection();
                fontCollection.AddFontFile(fontFile);
                return fontCollection.Families[0].Name;
            }
            catch (FileNotFoundException)
            {
                return string.Empty;
            }
        }

        #endregion

        #region overrides

        /// <summary>
        /// Creates a new TextStyle that is a copy of the current instance.
        /// </summary>
        /// <param name="newName">TextStyle name of the copy.</param>
        /// <returns>A new TextStyle that is a copy of this instance.</returns>
        public override TableObject Clone(string newName)
        {
            TextStyle copy;

            if (string.IsNullOrEmpty(file))
            {
                copy = new TextStyle(newName, fontFamilyName, fontStyle)
                {
                    Height = height,
                    IsBackward = isBackward,
                    IsUpsideDown = isUpsideDown,
                    IsVertical = isVertical,
                    ObliqueAngle = obliqueAngle,
                    WidthFactor = widthFactor
                };
            }
            else
            {
                copy = new TextStyle(newName, file)
                {
                    Height = height,
                    IsBackward = isBackward,
                    IsUpsideDown = isUpsideDown,
                    IsVertical = isVertical,
                    ObliqueAngle = obliqueAngle,
                    WidthFactor = widthFactor
                };
            }
            
            foreach (XData data in XData.Values)
                copy.XData.Add((XData)data.Clone());

            return copy;
        }

        /// <summary>
        /// Creates a new TextStyle that is a copy of the current instance.
        /// </summary>
        /// <returns>A new TextStyle that is a copy of this instance.</returns>
        public override object Clone()
        {
            return Clone(Name);
        }

        #endregion
    }
}
