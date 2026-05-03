using Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace DAL
{
    public class InboundDAL
    {

        
        private string connStr = ConfigurationManager.ConnectionStrings["strConn"].ConnectionString;

        /// <summary>
        /// 执行药品入库：插入流水记录并同步更新总库存
        /// </summary>
        /// <param name="model">入库实体对象</param>
        /// <returns>是否执行成功</returns>
        public bool AddInbound(Inbound model)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                // 2. 开启事务：确保“记账”和“加库存”要么都成功，要么都失败
                SqlTransaction trans = conn.BeginTransaction();

                try
                {
                    // --- 步骤 A：向 T_Inbound 插入入库记录 ---
                    // 注意：InboundID 是自增的，不需要插入；InboundTime 使用 GETDATE() 自动生成
                    string sqlInbound = @"INSERT INTO T_Inbound 
                                       (MedicineID, BatchNumber, Quantity, OperatorID, InboundTime, ExpiryDate) 
                                       VALUES 
                                       (@MedID, @BatchNum, @Qty, @OpID, GETDATE(), @ExpDate)";

                    using (SqlCommand cmdInsert = new SqlCommand(sqlInbound, conn, trans))
                    {
                        cmdInsert.Parameters.AddWithValue("@MedID", model.MedicineID);
                        cmdInsert.Parameters.AddWithValue("@BatchNum", model.BatchNumber);
                        cmdInsert.Parameters.AddWithValue("@Qty", model.Quantity);
                        cmdInsert.Parameters.AddWithValue("@OpID", model.OperatorID);
                        cmdInsert.Parameters.AddWithValue("@ExpDate", model.ExpiryDate);
                        cmdInsert.ExecuteNonQuery();
                    }

                    string sqlUpsertBatch = @"
                        IF EXISTS (SELECT 1 FROM T_Medicine_Batch WHERE MedicineID = @MedID AND BatchNo = @BatchNo)
                        BEGIN
                            -- 老批次：叠加剩余库存 (BatchStock)
                            UPDATE T_Medicine_Batch 
                            SET BatchStock = BatchStock + @Qty ,InPrice = @Price
                            WHERE MedicineID = @MedID AND BatchNo = @BatchNo
                        END
                        ELSE
                        BEGIN
                            -- 新批次：插入全新记录
                            INSERT INTO T_Medicine_Batch 
                            (MedicineID, BatchNo, ExpiryDate, BatchStock, InPrice, CreateTime)
                            VALUES 
                            (@MedID, @BatchNo, @ExpDate, @Qty, @Price, GETDATE()) 
                            -- 注：InPrice 暂设为 0，防止数据库报错。以后界面加了进价再传真实值
                        END";

                    using (SqlCommand cmdBatch = new SqlCommand(sqlUpsertBatch, conn, trans))
                    {
                        cmdBatch.Parameters.AddWithValue("@MedID", model.MedicineID);
                        cmdBatch.Parameters.AddWithValue("@BatchNo", model.BatchNumber); // 匹配你表里的 BatchNo
                        cmdBatch.Parameters.AddWithValue("@Qty", model.Quantity);
                        cmdBatch.Parameters.AddWithValue("@ExpDate", model.ExpiryDate);
                        cmdBatch.Parameters.AddWithValue("@Price", model.InPrice); //  传入进价
                        cmdBatch.ExecuteNonQuery();
                    }


                    // 这里是核心：在原有 Stock 基础上加上本次入库的 Quantity
                    string sqlUpdateStock = "UPDATE T_Medicine SET Stock = Stock + @Qty WHERE MedicineID = @MedID";
                    using (SqlCommand cmdUpdate = new SqlCommand(sqlUpdateStock, conn, trans))
                    {
                        cmdUpdate.Parameters.AddWithValue("@Qty", model.Quantity);
                        cmdUpdate.Parameters.AddWithValue("@MedID", model.MedicineID);
                        cmdUpdate.ExecuteNonQuery();
                    }
                    // 3. 提交事务
                    trans.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    // 4. 出错回滚：保护数据不被搞乱
                    trans.Rollback();
                    // 这里可以直接抛出异常，让 UI 层捕获并弹窗提醒
                    throw new Exception("入库数据库操作失败：" + ex.Message);
                }
            }
        }

        /// <summary>
        /// 获取所有药品列表 (用于绑定下拉框)
        /// </summary>
        public DataTable GetMedicineList()
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string sql = "SELECT MedicineID, MedicineName FROM T_Medicine";
                SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        /// <summary>
        /// 获取今日入库流水
        /// </summary>
        public DataTable GetTodayInboundHistory()
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string sql = @"
                    SELECT i.InboundID AS [流水号], m.MedicineName AS [药品名称], 
                           i.BatchNumber AS [批号], i.Quantity AS [入库数量], 
                           i.ExpiryDate AS [有效期], i.InboundTime AS [入库时间]
                    FROM T_Inbound i
                    INNER JOIN T_Medicine m ON i.MedicineID = m.MedicineID
                    WHERE CAST(i.InboundTime AS DATE) = CAST(GETDATE() AS DATE)
                    ORDER BY i.InboundTime DESC";

                SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        /// 获取效期预警数据 
        /// </summary>
        public DataTable GetExpiryAlerts()
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string sql = @"
                    SELECT m.MedicineName AS [预警药品], i.BatchNumber AS [问题批号], 
                           i.ExpiryDate AS [有效期], i.Quantity AS [批次剩余], 
                           DATEDIFF(day, GETDATE(), i.ExpiryDate) AS [距离过期(天)]
                    FROM T_Inbound i
                    INNER JOIN T_Medicine m ON i.MedicineID = m.MedicineID
                    WHERE DATEDIFF(day, GETDATE(), i.ExpiryDate) <= 30
                    ORDER BY i.ExpiryDate ASC";

                SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

    }
}
