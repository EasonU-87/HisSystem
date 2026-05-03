using DAL;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL
{
    public class PatientBLL
    {
        private PatientDAL dal = new PatientDAL(); 

        public List<Patient> GetPatientList()
        {
            return dal.GetAllPatients();
        }
    }

}
