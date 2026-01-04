using Core.Entities;
using Entities.Concrete.TableModels.Membership;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Concrete.TableModels
{
    public class Notification : BaseEntity
    {
        public int UserId { get; set; }
        public ApplicationUser User { get; set; }

        public string Message { get; set; }
        public bool IsRead { get; set; } = false;
    }
}
