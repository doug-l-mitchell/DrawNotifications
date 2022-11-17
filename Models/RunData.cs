namespace rodeogo;

internal class RunData
{
	public int CustomerId { get; set; }
	public int EventId { get; set; }
	public int EventRunId { get; set; }
	public DateTime EventDate { get; set; }
	public string? EventSeriesName { get; set; }
	public string? EventTypeDescription { get; set; }
	public string? FirstName { get; set; }
	public string? LastName { get; set; }
	public string? MobileNumber { get; set; }
	public int RunId { get; set; }
	public string? Horse { get; set; }
	public decimal RunTime { get; set; }
	public decimal TotalPenalties { get; set; }
	public decimal TotalRunTime { get; set; }
}