using Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class PatientDAL
    {
        // 获取所有候诊病人
        public List<Patient> GetAllPatients()
        {
            // 按挂号时间倒序，新挂号的在最上面
            string sql = "SELECT * FROM T_Patient ORDER BY CreateTime DESC";

            // 调用 SqlHelper 执行查询
            DataTable dt = SqlHelper.ExecuteDataTable(sql);

            List<Patient> list = new List<Patient>();

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new Patient()
                {
                    PatientID = Convert.ToInt32(row["PatientID"]),
                    PatientName = row["PatientName"].ToString(),
                    Gender = row["Gender"].ToString(),
                    Age = Convert.ToInt32(row["Age"]),

                    CardID = row["CardID"] != DBNull.Value ? row["CardID"].ToString() : "",
                    Phone = row["Phone"] != DBNull.Value ? row["Phone"].ToString() : "",

                    CreateTime = Convert.ToDateTime(row["CreateTime"])
                });
            }
            return list;
        }
    }
}
