using DAL;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace BLL
{
    public class InboundBLL
    {
        private InboundDAL dal = new InboundDAL();


        public bool AddInbound(Inbound model)
        {

            // 规则1：入库数量的绝对安全校验
            if (model.Quantity <= 0)
            {
                throw new Exception("业务异常：入库数量必须大于 0！");
            }

            // 规则2：效期安全红线
            if (model.ExpiryDate.Date <= DateTime.Now.Date)
            {
                throw new Exception("安全拦截：禁止将临期或已过期的药品录入系统！");
            }

            if (model.InPrice < 0)
            {
                throw new Exception("进货单价不能为负数！");
            }

            return dal.AddInbound(model);
        }

        public DataTable GetMedicineList()
        {
            return dal.GetMedicineList();
        }

        public DataTable GetTodayInboundHistory()
        {
            return dal.GetTodayInboundHistory();
        }

        public DataTable GetExpiryAlerts()
        {
            return dal.GetExpiryAlerts();
        }




    }
}
