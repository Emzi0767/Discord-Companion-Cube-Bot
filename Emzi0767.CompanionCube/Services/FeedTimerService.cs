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
