namespace PdfButcher.Internals.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using PdfButcher.Internals.IO.LookAhead;
    using PdfButcher.Internals.Model;
    using PdfButcher.Internals.StreamExtensions;
    using PdfButcher.Internals.StreamExtensions.Internals;

    /// <summary>
    /// Responsible for reading/parsing PDF document.
    /// Wasn't called Parser to be keep consistent naming: PdfWriter, PdfReader.
    /// </summary>
    public class PdfReader
    {
        private readonly Stream _stream;
        private readonly PdfLexer _lexer;
        private readonly FuncLookAheadReader<PdfToken> _la;

        private readonly List<PdfToken> _tokensToDispose = new List<PdfToken>();

        internal PdfReader(Stream stream)
        {
            _stream = new BufferedStream(stream, PdfConstants.OptimalBufferSize);
            _lexer = new PdfLexer(_stream);
            _la = new FuncLookAheadReader<PdfToken>(() => _lexer.Next());
        }

        public static PdfReaderDocument ReadDocument(string filename)
        {
            return ReadDocument(File.OpenRead(filename), false);
        }

        public static PdfReaderDocument ReadDocument(Stream stream, bool documentShouldLeaveStreamOpen)
        {
            var reader = new PdfReader(stream);

            return reader.Read(documentShouldLeaveStreamOpen);
        }

        internal PdfReaderDocument Read(bool documentShouldLeaveStreamOpen)
        {
            try
            {
                var version = ReadVersion();

                var result = new PdfReaderDocument(_stream, this, documentShouldLeaveStreamOpen);
                result.Version = version;
                result.Trailer = new PdfDictionary();

                var nextXRefPosition = FindStartXRefPosition();

                if (nextXRefPosition < 0)
                {
                    throw CreateParseException($"{PdfKeywords.StartXRef} not found or broken");
                }

                while (nextXRefPosition > 0)
                {
                    _stream.Position = nextXRefPosition;

                    Reset();

                    SkipTokens(PdfTokenType.Whitespace, PdfTokenType.Newline);

                    // clippath.pdf example - support incorrect xref address
                    // Not sure if such crazy edge-cases should be supported.
                    // However - from user perspective it's best if tool can read any PDF, even if it's broken.
                    // if (LookAheadToken().TokenType != PdfTokenType.Integer || (LookAheadToken().TokenType != PdfTokenType.Keyword && LookAheadToken().KeywordValue != PdfKeywords.XRef))
                    // {
                    //     _stream.Position = nextXRefPosition;
                    //     Reset();
                    //
                    //     var foundIndex = NaiveSearch.IndexOf(_stream.ToBytesSource(), PdfKeywords.XRefBytes);
                    //     if (foundIndex > -1)
                    //     {
                    //         _stream.Position = nextXRefPosition + foundIndex;
                    //     }
                    // }

                    PdfObject prev;
                    PdfObject xRefStm;

                    if (LookAheadToken().TokenType == PdfTokenType.Integer)
                    {
                        // PDF 1.5 - Support for xref in stream.
                        // xref and trailer keyword should not be used.
                        // trailer Root property is inside XRef object.
                        var xRefObject = ReadIndirectObject();
                        result.XRefTable.AddIfNotExists(ReadStreamXRefEntries(xRefObject, result));

                        var xRefDictionary = xRefObject.PdfObject.AsOrThrow<PdfDictionary>();

                        // Prev - support for incremental update.
                        prev = xRefDictionary.Get(PdfNames.Prev);

                        xRefStm = null;

                        // ISO requires Size and Root
                        // Overwrite = false. Last xref update wins.
                        result.Trailer.MergeSelected(xRefDictionary, false, PdfNames.Root, PdfNames.Size, PdfNames.Info);
                    }
                    else
                    {
                        result.XRefTable.AddIfNotExists(ReadClassicXRefEntries());

                        SkipTokens(PdfTokenType.Newline);

                        ReadToken().RequireKeyword(PdfKeywords.Trailer);
                        var readTrailer = ReadObject().AsOrThrow<PdfDictionary>();

                        // Prev - support for incremental update.
                        prev = readTrailer.Get(PdfNames.Prev);

                        // XRefStm - hybrid xref table
                        xRefStm = readTrailer.Get(PdfNames.XRefStm);

                        result.Trailer.MergeSelected(readTrailer, false, PdfNames.Root, PdfNames.Size, PdfNames.Info);
                    }

                    if (xRefStm != null)
                    {
                        nextXRefPosition = xRefStm.AsOrThrow<PdfInteger>().Value;
                    }
                    else if (prev != null)
                    {
                        nextXRefPosition = prev.AsOrThrow<PdfInteger>().Value;
                    }
                    else
                    {
                        nextXRefPosition = -1;
                    }
                }

                var catalogRef = result.Trailer.GetOrThrow(PdfNames.Root).AsOrThrow<PdfReference>();
                var catalog = result.GetIndirectObjectOrThrow(catalogRef.ReferencedObjectId);
                result.Catalog = catalog;

                var catalogDictionary = catalog.PdfObject.AsOrThrow<PdfDictionary>();
                if (catalogDictionary.TryGet(PdfNames.Version, out var versionObject))
                {
                    var versionString = versionObject.ToString();
                    var readVersion = ParseVersionFromString(versionString);
                    if (readVersion > -1)
                    {
                        result.Version = readVersion;
                    }
                }

                return result;
            }
            catch
            {
                // We didn't manage to create document. Close stream if needed.
                if (!documentShouldLeaveStreamOpen)
                {
                    _stream.Dispose();
                }

                throw;
            }
            finally
            {
                _la.Clear();
            }
        }

        internal PdfIndirectObject ReadIndirectObjectAtOffset(long offset)
        {
            _stream.Position = offset;

            Reset();

            return ReadIndirectObject();
        }

        internal IEnumerable<PdfIndirectObject> ReadIndirectObjectsFromObjectsStream(IPdfIndirectObjectsResolver resolver, PdfIndirectObject pdfIndirectObject)
        {
            var streamDictionary = pdfIndirectObject.PdfObject.AsOrThrow<PdfDictionary>();

            // Do we need to validate if it contains /Type /XRef? Maybe for reading not.
            var numberOfObjects = streamDictionary.Get(PdfNames.N).ResolveValueOrThrow(resolver).AsOrThrow<PdfInteger>();
            var firstObjectOffset = streamDictionary.Get(PdfNames.First).ResolveValueOrThrow(resolver).AsOrThrow<PdfInteger>();
            var extends = streamDictionary.Get(PdfNames.Extends)?.AsOrNull<PdfReference>();
            if (extends != null)
            {
                throw new PdfException("TODO add support for /Extends in objects streams");
            }

            var decodedStream = pdfIndirectObject.PdfStream.GetDecodedStream(streamDictionary, resolver);
            using (var bufferStream = new MemoryStream())
            {
                decodedStream.CopyTo(bufferStream);
                bufferStream.Seek(0, SeekOrigin.Begin);

                var objectStreamParser = new PdfReader(bufferStream);
                var objectNumbersAndOffsets = new Dictionary<long, long>();

                // Parse header
                for (int i = 0; i < numberOfObjects.Value; i++)
                {
                    objectStreamParser.SkipTokens(PdfTokenType.Whitespace);
                    var objectNumberToken = objectStreamParser.ReadToken().RequireType(PdfTokenType.Integer);
                    objectStreamParser.SkipTokens(PdfTokenType.Whitespace);
                    var offsetFromFirstToken = objectStreamParser.ReadToken().RequireType(PdfTokenType.Integer);
                    var offset = firstObjectOffset.Value + offsetFromFirstToken.IntegerValue;
                    objectNumbersAndOffsets[objectNumberToken.IntegerValue] = offset;
                }

                // Parse objects
                foreach (var pair in objectNumbersAndOffsets)
                {
                    var objectNumber = pair.Key;
                    var offset = pair.Value;

                    objectStreamParser.Reset();
                    objectStreamParser._stream.Position = offset;
                    var parsedObject = objectStreamParser.ReadObject();
                    var parsedIndirectObject = new PdfIndirectObject(new PdfObjectId(objectNumber, 0), parsedObject);

                    yield return parsedIndirectObject;
                }
            }

            yield break;
        }

        internal PdfIndirectObject ReadIndirectObject()
        {
            var objectNumber = SkipTokensAndNext(PdfTokenType.Whitespace, PdfTokenType.Newline, PdfTokenType.Comment)
                .RequireType(PdfTokenType.Integer).IntegerValue;

            ReadToken().RequireType(PdfTokenType.Whitespace);

            var revisionNumber = SkipTokensAndNext(PdfTokenType.Whitespace)
                .RequireType(PdfTokenType.Integer).IntegerValue;

            SkipTokensAndNext(PdfTokenType.Whitespace)
                .RequireKeyword(PdfKeywords.Obj);

            var pdfObjectId = new PdfObjectId(objectNumber, revisionNumber);
            var directObject = ReadObject();
            var pdfStream = ReadStream(directObject);

            var result = new PdfIndirectObject(pdfObjectId, directObject, pdfStream);

            return result;
        }

        internal PdfStream ReadStream(PdfObject owner)
        {
            if (owner is PdfDictionary)
            {
                var nextToken = SkipTokensAndNext(PdfTokenType.Whitespace, PdfTokenType.Newline);
                if (nextToken.TokenType == PdfTokenType.Keyword && nextToken.KeywordValue == PdfKeywords.Stream)
                {
                    var newLineToken = ReadToken().RequireOneOfTypes(PdfTokenType.Newline, PdfTokenType.Whitespace);

                    return new PdfStream
                    {
                        StartPosition = newLineToken.Position + newLineToken.Length,
                        EncodedStream = _stream,
                    };
                }
            }

            return null;
        }

        internal PdfObject ReadObject()
        {
            var token = SkipTokensAndNext(PdfTokenType.Whitespace, PdfTokenType.Newline, PdfTokenType.Comment);

            if (token.TokenType == PdfTokenType.Name)
            {
                return new PdfName(token.NameValue);
            }

            if (token.TokenType == PdfTokenType.DictionaryStart)
            {
                var pdfDictionary = new PdfDictionary();

                while (true)
                {
                    var subtoken = SkipTokensAndNext(PdfTokenType.Whitespace, PdfTokenType.Newline, PdfTokenType.Comment);

                    if (subtoken.TokenType == PdfTokenType.DictionaryEnd)
                    {
                        break;
                    }

                    var nameToken = subtoken.RequireType(PdfTokenType.Name);
                    var key = nameToken.NameValue;
                    var value = ReadObject();

                    pdfDictionary.Add(new PdfName(key), value);
                }

                return pdfDictionary;
            }

            if (token.TokenType == PdfTokenType.ArrayStart)
            {
                var pdfArray = new PdfArray();

                while (true)
                {
                    var ahead = LookAheadToken();
                    if (ahead.TokenType == PdfTokenType.Whitespace || ahead.TokenType == PdfTokenType.Newline)
                    {
                        ReadToken();

                        continue;
                    }

                    if (ahead.TokenType == PdfTokenType.ArrayEnd)
                    {
                        ReadToken();

                        break;
                    }

                    var nextObject = ReadObject();
                    if (nextObject != null)
                    {
                        pdfArray.Add(nextObject);
                    }
                }

                return pdfArray;
            }

            if (token.TokenType == PdfTokenType.Integer)
            {
                var ws1 = LookAheadToken(1);
                var token2 = LookAheadToken(2);
                var ws2 = LookAheadToken(3);
                var token3 = LookAheadToken(4);

                if (ws1.TokenType == PdfTokenType.Whitespace
                    && token2.TokenType == PdfTokenType.Integer
                    && ws2.TokenType == PdfTokenType.Whitespace
                    && token3.TokenType == PdfTokenType.Keyword
                    && token3.KeywordValue == PdfKeywords.R)
                {
                    ReadTokens(4);
                    var pdfObjectId = new PdfObjectId(token.IntegerValue, token2.IntegerValue);

                    return new PdfReference(pdfObjectId);
                }
                else
                {
                    return new PdfInteger(token.IntegerValue);
                }
            }

            if (token.TokenType == PdfTokenType.Real)
            {
                return new PdfReal(token.DecimalValue);
            }

            if (token.TokenType == PdfTokenType.True)
            {
                return new PdfBoolean(true);
            }

            if (token.TokenType == PdfTokenType.False)
            {
                return new PdfBoolean(false);
            }

            if (token.TokenType == PdfTokenType.LiteralString)
            {
                return new PdfString(token.LiteralStringValue);
            }

            if (token.TokenType == PdfTokenType.HexString)
            {
                return new PdfHexString(token.HexStringValue);
            }

            if (token.TokenType == PdfTokenType.Null)
            {
                return new PdfNull();
            }

            throw new PdfException($"Unknown object type at offset {_stream.Position}");
        }

        internal int ReadVersion()
        {
            _stream.Position = 0;

            Reset();

            var token = ReadToken();
            if (token.TokenType == PdfTokenType.Comment)
            {
                return ParseVersionFromString(token.CommentValue);
            }

            return -1;
        }

        private int ParseVersionFromString(string stringValue)
        {
            var numbers = stringValue.Where(c => char.IsDigit(c)).ToArray();
            var numbersString = new string(numbers);
            if (!string.IsNullOrEmpty(numbersString) && int.TryParse(numbersString, out var versionNumber))
            {
                return versionNumber;
            }

            return -1;
        }

        private IEnumerable<XRefEntry> ReadStreamXRefEntries(PdfIndirectObject xRefObject, IPdfIndirectObjectsResolver pdfIndirectObjectsResolver)
        {
            var streamOwner = xRefObject.PdfObject.AsOrThrow<PdfDictionary>();

            // Example
            // << /Root 1 0 R /Info 4 0 R /XRef (stream) /ID [ (E5-4A-71-C4-43-E) (E5-4A-71-C4-43-E) ]
            // /Type /XRef /W [ 1 2 2 ] /Index [ 0 5 7 7 ] /Size 14 /Filter /FlateDecode /Length 50 >>

            // ISO 7.5.8.2 Page 66
            // Type (required)
            // The type of PDF object that this dictionary describes; shall
            // be XRef for a cross-reference stream.
            var type = streamOwner.GetOrThrow(PdfNames.Type).AsOrThrow<PdfName>();
            if (type.Value != PdfNames.XRef)
            {
                throw new PdfException($"Expected {PdfNames.Type} to be {PdfNames.XRef} (cross-reference stream), but found {type.Value}");
            }

            // ISO 7.5.8.2 Page 66
            // Size (Required) The number one greater than the highest object number
            // used in this section or in any section for which this shall be an update.
            // It shall be equivalent to the Size entry in a trailer dictionary
            var size = streamOwner.Get(PdfNames.Size).AsOrThrow<PdfInteger>();

            // W Required array of 3 integers
            // Denotes the fields layout.
            var w = streamOwner.Get(PdfNames.W).AsOrThrow<PdfArray>();
            if (w.Count != 3)
            {
                throw new PdfException($"{PdfNames.XRef} object broken. {PdfNames.W} should be an array of 3 integers, but found: {w}");
            }

            // Index, Optional, default [0 Size]
            // Declaration of sub-sections.
            var index = streamOwner.Get(PdfNames.Index)
                ?.AsOrThrow<PdfArray>().Values.Select(v => v.AsOrThrow<PdfInteger>().Value).ToArray();
            index = index ?? new[] { 0, size.Value };

            // TODO: A value of zero for an element in the W array indicates that the
            // corresponding field shall not be present in the stream, and the default
            // value shall be used, if there is one. A value of zero shall not be used for
            // the second element of the array. If the first element is zero, the type
            // field shall not be present, and shall default to Type 1.
            // The sum of the items shall be the total length of each entry; it can be
            // used with the Index array to determine the starting position of each
            // subsection
            var fieldsLayout = w.Values.Select(g => g.AsOrThrow<PdfInteger>().Value).ToList();
            var field1Length = fieldsLayout[0];
            var field2Length = fieldsLayout[1];
            var field3Length = fieldsLayout[2];

            if (field1Length == 0)
            {
                throw new PdfException("TODO W case 1");
            }

            if (field3Length == 0)
            {
                throw new PdfException("TODO W case 2");
            }

            var stream = xRefObject.PdfStream.GetDecodedStream(streamOwner, pdfIndirectObjectsResolver);
            var fieldBuffer = new byte[field1Length + field2Length + field3Length];

            for (var i = 0; i < index.Length; i += 2)
            {
                var startObjectId = index[i];
                var objectsLeft = index[i + 1];
                var currentObjectId = startObjectId;

                while (true)
                {
                    Array.Clear(fieldBuffer, 0, fieldBuffer.Length);

                    var readBytes = stream.Read(fieldBuffer, 0, fieldBuffer.Length);
                    if (readBytes != fieldBuffer.Length)
                    {
                        throw new PdfException($"xref broken. Fields length should be {fieldBuffer.Length}");
                    }

                    var field1Type = (long)EncodingHelper.DecodeBigEndianInt(fieldBuffer, 0, (int)field1Length);
                    var field2 = (long)EncodingHelper.DecodeBigEndianInt(fieldBuffer, (int)field1Length, (int)field2Length);
                    var field3 = (long)EncodingHelper.DecodeBigEndianInt(fieldBuffer, (int)(field1Length + field2Length), (int)field3Length);

                    // 0 - linked list of free objects
                    // The type of this entry, which shall be 0. Type 0 entries,
                    // define the linked list of free objects (corresponding to f
                    // entries in a cross-reference table). Default value: 0
                    if (field1Type == 0)
                    {
                        yield return new XRefEntry
                        {
                            ObjectNumber = currentObjectId,
                            Free = true,
                            Offset = field2,
                            Revision = field3,
                        };
                    }

                    // Type 1 entries define objects that are in use but are not
                    // compressed (corresponding to n entries in a cross-reference table).
                    else if (field1Type == 1)
                    {
                        // Field 2
                        // The byte offset of the object, starting from the beginning of
                        // the PDF file.Default value: 0.
                        // Field 3
                        // The generation number of the object. Default value: 0.
                        yield return new XRefEntry
                        {
                            Free = false,
                            Offset = field2,
                            Revision = field3,
                            ObjectNumber = currentObjectId,
                        };
                    }

                    // Type 2 entries define compressed objects.
                    else if (field1Type == 2)
                    {
                        // Field 2
                        // The object number of the object stream in which this
                        // object is stored. (The generation number of the object
                        // stream shall be implicitly 0.)
                        // Field 3
                        // The index of this object within the object stream.
                        // NOTE This index value will be between zero and the value of
                        // N minus 1 from the associated object stream
                        // dictionary
                        yield return new XRefEntry
                        {
                            Free = false,
                            ObjectNumber = currentObjectId,
                            Offset = field3,
                            Revision = 0,
                            IsInStream = true,
                            StreamObjectNumber = field2,
                        };
                    }

                    currentObjectId++;
                    objectsLeft--;

                    if (objectsLeft <= 0)
                    {
                        break;
                    }
                }
            }
        }

        private IEnumerable<XRefEntry> ReadClassicXRefEntries()
        {
            ReadToken().RequireKeyword(PdfKeywords.XRef);

            // We should require new line, but there are examples having also just whitespace (CPP 1.10\!Index.pdf).
            ReadToken()
                .RequireOneOfTypes(PdfTokenType.Whitespace, PdfTokenType.Newline);

            while (true)
            {
                SkipTokens(PdfTokenType.Newline);
                var ahead = LookAheadToken();

                // keyword "trailer" ends xref table.
                if (ahead.TokenType == PdfTokenType.Keyword)
                {
                    break;
                }

                foreach (var entry in ReadXRefSection())
                {
                    yield return entry;
                }
            }
        }

        private PdfException CreateParseException(string message)
        {
            throw new PdfException($"Parse error at offset {_stream.Position}. {message}");
        }

        private IEnumerable<XRefEntry> ReadXRefSection()
        {
            var startObjectNumber = ReadToken()
                .RequireType(PdfTokenType.Integer).IntegerValue;

            ReadToken().RequireType(PdfTokenType.Whitespace);

            var numberOfObjectsInSection = ReadToken().RequireType(PdfTokenType.Integer).IntegerValue;

            ReadNewLine();

            for (int i = 0; i < numberOfObjectsInSection; i++)
            {
                var objectOffset = ReadToken().RequireType(PdfTokenType.Integer).IntegerValue;

                ReadToken().RequireType(PdfTokenType.Whitespace);

                var revisionNumber = ReadToken().RequireType(PdfTokenType.Integer).IntegerValue;

                ReadToken().RequireType(PdfTokenType.Whitespace);

                var status = ReadToken()
                    .RequireAnyKeyword(PdfKeywords.F, PdfKeywords.N)
                    .KeywordValue;

                ReadNewLine();

                yield return new XRefEntry
                {
                    ObjectNumber = i + startObjectNumber,
                    Free = status == PdfKeywords.F,
                    Offset = objectOffset,
                    Revision = revisionNumber,
                };
            }
        }

        private long FindStartXRefPosition()
        {
            var startPos = Math.Max(0, _stream.Length - 1024);
            _stream.Position = startPos;

            long lastFoundIndex = -1;

            var bytesSource = _stream.ToBytesSource();
            while (true)
            {
                var currentPosition = _stream.Position;
                var foundIndex = NaiveSearch.IndexOf(bytesSource, PdfKeywords.StartXRefBytes);
                if (foundIndex > -1)
                {
                    lastFoundIndex = currentPosition + foundIndex;
                }
                else
                {
                    break;
                }
            }

            if (lastFoundIndex < 0)
            {
                return -1;
            }

            _stream.Position = PdfKeywords.StartXRefBytes.Length + lastFoundIndex;

            Reset();

            ReadNewLine();

            var result = ReadToken().RequireType(PdfTokenType.Integer).IntegerValue;

            return result;
        }

        private void Reset()
        {
            _la.Clear();
            _lexer.Reset();
        }

        private PdfToken LookAheadToken(int howFar = 1)
        {
            var token = _la.LookAhead(howFar);

            return token;
        }

        private void SkipTokens(params PdfTokenType[] types)
        {
            while (true)
            {
                var ahead = LookAheadToken();

                // Faster than Enumerable.Contains
                var found = false;
                foreach (var tokenType in types)
                {
                    if (tokenType == ahead.TokenType)
                    {
                        found = true;

                        break;
                    }
                }

                if (found)
                {
                    ReadToken();
                }
                else
                {
                    break;
                }
            }
        }

        private PdfToken SkipTokensAndNext(params PdfTokenType[] types)
        {
            SkipTokens(types);

            return ReadToken();
        }

        private void ReadTokens(int count)
        {
            for (int i = 0; i < count; i++)
            {
                ReadToken();
            }
        }

        private void ReadNewLine()
        {
            // There should be no whitespaces, but Pdf14SimplestWithXmpRemovedXmpByIncrementalUpdate.pdf has them.
            // Pdf14Inkscape.pdf has spaces after xref entry.
            // bug1513120_reduced - breaks specification, but added support.
            ReadToken().RequireOneOfTypes(PdfTokenType.Newline, PdfTokenType.Whitespace);
            SkipTokens(PdfTokenType.Whitespace, PdfTokenType.Newline);
        }

        private PdfToken ReadToken()
        {
            var token = _la.Read();

            _tokensToDispose.Add(token);

            return token;
        }
    }
}