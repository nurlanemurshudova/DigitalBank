using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Concrete.Dtos.Membership
{
    public class LoginResponse
    {
        public string Token { get; set; }
        public UserInfo User { get; set; }
        public List<string> Roles { get; set; }
    }
}
