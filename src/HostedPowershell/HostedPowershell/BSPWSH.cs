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
    public class BSPWSH : BackgroundService
    {
        private readonly ILogger<BSPWSH> logger;
        private readonly IFileProvider fileProvider;
        private readonly IOptionsMonitor<BSPWSHOptions> optionsMonitor;

        public BSPWSH(ILogger<BSPWSH> logger, IFileProvider fileProvider, IOptionsMonitor<BSPWSHOptions> optionsMonitor)
        {
            this.logger = logger;
            this.fileProvider = fileProvider;
            this.optionsMonitor = optionsMonitor;
            if (optionsMonitor == null)
                throw new ArgumentException("please define scriptsv1");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation($"BSPWSH is running at: {DateTime.Now}");
                if (optionsMonitor.CurrentValue?.scriptsV1 == null)
                {
                    logger.LogInformation("please add scriptsv1");

                }
                else
                {
                    var scripts = optionsMonitor.CurrentValue.scriptsV1.Select(it=>it).ToArray();
                    foreach (var script in scripts)
                    {
                        try{
                        using (var stream = fileProvider.GetFileInfo(script.ScriptName).CreateReadStream())
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                var scriptText = await reader.ReadToEndAsync();
                                script.scriptText = scriptText;
                            }
                        }
                        }
                        catch (Exception ex){
                            logger.LogError(ex, $"error reading script {script.ScriptName}");
                        }
                    }
                    var executor = new ExecutePWSH(logger);
                    var dataToExecute= scripts.Where(x => x.scriptText != null).ToArray();
                    if(dataToExecute.Length>0){
                        var data = await executor.ExecuteParalelV1(dataToExecute);
                        string result = data.Aggregate("", (current, item) => current + $"{item.ScriptName} : {item.Result?.Length}");
                        logger.LogInformation($"BSPWSH results : {result}");
                    }
                    else{
                        logger.LogInformation("no scripts found to execute");
                    }

                }
                var delay = (optionsMonitor.CurrentValue?.SleepSeconds ?? 5);
                if (delay == 0)
                    delay = 5;

                logger.LogInformation($"BSPWSH delay: {delay}");
                await Task.Delay( delay * 1000, stoppingToken);
            }
        }
    }
}
