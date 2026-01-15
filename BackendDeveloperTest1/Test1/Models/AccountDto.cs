public class AccountDto
{
    public int UID { get; set; }
    public int LocationUid { get; set; }
    public Guid Guid { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? UpdatedUtc { get; set; }
    public int Status { get; set; }
    public DateTime? EndDateUtc { get; set; }
    public int AccountType { get; set; }
    public double? PaymentAmount { get; set; }
    public bool PendCancel { get; set; }
    public DateTime? PendCancelDateUtc { get; set; }
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public DateTime NextBillingUtc { get; set; }

}

