using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductApp.Models
{
    internal class ProductMasterModel
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductTypeId { get; set; }
        public int ProductStatus { get; set; }
        public string CreateUser { get; set; }
        public DateTime CreateDate { get; set; }
        public string UpdateUser { get; set; }
        public DateTime? UpdateDate { get; set; }
    }
}
