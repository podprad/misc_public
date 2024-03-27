namespace PdfButcher.Internals.StreamExtensions.Internals
{
    using System;
    using System.Linq;

    internal static class ArrayHelper
    {
        public static void ShiftLeft<T>(this T[] a)
        {
            Array.Copy(
                sourceArray: a,
                sourceIndex: 1,
                destinationArray: a,
                destinationIndex: 0,
                length: a.Length - 1);
        }

        public static bool ArrayEqual<T>(this T[] a, T[] b)
        {
            return a.SequenceEqual(b);
        }

        public static T GetLast<T>(this T[] a)
        {
            return a[a.Length - 1];
        }

        public static void SetLast<T>(this T[] a, T value)
        {
            a[a.Length - 1] = value;
        }

        public static bool ContainsSuffix(byte[] word, int wordLength, byte[] suffix, int suffixLength)
        {
            if (suffixLength == 0)
            {
                return false;
            }

            int matchCount = 0;

            int i = wordLength - 1;
            int j = suffixLength - 1;

            while (true)
            {
                if (j < 0 || i < 0)
                {
                    break;
                }

                if (word[i] == suffix[j])
                {
                    matchCount++;
                }
                else
                {
                    break;
                }

                i--;
                j--;
            }

            var result = matchCount == suffixLength;

            return result;
        }
    }
}