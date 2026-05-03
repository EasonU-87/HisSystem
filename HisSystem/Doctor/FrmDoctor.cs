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


namespace HisSystem.Doctor
{
    public partial class FrmDoctor : Form
    {

        private PatientBLL patientBLL = new PatientBLL();
        private MedicineBLL medicineBLL = new MedicineBLL();
        private PrescriptionBLL presBLL = new PrescriptionBLL();

        // 记录当前选中的病人
        private Patient currentPatient = null;

        // 购物车 (临时的处方明细列表)
        private BindingList<PrescriptionDetail> cartList = new BindingList<PrescriptionDetail>();

        public FrmDoctor()
        {
            InitializeComponent();
        }

        private void FrmDoctor_Load(object sender, EventArgs e)
        {
            // 显示当前医生名字
            if (AppSession.CurrentUser != null)
            {
                this.Text = $"医生工作站 - 当前医生：{AppSession.CurrentUser.RealName}";
            }

            // 加载左侧病人列表
            LoadPatientList();



            //  加载右侧药品下拉框
            try
            {
                List<Medicine> meds = medicineBLL.GetAllMedicines();
                cboMedicines.DataSource = meds;
                cboMedicines.DisplayMember = "MedicineName"; 
                cboMedicines.ValueMember = "MedicineID";     
                cboMedicines.SelectedIndex = -1;            
            }
            catch (Exception ex)
            {
                MessageBox.Show("药品加载失败：" + ex.Message);
            }
            // 窗体加载时，把购物车和表格永远绑定在一起
            dgvPrescription.DataSource = cartList;
        }

        private void LoadPatientList()
        {
            dgvPatients.DataSource = patientBLL.GetPatientList();
            // 隐藏不重要的列
            if (dgvPatients.Columns["PatientID"] != null) dgvPatients.Columns["PatientID"].Visible = false;
        }

        private void dgvPatients_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return; // 点了表头不管

            // 获取选中的行数据
            var row = dgvPatients.Rows[e.RowIndex];
            currentPatient = row.DataBoundItem as Patient;

