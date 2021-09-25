using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
namespace HostedPowershell
{
    public class BSPWSHOptions
    {
        public int SleepSeconds { get; set; }
        public ExecuteV1Script[] scriptsV1 { get; set; }
    }
    public class BSPWSH: BackgroundService
    {
        private readonly ILogger<BSPWSH> logger;
        private readonly IFileProvider fileProvider;
        private readonly IOptionsMonitor<BSPWSHOptions> optionsMonitor;

        public BSPWSH(ILogger<BSPWSH> logger,IFileProvider fileProvider, IOptionsMonitor<BSPWSHOptions> optionsMonitor)
        {
            this.logger = logger;
            this.fileProvider = fileProvider;
            this.optionsMonitor = optionsMonitor;
            
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation($"BSPWSH is running at: {DateTime.Now}");
                foreach (var script in optionsMonitor.CurrentValue.scriptsV1){
                    using(var stream = fileProvider.GetFileInfo(script.ScriptName).CreateReadStream()){
                        using(var reader = new StreamReader(stream)){
                            var scriptText = await reader.ReadToEndAsync();
                            script.scriptText=scriptText;
                        }
                    }
                }logger.LogInformation($"BSPWSH is running at: {DateTime.Now}");
                var executor=new ExecutePWSH(logger);
                var data= await executor.ExecuteParalelV1(optionsMonitor.CurrentValue.scriptsV1);
                string result = data.Aggregate("", (current, item) => current + $"{item.ScriptName} : {item.Result?.Length}");
                logger.LogInformation($"BSPWSH results : {result}");
                await Task.Delay(optionsMonitor.CurrentValue.SleepSeconds * 1000, stoppingToken);
            }
        }
    }
}
