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

using System;

namespace Emzi0767.CompanionCube
{
    /// <summary>
    /// Caused whenever a command execution is cancelled.
    /// </summary>
    public class CommandCancelledException : Exception
    {
        /// <summary>
        /// Creates a new execution cancelled exception with default reason.
        /// </summary>
        public CommandCancelledException()
            : base("Command execution was cancelled due to unmet criteria.")
        { }

        /// <summary>
        /// Creates a new execution cancelled exception with specified reason.
        /// </summary>
        /// <param name="message">Reason for cancellation.</param>
        public CommandCancelledException(string message)
            : base(message)
        { }

        /// <summary>
        /// Creates a new execution cancelled exception with default reason and specified cause.
        /// </summary>
        /// <param name="innerException">Cause of the cancellation.</param>
        public CommandCancelledException(Exception innerException)
            : base("Command execution was cancelled due to unmet criteria.", innerException)
        { }

        /// <summary>
        /// Creates a new execution cancelled exception with specified reason and cause.
        /// </summary>
        /// <param name="message">Reason for cancellation.</param>
        /// <param name="innerException">Cause of the cancellation.</param>
        public CommandCancelledException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
