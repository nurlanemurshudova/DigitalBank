using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Concrete.Dtos.Membership
{
    public class Register
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Address { get; set; }
        public int? Age { get; set; }
        public decimal Balance { get; set; } = 0;
        public string? AvatarUrl { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
