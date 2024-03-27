namespace PdfButcher.Internals.Model
{
    using System.Collections.Generic;
    using System.IO;

    public class PdfDictionary : PdfObject
    {
        private readonly Dictionary<PdfName, PdfObject> _dictionary = new Dictionary<PdfName, PdfObject>();

        private readonly List<PdfName> _keysInOrder = new List<PdfName>();

        public IEnumerable<PdfName> Keys => _dictionary.Keys;

        public IEnumerable<PdfObject> Values => _dictionary.Values;

        public void Set(string name, PdfObject pdfObject)
        {
            Set(new PdfName(name), pdfObject);
        }

        public void Set(PdfName pdfName, PdfObject pdfObject)
        {
            if (!_dictionary.ContainsKey(pdfName))
            {
                _keysInOrder.Add(pdfName);
            }

            _dictionary[pdfName] = pdfObject;
        }

        public void Add(PdfName pdfName, PdfObject pdfObject)
        {
            _dictionary.Add(pdfName, pdfObject);
            _keysInOrder.Add(pdfName);
        }

        public PdfObject GetOrThrow(PdfName pdfName)
        {
            var result = Get(pdfName);
            if (result == null)
            {
                throw new PdfException($"Key {pdfName} not found in {this}");
            }

            return result;
        }

        /// <summary>
        /// Tries to get value from the dictionary. If value does not exist then returns <see cref="defaultValue"/>.
        /// If value exists it is resolved, so <see cref="PdfReference"/> will be changed to referenced object.
        /// </summary>
        public TResult GetAndResolveValueOrDefault<TResult>(IPdfIndirectObjectsResolver resolver, string name, TResult defaultValue)
        {
            var pdfObject = Get(name);
            if (pdfObject == null)
            {
                return defaultValue;
            }

            if (!pdfObject.TryResolveValue<TResult>(resolver, out var value))
            {
                return defaultValue;
            }

            return value;
        }

        /// <summary>
        /// Gets value by name. Throws if value was not found.
        /// </summary>
        public PdfObject GetOrThrow(string key)
        {
            return GetOrThrow(new PdfName(key));
        }

        /// <summary>
        /// Gets value by name. Returns true if found, otherwise false.
        /// </summary>
        public bool TryGet(string name, out PdfObject result)
        {
            result = Get(name);

            return result != null;
        }

        /// <summary>
        /// Gets value by name. Returns null if not found.
        /// </summary>
        public PdfObject Get(string name)
        {
            return Get(new PdfName(name));
        }

        /// <summary>
        /// Gets value by name. Returns null if not found.
        /// </summary>
        public PdfObject Get(PdfName pdfName)
        {
            if (_dictionary.TryGetValue(pdfName, out PdfObject pdfObject))
            {
                return pdfObject;
            }

            return null;
        }

        public override PdfObject Clone(IPdfIndirectObjectsResolver resolver)
        {
            var clone = new PdfDictionary();
            foreach (var key in _keysInOrder)
            {
                var value = GetOrThrow(key);
                clone.Add((PdfName)key.Clone(resolver), value.Clone(resolver));
            }

            return clone;
        }

        public override void WriteTo(Stream stream)
        {
            stream.Write(PdfConstants.DictionaryStartArray, 0, PdfConstants.DictionaryStartArray.Length);
            stream.WriteByte(PdfConstants.WhiteSpaceByte);

            for (var index = 0; index < _keysInOrder.Count; index++)
            {
                var key = _keysInOrder[index];
                var value = _dictionary[key];

                key.WriteTo(stream);
                stream.WriteByte(PdfConstants.WhiteSpaceByte);
                value.WriteTo(stream);

                if (index != _keysInOrder.Count - 1)
                {
                    stream.WriteByte(PdfConstants.WhiteSpaceByte);
                }
            }

            stream.WriteByte(PdfConstants.WhiteSpaceByte);
            stream.Write(PdfConstants.DictionaryEndArray, 0, PdfConstants.DictionaryEndArray.Length);
        }

        public void MergeNotExisting(PdfDictionary another)
        {
            foreach (var key in another.Keys)
            {
                if (Get(key) == null)
                {
                    var value = another.Get(key);
                    Add(key, value);
                }
            }
        }

        public void MergeSelected(PdfDictionary another, bool overwrite, params string[] keys)
        {
            foreach (var key in keys)
            {
                var value = another.Get(key);
                if (value != null)
                {
                    var existing = Get(key);
                    if (existing != null)
                    {
                        if (overwrite)
                        {
                            Set(key, value);
                        }
                    }
                    else
                    {
                        Set(key, value);
                    }
                }
            }
        }
    }
}