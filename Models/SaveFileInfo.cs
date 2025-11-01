using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace u22710362_HW03.Models
{
    public class SavedFileInfo
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public DateTime CreatedDate { get; set; }
        public string FilePath { get; set; }
    }
}