            // 更新顶部显示的病人信息
            if (currentPatient != null)
            {
                lblPatientInfo.Text = $"当前就诊：{currentPatient.PatientName} | {currentPatient.Gender} | {currentPatient.Age}岁";
                lblPatientInfo.ForeColor = System.Drawing.Color.Blue;


                // 简单处理：换人就清空购物车
                cartList.Clear();
                RefreshCartGrid();
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            // --- 校验 ---
            if (currentPatient == null)
            {
                MessageBox.Show("请先在左侧选择一位病人！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (cboMedicines.SelectedItem == null)
            {
                MessageBox.Show("请选择药品！");
                return;
            }
            // 检查数量是不是数字
            if (!int.TryParse(txtQuantity.Text.Trim(), out int qty) || qty <= 0)
            {
                MessageBox.Show("数量必须是大于0的整数！");
                return;
            }

            // --- 取值 ---
            Medicine selectedMed = cboMedicines.SelectedItem as Medicine;

            // --- 创建明细对象 ---
            PrescriptionDetail detail = new PrescriptionDetail();
            detail.MedicineID = selectedMed.MedicineID;
            detail.MedicineName = selectedMed.MedicineName; // 冗余存名字
            detail.Price = selectedMed.Price;               // 存当时的单价
            detail.Quantity = qty;
            detail.Usage = txtUsage.Text.Trim();            // 用法
            detail.Frequency = txtFrequency.Text.Trim();    // 频次

            // --- 加入购物车 ---
            cartList.Add(detail);

            // --- 刷新显示 ---
            RefreshCartGrid();

            // --- 体验优化：加完一次，把数量清空，方便加下一个 ---
            txtQuantity.Clear();
            txtUsage.Clear();
            txtFrequency.Clear();
        }
        private void RefreshCartGrid()
        {


            // 1. 重新计算总金额
            decimal total = 0;
            foreach (var item in cartList)
            {
                total += item.Amount;
            }
            lblTotalMoney.Text = $"总金额：{total} 元";

            // 2. 隐藏不需要的列 
            string[] hideCols = { "DetailID", "PrescriptionID", "MedicineID" };
            foreach (var col in hideCols)
            {
                if (dgvPrescription.Columns[col] != null)
                    dgvPrescription.Columns[col].Visible = false;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            
            // 1. 检查有没有选病人
            if (currentPatient == null)
            {
                MessageBox.Show("请先在左侧选择一位就诊病人！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. 检查购物车是不是空的
            if (cartList.Count == 0)
            {
                MessageBox.Show("处方为空，请先添加药品！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RuleCheckResult safeResult = presBLL.CheckSafety(new List<PrescriptionDetail>(cartList));
            // 2. 如果查出了警告信息
            if (safeResult.Messages.Count > 0)
            {
                // 把所有警告信息拼接到一起，准备弹窗显示
                string allWarnings = string.Join("\n\n", safeResult.Messages);

                if (safeResult.IsBlocked == true)
                {
                    // 命中了【禁止】级别：弹出带红叉的警告！
                    MessageBox.Show(allWarnings, " 严重用药安全警告", MessageBoxButtons.OK, MessageBoxIcon.Stop);

                    return;
                }
                else
                {
                    //  命中了【慎用/一般】级别：给个黄色的警告，让医生自己选
                    DialogResult dr = MessageBox.Show(allWarnings + "\n\n是否确认无视风险，强行提交此处方？",
                        " 用药风险提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (dr == DialogResult.No)
                    {
                        //  医生点了“否”，放弃提交
                        return;
                    }
                    // 如果医生点了“是”，没有 return，代码就会乖乖往下走，去存数据库！
                }
            }


            // 3. 准备主表数据 
            PrescriptionMain main = new PrescriptionMain();

            // 从当前选中的病人身上取数据
            main.PatientID = currentPatient.PatientID;
            main.PatientName = currentPatient.PatientName;
            main.PatientGender = currentPatient.Gender; // 
            main.PatientAge = currentPatient.Age;       // 

            // 从登录 Session 取医生数据
            main.DoctorID = AppSession.CurrentUser.UserID;
            main.DoctorName = AppSession.CurrentUser.RealName;

            // 状态默认设为 0（代表刚开立，未去收费处缴费）
            main.Status = 0;

            // 计算总金额
            decimal total = 0;
            foreach (var item in cartList)
            {
                total += item.Amount;
            }
            main.TotalAmount = total;

            try
            {
                // 把 BindingList 转回 List 传给底层
                List<PrescriptionDetail> detailList = new List<PrescriptionDetail>(cartList);

                bool isSuccess = presBLL.SavePrescription(main, detailList);

                if (isSuccess)
                {
                    MessageBox.Show("处方提交成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // 提交成功后，清空购物车，让医生接着看下一个病人
                    cartList.Clear();
                    lblTotalMoney.Text = "总金额：0 元";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "系统错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvPrescription_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

            // 如果 e.RowIndex 是 -1 (说明点的是表头/标题)，直接返回
            // ==========================================================
            if (e.RowIndex < 0) return;




            var selectedDetail = dgvPrescription.Rows[e.RowIndex].DataBoundItem as PrescriptionDetail;

            //  弹窗询问是否删除
            if (selectedDetail != null)
            {
                string msg = $"确定移除【{selectedDetail.MedicineName}】吗？";
                if (MessageBox.Show(msg, "删除确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    //  从购物车(cartList)删除
                    cartList.Remove(selectedDetail);

                    //  刷新表格
                    RefreshCartGrid();
                }
            }
        }

        private void dgvPrescription_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {

            foreach (DataGridViewColumn col in dgvPrescription.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }
    }
}
