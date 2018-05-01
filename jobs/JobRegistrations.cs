using FluentScheduler;
using Microsoft.AspNetCore.Hosting;
using tradeBot.lib;

namespace tradeBot.Jobs
{
    public class ApiJobRegistrations : Registry
    {
        public ApiJobRegistrations(IHostingEnvironment _appEnvironment, IRestApi _restApi)
        {
            ApiJob apiJob = new ApiJob(_appEnvironment, _restApi);
            Schedule(apiJob).NonReentrant().ToRunEvery(60).Minutes();
        }        
    }
    public class BotJobRegistrations : Registry
    {
        public BotJobRegistrations(IHostingEnvironment _appEnvironment, IBot _bot)
        {
            BotJob botJob = new BotJob(_appEnvironment, _bot);
            Schedule(botJob).NonReentrant().ToRunEvery(1).Seconds();
        }
    }
}
