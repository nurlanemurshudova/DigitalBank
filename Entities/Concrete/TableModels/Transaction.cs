using Core.Entities;
using Entities.Concrete.TableModels.Membership;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Concrete.TableModels
{
    public class Transaction : BaseEntity
    {
        public int SenderId { get; set; }
        public ApplicationUser Sender { get; set; }

        public int ReceiverId { get; set; }
        public ApplicationUser Receiver { get; set; }

        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public TransactionStatus Status { get; set; }
    }

    public enum TransactionStatus
    {
        Success = 1,
        Failed = 2
    }
}
