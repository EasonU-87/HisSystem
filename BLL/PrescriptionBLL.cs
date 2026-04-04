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
        // =================================================================================
        // 💥 新增核心引擎：用药安全智能审方大脑
        // =================================================================================
        public RuleCheckResult CheckSafety(List<PrescriptionDetail> cartList)
        {
            RuleCheckResult result = new RuleCheckResult();

            // 1. 防呆设计：如果购物车里只有1个药或者干脆没药，绝不可能有组合冲突，直接放行！
            if (cartList == null || cartList.Count < 2)
            {
                return result;
            }

            // 2. 提取药品 ID：把购物车里所有的 MedicineID 抽出来，拼成 "1,2,5" 这样的字符串
            List<int> medIds = new List<int>();
            foreach (var item in cartList)
            {
                medIds.Add(item.MedicineID);
            }
            string idString = string.Join(",", medIds);

            // 3. 派 DAL 去数据库查：有没有命中什么法律条文？
            DataTable dtRules = dal.GetIncompatibilityRules(idString);

            // 4. 大脑开始审判！
            if (dtRules != null && dtRules.Rows.Count > 0)
            {
                foreach (DataRow row in dtRules.Rows)
                {
                    // 提取数据库里的风险等级和描述信息
                    string riskLevel = row["RiskLevel"].ToString();
                    string msg = row["Description"].ToString();

                    // 把信息包装得专业一点，存进结果盒子里
                    result.Messages.Add($"[{riskLevel}] {msg}");

                    // 💥 最关键的审判逻辑：如果风险等级包含“禁止”两个字，立刻触发强制阻断开关！
                    if (riskLevel.Contains("禁止"))
                    {
                        result.IsBlocked = true;
                    }
                }
            }

            // 把判决书交出去
            return result;
        }

        public bool SavePrescription(PrescriptionMain main, List<PrescriptionDetail> details)
        {
            // 防呆校验：如果不小心传了空数据，直接拦住
            if (details == null || details.Count == 0) return false;

            return dal.InsertPrescription(main, details);
        }

        public List<PrescriptionDetailDto> GetUnpaidPrescriptions(string searchId)
        {
            // 🛡️ BLL 第一道防线：参数校验
            if (string.IsNullOrEmpty(searchId))
            {
                throw new Exception("业务拦截：查询单号不能为空，请重新输入！");
            }

            // 规则通过，呼叫 DAL 层去底层真正的数据库里拿数据
            return dal.GetUnpaidPrescriptions(searchId);
        }

        /// <summary>
        /// 核心业务逻辑：收费并扣减库存
        /// </summary>
        /// <param name="searchId">患者卡号或处方号</param>
        /// <param name="medicineList">要收费的药品明细列表</param>
        /// <returns>是否收费成功</returns>
        public bool PayAndCheckout(string searchId, List<PrescriptionDetailDto> medicineList)
        {
            // ==========================================
            // 🛡️ 第一道防线：参数基础校验
            // ==========================================
            if (string.IsNullOrEmpty(searchId))
            {
                // 直接抛出异常，UI 层收到后会弹窗提示给收费员
                throw new Exception("业务拦截：缴费单号不能为空，请重新输入！");
            }

            if (medicineList == null || medicineList.Count == 0)
            {
                throw new Exception("业务拦截：处方明细为空，该单号没有需要缴费的药品！");
            }

            // ==========================================
            // 🛡️ 第二道防线：深入业务规则校验（可选，显专业）
            // ==========================================
            foreach (var item in medicineList)
            {
                if (item.Quantity <= 0)
                {
                    throw new Exception($"业务拦截：药品【{item.MedicineName}】的数量异常（不能小于等于0）！");
                }
                if (item.UnitPrice < 0)
                {
                    throw new Exception($"业务拦截：药品【{item.MedicineName}】的单价异常（不能为负数）！");
                }
            }

            // ==========================================
            // 🚀 防线全部通过，放行！交由 DAL 层执行底层生死事务！
            // ==========================================
            // 这里的 ConfirmPaymentTransaction 就是你在 DAL 层写的那个带有 SqlTransaction 事务的方法
            return dal.ConfirmPaymentTransaction(searchId, medicineList);
        }




    }
}
