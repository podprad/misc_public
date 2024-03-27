namespace PdfButcher.Internals.IO
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;

    internal static class EncodingHelper
    {
        public static void RawWrite(Stream stream, string value)
        {
            var bytes = RawEncode(value);
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void RawWriteLine(Stream stream, string value = "")
        {
            var bytes = RawEncode(value + "\r\n");
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void RawWrite(Stream stream, char value)
        {
            stream.WriteByte((byte)value);
        }

        public static byte[] RawEncode(string value)
        {
            return PdfConstants.PdfFileEncoding.GetBytes(value);
        }

        public static string RawDecode(byte[] value)
        {
            return PdfConstants.PdfFileEncoding.GetString(value);
        }

        public static string RawDecode(byte[] value, int offset, int length)
        {
            return PdfConstants.PdfFileEncoding.GetString(value, offset, length);
        }

        public static ulong DecodeBigEndianInt(byte[] buffer, int offset, int length)
        {
            var temp = new byte[8];
            var destIndex = (temp.Length - length);
            Array.Copy(buffer, offset, temp, destIndex, length);

            if (BitConverter.IsLittleEndian)
            {
                // PDF uses big endian, switch to little endian.
                temp = temp.Reverse().ToArray();
            }

            var result = BitConverter.ToUInt64(temp, 0);

            return result;
        }

        public static bool IsNameCharacter(char b)
        {
            return CommonHelpers.IsLetter(b) || CommonHelpers.IsDigit(b) || ",.;_-*?$@+&".Contains(b);
        }

        public static long DecodeNumber(byte[] data, int count)
        {
            long parsedNumber = 0;
            bool negative = false;
            const byte Diff = (byte)'0';
            const byte Plus = (byte)'+';
            const byte Minus = (byte)'-';
            for (int i = 0; i < count; i++)
            {
                var character = data[i];
                if (character == Plus)
                {
                    continue;
                }

                if (character == Minus)
                {
                    negative = true;

                    continue;
                }

                var numeric = character - Diff;
                parsedNumber = (parsedNumber * 10) + numeric;
            }

            if (negative)
            {
                parsedNumber *= -1;
            }

            return parsedNumber;
        }

        public static string DecodeName(byte[] data, int count, long infoPosition)
        {
            // Start from 1. Name token guarantees that first char is /
            var result = new StringBuilder();
            for (int i = 1; i < count; i++)
            {
                if (data[i] == '#')
                {
                    var number1 = (char)data.ElementAtOrDefault(i + 1);
                    var number2 = (char)data.ElementAtOrDefault(i + 2);
                    if (number1 == 0 || number2 == 0)
                    {
                        throw new PdfException($"Position {infoPosition}. Incorrect name.");
                    }

                    var hexValue = $"{number1}{number2}";
                    var decodedChar = Convert.ToInt32(hexValue, 16);
                    var c = (char)decodedChar;
                    result.Append(c);

                    i += 2;
                }
                else
                {
                    result.Append((char)data[i]);
                }
            }

            return result.ToString();
        }

        public static void EncodeName(string value, Stream stream)
        {
            stream.WriteByte((byte)'/');

            var array = value.ToArray();

            for (int i = 0; i < array.Length; i++)
            {
                var c = array[i];

                if (IsNameCharacter(c))
                {
                    stream.WriteByte((byte)c);
                }
                else
                {
                    stream.WriteByte((byte)'#');

                    var hexString = Convert.ToString(c, 16).ToUpper();
                    if (hexString.Length < 2)
                    {
                        stream.WriteByte((byte)'0');
                        stream.WriteByte((byte)hexString[0]);
                    }
                    else
                    {
                        stream.WriteByte((byte)hexString[0]);
                        stream.WriteByte((byte)hexString[1]);
                    }
                }
            }
        }

        public static void EncodeLiteralString(string value, Stream stream)
        {
            var inputArray = value.ToArray();

            RawWrite(stream, '(');

            for (var i = 0; i < inputArray.Length; i++)
            {
                // nrtbf()\\
                var c = inputArray[i];

                if (c == '\n')
                {
                    RawWrite(stream, '\\');
                    RawWrite(stream, 'n');
                }
                else if (c == '\r')
                {
                    RawWrite(stream, '\\');
                    RawWrite(stream, 'r');
                }
                else if (c == '\t')
                {
                    RawWrite(stream, '\\');
                    RawWrite(stream, 't');
                }
                else if (c == '\b')
                {
                    RawWrite(stream, '\\');
                    RawWrite(stream, 'b');
                }
                else if (c == '\f')
                {
                    RawWrite(stream, '\\');
                    RawWrite(stream, 'f');
                }
                else if (c == '\\')
                {
                    RawWrite(stream, '\\');
                    RawWrite(stream, '\\');
                }
                else if (c == '(')
                {
                    RawWrite(stream, '\\');
                    RawWrite(stream, '(');
                }
                else if (c == ')')
                {
                    RawWrite(stream, '\\');
                    RawWrite(stream, ')');
                }
                else
                {
                    RawWrite(stream, c);
                }
            }

            RawWrite(stream, ')');
        }

        public static string DecodeLiteralString(byte[] data, int count)
        {
            var result = new StringBuilder();

            var encodingIn = new byte[1];
            var encodingOut = new char[1];

            for (int i = 0; i < count; i++)
            {
                // skip ( or )
                if (i == 0 || i == count - 1)
                {
                    continue;
                }

                var c = (char)data[i];

                // decode escaped char
                if (c == '\\')
                {
                    if (TryDecodeEscapedChar(data, i + 1, out var escapedChar))
                    {
                        i += 1;
                        result.Append(escapedChar);

                        continue;
                    }

                    if (TryDecodeOctalChar(data, i + 1, out var octalChar, out var charsConsumed))
                    {
                        i += charsConsumed;
                        result.Append(octalChar);

                        continue;
                    }
                }

                encodingIn[0] = (byte)c;

                PdfConstants.PdfFileEncoding.GetChars(encodingIn, 0, 1, encodingOut, 0);
                result.Append(encodingOut[0]);
            }

            return result.ToString();
        }

        public static string EncodeHexString(byte[] characters)
        {
            var sequence = characters ?? Array.Empty<byte>();

            var result = new StringBuilder();
            result.Append("<");

            foreach (var b in sequence)
            {
                var hexValue = Convert.ToString(b, 16).ToUpper();
                if (hexValue.Length == 1)
                {
                    hexValue = "0" + hexValue;
                }

                result.Append(hexValue);
            }

            result.Append(">");

            return result.ToString();
        }

        /// <summary>
        /// "&lt;AF98&gt;" => [ 0xAF, 0x98 ]
        /// </summary>
        public static byte[] DecodeHexCharacters(byte[] hexCharacters, int length)
        {
            var charArray = hexCharacters
                .Take(length)
                .Where(g => g != '<' && g != '>')
                .Select(c => (char)c)
                .ToArray();

            var resultLength = charArray.Length;
            if (resultLength % 2 != 0)
            {
                // Make it even.
                resultLength++;
            }

            resultLength /= 2;

            var resultArray = new byte[resultLength];
            var resultIndex = 0;

            for (int i = 0; i < charArray.Length; i += 2)
            {
                var highByte = (byte)charArray[i];
                var lowByte = (i + 1 >= charArray.Length) ? (byte)'0' : (byte)charArray[i + 1];

                highByte = Convert.ToByte(((char)highByte).ToString(), 16);
                lowByte = Convert.ToByte(((char)lowByte).ToString(), 16);

                highByte <<= 4;

                var mergedByte = highByte + lowByte;
                resultArray[resultIndex] = (byte)mergedByte;
                resultIndex++;
            }

            return resultArray;
        }

        private static bool TryDecodeEscapedChar(byte[] source, int startPos, out char escapedChar)
        {
            escapedChar = '\0';

            var c1 = (char)source.ElementAtOrDefault(startPos);

            switch (c1)
            {
                case 'n':
                    escapedChar = '\n';

                    return true;

                case 'r':
                    escapedChar = '\r';
                    ;

                    return true;

                case 't':
                    escapedChar = '\t';

                    return true;

                case 'b':
                    escapedChar = '\b';

                    return true;

                case 'f':
                    escapedChar = '\f';

                    return true;

                case '(':
                    escapedChar = '(';

                    return true;

                case ')':
                    escapedChar = ')';

                    return true;

                case '\\':
                    escapedChar = '\\';

                    return true;
            }

            return false;
        }

        private static bool TryDecodeOctalChar(byte[] source, int startPos, out char escapedChar, out int charsConsumed)
        {
            var digit1 = source.ElementAtOrDefault(startPos);

            var word = string.Empty;
            if (CommonHelpers.IsOctDigit(digit1))
            {
                word += (char)digit1;

                var digit2 = source.ElementAtOrDefault(startPos + 1);
                if (CommonHelpers.IsOctDigit(digit2))
                {
                    word += (char)digit2;

                    var digit3 = source.ElementAtOrDefault(startPos + 2);
                    if (CommonHelpers.IsOctDigit(digit3))
                    {
                        word += (char)digit3;
                    }
                }
            }

            if (word.Length > 0)
            {
                var characterCode = Convert.ToInt32(word, 8);
                escapedChar = (char)characterCode;
                charsConsumed = word.Length;

                return true;
            }

            escapedChar = '\0';
            charsConsumed = 0;

            return false;
        }
    }
}