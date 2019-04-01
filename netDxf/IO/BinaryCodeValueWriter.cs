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
using System.Diagnostics;
using System.IO;

namespace netDxf.IO
{
    internal class BinaryCodeValueWriter :
        ICodeValueWriter
    {
        #region private fields

        private readonly BinaryWriter writer;
        private short dxfCode;
        private object dxfValue;

        #endregion

        #region constructors

        public BinaryCodeValueWriter(BinaryWriter writer)
        {
            this.writer = writer;

            // binary DXF file begins with a 22-byte sentinel consisting of the following
            // AutoCAD Binary DXF<CR><LF><SUB><NULL>
            //char[] sentinel = {'A', 'u', 't', 'o', 'C', 'A', 'D', ' ', 'B', 'i', 'n', 'a', 'r', 'y', ' ', 'D', 'X', 'F', '\r', '\n', (char)26, '\0'};
            byte[] sentinel = {65, 117, 116, 111, 67, 65, 68, 32, 66, 105, 110, 97, 114, 121, 32, 68, 88, 70, 13, 10, 26, 0};
            this.writer.Write(sentinel);

            dxfCode = 0;
            dxfValue = null;
        }

        #endregion

        #region public properties

        public short Code
        {
            get { return dxfCode; }
        }

        public object Value
        {
            get { return dxfValue; }
        }

        public long CurrentPosition
        {
            get { return writer.BaseStream.Position; }
        }

        #endregion

        #region public methods

        public void Write(short code, object value)
        {
            dxfCode = code;
            writer.Write(code);
            if (code >= 0 && code <= 9) // string
            {
                Debug.Assert(value is string, "Incorrect value type.");
                WriteString((string) value);
            }
            else if (code >= 10 && code <= 39) // double precision 3D point value
            {
                Debug.Assert(value is double, "Incorrect value type.");
                WriteDouble((double) value);
            }
            else if (code >= 40 && code <= 59) // double precision floating point value
            {
                Debug.Assert(value is double, "Incorrect value type.");
                WriteDouble((double) value);
            }
            else if (code >= 60 && code <= 79) // 16-bit integer value
            {
                Debug.Assert(value is short, "Incorrect value type.");
                WriteShort((short) value);
            }
            else if (code >= 90 && code <= 99) // 32-bit integer value
            {
                Debug.Assert(value is int, "Incorrect value type.");
                WriteInt((int) value);
            }
            else if (code == 100) // string (255-character maximum; less for Unicode strings)
            {
                Debug.Assert(value is string, "Incorrect value type.");
                WriteString((string) value);
            }
            else if (code == 101) // string (255-character maximum; less for Unicode strings). This code is undocumented and seems to affect only the AcdsData in dxf version 2013
            {
                Debug.Assert(value is string, "Incorrect value type.");
                WriteString((string) value);
            }
            else if (code == 102) // string (255-character maximum; less for Unicode strings)
            {
                Debug.Assert(value is string, "Incorrect value type.");
                WriteString((string) value);
            }
            else if (code == 105) // string representing hexadecimal (hex) handle value
            {
                Debug.Assert(value is string, "Incorrect value type.");
                WriteString((string) value);
            }
            else if (code >= 110 && code <= 119) // double precision floating point value
            {
                Debug.Assert(value is double, "Incorrect value type.");
                WriteDouble((double) value);
            }
            else if (code >= 120 && code <= 129) // double precision floating point value
            {
                Debug.Assert(value is double, "Incorrect value type.");
                WriteDouble((double) value);
            }
            else if (code >= 130 && code <= 139) // double precision floating point value
            {
                Debug.Assert(value is double, "Incorrect value type.");
                WriteDouble((double) value);
            }
            else if (code >= 140 && code <= 149) // double precision scalar floating-point value
            {
                Debug.Assert(value is double, "Incorrect value type.");
                WriteDouble((double) value);
            }
            else if (code >= 160 && code <= 169) // 64-bit integer value
            {
                Debug.Assert(value is long, "Incorrect value type.");
                WriteLong((long) value);
            }
            else if (code >= 170 && code <= 179) // 16-bit integer value
            {
                Debug.Assert(value is short, "Incorrect value type.");
                WriteShort((short) value);
            }
            else if (code >= 210 && code <= 239) // double precision scalar floating-point value
            {
                Debug.Assert(value is double, "Incorrect value type.");
                WriteDouble((double) value);
            }
            else if (code >= 270 && code <= 279) // 16-bit integer value
            {
                Debug.Assert(value is short, "Incorrect value type.");
                WriteShort((short) value);
            }
            else if (code >= 280 && code <= 289) // 16-bit integer value
            {
                Debug.Assert(value is short, "Incorrect value type.");
                WriteShort((short) value);
            }
            else if (code >= 290 && code <= 299) // byte (boolean flag value)
            {
                Debug.Assert(value is bool, "Incorrect value type.");
                WriteBool((bool) value);
            }
            else if (code >= 300 && code <= 309) // arbitrary text string
            {
                Debug.Assert(value is string, "Incorrect value type.");
                WriteString((string) value);
            }
            else if (code >= 310 && code <= 319) // string representing hex value of binary chunk
            {
                Debug.Assert(value is byte[], "Incorrect value type.");
                WriteBytes((byte[]) value);
            }
            else if (code >= 320 && code <= 329) // string representing hex handle value
            {
                Debug.Assert(value is string, "Incorrect value type.");
                WriteString((string) value);
            }
            else if (code >= 330 && code <= 369) // string representing hex object IDs
            {
                Debug.Assert(value is string, "Incorrect value type.");
                WriteString((string) value);
            }
            else if (code >= 370 && code <= 379) // 16-bit integer value
            {
                Debug.Assert(value is short, "Incorrect value type.");
                WriteShort((short) value);
            }
            else if (code >= 380 && code <= 389) // 16-bit integer value
            {
                Debug.Assert(value is short, "Incorrect value type.");
                WriteShort((short) value);
            }
            else if (code >= 390 && code <= 399) // string representing hex handle value
            {
                Debug.Assert(value is string, "Incorrect value type.");
                WriteString((string) value);
            }
            else if (code >= 400 && code <= 409) // 16-bit integer value
            {
                Debug.Assert(value is short, "Incorrect value type.");
                WriteShort((short) value);
            }
            else if (code >= 410 && code <= 419) // string
            {
                Debug.Assert(value is string, "Incorrect value type.");
                WriteString((string) value);
            }
            else if (code >= 420 && code <= 429) // 32-bit integer value
            {
                Debug.Assert(value is int, "Incorrect value type.");
                WriteInt((int) value);
            }
            else if (code >= 430 && code <= 439) // string
            {
                Debug.Assert(value is string, "Incorrect value type.");
                WriteString((string) value);
            }
            else if (code >= 440 && code <= 449) // 32-bit integer value
            {
                Debug.Assert(value is int, "Incorrect value type.");
                WriteInt((int) value);
            }
            else if (code >= 450 && code <= 459) // 32-bit integer value
            {
                Debug.Assert(value is int, "Incorrect value type.");
                WriteInt((int) value);
            }
            else if (code >= 460 && code <= 469) // double-precision floating-point value
            {
                Debug.Assert(value is double, "Incorrect value type.");
                WriteDouble((double) value);
            }
            else if (code >= 470 && code <= 479) // string
            {
                Debug.Assert(value is string, "Incorrect value type.");
                WriteString((string) value);
            }
            else if (code >= 480 && code <= 481) // string representing hex handle value
            {
                Debug.Assert(value is string, "Incorrect value type.");
                WriteString((string) value);
            }
            else if (code == 999) // comment (string)
            {
                throw new Exception(string.Format("The comment group, 999, is not used in binary DXF files at byte address {0}", writer.BaseStream.Position));
            }
            else if (code >= 1010 && code <= 1059) // double-precision floating-point value
            {
                Debug.Assert(value is double, "Incorrect value type.");
                WriteDouble((double) value);
            }
            else if (code >= 1000 && code <= 1003) // string (same limits as indicated with 0-9 code range)
            {
                Debug.Assert(value is string, "Incorrect value type.");
                WriteString((string) value);
            }
            else if (code == 1004) // string representing hex value of binary chunk
            {
                Debug.Assert(value is byte[], "Incorrect value type.");
                WriteBytes((byte[]) value);
            }
            else if (code >= 1005 && code <= 1009) // string (same limits as indicated with 0-9 code range)
            {
                Debug.Assert(value is string, "Incorrect value type.");
                WriteString((string) value);
            }
            else if (code >= 1060 && code <= 1070) // 16-bit integer value
            {
                Debug.Assert(value is short, "Incorrect value type.");
                WriteShort((short) value);
            }
            else if (code == 1071) // 32-bit integer value
            {
                Debug.Assert(value is int, "Incorrect value type.");
                WriteInt((int) value);
            }
            else
            {
                throw new Exception(string.Format("Code {0} not valid at byte address {1}", dxfCode, writer.BaseStream.Position));
            }

            dxfValue = value;
        }

        public void WriteByte(byte value)
        {
            writer.Write(value);
        }

        public void WriteBytes(byte[] value)
        {
            writer.Write((byte) value.Length);
            writer.Write(value);
        }

        public void WriteShort(short value)
        {
            writer.Write(value);
        }

        public void WriteInt(int value)
        {
            writer.Write(value);
        }

        public void WriteLong(long value)
        {
            writer.Write(value);
        }

        public void WriteBool(bool value)
        {
            if (value)
                writer.Write((byte) 1);
            else
                writer.Write((byte) 0);
        }

        public void WriteDouble(double value)
        {
            writer.Write(value);
        }

        public void WriteString(string value)
        {
            writer.Write(value.ToCharArray());
            writer.Write('\0'); // strings always end with a 0 byte (char NULL)
        }

        public void Flush()
        {
            writer.Flush();
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}", dxfCode, dxfValue);
        }

        #endregion   
    }
}