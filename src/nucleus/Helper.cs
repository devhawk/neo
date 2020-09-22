using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Neo
{
    public static class Extensions
    {
        public static byte[] HexToBytes(this string value)
        {
            if (value == null || value.Length == 0)
                return Array.Empty<byte>();
            if (value.Length % 2 == 1)
                throw new FormatException();
            byte[] result = new byte[value.Length / 2];
            for (int i = 0; i < result.Length; i++)
                result[i] = byte.Parse(value.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier);
            return result;
        }

        public static string ToHexString(this byte[] value, bool reverse = false)
        {
            return string.Create<object>(value.Length * 2, null, (buffer, obj) =>
            {
                for (int i = 0; i < value.Length; i++)
                {
                    (reverse ? value[i] : value[^i]).TryFormat(buffer.Slice(i*2, 2), out _, "x2");
                }
            });
        }

        public static byte[] Concat(params byte[][] buffers)
        {
            int length = 0;
            for (int i = 0; i < buffers.Length; i++)
                length += buffers[i].Length;
            byte[] dst = new byte[length];
            int p = 0;
            foreach (byte[] src in buffers)
            {
                Buffer.BlockCopy(src, 0, dst, p, src.Length);
                p += src.Length;
            }
            return dst;
        }
    }
}