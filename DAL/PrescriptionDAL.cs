using Model;
using System;
using System.Collections.Generic;
using System.Configuration; // 需要在DAL项目引用System.Configuration
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class PrescriptionDAL
    {

        private string connStr = ConfigurationManager.ConnectionStrings["strConn"].ConnectionString;
        public DataTable GetIncompatibilityRules(string idString)
        {
            DataTable dtRules = new DataTable();


            string sql = $@"SELECT * FROM T_Rule_Incompatibility 
                           WHERE MedicineID_A IN ({idString}) 
                           AND MedicineID_B IN ({idString})";

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                da.Fill(dtRules);
            }
            return dtRules;
        }

        public bool InsertPrescription(PrescriptionMain main, List<PrescriptionDetail> details)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                // 开启事务：保证主表和明细表要么全成功，要么全失败
                SqlTransaction trans = conn.BeginTransaction();

                try
                {
                    // ==========================================
                    // 1. 插入主表 (T_Prescription_Main)
                    // ==========================================
                    string sqlMain = @"INSERT INTO T_Prescription_Main 
                                     (PrescriptionNo, PatientID, PatientName, PatientGender, PatientAge, DoctorID, DoctorName, TotalAmount, Status, CreateTime) 
                                     VALUES 
                                     (@PrescriptionNo, @PatientID, @PatientName, @PatientGender, @PatientAge, @DoctorID, @DoctorName, @TotalAmount, @Status, GETDATE());
                                     SELECT @@IDENTITY;"; 

                    SqlCommand cmdMain = new SqlCommand(sqlMain, conn, trans);

                    string presNo = "PRE" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    cmdMain.Parameters.AddWithValue("@PrescriptionNo", presNo);

                    cmdMain.Parameters.AddWithValue("@PatientID", main.PatientID);
                    cmdMain.Parameters.AddWithValue("@PatientName", main.PatientName);

                    cmdMain.Parameters.AddWithValue("@PatientGender", string.IsNullOrEmpty(main.PatientGender) ? (object)DBNull.Value : main.PatientGender);
                    cmdMain.Parameters.AddWithValue("@PatientAge", main.PatientAge.HasValue ? (object)main.PatientAge.Value : DBNull.Value);

                    cmdMain.Parameters.AddWithValue("@DoctorID", main.DoctorID);
                    cmdMain.Parameters.AddWithValue("@DoctorName", main.DoctorName);
                    cmdMain.Parameters.AddWithValue("@TotalAmount", main.TotalAmount);
                    cmdMain.Parameters.AddWithValue("@Status", main.Status); // 状态 (例如：0代表未缴费)

                    // 执行主表插入，并拿到新生成的 ID
                    int newPresID = Convert.ToInt32(cmdMain.ExecuteScalar());


                    foreach (var item in details)
                    {
                        string sqlDetail = @"INSERT INTO T_Prescription_Detail 
                                           (PrescriptionID, MedicineID, MedicineName, Price, Quantity, Usage, Frequency) 
                                           VALUES 
                                           (@PrescriptionID, @MedicineID, @MedicineName, @Price, @Quantity, @Usage, @Frequency)";

                        SqlCommand cmdDetail = new SqlCommand(sqlDetail, conn, trans);
                        cmdDetail.Parameters.AddWithValue("@PrescriptionID", newPresID);
                        cmdDetail.Parameters.AddWithValue("@MedicineID", item.MedicineID);
                        cmdDetail.Parameters.AddWithValue("@MedicineName", item.MedicineName);
                        cmdDetail.Parameters.AddWithValue("@Price", item.Price);
                        cmdDetail.Parameters.AddWithValue("@Quantity", item.Quantity);

                        // 处理用法和频次可能为空的情况
                        cmdDetail.Parameters.AddWithValue("@Usage", string.IsNullOrEmpty(item.Usage) ? (object)DBNull.Value : item.Usage);
                        cmdDetail.Parameters.AddWithValue("@Frequency", string.IsNullOrEmpty(item.Frequency) ? (object)DBNull.Value : item.Frequency);

                        cmdDetail.ExecuteNonQuery();
                    }


                    trans.Commit();
                    return true;
                }
                catch (Exception ex)
                {

                    trans.Rollback();
                    throw new Exception("数据库保存失败：" + ex.Message);
                }
            }
        }
        public List<PrescriptionDetailDto> GetUnpaidPrescriptions(string searchId)
        {
            List<PrescriptionDetailDto> list = new List<PrescriptionDetailDto>();

            string sql = @"
        SELECT 
            d.MedicineName, 
            med.Spec AS Specification,  
            d.Price AS UnitPrice,       
            d.Quantity 
         FROM T_Prescription_Detail d
         INNER JOIN T_Prescription_Main m ON d.PrescriptionID = m.PrescriptionID
         INNER JOIN T_Medicine med ON d.MedicineID = med.MedicineID
         WHERE (m.PrescriptionNo = @searchId OR CAST(m.PatientID AS VARCHAR) = @searchId) 
         AND m.Status = 0";

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@searchId", searchId);
                    Console.WriteLine($"[调试信息] 正在查询单号: '{searchId}'");

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            // 如果进到这里，说明 SQL 语法没错，但数据库里确实没搜到符合条件的记录
                            Console.WriteLine(" 警告：数据库返回 0 行结果。请检查 Status 是否为 0 或单号是否存在。");
                        }
                        while (reader.Read())
                        {
                            PrescriptionDetailDto detail = new PrescriptionDetailDto();
                            detail.MedicineName = reader["MedicineName"].ToString();
                            detail.Specification = reader["Specification"].ToString();
                            detail.UnitPrice = Convert.ToDecimal(reader["UnitPrice"]);
                            detail.Quantity = Convert.ToInt32(reader["Quantity"]);

                            // 装车
                            list.Add(detail);
                        }
                    }
                }
            }
            return list;
        }


        public bool ConfirmPaymentTransaction(string searchId)
        {
            // 定义 SQL：只改状态，Status 从 0 变 1
            string sql = "UPDATE T_Prescription_Main SET Status = 1 WHERE (PrescriptionNo = @id OR CAST(PatientID AS VARCHAR) = @id) AND Status = 0";

            SqlParameter[] paras = {
        new SqlParameter("@id", searchId)
    };

            // 使用你新改的 SqlHelper (非事务版即可，因为只有一条 SQL)
            // 注意：根据你的 SqlHelper 定义，如果是单条语句，不需要手动开事务
            int rows = SqlHelper.ExecuteNonQuery(sql, paras);

            if (rows == 0)
            {
                throw new Exception("未能找到待缴费处方，可能已缴费或单号错误！");
            }
            return true;
        }

        public bool DispenseWithFEFO(int prescriptionID, int operatorID)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["strConn"].ConnectionString))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. 获取明细（这里可以用原生的，因为需要读数据）
                        string sqlDetails = "SELECT MedicineID, Quantity FROM T_Prescription_Detail WHERE PrescriptionID = @pid";
                        DataTable dtDetails = new DataTable();
                        using (SqlCommand cmd = new SqlCommand(sqlDetails, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@pid", prescriptionID);
                            using (SqlDataAdapter adapter = new SqlDataAdapter(cmd)) { adapter.Fill(dtDetails); }
                        }

                        foreach (DataRow row in dtDetails.Rows)
                        {
                            int medId = Convert.ToInt32(row["MedicineID"]);
                            int requiredQty = Convert.ToInt32(row["Quantity"]);

                            // 2. 查批次
                            string sqlBatches = "SELECT BatchID, BatchStock FROM T_Medicine_Batch WHERE MedicineID = @mid AND BatchStock > 0 ORDER BY ExpiryDate ASC";
                            DataTable dtBatches = new DataTable();
                            using (SqlCommand cmd = new SqlCommand(sqlBatches, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@mid", medId);
                                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd)) { adapter.Fill(dtBatches); }
                            }

                            foreach (DataRow batch in dtBatches.Rows)
                            {
                                if (requiredQty <= 0) break;
                                int bId = Convert.ToInt32(batch["BatchID"]);
                                int bStock = Convert.ToInt32(batch["BatchStock"]);
                                int take = Math.Min(bStock, requiredQty);
                                int afterStock = bStock - take;
                                // 3. 使用【升级版 SqlHelper】执行事务操作
                                // 扣批次
                                SqlHelper.ExecuteNonQuery(trans,"UPDATE T_Medicine_Batch SET BatchStock = @after WHERE BatchID = @bid",
                                    new SqlParameter("@after", afterStock),
                                    new SqlParameter("@bid", bId));

                                // 记流水
                                SqlHelper.ExecuteNonQuery(trans,"INSERT INTO T_Stock_Log (MedicineID, BatchID, ChangeQty, " +
                                    "AfterStock, OpType, OperatorID, PrescriptionID) " +"VALUES (@mid, @bid, @q, @after, '发药', @oid, @pid)",
                                    new SqlParameter("@mid", medId),
                                    new SqlParameter("@bid", bId),
                                    new SqlParameter("@q", -take),
                                    new SqlParameter("@after", afterStock), // 👈 这里把算好的结存库存传给数据库
                                    new SqlParameter("@oid", operatorID),
                                    new SqlParameter("@pid", prescriptionID));

                                requiredQty -= take;
                            }
                            if (requiredQty > 0) throw new Exception("库存不足！");

                            // 4. 更新总库存
                            SqlHelper.ExecuteNonQuery(trans, "UPDATE T_Medicine SET Stock = Stock - @q WHERE MedicineID = @mid",
                                new SqlParameter("@q", row["Quantity"]), new SqlParameter("@mid", medId));
                        }

                        // 5. 改处方状态
                        SqlHelper.ExecuteNonQuery(trans, "UPDATE T_Prescription_Main SET Status = 2 WHERE PrescriptionID = @pid",
                            new SqlParameter("@pid", prescriptionID));

                        trans.Commit();
                        return true;
                    }
                    catch { trans.Rollback(); throw; }
                }
            }
        }

        // 在 DAL/PrescriptionDAL.cs 中修改
        public DataTable GetPaidList()
        {
            string sql = "SELECT * FROM T_Prescription_Main WHERE Status = 1 ORDER BY CreateTime ASC";

            // 对应你的：ExecuteDataTable(string sql, params SqlParameter[] parameters)
            // 既然没有参数，第二个参数传 null 即可
            return SqlHelper.ExecuteDataTable(sql, null);
        }

    }
}
