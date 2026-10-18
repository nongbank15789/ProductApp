using ProductApp.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductApp.Repositories
{
    internal interface IProductRepository
    {
        Task<DataTable> GetProductTypesAsync();
        Task<DataTable> SearchProductsAsync(string keyword, int? statusFilter);
        Task<bool> IsProductIdExistsAsync(string productId);
        Task<int> InsertProductAsync(ProductMasterModel model);
        Task<int> UpdateProductAsync(ProductMasterModel model);
        Task<int> CancelProductAsync(string productId, string updateUser);
    }
}
