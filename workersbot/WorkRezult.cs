using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kpworkersbot
{
    internal class WorkRezult
    {
        public string ID;
        public string? name;
        
        public DateTime tBegin;
        public string? project;
        public DateTime tEnd;
        public float timeOfWork;
        public int pricePerHour;
        public decimal salary;

        public WorkRezult(string ID,string name, UserInfo beginAndProject, int pricePerHour)
        {
            this.ID = ID;
            this.name = name ?? "Нет имени";
            this.tBegin = beginAndProject.tBegin;
            this.project = beginAndProject.project?? "Проект не назначен";
            this.tEnd = DateTime.Now;
            this.timeOfWork=Convert.ToSingle((DateTime.Now-beginAndProject.tBegin).TotalHours);
            this.pricePerHour = pricePerHour;
            this.salary = pricePerHour * Convert.ToDecimal((DateTime.Now - beginAndProject.tBegin).TotalHours);
        }

    }
}
