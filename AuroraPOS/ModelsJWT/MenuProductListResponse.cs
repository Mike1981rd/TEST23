﻿using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class MenuProductListResponse
    {
        public MenuProductList? Valor { get; set; }
        public string? Error { get; set; }
        public bool Success { get; set; }
    }
}
