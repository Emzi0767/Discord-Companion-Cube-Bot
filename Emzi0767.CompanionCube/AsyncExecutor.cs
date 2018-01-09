// This file is part of Emzi0767.CompanionCube project
//
// Copyright 2017 Emzi0767
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
    public class AsyncExecutor
    {
        private SemaphoreSlim Semaphore { get; }

        public AsyncExecutor()
        {
            this.Semaphore = new SemaphoreSlim(1, 1);
        }

        public void Execute(Task task)
        {
            this.Semaphore.Wait();

            Exception taskex = null;

            var are = new AutoResetEvent(false);
            _ = Task.Run(Executor);
            are.WaitOne();

            this.Semaphore.Release();

            if (taskex != null)
                throw taskex;

            async Task Executor()
            {
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    taskex = ex;
                }

                are.Set();
            }
        }

        public T Execute<T>(Task<T> task)
        {
            this.Semaphore.Wait();

            Exception taskex = null;
            T result = default;

            var are = new AutoResetEvent(false);
            _ = Task.Run(Executor);
            are.WaitOne();

            this.Semaphore.Release();

            if (taskex != null)
                throw taskex;

            return result;

            async Task Executor()
            {
                try
                {
                    result = await task;
                }
                catch (Exception ex)
                {
                    taskex = ex;
                }

                are.Set();
            }
        }
    }
}
