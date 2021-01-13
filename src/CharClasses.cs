﻿using System;
using System.Collections.Generic;
using System.Text;

#nullable enable

namespace kdl_net
{
    /**
     * Various functions used during parsing and printing to check character membership in various character classes.
     *
     * Also contains functions for transforming characters into their escape sequences.
     */
    public class CharClasses
    {

        /**
         * Check if the character is valid at the beginning of a numeric value
         *
         * @param c the character to check
         * @return true if the character is valid, false otherwise
         */
        public static bool IsValidNumericStart(int c)
        {
            switch (c)
            {
                case '+':
                case '-':
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    return true;
                default:
                    return false;
            }
        }

        /**
         * Check if the character is valid in a bare identifier after the first character
         *
         * @param c the character to check
         * @return true if the character is valid, false otherwise
         */
        public static bool IsValidBareIdChar(int c)
        {
            switch (c)
            {
                case '\n':
                case '\u000C':
                case '\r':
                case '\u0085':
                case '\u2028':
                case '\u2029':
                case '\\':
                case '{':
                case '}':
                case '<':
                case '>':
                case ';':
                case '[':
                case ']':
                case '=':
                case ',':
                case '"':
                case '\u0009':
                case '\u0020':
                case '\u00A0':
                case '\u1680':
                case '\u2000':
                case '\u2001':
                case '\u2002':
                case '\u2003':
                case '\u2004':
                case '\u2005':
                case '\u2006':
                case '\u2007':
                case '\u2008':
                case '\u2009':
                case '\u200A':
                case '\u202F':
                case '\u205F':
                case '\u3000':
                    return false;
                default:
                    return true;
            }
        }

        /**
         * Check if the character is valid in a bare identifier as the first character
         *
         * @param c the character to check
         * @return true if the character is valid, false otherwise
         */
        public static bool IsValidBareIdStart(int c) => !IsValidDecimalChar(c) && IsValidBareIdChar(c);

        /**
         * Check if the character is a valid decimal digit
         *
         * @param c the character to check
         * @return true if the character is valid, false otherwise
         */
        public static bool IsValidDecimalChar(int c) => ('0' <= c && c <= '9');

        /**
         * Check if the character is a valid hexadecimal digit
         *
         * @param c the character to check
         * @return true if the character is valid, false otherwise
         */
        public static bool IsValidHexChar(int c)
        {
            switch (c)
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case 'A':
                case 'a':
                case 'B':
                case 'b':
                case 'C':
                case 'c':
                case 'D':
                case 'd':
                case 'E':
                case 'e':
                case 'F':
                case 'f':
                    return true;
                default:
                    return false;
            }
        }

        /**
         * Check if the character is a valid octal digit
         *
         * @param c the character to check
         * @return true if the character is valid, false otherwise
         */
        public static bool IsValidOctalChar(int c) => ('0' <= c && c <= '7');

        /**
         * Check if the character is a valid binary digit
         *
         * @param c the character to check
         * @return true if the character is valid, false otherwise
         */
        public static bool IsValidBinaryChar(int c) => (c == '0' || c == '1');

        /**
         * Check if the character is contained in one of the three literal values: true, false, and null
         *
         * @param c the character to check
         * @return true if the character appears in a literal, false otherwise
         */
        public static bool IsLiteralChar(int c)
        {
            switch (c)
            {
                case 't':
                case 'r':
                case 'u':
                case 'e':
                case 'n':
                case 'l':
                case 'f':
                case 'a':
                case 's':
                    return true;
                default:
                    return false;
            }
        }

        /**
         * Check if the character is a unicode newline of any kind
         *
         * @param c the character to check
         * @return true if the character is a unicode newline, false otherwise
         */
        public static bool IsUnicodeLinespace(int c)
        {
            switch (c)
            {
                case '\r':
                case '\n':
                case '\u0085':
                case '\u000C':
                case '\u2028':
                case '\u2029':
                    return true;
                default:
                    return false;
            }
        }

        /**
         * Check if the character is unicode whitespace of any kind
         *
         * @param c the character to check
         * @return true if the character is unicode whitespace, false otherwise
         */
        public static bool IsUnicodeWhitespace(int c)
        {
            switch (c)
            {
                case '\u0009':
                case '\u0020':
                case '\u00A0':
                case '\u1680':
                case '\u2000':
                case '\u2001':
                case '\u2002':
                case '\u2003':
                case '\u2004':
                case '\u2005':
                case '\u2006':
                case '\u2007':
                case '\u2008':
                case '\u2009':
                case '\u200A':
                case '\u202F':
                case '\u205F':
                case '\u3000':
                    return true;
                default:
                    return false;
            }
        }

        /**
         * Check if the character is an ASCII character that can be printed unescaped
         *
         * @param c the character to check
         * @return true if the character is printable unescaped, false otherwise
         */
        public static bool IsPrintableAscii(int c)
        {
            return ' ' <= c && c <= '~' && c != '/' && c != '"';
        }

        private static readonly string ESC_BACKSLASH = "\\\\";
        private static readonly string ESC_BACKSPACE = "\\b";
        private static readonly string ESC_NEWLINE = "\\n";
        private static readonly string ESC_FORM_FEED = "\\f";
        private static readonly string ESC_FORWARD_SLASH = "\\/";
        private static readonly string ESC_TAB = "\\t";
        private static readonly string ESC_CR = "\\r";
        private static readonly string ESC_QUOTE = "\\\"";

        /**
         * Get the escape sequence for characters from the ASCII character set
         *
         * @param c the character to check
         * @return An Optional wrapping the escape sequence string if the character needs to be escaped, or false otherwise
         */
        public static string? GetCommonEscape(int c) =>  c switch {
            '\\' => ESC_BACKSLASH,
            '\b' => ESC_BACKSPACE,
            '\n' => ESC_NEWLINE,
            '\f' => ESC_FORM_FEED,
            '/' => ESC_FORWARD_SLASH,
            '\t' => ESC_TAB,
            '\r' => ESC_CR,
            '"' => ESC_QUOTE,
            _ => null,
        };

        /**
         * Get the escape sequence for any character
         *
         * @param c the character to check
         * @return The escape sequence string
         */
        public static string GetEscapeIncludingUnicode(int c) => GetCommonEscape(c) ?? string.Format("\\u{0:x}", c);
    }
}
