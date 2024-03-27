// PngStream and TiffStream MODIFIED / PORTED FROM PDF.JS, PDF.JS is licensed as follows:
/* Copyright 2012 Mozilla Foundation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace PdfButcher.Internals.ThirdParty.PdfJs
{
    using System;
    using System.IO;
    using PdfButcher.Internals.Filters;

    internal class PngStream : MinBufferStream
    {
        private readonly int pixBytes;
        private readonly int rowBytes;
        private byte[] prevRow;
        private byte[] rawBytes;

        public PngStream(Stream inner, int bpc, int colors, int columns)
            : base(inner, ((columns * colors * bpc) + 7) >> 3)
        {
            this.pixBytes = ((colors * bpc) + 7) >> 3;
            this.rowBytes = ((columns * colors * bpc) + 7) >> 3;

            this.prevRow = new byte[rowBytes];
            this.rawBytes = new byte[rowBytes];
        }

        protected override int FillBuffer(byte[] outgoing, int offset, int count)
        {
            int i;
            var j = offset;
            byte up;
            byte c;

            var predictor = (int)inner.ReadByte();

            if (!Fill())
            {
                return 0;
            }

            switch (predictor)
            {
                case 0:
                    Array.Copy(rawBytes, 0, outgoing, offset, rawBytes.Length);
                    Array.Copy(rawBytes, prevRow, rawBytes.Length);

                    return rawBytes.Length;

                case 1:
                    j = offset;
                    i = 0;
                    for (; i < pixBytes; i++)
                    {
                        outgoing[j++] = rawBytes[i];
                    }

                    for (; i < rowBytes; i++)
                    {
                        outgoing[j] = (byte)((outgoing[j - pixBytes] + rawBytes[i]) & 0xff);
                        j++;
                    }

                    break;

                case 2:
                    j = offset;
                    for (i = 0; i < rowBytes; ++i)
                    {
                        outgoing[j++] = (byte)((prevRow[i] + rawBytes[i]) & 0xff);
                    }

                    break;

                case 3:
                    j = offset;
                    for (i = 0; i < pixBytes; ++i)
                    {
                        outgoing[j++] = (byte)((prevRow[i] >> 1) + rawBytes[i]);
                    }

                    for (; i < rowBytes; ++i)
                    {
                        outgoing[j] = (byte)
                            ((((prevRow[i] + outgoing[j - pixBytes]) >> 1) + rawBytes[i]) & 0xff);
                        j++;
                    }

                    break;

                case 4:
                    j = offset;
                    for (i = 0; i < pixBytes; ++i)
                    {
                        up = prevRow[i];
                        c = rawBytes[i];
                        outgoing[j++] = (byte)(up + c);
                    }

                    for (; i < rowBytes; ++i)
                    {
                        up = prevRow[i];
                        var upLeft = prevRow[i - pixBytes];
                        var left = outgoing[j - pixBytes];
                        var p = (left + up) - upLeft;

                        var pa = p - left;
                        if (pa < 0)
                        {
                            pa = -pa;
                        }

                        var pb = p - up;
                        if (pb < 0)
                        {
                            pb = -pb;
                        }

                        var pc = p - upLeft;
                        if (pc < 0)
                        {
                            pc = -pc;
                        }

                        c = rawBytes[i];
                        if (pa <= pb && pa <= pc)
                        {
                            outgoing[j++] = (byte)(left + c);
                        }
                        else if (pb <= pc)
                        {
                            outgoing[j++] = (byte)(up + c);
                        }
                        else
                        {
                            outgoing[j++] = (byte)(upLeft + c);
                        }
                    }

                    break;

                default:
                    throw new NotSupportedException($"FlateDecode Predictor with value {predictor} is not supported.");
            }

            Array.Copy(outgoing, offset, prevRow, 0, rawBytes.Length);

            return j - offset;

            bool Fill()
            {
                var os = 0;
                var read = 0;
                while (os < rowBytes && (read = inner.Read(rawBytes, os, rowBytes - os)) > 0)
                {
                    os += read;
                }

                if (os != rowBytes)
                {
                    return false;
                }

                return true;
            }
        }
    }
}