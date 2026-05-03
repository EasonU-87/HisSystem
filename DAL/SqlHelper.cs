using System;
using System.Configuration; // 
using System.Data;
using System.Data.SqlClient; // 

namespace DAL
{
    public class SqlHelper
    {
        // 1. 读取连接字符串
        // "strConn" 必须和你主程序 App.config 里的 name 一模一样
        private static readonly string connStr = ConfigurationManager.ConnectionStrings["strConn"].ConnectionString;

        /// <summary>
        /// 执行增、删、改 (INSERT, UPDATE, DELETE)
        /// </summary>
        /// <returns>受影响的行数</returns>
        public static int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }
                    conn.Open();
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// 执行查询，返回表格 (SELECT)
        /// </summary>
        /// <returns>DataTable (一张表)</returns>
        public static DataTable ExecuteDataTable(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt); // 自动 Open 和 Close
                        return dt;
                    }
                }
            }
        }

        /// <summary>
        /// 执行查询，返回第一行第一列 (SELECT COUNT(*), SELECT MAX...)
        /// </summary>
        /// <returns>单个值 (Object)</returns>
        public static object ExecuteScalar(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }
                    conn.Open();
                    return cmd.ExecuteScalar();
                }
            }
        }
        // 在 SqlHelper 类中添加这个新方法，支持事务
        public static int ExecuteNonQuery(SqlTransaction trans, string sql, params SqlParameter[] parameters)
        {
            // 注意：这里不能用 using(conn)，因为事务依赖于外部传入的长连接
            SqlCommand cmd = new SqlCommand(sql, trans.Connection, trans);
            if (parameters != null)
            {
                cmd.Parameters.AddRange(parameters);
            }
            // 事务环境下，连接已经是 Open 状态，直接执行
            return cmd.ExecuteNonQuery();
        }
    }
}