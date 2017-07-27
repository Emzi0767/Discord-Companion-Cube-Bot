// This file is part of Companion Cube project
//
// Copyright (C) 2018-2021 Emzi0767
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using Emzi0767.CompanionCube.Services;

namespace Emzi0767.CompanionCube
{
    /// <summary>
    /// Comparer implementation which uses a cryptographically-secure random number generator to shuffle items.
    /// </summary>
    /// <typeparam name="T">Type of the item.</typeparam>
    public class Shuffler<T> : IComparer<T>
    {
        public SecureRandom RNG { get; }

        /// <summary>
        /// Creates a new shuffler.
        /// </summary>
        /// <param name="rng">Cryptographically-secure random number generator.</param>
        public Shuffler(SecureRandom rng)
        {
            this.RNG = rng;
        }

        /// <summary>
        /// Returns a random order for supplied items.
        /// </summary>
        /// <param name="x">First item.</param>
        /// <param name="y">Second item.</param>
        /// <returns>Random order for the items.</returns>
        public int Compare(T x, T y)
        {
            var val1 = this.RNG.Next();
            var val2 = this.RNG.Next();

            if (val1 > val2)
                return 1;
            if (val1 < val2)
                return -1;
            return 0;
        }
    }
}
