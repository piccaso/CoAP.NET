﻿/*
 * Copyright (c) 2011, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace CoAP
{
    /// <summary>
    /// This class describes the options of the CoAP messages
    /// </summary>
    public class Option
    {
        private static readonly IConvertor int32Convertor = new Int32Convertor();
        private static readonly IConvertor stringConvertor = new StringConvertor();
        private OptionType _type;
        /// <summary>
        /// NOTE: value bytes in network byte order (big-endian)
        /// </summary>
        private Byte[] _valueBytes;

        /// <summary>
        /// Initializes an option.
        /// </summary>
        /// <param name="type">The type of the option</param>
        protected Option(OptionType type)
        {
            this._type = type;
        }

        /// <summary>
        /// Gets the type of the option.
        /// </summary>
        public OptionType Type
        {
            get { return _type; }
        }

        /// <summary>
        /// Gets the name of the option that corresponds to its type.
        /// </summary>
        public String Name
        {
            get { return Option.ToString(_type); }
        }

        /// <summary>
        /// Gets the value's length in bytes of the option.
        /// </summary>
        public Int32 Length
        {
            get { return null == this._valueBytes ? 0 : this._valueBytes.Length; }
        }

        /// <summary>
        /// Gets or sets raw bytes value of the option in network byte order (big-endian).
        /// </summary>
        public Byte[] RawValue
        {
            get { return this._valueBytes; }
            set { this._valueBytes = value; }
        }

        /// <summary>
        /// Gets or sets string value of the option.
        /// </summary>
        public String StringValue
        {
            get
            {
                return stringConvertor.Decode(this._valueBytes) as String;
            }
            set
            {
                if (!String.IsNullOrEmpty(value))
                    this._valueBytes = stringConvertor.Encode(value);
            }
        }

        /// <summary>
        /// Gets or sets int value of the option.
        /// </summary>
        public Int32 IntValue
        {
            get
            {
                return (Int32)int32Convertor.Decode(this._valueBytes);
            }
            set
            {
                this._valueBytes = int32Convertor.Encode(value);
            }
        }

        /// <summary>
        /// Gets the value of the option according to its type.
        /// </summary>
        public Object Value
        {
            get
            {
                IConvertor convertor = GetConvertor(this._type);
                return null == convertor ? null : convertor.Decode(this._valueBytes);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the option has a default value according to the draft.
        /// </summary>
        public Boolean IsDefault
        {
            get
            {
                // TODO refactor
                switch (this._type)
                {
                    case OptionType.MaxAge:
                        return IntValue == 60;
                    case OptionType.Token:
                        return Length == 0;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override String ToString()
        {
            switch (this._type)
            {
                case OptionType.ContentType:
                    return MediaType.ToString(IntValue);
                case OptionType.MaxAge:
                    return String.Format("{0} s", IntValue);
                case OptionType.ETag:
                case OptionType.Token:
                    return Hex(RawValue);
                case OptionType.UriPort:
                case OptionType.Observe:
                case OptionType.Block2:
                case OptionType.Block1:
                    return IntValue.ToString();
                case OptionType.ProxyUri:
                case OptionType.UriHost:
                case OptionType.LocationPath:
                case OptionType.LocationQuery:
                case OptionType.UriPath:
                case OptionType.UriQuery:
                    return StringValue;
                default:
                    return Hex(RawValue);
            }
        }

        /// <summary>
        /// Gets the hash code of this object
        /// </summary>
        /// <returns>The hash code</returns>
        public override Int32 GetHashCode()
        {
            const Int32 prime = 31;
            Int32 result = 1;
            result = prime * result + (Int32)this._type;
            result = prime * result + ComputeHash(this.RawValue);
            return result;
        }

        public override Boolean Equals(Object obj)
        {
            if (null == obj)
                return false;
            if (Object.ReferenceEquals(this, obj))
                return true;
            if (this.GetType() != obj.GetType())
                return false;
            Option other = (Option)obj;
            if (this._type != other._type)
                return false;
            if (null == this.RawValue && null != other.RawValue)
                return false;
            else if (null != this.RawValue && null == other.RawValue)
                return false;
            else
                // TODO 有没有更合适的方法判断？
                //return Array.Equals(this.RawValue, other.RawValue);
                return this.GetHashCode().Equals(other.GetHashCode());
        }

        /// <summary>
        /// Creates an option.
        /// </summary>
        /// <param name="type">The type of the option</param>
        /// <returns>The new option</returns>
        public static Option Create(OptionType type)
        {
            switch (type)
            {
                case OptionType.Block1:
                case OptionType.Block2:
                    return new BlockOption(type);
                default:
                    return new Option(type);
            }
        }

        /// <summary>
        /// Creates an option.
        /// </summary>
        /// <param name="type">The type of the option</param>
        /// <param name="raw">The raw bytes value of the option</param>
        /// <returns>The new option</returns>
        public static Option Create(OptionType type, Byte[] raw)
        {
            Option opt = Create(type);
            opt.RawValue = raw;
            return opt;
        }

        /// <summary>
        /// Creates an option.
        /// </summary>
        /// <param name="type">The type of the option</param>
        /// <param name="str">The string value of the option</param>
        /// <returns>The new option</returns>
        public static Option Create(OptionType type, String str)
        {
            Option opt = Create(type);
            opt.StringValue = str;
            return opt;
        }

        /// <summary>
        /// Creates an option.
        /// </summary>
        /// <param name="type">The type of the option</param>
        /// <param name="val">The int value of the option</param>
        /// <returns>The new option</returns>
        public static Option Create(OptionType type, Int32 val)
        {
            Option opt = Create(type);
            opt.IntValue = val;
            return opt;
        }

        /// <summary>
        /// Splits a string into a set of options, e.g. a uri path.
        /// </summary>
        /// <param name="type">The type of options</param>
        /// <param name="s">The string to be splited</param>
        /// <param name="delimiter">The seperator string</param>
        /// <returns><see cref="System.Collections.Generic.IList"/> of options</returns>
        public static IList<Option> Split(OptionType type, String s, String delimiter)
        {
            List<Option> opts = new List<Option>();
            if (!String.IsNullOrEmpty(s))
            {
                foreach (String segment in s.Split(new String[] { delimiter }, StringSplitOptions.None))
                {
                    if (!String.IsNullOrEmpty(segment))
                    {
                        opts.Add(Create(type, segment));
                    }
                }
            }
            return opts;
        }

        /// <summary>
        /// Joins the string values of a set of options.
        /// </summary>
        /// <param name="options">The list of options to be joined</param>
        /// <param name="delimiter">The seperator string</param>
        /// <returns>The joined string</returns>
        public static String Join(IList<Option> options, String delimiter)
        {
            if (null == options)
            {
                return String.Empty;
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                foreach (Option opt in options)
                {
                    sb.Append(delimiter);
                    sb.Append(opt.StringValue);
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Checks whether an option is a fencepost option.
        /// </summary>
        /// <param name="type">The option type to check</param>
        /// <returns>True iff the option is a fencepost option</returns>
        public static Boolean IsFencepost(OptionType type)
        {
            return (Int32)type % (Int32)OptionType.FENCEPOST_DIVISOR == 0;
        }

        /// <summary>
        /// Returns the next fencepost option number following a given option number.
        /// </summary>
        /// <param name="optionNumber">The option number</param>
        /// <returns>The smallest fencepost option number larger than the given option</returns>
        public static Int32 NextFencepost(Int32 optionNumber)
        {
            return (optionNumber / (Int32)OptionType.FENCEPOST_DIVISOR + 1) * (Int32)OptionType.FENCEPOST_DIVISOR;
        }

        /// <summary>
        /// Returns a string representation of the option type.
        /// </summary>
        /// <param name="type">The option type to describe</param>
        /// <returns>A string describing the option type</returns>
        public static String ToString(OptionType type)
        {
            switch (type)
            {
                case OptionType.Reserved:
                    return "Reserved (0)";
                case OptionType.ContentType:
                    return "Content-Type";
                case OptionType.MaxAge:
                    return "Max-Age";
                case OptionType.ProxyUri:
                    return "Proxy-Uri";
                case OptionType.ETag:
                    return "ETag";
                case OptionType.UriHost:
                    return "Uri-Host";
                case OptionType.LocationPath:
                    return "Location-Path";
                case OptionType.UriPort:
                    return "Uri-Port";
                case OptionType.LocationQuery:
                    return "Location-Query";
                case OptionType.UriPath:
                    return "Uri-Path";
                case OptionType.Token:
                    return "Token";
                case OptionType.UriQuery:
                    return "Uri-Query";
                case OptionType.Observe:
                    return "Observe";
                case OptionType.Accept:
                    return "Accept";
                case OptionType.IfMatch:
                    return "If-Match";
                case OptionType.FENCEPOST_DIVISOR:
                    return "Fencepost-Divisor";
                case OptionType.Block2:
                    return "Block2";
                case OptionType.Block1:
                    return "Block1";
                case OptionType.IfNoneMatch:
                    return "If-None-Match";
                default:
                    return String.Format("Unknown option [number {0}]", type);
            }
        }

        private static Int32 ComputeHash(params Byte[] data)
        {
            unchecked
            {
                const Int32 p = 16777619;
                Int32 hash = (Int32)2166136261;

                for (Int32 i = 0; i < data.Length; i++)
                    hash = (hash ^ data[i]) * p;

                hash += hash << 13;
                hash ^= hash >> 7;
                hash += hash << 3;
                hash ^= hash >> 17;
                hash += hash << 5;
                return hash;
            }
        }

        private static String Hex(Byte[] data)
        {
            const String digits = "0123456789ABCDEF";
            if (data != null)
            {
                StringBuilder builder = new StringBuilder(data.Length * 3);
                for (int i = 0; i < data.Length; i++)
                {
                    builder.Append(digits[(data[i] >> 4) & 0xF]);
                    builder.Append(digits[data[i] & 0xF]);
                    if (i < data.Length - 1)
                    {
                        builder.Append(' ');
                    }
                }
                return builder.ToString();
            }
            else
            {
                return null;
            }
        }

        private static IConvertor GetConvertor(OptionType type)
        {
            switch (type)
            {
                case OptionType.Reserved:
                    return null;
                case OptionType.ContentType:
                case OptionType.MaxAge:
                case OptionType.UriPort:
                case OptionType.Observe:
                case OptionType.Block2:
                case OptionType.Block1:
                case OptionType.Accept:
                case OptionType.FENCEPOST_DIVISOR:
                    return int32Convertor;
                case OptionType.ProxyUri:
                case OptionType.ETag:
                case OptionType.UriHost:
                case OptionType.LocationPath:
                case OptionType.LocationQuery:
                case OptionType.UriPath:
                case OptionType.Token:
                case OptionType.UriQuery:
                case OptionType.IfMatch:
                case OptionType.IfNoneMatch:
                    return stringConvertor;
                default:
                    return null;
            }
        }

        interface IConvertor
        {
            Object Decode(Byte[] bytes);
            Byte[] Encode(Int32 value);
            Byte[] Encode(String value);
        }

        class Int32Convertor : IConvertor
        {
            public Object Decode(Byte[] bytes)
            {
                if (null == bytes)
                    return 0;

                Int32 iOutcome = 0;
                Byte bLoop;
                for (Int32 i = 0; i < bytes.Length; i++)
                {
                    bLoop = bytes[i];
                    //iOutcome |= (bLoop & 0xFF) << (8 * i);
                    iOutcome <<= 8;
                    iOutcome |= (bLoop & 0xFF);
                }
                return iOutcome;
            }

            public Byte[] Encode(Int32 value)
            {
                Byte[] ret;

                if (value == 0)
                {
                    ret = new Byte[1];
                    ret[0] = 0;
                    return ret;
                }

                Int32 val = System.Net.IPAddress.HostToNetworkOrder(value);
                Byte[] allBytes = BitConverter.GetBytes(val);
                Int32 neededBytes = allBytes.Length;
                //for (Int32 i = allBytes.Length - 1; i >= 0; i--)
                for (Int32 i = 0; i < allBytes.Length; i++)
                {
                    if (allBytes[i] == 0x00)
                        neededBytes--;
                    else
                        break;
                }
                if (neededBytes == allBytes.Length)
                    ret = allBytes;
                else
                {
                    ret = new Byte[neededBytes];
                    Array.Copy(allBytes, allBytes.Length - neededBytes, ret, 0, neededBytes);
                }

                return ret;
            }

            public Byte[] Encode(String value)
            {
                throw new NotSupportedException();
            }
        }

        class StringConvertor : IConvertor
        {
            public Object Decode(Byte[] bytes)
            {
                return null == bytes ? null : System.Text.Encoding.UTF8.GetString(bytes);
            }

            public Byte[] Encode(String value)
            {
                return System.Text.Encoding.UTF8.GetBytes(value);
            }

            public Byte[] Encode(Int32 value)
            {
                throw new NotSupportedException();
            }
        }
    }

    /// <summary>
    /// CoAP option types
    /// </summary>
    public enum OptionType
    {
        Reserved = 0,
        /// <summary>
        /// C, 8-bit uint, 1 B, 0 (text/plain)
        /// </summary>
        ContentType = 1,
        /// <summary>
        /// E, variable length, 1--4 B, 60 Seconds
        /// </summary>
        MaxAge = 2,
        /// <summary>
        /// C, String, 1-270 B, "coap"
        /// </summary>
        ProxyUri = 3,
        /// <summary>
        /// E, sequence of bytes, 1-4 B, -
        /// </summary>
        ETag = 4,
        /// <summary>
        /// C, String, 1-270 B, ""
        /// </summary>
        UriHost = 5,
        /// <summary>
        /// E, String, 1-270 B, -
        /// </summary>
        LocationPath = 6,
        /// <summary>
        /// C, uint, 0-2 B
        /// </summary>
        UriPort = 7,
        /// <summary>
        /// E, String, 1-270 B, -
        /// </summary>
        LocationQuery = 8,
        /// <summary>
        /// C, String, 1-270 B, ""
        /// </summary>
        UriPath = 9,
        /// <summary>
        /// C, Sequence of Bytes, 1-2 B, -
        /// </summary>
        Token = 11,
        /// <summary>
        /// C, String, 1-270 B, ""
        /// </summary>
        UriQuery = 15,

        /// <summary>
        /// E, Duration, 1 B, 0
        /// <remarks>option types from draft-hartke-coap-observe-01</remarks>
        /// </summary>
        Observe = 10,

        /// <summary>
        /// E  Sequence of Bytes, 1-n B, -
        /// <remarks>selected option types from draft-bormann-coap-misc-04</remarks>
        /// </summary>
        Accept = 12,
        /// <summary>
        /// C, unsigned integer, 1--3 B, 0
        /// <remarks>selected option types from draft-bormann-coap-misc-04</remarks>
        /// </summary>
        IfMatch = 13,
        /// <summary>
        /// no-op for fenceposting
        /// <remarks>selected option types from draft-bormann-coap-misc-04</remarks>
        /// </summary>
        FENCEPOST_DIVISOR = 14,
        Block2 = 17,
        Block1 = 19,
        IfNoneMatch = 21,
    }
}