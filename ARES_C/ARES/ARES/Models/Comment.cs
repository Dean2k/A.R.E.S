using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARES.Models
{
    public class Comments
    {
        public int id { get; set; }
        public string Created { get; set; }
        public string AvatarId { get; set; }
        public string Comment { get; set; }
    }

    public class RootComment
    {
        public List<Comments> records { get; set; }
    }

}
