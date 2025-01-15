using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class PrepareTypesList
    {
        public int recordsFiltered { get; set; } = 0;
        public int recordsTotal { get; set; } = 0;
        public List<PrepareTypes>? prepareTypes { get; set; }
    }
}
