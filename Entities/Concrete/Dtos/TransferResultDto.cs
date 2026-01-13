using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Concrete.Dtos
{
    public class TransferResultDto
    {
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public decimal SenderNewBalance { get; set; }

        public int ReceiverId { get; set; }
        public string ReceiverName { get; set; }
        public decimal ReceiverNewBalance { get; set; }

        public decimal Amount { get; set; }
    }
}