using Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class UserDAL
    {
        // 登录查库方法
        public User Login(string username, string password)
        {
            string sql = "SELECT * FROM T_User WHERE UserName = @u AND Password = @p";

            SqlParameter[] paras = {
                new SqlParameter("@u", username),
                new SqlParameter("@p", password)
            };

            // 调用你的 SqlHelper (确保 SqlHelper 也在 DAL 项目里)
            DataTable dt = SqlHelper.ExecuteDataTable(sql, paras);

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                return new User()
                {
                    UserID = Convert.ToInt32(row["UserID"]),
                    UserName = row["UserName"].ToString(),
                    Password = row["Password"].ToString(),
                    RealName = row["RealName"].ToString(),
                    RoleType = row["RoleType"].ToString()
                };
            }
            return null; // 查不到就返回空
        }
    }
}
