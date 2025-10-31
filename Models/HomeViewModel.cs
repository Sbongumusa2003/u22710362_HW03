using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace u22710362_HW03.Models
{
    public class HomeViewModel
    {
        public List<staffs> Staffs { get; set; }
        public List<customers> Customers { get; set; }
        public List<products> Products { get; set; }
    }
}