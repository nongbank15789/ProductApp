using ProductApp.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductApp.Repositories
{
    internal class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<DataTable> GetProductTypesAsync()
        {
            const string sql = @"
                SELECT PRODUCTTYPEID, 
                       (PRODUCTTYPEID + ' : ' + PRODUCTTYPENAME) AS DISPLAY_NAME 
                FROM dbo.PRODUCTTYPE WITH (NOLOCK)
                ORDER BY PRODUCTTYPEID;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var dt = new DataTable();
                    dt.Load(reader);
                    return dt;
                }
            }
        }

        public async Task<DataTable> SearchProductsAsync(string keyword, int? statusFilter)
        {
            const string sql = @"
                SELECT m.PRODUCTID,
                       m.PRODUCTNAME,
                       m.PRODUCTTYPEID,
                       ISNULL(t.PRODUCTTYPENAME, '-') AS PRODUCTTYPENAME,
                       m.PRODUCTSTATUS,
                       CASE m.PRODUCTSTATUS 
                            WHEN 0 THEN N'ใช้งาน (0)' 
                            WHEN 9 THEN N'ยกเลิก (9)' 
                            ELSE N'ไม่ระบุ' 
                       END AS STATUS_DISPLAY,
                       m.CREATEUSER,
                       m.CREATEDATE,
                       m.UPDATEUSER,
                       m.UPDATEDATE
                FROM dbo.PRODUCTMASTER m WITH (NOLOCK)
                LEFT JOIN dbo.PRODUCTTYPE t WITH (NOLOCK) ON m.PRODUCTTYPEID = t.PRODUCTTYPEID
                WHERE (@Keyword = '' OR m.PRODUCTID LIKE @SearchPattern ESCAPE '\' OR m.PRODUCTNAME LIKE @SearchPattern ESCAPE '\')
                  AND (@StatusFilter IS NULL OR m.PRODUCTSTATUS = @StatusFilter)
                ORDER BY m.CREATEDATE DESC, m.PRODUCTID ASC;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                string cleanKeyword = (keyword ?? string.Empty).Trim();
                string escapedKeyword = cleanKeyword.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");

                cmd.Parameters.Add("@Keyword", SqlDbType.VarChar, 50).Value = cleanKeyword;
                cmd.Parameters.Add("@SearchPattern", SqlDbType.VarChar, 60).Value = $"%{escapedKeyword}%";
                cmd.Parameters.Add("@StatusFilter", SqlDbType.Int).Value = statusFilter.HasValue ? (object)statusFilter.Value : DBNull.Value;

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var dt = new DataTable();
                    dt.Load(reader);
                    return dt;
                }
            }
        }

        public async Task<bool> IsProductIdExistsAsync(string productId)
        {
            const string sql = "SELECT COUNT(1) FROM dbo.PRODUCTMASTER WITH (NOLOCK) WHERE PRODUCTID = @ProductId;";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@ProductId", SqlDbType.VarChar, 20).Value = productId.Trim();
                await conn.OpenAsync();
                int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                return count > 0;
            }
        }

        public async Task<int> InsertProductAsync(ProductMasterModel model)
        {
            const string sql = @"
                INSERT INTO dbo.PRODUCTMASTER 
                    (PRODUCTID, PRODUCTNAME, PRODUCTTYPEID, PRODUCTSTATUS, CREATEUSER, CREATEDATE)
                VALUES 
                    (@ProductId, @ProductName, @ProductTypeId, @ProductStatus, @CreateUser, GETDATE());";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@ProductId", SqlDbType.VarChar, 20).Value = model.ProductId;
                cmd.Parameters.Add("@ProductName", SqlDbType.VarChar, 20).Value = model.ProductName;
                cmd.Parameters.Add("@ProductTypeId", SqlDbType.VarChar, 5).Value = model.ProductTypeId;
                cmd.Parameters.Add("@ProductStatus", SqlDbType.Int).Value = model.ProductStatus;
                cmd.Parameters.Add("@CreateUser", SqlDbType.VarChar, 20).Value = model.CreateUser;

                await conn.OpenAsync();
                return await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<int> UpdateProductAsync(ProductMasterModel model)
        {
            const string sql = @"
                UPDATE dbo.PRODUCTMASTER
                SET PRODUCTNAME   = @ProductName,
                    PRODUCTTYPEID = @ProductTypeId,
                    PRODUCTSTATUS = @ProductStatus,
                    UPDATEUSER    = @UpdateUser,
                    UPDATEDATE    = GETDATE()
                WHERE PRODUCTID   = @ProductId;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@ProductId", SqlDbType.VarChar, 20).Value = model.ProductId;
                cmd.Parameters.Add("@ProductName", SqlDbType.VarChar, 20).Value = model.ProductName;
                cmd.Parameters.Add("@ProductTypeId", SqlDbType.VarChar, 5).Value = model.ProductTypeId;
                cmd.Parameters.Add("@ProductStatus", SqlDbType.Int).Value = model.ProductStatus;
                cmd.Parameters.Add("@UpdateUser", SqlDbType.VarChar, 20).Value = model.UpdateUser;

                await conn.OpenAsync();
                return await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<int> CancelProductAsync(string productId, string updateUser)
        {
            const string sql = @"
                UPDATE dbo.PRODUCTMASTER
                SET PRODUCTSTATUS = @CancelStatus,
                    UPDATEUSER    = @UpdateUser,
                    UPDATEDATE    = GETDATE()
                WHERE PRODUCTID   = @ProductId;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@ProductId", SqlDbType.VarChar, 20).Value = productId;
                cmd.Parameters.Add("@CancelStatus", SqlDbType.Int).Value = ProductStatus.Canceled;
                cmd.Parameters.Add("@UpdateUser", SqlDbType.VarChar, 20).Value = updateUser;

                await conn.OpenAsync();
                return await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}
