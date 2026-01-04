using Core.Entities;
using Entities.Concrete.TableModels.Membership;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Concrete.TableModels
{
    public class Message : BaseEntity
    {
        public int SenderId { get; set; }
        public ApplicationUser Sender { get; set; }

        public int ReceiverId { get; set; }
        public ApplicationUser Receiver { get; set; }

        public string Content { get; set; }
        public bool IsRead { get; set; } = false;
    }
}
