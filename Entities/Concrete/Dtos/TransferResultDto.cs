using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Concrete.Dtos
{
    public class TransferResultDto
    {
        public int ReceiverId { get; set; }
        public string ReceiverName { get; set; }
        public decimal NewBalance { get; set; }
        public decimal Amount { get; set; }
        public string SenderName { get; set; }
    }
}
