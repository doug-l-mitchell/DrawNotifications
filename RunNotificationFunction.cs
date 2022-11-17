using System;
using Azure.Storage.Queues;
using Dapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Twilio;
using Twilio.Types;
using Twilio.Rest.Api.V2010.Account;

namespace rodeogo
{
	public class RunNotificationFunction
	{
		private readonly ILogger _logger;
		private readonly IConfiguration _config;

		public RunNotificationFunction(ILoggerFactory loggerFactory,
			IConfiguration config)
		{
			_logger = loggerFactory.CreateLogger<RunNotificationFunction>();
			_config = config;
			TwilioClient.Init(_config["twilio:acctSid"], _config["twilio:auth"]);
		}

		[Function("RunNotificationFunction")]
		public void Run([QueueTrigger("run-notify-execute", Connection = "AzureWebJobsStorage")] string msg)
		{
			var runQueue = new QueueClient(_config["AzureWebJobsStorage"], "run-notify-execute");

			using (var conn = new MySqlConnection(_config["dbConn"]))
			{
				var data = conn.Query<RunData>(RunsQuery).ToList();
				foreach (var run in data)
				{
					SendMessage(new SMS { Body = GetSmsBody(run), To = run.MobileNumber, From = _config["twilio:msgSvc"] });
					conn.Execute("update RunNotifications set Notified = 1 where CustomerId = @CustomerId and EventId = @EventId and EventRunId = @EventRunId", run);
				}

				// when should we run this function again?
				// if we processed messages, 5 minutes
				// if we didn't process messages
				//   1 hour * count of tries (tries are recorded in the input message)
				// after 4 - 1 hour tries, stop
                var cnt = 0;
                int.TryParse(msg, out cnt);
				if ( cnt < 5)
				{
					var ts = data.Count() > 0 ? TimeSpan.FromMinutes(5)
						: TimeSpan.FromHours(1);
                    runQueue.SendMessage((++cnt).ToString(), ts);
				}
			}

		}

		private static string SendMessage(SMS m)
		{
			var res = MessageResource.Create(
				body: m.Body,
				from: new PhoneNumber(m.From),
				to: new PhoneNumber(m.To)
			);
			return res.Sid;
		}

		private static string GetSmsBody(RunData d)
		{
			return $@"{d.EventDate.ToShortDateString()}
{d.EventSeriesName}
{d.EventTypeDescription}
{d.FirstName} {d.LastName}
Run# : {d.RunId}
Horse : {d.Horse}
Time : {d.RunTime}
Total Penalty : {d.TotalPenalties}
Final Time : {d.TotalRunTime}";
		}

		private static string RunsQuery
		{
			get
			{
				return @"select rn.CustomerId, rn.EventId, rn.EventRunId,
er.RunId,
h.HorseName,
c.MobileNumber,
c.FirstName,
c.LastName,
ev.EventDate,
es.EventSeriesName,
et.EventTypeDescription 
from RunNotifications rn
join EventRun er on er.CustomerId = rn.CustomerId and er.EventId = rn.EventId and er.EventRunId = rn.EventRunId
join Horses h on h.HorseId = er.HorseId and h.CustomerId = er.CustomerId
join Contestants c on c.CustomerId = er.CustomerId and c.ContestantId = er.ContestantId
join Events ev on ev.CustomerId = er.CustomerId and ev.EventId = er.EventId
join EventSeries es on es.CustomerId = er.CustomerId and es.EventSeriesId = ev.EventSeriesId
join EventTypes et on et.CustomerId = er.CustomerId and et.EventTypeId = ev.EventTypeId
where rn.Notified = 0 and c.MobileNumber is not null and c.MobileNumber <> '';";
			}
		}
	}
}
