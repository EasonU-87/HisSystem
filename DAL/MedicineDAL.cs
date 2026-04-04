using Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class MedicineDAL
    {
        public List<Medicine> GetAllMedicines()
        {
            string sql = "SELECT * FROM T_Medicine";
            DataTable dt = SqlHelper.ExecuteDataTable(sql);

            List<Medicine> list = new List<Medicine>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new Medicine()
                {
                    MedicineID = (int)row["MedicineID"],
                    MedicineName = row["MedicineName"].ToString(),
                    PyCode = row["PyCode"].ToString(),
                    Spec = row["Spec"].ToString(),
                    Unit = row["Unit"].ToString(),
                    Price = (decimal)row["Price"],
                    Stock = (int)row["Stock"]
                });
            }
            return list;
        }
    }
}
