﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

#nullable enable

namespace KdlDotNet
{
    /** A KDL Number can be internally stored as either an int or double. This enum tells you the native storage type used by a given KDLNumber instance */
    public enum KDLNumberType : int
    {
        Int32,
        Int64,
        Float64,
        BigInteger
    }

    // used to pass context information from KDLParser.ParseNumber into KDLNumber.From to reduce duplicated logic
    [Flags]
    internal enum KDLNumberParseFlags
    {
        None = 0,
        HasDecimalPoint = 1,
        HasScientificNotation = 2,
    }

    /**
     * Representation of a KDL number. Numbers may be base 16, 10, 8, or 2 as stored in the radix field. Base 10 numbers may
     * be fractional, but all others are limited to integers.
     */
    public abstract class KDLNumber : KDLValue
    {
        public static KDLNumber Zero(int radix) => Zero(radix, null);

        public static KDLNumber Zero(int radix, string? type)
        {
            switch (radix)
            {
                case 2:
                case 8:
                case 10:
                case 16:
                    return new KDLNumberInt32(0, radix, type);
                default:
                    throw new ArgumentException("Radix must be one of: [2, 8, 10, 16]", nameof(radix));
            }
        }

        public static KDLNumber From(int value, int radix = 10, string? type = null) => new KDLNumberInt32(value, radix, type);

        public static KDLNumber From(long value, int radix = 10, string? type = null) => new KDLNumberInt64(value, radix, type);

        public static KDLNumber From(double value, int radix = 10, string? type = null) => new KDLNumberDouble(value, radix, type);

        public static KDLNumber From(BigInteger value, int radix = 10, string? type = null) => new KDLNumberBigInteger(value, radix, type);

        public static KDLNumber? From(string val, string? type = null)
        {
            if (val.Length == 0)
                return null;

            int radix;
            string toParse;
            KDLNumberParseFlags parseFlags = KDLNumberParseFlags.None;
            if (val[0] == '0')
            {
                if (val.Length == 1)
                {
                    return Zero(10, type);
                }

                switch (val[1])
                {
                    case 'x':
                        radix = 16;
                        toParse = val.Substring(2);
                        break;
                    case 'o':
                        radix = 8;
                        toParse = val.Substring(2);
                        break;
                    case 'b':
                        radix = 2;
                        toParse = val.Substring(2);
                        break;
                    default:
                        radix = 10;
                        toParse = val;
                        break;
                }
            }
            else
            {
                radix = 10;
                toParse = val;
            }

            foreach (var ch in toParse)
            {
                switch (ch)
                {
                    case 'e':
                    case 'E':
                        parseFlags |= KDLNumberParseFlags.HasScientificNotation;
                        break;
                    case '.':
                        parseFlags |= KDLNumberParseFlags.HasDecimalPoint;
                        break;
                }
            }

            return From(toParse, radix, type, parseFlags);
        }

        // val should be the number AFTER stripping off any 0x or 0b prefix
        internal static KDLNumber? From(string val, int radix, string? type, KDLNumberParseFlags flags)
        {
            if (val.Length == 0)
                return null;

            try
            {
                if (radix == 10 && flags.HasFlag(KDLNumberParseFlags.HasDecimalPoint))
                {
                    return new KDLNumberDouble(double.Parse(val), flags, radix, type); // deliberate throw on parse so we can catch formatException
                }
                else if (radix == 10 && flags.HasFlag(KDLNumberParseFlags.HasScientificNotation))
                {
                    return new KDLNumberDouble((double)decimal.Parse(val, System.Globalization.NumberStyles.Float), flags, radix, type); // the literal 1e10 is double in C#, so we use that
                }

                try // the vast majority of numbers we are likely to see in a KDL file will be ints, so fast-path that
                {
                    var int32 = Convert.ToInt32(val, radix);
                    if (int32 < 0 && radix != 10)
                        throw new OverflowException("can only have negative numbers in base 10; promote to Int64");

                    return From(int32, radix, type);
                }
                catch (OverflowException) // fallback to int64 for bigger numbers
                {
                    try
                    {
                        var int64 = Convert.ToInt64(val, radix);
                        if (int64 < 0 && radix != 10)
                            throw new OverflowException("can only have negative numbers in base 10; promote to BigInteger");
                        return From(int64, radix, type);
                    }
                    catch (OverflowException) // final fallback to BigInteger
                    {
                        var numberStyle = radix switch
                        {
                            10 => NumberStyles.None,
                            16 => NumberStyles.HexNumber,
                            _ => throw new NotSupportedException($"KdlDotNet does not support base {radix} numbers larger than signed Int64")
                        };
                        // https://stackoverflow.com/a/2983770 - BigInteger will parse a negative number if it thinks the highest bit is set
                        // this is not what we want; we don't support negative hex numbers
                        return From(BigInteger.Parse("0" + val, numberStyle), radix, type);
                    }
                }
            }
            catch (FormatException)
            {
                return null;
            }
        }

