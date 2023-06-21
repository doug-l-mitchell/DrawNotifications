using System.Net;
using System.Text;
using Dapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using rodeogo;

namespace rgo
{
	public class GetProducers
	{
		private readonly ILogger _logger;
		private readonly IConfiguration _config;

		public GetProducers(ILoggerFactory loggerFactory,
		IConfiguration config)
		{
			_config = config;
			_logger = loggerFactory.CreateLogger<GetProducers>();
		}

		[Function("GetProducers")]
		public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
		{
			using (var conn = new MySqlConnection(_config["dbConn"]))
			{
				var producers = await conn.QueryAsync<Producer>(@"
select distinct(p.ProducerName) as Name, p.ProducerId as Id from Producers p
join Events e on e.CustomerId = p.CustomerId and e.ProducerId = p.ProducerId
join EventRun r on r.CustomerId = p.CustomerId and r.EventId = e.EventId
where p.CustomerId = 95 and e.EventDate >  date_sub(curdate(), interval 2 month);
");
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(producers);
                return response;
			}
		}
	}
}
