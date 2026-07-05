namespace RidePR.Application.DTOs;

public class AdminRevenuePointDto
{
    public DateTime Date { get; set; }

    public decimal GrossRevenue { get; set; }

    public decimal RefundedAmount { get; set; }

    public decimal NetRevenue { get; set; }

    public int PaidPayments { get; set; }
}
