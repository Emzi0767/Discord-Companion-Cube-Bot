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
