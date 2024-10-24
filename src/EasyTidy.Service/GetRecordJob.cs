using EasyTidy.Log;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Service;

public class GetRecordJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        LogService.Logger.Info("GetRecordJob");
        await Task.CompletedTask;
    }
}
