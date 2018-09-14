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
        public CSPRNG RNG { get; }

        /// <summary>
        /// Creates a new shuffler.
        /// </summary>
        /// <param name="rng">Cryptographically-secure random number generator.</param>
        public Shuffler(CSPRNG rng)
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
