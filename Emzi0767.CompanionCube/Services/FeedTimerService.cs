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
using Emzi0767.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Emzi0767.CompanionCube.Services
{
    public sealed class FeedTimerService : IDisposable
    {
        private Timer Dispatcher { get; }
        private AsyncExecutor Async { get; }
        private IServiceProvider Services { get; }

        public FeedTimerService(IServiceProvider services)
        {
            this.Services = services;
            this.Async = new AsyncExecutor();
            this.Dispatcher = new Timer(Tick, this, Timeout.Infinite, Timeout.Infinite);
        }

        public void Start()
        {
            this.Dispatcher.Change(TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(10));
        }

        public void Dispose()
        {
            this.Dispatcher.Change(Timeout.Infinite, Timeout.Infinite);
            this.Dispatcher.Dispose();
        }

        private static void Tick(object o)
        {
            var feeds = o as FeedTimerService;

            using (var scope = feeds.Services.CreateScope())
            {
                var srv = scope.ServiceProvider;
                var rss = srv.GetRequiredService<FeedService>();

                feeds.Async.Execute(rss.ProcessFeedsAsync());
            }
        }
    }
}
