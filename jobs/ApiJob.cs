using FluentScheduler;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;
using tradeBot.lib;

namespace tradeBot.Jobs
{
    public class ApiJob : IJob
    {
        private readonly object _lock = new object();

        private bool _shuttingDown;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IRestApi _restApi;

        public ApiJob(IHostingEnvironment hostingEnvironment, IRestApi restApi)
        {
            this._hostingEnvironment = hostingEnvironment;
            this._restApi = restApi;
        }

        public void Execute()
        {
            lock (_lock)
            {
                if (_shuttingDown)
                    return;
                Task<bool> result = _restApi.Execute();
                result.Wait();                
            }
        }

        public void Stop(bool immediate)
        {
            // Locking here will wait for the lock in Execute to be released until this code can continue.
            lock (_lock)
            {
                _shuttingDown = true;
            }
        }
    }
}
