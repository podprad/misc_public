namespace PdfButcher.Internals.Model
{
    using System;
    using System.IO;
    using PdfButcher.Internals.IO;

    public abstract class PdfObject
    {
        /// <summary>
        /// If object is <see cref="PdfReference"/> then it uses <see cref="IPdfIndirectObjectsResolver"/> to resolve the value.
        /// Otherwise it returns self.
        /// </summary>
        public virtual PdfObject ResolveValueOrThrow(IPdfIndirectObjectsResolver resolver)
        {
            return this;
        }

        /// <summary>
        /// Tries to resolve .NET runtime value (like <see cref="long"/>).
        /// </summary>
        public bool TryResolveValue<TResult>(IPdfIndirectObjectsResolver resolver, out TResult value)
        {
            if (TryResolveValue(resolver, typeof(TResult), out var objectValue))
            {
                value = (TResult)objectValue;

                return true;
            }

            value = default;

            return false;
        }

        public virtual bool TryResolveValue(IPdfIndirectObjectsResolver resolver, Type type, out object value)
        {
            value = null;

            return false;
        }

        public override string ToString()
        {
            using (var buffer = new MemoryStream())
            {
                WriteTo(buffer);

                var array = buffer.ToArray();

                return EncodingHelper.RawDecode(array);
            }
        }

        public abstract PdfObject Clone(IPdfIndirectObjectsResolver resolver);

        public abstract void WriteTo(Stream stream);
    }
}