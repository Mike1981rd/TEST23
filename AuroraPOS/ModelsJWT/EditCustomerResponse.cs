using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT;

public class EditCustomerResponse
{
    public long? Id { get; set; }
    public int status { get; set; }
    public string Message { get; set; }
    public bool Success { get; set; }
}