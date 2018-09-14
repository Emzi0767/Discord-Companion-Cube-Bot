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
using System.Threading;
using System.Threading.Tasks;

namespace Emzi0767.CompanionCube
{
    /// <summary>
    /// Provides a simplified way of executing asynchronous code synchronously.
    /// </summary>
    public class AsyncExecutor
    {
        // this is used to wait for tasks to finish executing.
        private SemaphoreSlim Semaphore { get; }

        /// <summary>
        /// Creates a new instance of asynchronous executor.
        /// </summary>
        public AsyncExecutor()
        {
            this.Semaphore = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Executes a specified task in an asynchronous manner, waiting for its completion.
        /// </summary>
        /// <param name="task">Task to execute.</param>
        public void Execute(Task task)
        {
            // wait for execution slot
            this.Semaphore.Wait();

            // this is used to capture task execution exception
            Exception taskex = null;

            // queue a task and wait for it to finish executing
            var are = new AutoResetEvent(false);
            _ = Task.Run(Executor);
            are.WaitOne();

            // release execution slot
            this.Semaphore.Release();

            // check for and rethrow any exceptions
            if (taskex != null)
                throw new Exception("Exception occured while executing asynchronous code.", taskex);

            // executor method
            async Task Executor()
            {
                try
                {
                    // try and execute the supplied task
                    await task;
                }
                catch (Exception ex)
                {
                    // capture any exceptions
                    taskex = ex;
                }

                // signal that the execution is done
                are.Set();
            }
        }

        /// <summary>
        /// Executes a specified task in an asynchronous manner, waiting for its completion, and returning the result.
        /// </summary>
        /// <typeparam name="T">Type of the Task's return value.</typeparam>
        /// <param name="task">Task to execute.</param>
        /// <returns>Task's result.</returns>
        public T Execute<T>(Task<T> task)
        {
            // wait for execution slot
            this.Semaphore.Wait();

            // this is used to capture task execution exception and result
            Exception taskex = null;
            T result = default;

            // queue a task and wait for it to finish executing
            var are = new AutoResetEvent(false);
            _ = Task.Run(Executor);
            are.WaitOne();

            // release execution slot
            this.Semaphore.Release();

            // check for and rethrow any exceptions
            if (taskex != null)
                throw new Exception("Exception occured while executing asynchronous code.", taskex);

            // return the execution result
            return result;

            // executor method
            async Task Executor()
            {
                try
                {
                    // try and execute the supplied task
                    result = await task;
                }
                catch (Exception ex)
                {
                    // capture any exceptions
                    taskex = ex;
                }

                // signal that the execution is done
                are.Set();
            }
        }
    }
}
