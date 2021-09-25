using System;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HostedPowershell
{
    class ExecutePWSH
    {
        private readonly ILogger logger;

        public ExecutePWSH(ILogger logger)
        {
            this.logger = logger;
        }
        async Task<string[]> ExecuteV1(ExecuteV1Script executeV1Script)
        {
            try{
                using (var ps = PowerShell.Create())
                {
                    var scriptData = ps
                        .AddScript(executeV1Script.scriptText)
                        ;
                    var data = scriptData.InvokeAsync();
                    var res = await Task.WhenAny(Task.Delay(executeV1Script.timeoutSeconds * 1000), data);

                    if (data.IsCompletedSuccessfully)
                    {
                        var result = data.Result;
                        if (result.Count == 0)
                        {
                            logger.LogInformation($"No result for {executeV1Script.ScriptName}");
                            return new string[0];
                        }
                        return data.Result.Select(it => it.ToString()).ToArray();

                    }
                    else
                    {
                        logger.LogWarning("ExecuteV1: timeout for {executeV1Script.ScriptName}");
                    }

                    return null;
                }
            }
            catch(Exception ex){
                logger.LogError(ex, "Error in ExecuteV1");
                return null;

            }
        }
        public async Task<ResultV1[]> ExecuteParalelV1(ExecuteV1Script[] data){
            var tasks = data.Select(it => new{it.ScriptName, t = ExecuteV1(it) })
                .ToDictionary(it => it.ScriptName, it => it.t);


            await Task.WhenAll(tasks.Values);
            return tasks.Select(it => new ResultV1{
                ScriptName = it.Key,
                Result = it.Value.Result
            }).ToArray();

        }
    }
}
