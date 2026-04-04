using BLL;
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

namespace HisSystem.Cashier
{
    public partial class FrmCashier : Form
    {
        // 🔥 核心血管：声明一个绑定列表！以后我们只管操作这个列表，界面表格会自动跟着变！
        private BindingList<PrescriptionDetailDto> prescriptionList;
        private PrescriptionBLL presBLL = new PrescriptionBLL();
        public FrmCashier()
        {
            InitializeComponent();
            // 1. 初始化这个空列表
            prescriptionList = new BindingList<PrescriptionDetailDto>();

            // 2. 告诉表格：千万别自己瞎生成列，用我之前在设计器里配好的列！
            dgvPrescription.AutoGenerateColumns = false;

            // 3. 将血管插进表格：把数据源绑定为我们的列表
            dgvPrescription.DataSource = prescriptionList;
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string searchId = txtSearchID.Text.Trim();
            if (string.IsNullOrEmpty(searchId))
            {
                MessageBox.Show("请先输入患者卡号或处方号！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                // 1. 呼叫业务逻辑层工具查数据
                List<PrescriptionDetailDto> dataFromDb = presBLL.GetUnpaidPrescriptions(searchId);

                // 2. 清空旧数据，把新查到的数据倒进界面绑定的列表里
                prescriptionList.Clear();
                foreach (var item in dataFromDb)
                {
                    prescriptionList.Add(item);
                }

                // 3. 如果没查到数据的提示
                if (prescriptionList.Count == 0)
                {
                    MessageBox.Show("未找到该单号的未缴费处方明细，或输入有误！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                // 4. 更新右下角的总金额
                CalculateTotalMoney();
            }
            catch (Exception ex)
            {
                // 稳稳接住 BLL 抛出的业务异常或数据库异常
                MessageBox.Show(ex.Message, "查询失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // 最后一步：调用算钱方法，更新右下角的总金额！
            CalculateTotalMoney();
        }

        private void btnPay_Click(object sender, EventArgs e)
        {
            string searchId = txtSearchID.Text.Trim();

            // 将界面上绑定的数据转换成标准 List，交给 BLL 处理
            List<PrescriptionDetailDto> payList = new List<PrescriptionDetailDto>(prescriptionList);

            try
            {
                // 发起底层生死事务！(UI 彻底当甩手掌柜)
                bool isSuccess = presBLL.PayAndCheckout(searchId, payList);

                if (isSuccess)
                {
                    // 胜利收尾
                    MessageBox.Show(" 收费成功！处方状态已更新，药房库存已扣减！", "交易完成", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // 清空界面，准备迎接下一位患者
                    prescriptionList.Clear();
                    txtSearchID.Clear();
                    CalculateTotalMoney();
                }
            }
            catch (Exception ex)
            {
                // 如果库存不足、拦截校验不通过，都在这里弹窗提示
                MessageBox.Show(ex.Message, "交易失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }




        // 🧮 算账专用方法：遍历列表，把小计加起来
        private void CalculateTotalMoney()
        {
            decimal total = 0;

            // 注意：我们是遍历数据列表 _prescriptionList，而不是去遍历界面的 DGV 行，这才是真正的解耦！
            foreach (var item in prescriptionList)
            {
                total += item.SubTotal;
            }

            // 更新右下角的超大红色字体 Label
            lblTotalMoney.Text = $"待收总金额：{total:F2} 元";
        }
    }
}
