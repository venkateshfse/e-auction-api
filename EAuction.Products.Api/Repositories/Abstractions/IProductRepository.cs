﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using EAuction.Products.Api.Entities;

namespace EAuction.Products.Api.Repositories.Abstractions
{
    public interface IProductRepository
    {
        Task Create(Product product);
        Task<Product> GetProduct(string id);
        Task<bool> Delete(string id);
    }
}