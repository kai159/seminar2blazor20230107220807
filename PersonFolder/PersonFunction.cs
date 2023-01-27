using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Seminar2Function.PersonFolder;

namespace Seminar2Function
{
    public class PersonFunction
    {

        private readonly string _salt;

        public PersonFunction()
        {
            _salt = Environment.GetEnvironmentVariable("Salt");
        }

        [FunctionName("GetPersons")]
        [OpenApiOperation(operationId: "GetPersons", tags: new[] { "Person" })]
        [OpenApiParameter(name: "personname", In = ParameterLocation.Query, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<Person>), Description = "The OK response")]
        public ActionResult<List<Person>> GetPerson(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "persons")] HttpRequest req,
            [Sql("select * from persons",
            CommandType = CommandType.Text,
            ConnectionStringSetting = "SQLConnection")]
            IEnumerable<Person> persons)
        {
            if (req.Query["personname"].Count != 0) persons = persons.Where(p => p.Personname == req.Query["personname"]).ToList();
            return new OkObjectResult(persons);
        }

        [FunctionName("GetPersonId")]
        [OpenApiOperation(operationId: "GetPersonId", tags: new[] { "Person" })]
        [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(int))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PersonResponse))]
        public ActionResult<Person> GetPersoneId(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "persons/{id}")] HttpRequest req,
        [Sql("select * from persons where id = @id",
            CommandType = CommandType.Text,
            Parameters = "@id={id}",
            ConnectionStringSetting = "SQLConnection")]
            IEnumerable<Person> persons)
        {
            return new OkObjectResult(persons.FirstOrDefault());
        }

        [FunctionName("GetPersonTime")]
        [OpenApiOperation(operationId: "GetPersonTime", tags: new[] { "Person" })]
        [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(int))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<TrackTime>))]
        public ActionResult<Person> GetPersonTime(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "persons/{id}/times")] HttpRequest req,
            [Sql("select * from tracktime where personid = @id",
            CommandType = CommandType.Text,
            Parameters = "@id={id}",
            ConnectionStringSetting = "SQLConnection")]
            IEnumerable<TrackTime> times)
        {
            return new OkObjectResult(times);
        }

        [FunctionName("CreatePerson")]
        [OpenApiOperation(operationId: "CreatePerson", tags: new[] { "Person" })]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(PersonRequest))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PersonResponse))]
        public async Task<ActionResult<Person>> CreatePerson(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "persons")] HttpRequest req,
        [Sql("dbo.persons", ConnectionStringSetting = "SQLConnection")] IAsyncCollector<Person> persons)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<Person>(requestBody);

            data.Passwordhash = BCrypt.Net.BCrypt.HashPassword(data.Passwordhash, _salt);

            await persons.AddAsync(data);
            await persons.FlushAsync();

            return new OkObjectResult(data);
        }


        [FunctionName("UpdatePerson")]
        [OpenApiOperation(operationId: "UpdatePerson", tags: new[] { "Person" })]
        [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(int))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(Person))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PersonRequest))]
        public async Task<IActionResult> UpdatePerson(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "persons/{id}")] HttpRequest req,
        [Sql("dbo.persons",
            ConnectionStringSetting = "SQLConnection")]
            IAsyncCollector<Person> persons,
        [Sql("select * from Person where id = @id",
            CommandType = CommandType.Text,
            Parameters = "@id={id}",
            ConnectionStringSetting = "SQLConnection")]
        IEnumerable<Person> readPersonn)
        {
            if (req.Path.Value.Split("/")[0] == null) return new BadRequestObjectResult("No id parameter.");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<Person>(requestBody);
            var person = readPersonn.FirstOrDefault();

            person.Personname = data.Personname ?? person.Personname;
            person.Passwordhash = (data.Passwordhash != null) ? data.Passwordhash = BCrypt.Net.BCrypt.HashPassword(data.Passwordhash, _salt) : person.Passwordhash;

            await persons.AddAsync(person);
            await persons.FlushAsync();

            return new OkObjectResult(person);
        }

        [FunctionName("DeletePerson")]
        [OpenApiOperation(operationId: "DeletePerson", tags: new[] { "Person" })]
        [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(int))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PersonResponse))]
        public IActionResult DeleteKrankenhausketten(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "persons/{id}")] HttpRequest req,
        ILogger log,
        [Sql("delete from dbo.persons where id = @id",
            CommandType = CommandType.Text,
            Parameters = "@id={id}",
            ConnectionStringSetting = "SQLConnection")]
        IEnumerable<Person> persons)
        {
            return new OkObjectResult(persons.FirstOrDefault());
        }

        [FunctionName("CreateTrackTime")]
        [OpenApiOperation(operationId: "CreateTrackTime", tags: new[] { "Person" })]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(TrackTime))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(TrackTime))]
        public async Task<ActionResult<TrackTime>> CreateTrackTime(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "persons/{id}/times")] HttpRequest req,
        [Sql("dbo.tracktime", ConnectionStringSetting = "SQLConnection")] IAsyncCollector<TrackTime> tracktimes)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<TrackTime>(requestBody);

            await tracktimes.AddAsync(data);
            await tracktimes.FlushAsync();

            return new OkObjectResult(data);
        }


        [FunctionName("UpdateTrackTime")]
        [OpenApiOperation(operationId: "UpdateTrackTime", tags: new[] { "Person" })]
        [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(int))]
        [OpenApiParameter(name: "start", In = ParameterLocation.Path, Required = true, Type = typeof(DateTime))]
        [OpenApiParameter(name: "end", In = ParameterLocation.Path, Required = true, Type = typeof(DateTime))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(TrackTime))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(TrackTime))]
        [return: ServiceBus("storetime", Connection = "ServiceBusConnection")]
        public async Task<TrackTime> UpdateTrackTime(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "persons/{id}/times/{start}/{end}")] HttpRequest req,
        [Sql("dbo.tracktime",
            ConnectionStringSetting = "SQLConnection")]
            IAsyncCollector<TrackTime> tracktimes,
        [Sql("select * from TrackTime where personid = @id and starttime = @start and endtime = @end",
            CommandType = CommandType.Text,
            Parameters = "@id={id},@start={start},@end={end}",
            ConnectionStringSetting = "SQLConnection")]
        IEnumerable<TrackTime> readTrackTime)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<TrackTime>(requestBody);
            var tracktime = readTrackTime.FirstOrDefault();

            tracktime.Starttime = data.Starttime ?? tracktime.Starttime;
            tracktime.EndTime = data.EndTime ?? tracktime.EndTime;

            await tracktimes.AddAsync(tracktime);
            await tracktimes.FlushAsync();

            return tracktime;
        }
    }
}
