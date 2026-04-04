using DAL;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL
{
    public class MedicineBLL
    {
        private MedicineDAL dal = new MedicineDAL();

        public List<Medicine> GetAllMedicines()
        {
            return dal.GetAllMedicines();
        }
    }
}
