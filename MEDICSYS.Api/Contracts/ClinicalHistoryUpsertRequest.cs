using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace MEDICSYS.Api.Contracts;

public class ClinicalHistoryUpsertRequest
{
    [Required]
    public JsonElement Data { get; set; }
}
