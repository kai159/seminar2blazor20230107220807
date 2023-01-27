using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seminar2Function
{
    public class TrackTimeFunction
    {
        [FunctionName("ProcessToCosmos")]
        public void ProcessToCosmos(
            [ServiceBusTrigger("storetime", Connection = "ServiceBusConnection")]
            TrackTime trackTime,
            ILogger log,
            [CosmosDB(
            databaseName: "StoreTime",
            collectionName: "Changes",
            ConnectionStringSetting = "CosmosConnection"
            )]out dynamic document)
        {
            document = new
            {
                id = Guid.NewGuid(),
                personid = trackTime.PersonId,
                starttime = trackTime.Starttime,
                endtime = trackTime.EndTime
            };
            log.LogInformation($"item: {trackTime}");
        }
    }
}
