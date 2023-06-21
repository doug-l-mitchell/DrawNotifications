using System;
using Azure.Storage.Queues;
using Dapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace rodeogo
{
	public class DrawNotificationsFunction
	{
		private readonly ILogger _logger;
		private readonly IConfiguration _config;

		public DrawNotificationsFunction(ILoggerFactory loggerFactory,
			IConfiguration config)
		{
			_config = config;
			_logger = loggerFactory.CreateLogger<DrawNotificationsFunction>();
			TwilioClient.Init(_config["twilio:acctSid"], _config["twilio:auth"]);
		}

		[Function("DrawNotificationsFunction")]
		public void Run([TimerTrigger("*/30 * * * *", RunOnStartup = false)] MyInfo myTimer)
		{
			// var runQueue = new QueueClient(_config["AzureWebJobsStorage"], "run-notify-execute");
			_logger.LogInformation("starting");
			using (var conn = new MySqlConnection(_config["dbConn"]))
			{
				var data = conn.Query<DrawData>(DrawsQuery).ToList();
				_logger.LogInformation($"found {data.Count()} records");
				foreach (var draw in data)
				{
					SendMessage(new SMS { Body = GetSmsBody(draw), To = draw.MobileNumber, From = _config["twilio:msgSvc"] });
					conn.Execute("update DrawNotifications set Notified = 1 where CustomerId = @CustomerId and EventId = @EventId and EventRunId = @EventRunId", draw);
				}

				// find dates that the events will start in order to trigger the run function
				// foreach(var dt in data.Select(d => d.EventDate).Distinct())
				// {
				// 	try
				// 	{
				//     // this should be a message that starts on...
				//     runQueue.SendMessage("0", DateTime.UtcNow-dt);
				// 	}
				// 	catch(Exception ex)
				// 	{
				// 		_logger.LogError(ex, $"Failed to set message for event with date of {dt}");
				// 	}
				// }
			}

			_logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
		}

		private void SendMessage(SMS m)
		{
			try
			{
				var res = MessageResource.Create(
					body: m.Body,
					from: new PhoneNumber(m.From),
					to: new PhoneNumber(m.To)
				);
			}
			catch (Exception e)
			{
				_logger.LogInformation($"Failed to send to {m.To}: {e.ToString()}");
			}

		}

		private static string GetSmsBody(DrawData d)
		{
			return $@"{d.EventDate.ToShortDateString()}
{d.LocationName}
{d.FirstName} {d.LastName}
Run #: {d.RunId} on {d.HorseName}
Rotation : {d.Rotation}
Checkout www.rodeogo.com for Draw and Results";
		}

		private static string DrawsQuery
		{
			get
			{
				return @"select dn.CustomerId, dn.EventId, dn.EventRunId,
er.RunId,
h.HorseName,
er.Rotation,
c.MobileNumber,
c.SendMobile,
c.FirstName,
c.LastName,
ev.EventDate,
el.LocationName
from DrawNotifications dn
join EventRun er on er.CustomerId = dn.CustomerId and er.EventId = dn.EventId and er.EventRunId = dn.EventRunId
join Horses h on h.HorseId = er.HorseId and h.CustomerId = er.CustomerId
join Contestants c on c.CustomerId = er.CustomerId and c.ContestantId = er.ContestantId
join Events ev on ev.CustomerId = er.CustomerId and ev.EventId = er.EventId
join EventLocations el on el.CustomerId = er.CustomerId and el.EventLocationId = ev.EventLocationId
where dn.Notified = 0 and c.MobileNumber is not null and c.MobileNumber <> '';";
			}
		}
	}

	public class MyInfo
	{
		public MyScheduleStatus? ScheduleStatus { get; set; }

		public bool IsPastDue { get; set; }
	}

	public class MyScheduleStatus
	{
		public DateTime Last { get; set; }

		public DateTime Next { get; set; }

		public DateTime LastUpdated { get; set; }
	}

	internal class SMS
	{
		public string? Body { get; set; }
		public string? To { get; set; }
		public string? From { get; set; }
	}
}
