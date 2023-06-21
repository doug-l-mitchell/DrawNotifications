using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Dapper;
using rodeogo;

namespace rgo
{
    public class GetEventsArgs
    {
        public int producerId {get;set;}
    }

    public class GetEvents
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;

        public GetEvents(ILoggerFactory loggerFactory,
            IConfiguration config)
        {
            _config = config;
            _logger = loggerFactory.CreateLogger<GetEvents>();
        }

        [Function("GetEvents")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get",
            Route = "series/{prodId:int}")] HttpRequestData req, int prodId)
        {
			using (var conn = new MySqlConnection(_config["dbConn"]))
			{
				var events = await conn.QueryAsync<Events>(@"
select distinct(e.EventSeriesId) as Id, es.EventSeriesName as series, date_format(e.EventDate, '%Y-%m-%e') as EventDate from Events e
join EventSeries es on es.CustomerId = e.CustomerId and es.EventSeriesId = e.EventSeriesId
join EventRun r on r.CustomerId = e.CustomerId and r.EventId = e.EventId
where e.ProducerId = @Producer and e.CustomerId = 95 and e.EventDate > date_sub(curdate(), interval 30 day);
", new { Producer = prodId});
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(events);
                return response;
			}
		}
    }
}
