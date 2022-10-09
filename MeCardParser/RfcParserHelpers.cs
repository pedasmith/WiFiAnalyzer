using System;
using System.Collections.Generic;
using System.Text;

namespace MeCardParser
{
    public static class RfcParserHelpers
    {
        /// <summary>
        /// True iff char is a-z A-Z or 0-9
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static bool IsAlphaNumericDigit(this char ch)
        {
            var retval = (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z');
            return retval;
        }
        public static bool IsHexDigit(this char ch)
        {
            var retval = (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
            return retval;
        }
        public static bool IsHexDigit(this string str)
        {
            foreach (var ch in str)
            {
                if (!ch.IsHexDigit()) return false;
            }
            return true;
        }
        /// <summary>
        /// BASE-64 encoding:  https://datatracker.ietf.org/doc/html/rfc4648#section-4
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static bool IsPKChar(this char ch)
        {
            var retval = ch.IsAlphaNumericDigit() || ch== '+' || ch=='/' || ch=='=';
            return retval;
        }
        public static bool IsPKChar(this string str)
        {
            foreach (var ch in str)
            {
                if (!ch.IsPKChar()) return false;
            }
            return true;
        }
        public static bool IsPrintable(this char ch)
        {
            var retval = (ch >= ' ' && ch <= '~') && ch != '%';
            return retval;
        }
        public static bool IsPrintable(this string str)
        {
            foreach (var ch in str)
            {
                if (!ch.IsPrintable()) return false;
            }
            return true;
        }
        /// <summary>
        /// From [7]: unreserved is ALPHA / DIGIT / "-" / "." / "_" / "~"
        /// [7] IETF RFC 3986, Uniform Resource Identifier (URI): Generic Syntax, https://tools.ietf.org/html/rfc3986
        /// </summary>
        public static bool IsUnreserved(this char ch)
        {
            var retval = (ch >= 'a' && ch <= 'z')
                || (ch >= 'A' && ch <= 'Z')
                || (ch >= '0' && ch <= '9')
                || ch == '-' || ch == '.' || ch == '_' || ch == '~'
                ;
            return retval;
        }
        /// <summary>
        /// From [7]: unreserved is ALPHA / DIGIT / "-" / "." / "_" / "~"
        /// [7] IETF RFC 3986, Uniform Resource Identifier (URI): Generic Syntax, https://tools.ietf.org/html/rfc3986
        /// </summary>
        public static bool IsUnreserved(this string str)
        {
            foreach (var ch in str)
            {
                if (!ch.IsUnreserved()) return false;
            }
            return true;
        }

        public static bool IsPercentEncoded(this char ch)
        {
            var retval = ch.IsUnreserved() || ch == '%';
            return retval;
        }
        public static bool IsPercentEncoded(this string str)
        {
            foreach (var ch in str)
            {
                if (!ch.IsPercentEncoded()) return false;
            }
            return true;
        }
    }
}
