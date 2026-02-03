using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace MEDICSYS.Api.Contracts;

public class ClinicalHistoryUpsertRequest
{
    public Guid? PatientId { get; set; }

    [Required]
    public JsonElement Data { get; set; }
}
