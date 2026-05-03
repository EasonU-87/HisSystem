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

        private BindingList<PrescriptionDetailDto> prescriptionList;
        private PrescriptionBLL presBLL = new PrescriptionBLL();
        public FrmCashier()
        {
            InitializeComponent();

            prescriptionList = new BindingList<PrescriptionDetailDto>();

            dgvPrescription.AutoGenerateColumns = false;

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
                // 1. 查数据
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

            

            try
            {
                bool isSuccess = presBLL.PayAndCheckout(searchId);

                if (isSuccess)
                {
                    MessageBox.Show(" 收费成功！\n处方已转入“待发药”状态，请指引患者前往药房窗口取药。",
                            "交易完成", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // 清空界面
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




        //算账专用方法：遍历列表，把小计加起来
        private void CalculateTotalMoney()
        {
            decimal total = 0;

            // 注意：我们是遍历数据列表 prescriptionList，
            foreach (var item in prescriptionList)
            {
                total += item.SubTotal;
            }

            lblTotalMoney.Text = $"待收总金额：{total:F2} 元";
        }
    }
}
