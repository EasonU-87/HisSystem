using DAL;
using Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL
{
    public class PrescriptionBLL
    {
        private PrescriptionDAL dal = new PrescriptionDAL();

        public RuleCheckResult CheckSafety(List<PrescriptionDetail> cartList)
        {
            RuleCheckResult result = new RuleCheckResult();

            // 如果购物车里只有1个药或者没药，绝不可能有组合冲突
            if (cartList == null || cartList.Count < 2)
            {
                return result;
            }

            // 提取药品 ID：把购物车里所有的 MedicineID 抽出来，拼成 "1,2,5" 这样的字符串
            List<int> medIds = new List<int>();
            foreach (var item in cartList)
            {
                medIds.Add(item.MedicineID);
            }
            string idString = string.Join(",", medIds);

           
            DataTable dtRules = dal.GetIncompatibilityRules(idString);

            
            if (dtRules != null && dtRules.Rows.Count > 0)
            {
                foreach (DataRow row in dtRules.Rows)
                {
                    // 提取数据库里的风险等级和描述信息
                    string riskLevel = row["RiskLevel"].ToString();
                    string msg = row["Description"].ToString();

                 
                    result.Messages.Add($"[{riskLevel}] {msg}");

                    // 审判逻辑：如果风险等级包含“禁止”两个字，立刻触发强制阻断开关
                    if (riskLevel.Contains("禁止"))
                    {
                        result.IsBlocked = true;
                    }
                }
            }

            return result;
        }

        public bool SavePrescription(PrescriptionMain main, List<PrescriptionDetail> details)
        {
            // 如果不小心传了空数据，直接拦住
            if (details == null || details.Count == 0) return false;

            return dal.InsertPrescription(main, details);
        }

        public List<PrescriptionDetailDto> GetUnpaidPrescriptions(string searchId)
        {
          
            if (string.IsNullOrEmpty(searchId))
            {
                throw new Exception("业务拦截：查询单号不能为空，请重新输入！");
            }

          
            return dal.GetUnpaidPrescriptions(searchId);
        }

        /// 核心业务逻辑：收费并扣减库存
        public bool PayAndCheckout(string searchId)
        {

            if (string.IsNullOrEmpty(searchId))
            {
                // 直接抛出异常，UI 层收到后会弹窗提示给收费员
                throw new Exception("业务拦截：缴费单号不能为空，请重新输入！");
            }

            return dal.ConfirmPaymentTransaction(searchId);
        }

        /// 药师站专用：获取所有已缴费、待发药的处方列表 (Status = 1)
        public DataTable GetPaidList()
        {
            return dal.GetPaidList();
        }

        /// 逻辑：调用 DAL 层的 FEFO 算法，跨表扣减批次库存、总库存并记录流水
        /// </summary>
        /// <param name="prescriptionID">处方主键ID</param>
        /// <param name="operatorID">当前操作的药师ID</param>
        public bool DispenseWithFEFO(int prescriptionID, int operatorID)
        {
            // 1. 业务校验
            if (prescriptionID <= 0)
            {
                throw new Exception("业务拦截：处方 ID 异常，无法发药！");
            }
            if (operatorID <= 0)
            {
                throw new Exception("业务拦截：操作人信息丢失，请重新登录！");
            }

            // 2. 执行 DAL 层复杂的事务发药逻辑
            return dal.DispenseWithFEFO(prescriptionID, operatorID);
        }

    }
}
