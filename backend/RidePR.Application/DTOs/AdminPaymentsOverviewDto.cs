namespace RidePR.Application.DTOs;

public class AdminPaymentsOverviewDto
{
    public int TotalPayments { get; set; }

    public int PaidPayments { get; set; }

    public int PendingPayments { get; set; }

    public int FailedPayments { get; set; }

    public int RefundedPayments { get; set; }

    public decimal GrossRevenue { get; set; }

    public decimal RefundedAmount { get; set; }

    public decimal NetRevenue { get; set; }
}
