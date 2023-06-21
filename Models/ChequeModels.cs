using Newtonsoft.Json;

namespace rodeogo;

internal class Producer
{
	public int Id { get; set; }
	public string? Name {get; set; }
}

internal class Events
{
	public int Id { get; set; }
	public string? Series {get; set; }
	public string? Class {get; set; }
	public string? EventDate {get; set; }
}

internal class CheckData
{
	public string? Payee {get; set;}
	public string? HorseName { get;set;}
	public decimal TotalPayout {get;set;}
	public string? EventSeriesName { get;set;}
	public DateTime EventDate { get; set;}
	public string? Classification { get; set; }
	public string? ClassificationPlace {get; set; }
	public string? EventTypeDescription { get; set; }
	public string? Class { get; set; }
	public decimal TotalRunTime {get; set; }
	public string? ProducerName {get; set;}
	public string? LocationName {get; set;}
}