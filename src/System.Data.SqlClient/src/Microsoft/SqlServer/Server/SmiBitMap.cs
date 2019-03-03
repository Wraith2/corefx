// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//------------------------------------------------------------------------------

using System;
using System.Data;
using System.Diagnostics;

namespace Microsoft.SqlServer.Server
{
    internal sealed class SmiBitMap
    {
        private readonly byte[] _bytes;
        private readonly int _height;
        private readonly int _width;

        public SmiBitMap(bool[,] values)
        {
            _height = values.GetLength(0);
            _width = values.GetLength(1);
            int compressedHeight = Math.DivRem(_height, 8, out int additionalBits);
            if (additionalBits > 0)
            {
                compressedHeight += 1;
            }
            _bytes = new byte[compressedHeight * _width];
            for (int rowIndex = 0; rowIndex < _height; rowIndex++)
            {
                for (int colIndex = 0; colIndex < _width; colIndex++)
                {
                    if (values[rowIndex, colIndex])
                    {
                        int byteIndex = GetBitIndex(rowIndex, colIndex, out int bitIndex);
                        _bytes[byteIndex] |= (byte)(1 << bitIndex);
                    }
                }
            }
        }

        public bool this[ExtendedClrTypeCode row, SqlDbType column]
        {
            get
            {
                int byteIndex = GetBitIndex((int)row, (int)column, out int bitIndex);
                return (_bytes[byteIndex] & (1 << bitIndex)) != 0;
            }
            set
            {
                int byteIndex = GetBitIndex((int)row, (int)column, out int bitIndex);
                if (value)
                {
                    _bytes[byteIndex] |= (byte)(1 << bitIndex);
                }
                else
                {
                    _bytes[byteIndex] &= (byte)(~(1 << bitIndex));
                }
            }
        }

        public int Height => _height;
        public int Width => _width;

        private int GetBitIndex(int rowIndex, int colIndex, out int bitIndex)
        {
            return Math.DivRem(rowIndex + (colIndex * _height), 8, out bitIndex);
        }
    }
}
