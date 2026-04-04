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
        // 从 App.config 读取你的数据库连接字符串 (请确认名字叫 strCon 或你自己的名字)
        private string connStr = ConfigurationManager.ConnectionStrings["strConn"].ConnectionString;
        public DataTable GetIncompatibilityRules(string idString)
        {
            DataTable dtRules = new DataTable();

            // 只要规则表里的 药品A 和 药品B 【同时】出现在我们开的这堆药(idString)里，就被抓住了！
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
                                     SELECT @@IDENTITY;"; // 获取刚生成的 PrescriptionID 主键

                    SqlCommand cmdMain = new SqlCommand(sqlMain, conn, trans);

                    // 生成处方编号 (PRE + 年月日时分秒)
                    string presNo = "PRE" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    cmdMain.Parameters.AddWithValue("@PrescriptionNo", presNo);

                    cmdMain.Parameters.AddWithValue("@PatientID", main.PatientID);
                    cmdMain.Parameters.AddWithValue("@PatientName", main.PatientName);

                    // 处理允许为空的字段 (如果没填，要转成 DBNull.Value 存进数据库)
                    cmdMain.Parameters.AddWithValue("@PatientGender", string.IsNullOrEmpty(main.PatientGender) ? (object)DBNull.Value : main.PatientGender);
                    cmdMain.Parameters.AddWithValue("@PatientAge", main.PatientAge.HasValue ? (object)main.PatientAge.Value : DBNull.Value);

                    cmdMain.Parameters.AddWithValue("@DoctorID", main.DoctorID);
                    cmdMain.Parameters.AddWithValue("@DoctorName", main.DoctorName);
                    cmdMain.Parameters.AddWithValue("@TotalAmount", main.TotalAmount);
                    cmdMain.Parameters.AddWithValue("@Status", main.Status); // 状态 (例如：0代表未缴费)

                    // 执行主表插入，并拿到新生成的 ID
                    int newPresID = Convert.ToInt32(cmdMain.ExecuteScalar());

                    // ==========================================
                    // 2. 循环插入明细表 (T_Prescription_Detail)
                    // ==========================================
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

                    // 全部没报错，提交事务！
                    trans.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    // 报错了，全部回滚撤销！
                    trans.Rollback();
                    throw new Exception("数据库保存失败：" + ex.Message);
                }
            }
        }
        public List<PrescriptionDetailDto> GetUnpaidPrescriptions(string searchId)
        {
            List<PrescriptionDetailDto> list = new List<PrescriptionDetailDto>();

            // 架构师级 SQL：多表联查
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
                            Console.WriteLine("⚠️ 警告：数据库返回 0 行结果。请检查 Status 是否为 0 或单号是否存在。");
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


        public bool ConfirmPaymentTransaction(string searchId, List<PrescriptionDetailDto> medicineList)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                // 🌟 开启数据库事务！神圣的原子操作开始！
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    try
                    {
                        // 第一拳：修改处方主表的状态（0 变 1）
                        string updateStatusSql = "UPDATE T_Prescription_Main SET Status = 1 WHERE (PrescriptionNo = @id OR CAST(PatientID AS VARCHAR) = @id) AND Status = 0";
                        using (SqlCommand cmdStatus = new SqlCommand(updateStatusSql, conn, trans)) // 注意：必须把 trans 传给 cmd
                        {
                            cmdStatus.Parameters.AddWithValue("@id", searchId);
                            int rows = cmdStatus.ExecuteNonQuery();
                            if (rows == 0)
                            {
                                throw new Exception("未能找到需要缴费的处方，可能已缴费或单号错误！");
                            }
                        }

                        // 第二拳：循环扣减药房库存
                        // 🔥 架构师精准修正：表名改为 Medicine，库存字段改为 Stock
                        string updateStockSql = "UPDATE T_Medicine SET Stock = Stock - @qty WHERE MedicineName = @medName";

                        foreach (var item in medicineList)
                        {
                            using (SqlCommand cmdStock = new SqlCommand(updateStockSql, conn, trans))
                            {
                                cmdStock.Parameters.AddWithValue("@qty", item.Quantity);
                                cmdStock.Parameters.AddWithValue("@medName", item.MedicineName);

                                int stockRows = cmdStock.ExecuteNonQuery();
                                if (stockRows == 0)
                                {
                                    // 如果某个药找不到，直接抛出异常，触发整体回滚！
                                    throw new Exception($"药品【{item.MedicineName}】扣减库存失败，药房可能无此药！");
                                }
                            }
                        }

                        // 🌟 两拳全部打完没报错，提交事务！数据永久保存！
                        trans.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // 💥 一旦发生任何异常，瞬间回滚！就当无事发生过！
                        trans.Rollback();
                        throw new Exception("收费失败，系统已安全回滚：" + ex.Message);
                    }
                }
            }
        }






    }
}
