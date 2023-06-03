using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ACS_Common.Utils
{
    // code from https://github.com/NimaAra/Easy.Common by Nima Ara
    /// <summary>
    /// A set of extension methods for <see cref="Stream"/>.
    /// </summary>
    public static class StreamExtensions
    {
        private const char CR = '\r';
        private const char LF = '\n';
        private const char NULL = (char)0;

        /// <summary>
        /// Returns the number of lines in the given <paramref name="stream"/>.
        /// </summary>
        [DebuggerStepThrough]
        public static long CountLines(this Stream stream, Encoding encoding = default)
        {
            // todo null check
            // Ensure.NotNull(stream, nameof(stream));

            var lineCount = 0L;
            var byteBuffer = new byte[1024 * 1024];
            var detectedEOL = NULL;
            var currentChar = NULL;
            int bytesRead;

            if (encoding is null || Equals(encoding, Encoding.ASCII) || Equals(encoding, Encoding.UTF8))
            {
                while ((bytesRead = stream.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
                {
                    for (var i = 0; i < bytesRead; i++)
                    {
                        currentChar = (char)byteBuffer[i];

                        if (detectedEOL != NULL)
                        {
                            if (currentChar == detectedEOL)
                            {
                                lineCount++;
                            }
                        }
                        else if (currentChar == LF || currentChar == CR)
                        {
                            detectedEOL = currentChar;
                            lineCount++;
                        }
                    }
                }
            } 
            else
            {
                var charBuffer = new char[byteBuffer.Length];

                while ((bytesRead = stream.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
                {
                    var charCount = encoding.GetChars(byteBuffer, 0, bytesRead, charBuffer, 0);

                    for (var i = 0; i < charCount; i++)
                    {
                        currentChar = charBuffer[i];

                        if (detectedEOL != NULL)
                        {
                            if (currentChar == detectedEOL)
                            {
                                lineCount++;
                            }
                        }
                        else if (currentChar == LF || currentChar == CR)
                        {
                            detectedEOL = currentChar;
                            lineCount++;
                        }
                    }
                }
            }

            if (currentChar != LF && currentChar != CR && currentChar != NULL)
            {
                lineCount++;
            }

            return lineCount;
        }
    }
}