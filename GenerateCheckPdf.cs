using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Dapper;
using rodeogo;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Layout;
using iText.Layout.Properties;
using Nut;

namespace rgo
{
	public class GenerateCheckPdf
	{
		private readonly ILogger _logger;
		private readonly IConfiguration _config;

		readonly Options opts = new Options
		{
			MainUnitNotConvertedToText = false,
			SubUnitNotConvertedToText = true,
			MainUnitFirstCharUpper = true,
			CurrencyFirstCharUpper = false
		};

		public GenerateCheckPdf(ILoggerFactory loggerFactory,
			IConfiguration config)
		{
			_config = config;
			_logger = loggerFactory.CreateLogger<GenerateCheckPdf>();
		}

		[Function("GenerateCheckPdf")]
		public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get",
			Route="pdf/{seriesId:int}/{date}")] HttpRequestData req, int seriesId, DateTime date)
		{
			using (var conn = new MySqlConnection(_config["dbConn"]))
			{
				var data = await conn.QueryAsync<CheckData>(@"
select concat(c.FirstName, "" "", c.LastName) as Payee,
h.HorseName, r.TotalPayout, r.TotalRunTime, es.EventSeriesName, ev.EventDate,
r.Classification, r.ClassificationPlace, et.EventTypeDescription, ev.Class, p.ProducerName, el.LocationName
from EventRun r 
join Contestants c on c.CustomerId = r.CustomerId and c.ContestantId = r.ContestantId
join Events ev on ev.CustomerId = r.CustomerId and ev.EventId = r.EventId
join EventSeries es on es.CustomerId = r.CustomerId and es.EventSeriesId = ev.EventSeriesId
join Horses h on h.CustomerId = r.CustomerId and h.HorseId = r.HorseId
join EventTypes et on et.CustomerId = r.CustomerId and et.EventTypeId = ev.EventTypeId
join Producers p on p.CustomerId = r.CustomerId and p.ProducerId = ev.ProducerId
join EventLocations el on el.CustomerId = r.CustomerId and el.EventLocationId = ev.EventLocationId
where r.CustomerId = 95 and ev.EventSeriesId = @SeriesId and ev.EventDate = @EvDate
and r.TotalPayout > 0
order by r.Classification, r.ClassificationPlace",
	new { SeriesId = seriesId, EvDate = date }
				);

				using (var ms = new MemoryStream())
				{
					using (var writer = new PdfWriter(ms))
					{
						writer.SetCloseStream(false);

						using (var pdfDoc = new PdfDocument(writer))
						{
							var areaBreak = new AreaBreak(AreaBreakType.NEXT_PAGE);
							var doc = new Document(pdfDoc);
							doc.SetMargins(0, 0, 0, 0);   //T,R,B,L
							PdfFont font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
							doc.SetFont(font);
							doc.SetFontSize(10);

							if(!data.Any())
							{
								doc.SetFontSize(24);
								doc.Add(new Paragraph($"No data for Series {seriesId}")
									.SetFixedPosition(72, 600, 300));
								doc.Add(areaBreak);
							}

							foreach (var d in data)
							{
								var x = decimal.Round(d.TotalPayout, 2);

								var ps = pdfDoc.GetDefaultPageSize();
								var w = ps.GetWidth();
								var h = ps.GetHeight();

								// Check area
								doc.Add(new Paragraph(new Text($"Date: {DateTime.Now.ToString("d")}"))
											.SetFixedPosition(w / 2, h - 72, w)
											);

								doc.Add(new Paragraph(d.Payee).SetFixedPosition(72, h - (72 * 1.5f), 5 * 72));
								doc.Add(new Paragraph(x.ToString()).SetFixedPosition(72 * 7f, h - (72 * 1.5f), 2 * 72));

								doc.Add(new Paragraph(x.ToText("usd", "en", opts)).SetFixedPosition(72, h - (72 * 2f), 5 * 72));

								doc.Add(new Paragraph($"Memo: {d.Classification} - {d.ClassificationPlace}")
											.SetUnderline()
											.SetFixedPosition(72, h - (72 * 2.5f), 72 * 5));

								// Stub area
								var stub = new Paragraph($@"
{d.EventSeriesName} {d.Class} - {d.EventTypeDescription}
Check To: {d.Payee}
Place: {d.Classification} - {d.ClassificationPlace} Time: {d.TotalRunTime}
Amount: {x.ToString()}
EventDate: {d.EventDate.ToString("d")}
Producer: {d.ProducerName}
Location: {d.LocationName}");

								doc.Add(stub.SetFixedPosition(72 * 3, h-410, 5 * 72));
								doc.Add(stub.SetFixedPosition(72 * 3, h-690, 5 * 72));
								doc.Add(areaBreak);
							}

							pdfDoc.RemovePage(pdfDoc.GetLastPage());
						}
					}
					// return content
					var f = data.FirstOrDefault();
					var response = req.CreateResponse(HttpStatusCode.OK);
					await response.WriteBytesAsync(ms.ToArray());
					response.Headers.Add("Content-Type", "application/pdf");
					response.Headers.Add("content-disposition", $"attachment;filename={f?.EventSeriesName?.Replace(" ", "_") ?? seriesId.ToString()}_{f?.EventDate.ToString("yyyy_MM_dd")}.pdf");
					return response;
				}

			}
		}
	}
}
