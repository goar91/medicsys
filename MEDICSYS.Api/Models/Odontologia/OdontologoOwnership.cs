namespace MEDICSYS.Api.Models.Odontologia;

public class OdontologoInvoiceOwnership
{
    public Guid InvoiceId { get; set; }
    public Guid OdontologoId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OdontologoAccountingEntryOwnership
{
    public Guid AccountingEntryId { get; set; }
    public Guid OdontologoId { get; set; }
    public DateTime CreatedAt { get; set; }
}