        protected int radix;
        // derived classes store value 

        protected KDLNumber(int radix, string? type) : base(type) => this.radix = radix;

        public override KDLString AsString() => new KDLString(AsBasicString(), type: Type);

        public override KDLNumber? AsNumber() => this;

        public override KDLBoolean? AsBoolean() => null;

        public abstract string AsBasicString(int radix = 10);

        protected override void WriteKDLValue(StreamWriter writer, PrintConfig printConfig)
        {
            if (printConfig.RespectRadix)
            {
                switch (radix)
                {
                    case 10:
                        writer.Write(AsBasicString().Replace('E', printConfig.ExponentChar));
                        break;
                    case 2:
                        writer.Write("0b");
                        writer.Write(AsBasicString(radix));
                        break;
                    case 8:
                        writer.Write("0o");
                        writer.Write(AsBasicString(radix));
                        break;
                    case 16:
                        writer.Write("0x");
                        writer.Write(AsBasicString(radix));
                        break;
                }
            }
            else
            {
                writer.Write(AsBasicString().Replace('E', printConfig.ExponentChar));
            }
        }
            

        public override bool IsNumber => true;

        public override string ToString() => $"KDLNumber{{value='{AsBasicString()}', type={Type}}}";

        // derived classes must implement Equals and HashCode
    }

    public class KDLNumberInt32 : KDLNumber
    {
        public int Value { get; }

        public KDLNumberInt32(int value, int radix, string? type) : base(radix, type)
            => Value = value;

        public override string AsBasicString(int radix = 10) => Convert.ToString(Value, radix);

        public override bool Equals(object? obj)
            => obj is KDLNumberInt32 other && other.radix == radix && other.Value == Value;

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + radix.GetHashCode();
                hash = hash * 23 + Value.GetHashCode();
                return hash;
            }
        }
    }

    public class KDLNumberInt64 : KDLNumber
    {
        public long Value { get; }

        public KDLNumberInt64(long value, int radix, string? type) : base(radix, type)
            => Value = value;

        public override string AsBasicString(int radix = 10) => Convert.ToString(Value, radix);

        public override bool Equals(object? obj)
            => obj is KDLNumberInt64 other && other.radix == radix && other.Value == Value;

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + radix.GetHashCode();
                hash = hash * 23 + Value.GetHashCode();
                return hash;
            }
        }
    }

    public class KDLNumberDouble : KDLNumber
    {
        KDLNumberParseFlags Flags { get; }
        public double Value { get; }

        public KDLNumberDouble(double value, int radix, string? type) : this(value, KDLNumberParseFlags.None, radix, type)
        { }

        internal KDLNumberDouble(double value, KDLNumberParseFlags flags, int radix, string? type) : base(radix, type)
        {
            Flags = flags;
            Value = value;
        }

        // can't have floating point numbers in bases other than 10
        public override string AsBasicString(int radix = 10)
        {
            if (Flags.HasFlag(KDLNumberParseFlags.HasScientificNotation))
            {
                var format = Flags.HasFlag(KDLNumberParseFlags.HasDecimalPoint) ? "E1" : "E0";
                // "E1" in C# makes it output "1.0E-010", which is better than the default "1E-10" but not quite the "1.0E-10" that we are looking for
                return Value.ToString(format).Replace("E-0", "E-").Replace("E+0", "E+");
            } 
            // else all KDL floating point numbers must have a decimal point (e.g. 1.0 is output instead of 1)
            return Value.ToString("0.0");
        }

        public override bool Equals(object? obj)
            => obj is KDLNumberDouble other && other.radix == radix && other.Value == Value;

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + radix.GetHashCode();
                hash = hash * 23 + Value.GetHashCode();
                return hash;
            }
        }
    }

    public class KDLNumberBigInteger : KDLNumber
    {
        public BigInteger Value { get; }

        public KDLNumberBigInteger(BigInteger value, int radix, string? type) : base(radix, type)
            => Value = value;

        // TODO radix support for big integers
        public override string AsBasicString(int radix = 10) => Value.ToString();

        public override bool Equals(object? obj)
            => obj is KDLNumberBigInteger other && other.radix == radix && other.Value == Value;

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + radix.GetHashCode();
                hash = hash * 23 + Value.GetHashCode();
                return hash;
            }
        }
    }
}
