using DAL;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL
{
    public class UserBLL
    {
        // 1. 实例化 DAL 对象 
        private UserDAL dal = new UserDAL();

        // 2. 这就是你 FrmLogin 里调用的那个 Login 函数！
        public User Login(string username, string password)
        {
            // 这里可以写一点逻辑，比如：账号密码能不能为空？
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                throw new Exception("账号密码不能为空！");
            }

            // 指挥 DAL 去查数据，并把结果直接返回给 UI
            return dal.Login(username, password);
        }
    }
}
