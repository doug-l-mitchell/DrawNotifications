using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Dapper;


namespace rodeogo
{
	public class DrawNotificationDbTrigger
	{
		private readonly ILogger _logger;
		private readonly IConfiguration _config;

		public DrawNotificationDbTrigger(ILoggerFactory loggerFactory,
			IConfiguration config)
		{
			_config = config;
			_logger = loggerFactory.CreateLogger<DrawNotificationDbTrigger>();
		}


		[Function("DrawNotificationDbTrigger")]
		public void Run([TimerTrigger("*/20 * * * *")] MyInfo myTimer)
		{
			// The objective is to replace the MySql trigger, which fails to fire sometimes 
			// without any reported issues.
			using(var conn = new MySqlConnection(_config["dbConn"]))
			{
				_logger.LogInformation("Executing query to add DrawNotifications from EventRun");

				var count = conn.Execute(@"insert ignore into DrawNotifications (CustomerId, EventId, EventRunId)
select er.CustomerId, er.EventId, er.EventRunId
from EventRun er
join Events ev on ev.CustomerId = er.CustomerId and ev.EventId = er.EventId
where er.RunComplete = 0 
	and ev.EnableNotification = 1
	and er.LastUpdated > adddate(now(), INTERVAL -20 MINUTE)");

				_logger.LogInformation($"query complete: added {count} records");
			}
		}
	}
}
