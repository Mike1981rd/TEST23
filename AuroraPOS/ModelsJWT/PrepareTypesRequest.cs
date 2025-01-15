namespace AuroraPOS.ModelsJWT
{
    public class PrepareTypesRequest
    {
        public int Start { get; set; } = 0;
        public int Length { get; set; } = 10;
        public string SortColumn { get; set; } = "phone";
        public string SortColumnDirection { get; set; } = "desc";
        public string? SearchValue { get; set; }
        public bool JustData { get; set; } = false;
    }
}
