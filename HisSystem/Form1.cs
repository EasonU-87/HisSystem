using BLL;
using HisSystem.Admin;
using HisSystem.Doctor;
using HisSystem.Pharmacist;
using HisSystem.Cashier;
using Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HisSystem
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            // 1. 获取界面输入
            string name = txtUser.Text.Trim();
            string pwd = txtPwd.Text.Trim();

            // 简单非空判断
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(pwd))
            {
                MessageBox.Show("请输入账号和密码！");
                return;
            }

            // 2. 调用 BLL 层（BLL 会去调 DAL，DAL 去连数据库）
            UserBLL bll = new UserBLL();

            // --- 关键时刻：这里程序会尝试连接数据库 ---
            User user = bll.Login(name, pwd);

            // 3. 判断提取结果
            if (user != null)
            {
                // 1. 登录成功！把人存进全局变量
                AppSession.CurrentUser = user;

                // 2. 准备跳转的目标窗体
                Form mainForm = null;

                // 3. 根据角色分流
                switch (user.RoleType)
                {
                    case "医生":
                        mainForm = new FrmDoctor(); // 还没建这个窗体的话会报错，去建一个
                        break;
                    case "药师":
                        mainForm = new FrmPharmacist();
                        break;
                    case "管理员":
                        mainForm = new FrmAdmin();
                        break;
                    case "收银员":
                        mainForm = new FrmCashier();
                        break;
                    default:
                        MessageBox.Show("该用户角色未定义界面！", "系统错误");
                        return;
                }

                if (mainForm != null)
                {
                    this.Hide();            // 隐藏登录窗
                    mainForm.ShowDialog();  // 打开主窗体 (代码会停在这里，直到主窗体关闭)
                    this.Show();            // 主窗体关掉后，重新显示登录窗 (可选)
                }
            }
            else
            {
                MessageBox.Show(" 数据库连接通了，但账号或密码错误！\n请检查 T_User 表里的数据。", "登录失败");
            }
            


        }

    }
}
