// This file is part of Companion Cube project
//
// Copyright 2018 Emzi0767
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Security.Cryptography;

namespace Emzi0767.CompanionCube.Services
{
    public sealed class CSPRNG : IDisposable
    {
        public bool IsDisposed { get; set; } = false;
        private RandomNumberGenerator RNG { get; } = RandomNumberGenerator.Create();

        public byte[] GetBytes(int count)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException("Must get at least 1 byte.", nameof(count));

            var bts = new byte[count];
            this.RNG.GetBytes(bts);
            return bts;
        }
        
        public void GetBytes(int count, out byte[] bytes)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException("Must get at least 1 byte.", nameof(count));

            bytes = new byte[count];
            this.RNG.GetBytes(bytes);
        }

        public byte GetU8()
            => this.GetBytes(1)[0];

        public sbyte GetS8()
            => (sbyte)this.GetBytes(1)[0];

        public ushort GetU16()
            => BitConverter.ToUInt16(this.GetBytes(2), 0);

        public short GetS16()
            => BitConverter.ToInt16(this.GetBytes(2), 0);

        public uint GetU32()
            => BitConverter.ToUInt32(this.GetBytes(4), 0);

        public int GetS32()
            => BitConverter.ToInt32(this.GetBytes(4), 0);

        public ulong GetU64()
            => BitConverter.ToUInt64(this.GetBytes(8), 0);

        public long GetS64()
            => BitConverter.ToInt64(this.GetBytes(8), 0);

        public int Next()
            => this.Next(0, int.MaxValue);

        public int Next(int max)
            => this.Next(0, max);

        public int Next(int min, int max)
        {
            if (max <= min)
                throw new ArgumentException("Maximum needs to be greater than minimum.", nameof(max));

            var offset = 0;
            if (min < 0)
                offset = -min;

            min += offset;
            max += offset;

            return Math.Abs(this.GetS32()) % (max - min) + min - offset;
        }

        public float NextSingle()
        {
            var (l1, l2) = ((float)this.GetS32(), (float)this.GetS32());
            return Math.Abs(l1 / l2) % 1.0F;
        }

        public double NextDouble()
        {
            var (l1, l2) = ((double)this.GetS64(), (double)this.GetS64());
            return Math.Abs(l1 / l2) % 1.0;
        }

        public void Dispose()
        {
            if (this.IsDisposed)
                throw new ObjectDisposedException("This random number generator is already disposed.");

            this.IsDisposed = true;
            this.RNG.Dispose();
        }
    }
}
