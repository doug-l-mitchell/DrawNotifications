using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Dapper;


namespace rodeogo
{
	public class RunNotificationDbTrigger
	{
		private readonly ILogger _logger;
		private readonly IConfiguration _config;

		public RunNotificationDbTrigger(ILoggerFactory loggerFactory,
			IConfiguration config)
		{
			_config = config;
			_logger = loggerFactory.CreateLogger<RunNotificationDbTrigger>();
		}


		[Function("RunNotificationDbTrigger")]
		public void Run([TimerTrigger("*/20 * * * *")] MyInfo myTimer)
		{
			// The objective is to replace the MySql trigger, which fails to fire sometimes 
			// without any reported issues.
			using(var conn = new MySqlConnection(_config["dbConn"]))
			{
				_logger.LogInformation("Executing query to add RunNotifications from EventRun");

				var count = conn.Execute(@"insert ignore into RunNotifications (CustomerId, EventId, EventRunId)
select er.CustomerId, er.EventId, er.EventRunId
from EventRun er
join Events ev on ev.CustomerId = er.CustomerId and ev.EventId = er.EventId
where er.RunComplete = 1 
	and ev.EnableNotification = 1
	and ev.EventDate >= adddate(curdate(), INTERVAL -1 DAY)
	and er.LastUpdated > adddate(now(), INTERVAL -20 MINUTE)");

				_logger.LogInformation($"query complete: added {count} records");
			}
		}
	}
}
