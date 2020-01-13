using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColumnDesigner
{
    public class SaveOnQuitVM
    {
        public bool Save { get; set; } = false;
        public bool SaveAll { get; set; } = false;
        public bool Cancel { get; set; } = false;
    }
}
