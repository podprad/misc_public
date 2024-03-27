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

    internal class TiffStream : MinBufferStream
    {
        private int bits;
        private int colors;
        private int columns;
        private int rowBytes;
        private byte[] rawBytes;
        private byte[] compArray;

        public TiffStream(Stream stream, int bits, int colors, int columns)
            : base(stream, ((columns * colors * bits) + 7) >> 3)
        {
            rowBytes = ((columns * colors * bits) + 7) >> 3;
            this.bits = bits;
            this.colors = colors;
            this.columns = columns;
            rawBytes = new byte[rowBytes];
            compArray = new byte[colors + 1];
        }

        protected override int FillBuffer(byte[] outgoing, int offset, int count)
        {
            if (count < rowBytes)
            {
                throw new NotSupportedException("Stream requires at least " + rowBytes + " read at a time.");
            }

            if (!inner.TryFillArray(rawBytes))
            {
                return 0;
            }

            var inbuf = 0;
            var outbuf = 0;
            var inbits = 0;
            var outbits = 0;
            var pos = offset;
            var i = 0;

            if (bits == 1 && colors == 1)
            {
                // Optimized version of the loop in the "else"-branch
                // for 1 bit-per-component and 1 color TIFF images.
                for (i = 0; i < rowBytes; ++i)
                {
                    var c = rawBytes[i] ^ inbuf;
                    c ^= c >> 1;
                    c ^= c >> 2;
                    c ^= c >> 4;
                    inbuf = (c & 1) << 7;
                    outgoing[pos++] = (byte)c;
                }
            }
            else if (bits == 8)
            {
                for (i = 0; i < colors; ++i)
                {
                    outgoing[pos++] = rawBytes[i];
                }

                for (; i < rowBytes; ++i)
                {
                    outgoing[pos] = (byte)(outgoing[pos - colors] + rawBytes[i]);
                    pos++;
                }
            }
            else if (bits == 16)
            {
                var bytesPerPixel = colors * 2;
                for (i = 0; i < bytesPerPixel; ++i)
                {
                    outgoing[pos++] = rawBytes[i];
                }

                for (; i < rowBytes; i += 2)
                {
                    var sum =
                        ((rawBytes[i] & 0xff) << 8) +
                        (rawBytes[i + 1] & 0xff) +
                        ((outgoing[pos - bytesPerPixel] & 0xff) << 8) +
                        (outgoing[(pos - bytesPerPixel) + 1] & 0xff);
                    outgoing[pos++] = (byte)((sum >> 8) & 0xff);
                    outgoing[pos++] = (byte)(sum & 0xff);
                }
            }
            else
            {
                var bitMask = (1 << bits) - 1;
                var j = 0;
                var k = pos;
                for (i = 0; i < columns; ++i)
                {
                    for (var kk = 0; kk < colors; ++kk)
                    {
                        if (inbits < bits)
                        {
                            inbuf = (inbuf << 8) | (rawBytes[j++] & 0xff);
                            inbits += 8;
                        }

                        compArray[kk] = (byte)
                            ((compArray[kk] + (inbuf >> (inbits - bits))) & bitMask);
                        inbits -= bits;
                        outbuf = (outbuf << bits) | compArray[kk];
                        outbits += bits;
                        if (outbits >= 8)
                        {
                            outgoing[k++] = (byte)((outbuf >> (outbits - 8)) & 0xff);
                            outbits -= 8;
                        }
                    }
                }

                if (outbits > 0)
                {
                    outgoing[k++] = (byte)(
                        (outbuf << (8 - outbits)) + (inbuf & ((1 << (8 - outbits)) - 1))
                    );
                }
            }

            return rowBytes;
        }
    }
}