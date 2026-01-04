using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Utilities
{
    public static class AccountNumberHelper
    {
        public static string GenerateAccountNumber()
        {
            var random = new Random();
            var timestamp = DateTime.Now.Ticks.ToString().Substring(8, 8); 
            var randomPart = random.Next(10000000, 99999999).ToString(); 

            var accountNumber = "4200" + timestamp + randomPart;

            if (accountNumber.Length > 16)
                accountNumber = accountNumber.Substring(0, 16);

            return accountNumber;
        }

        public static string FormatAccountNumber(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber) || accountNumber.Length != 16)
                return accountNumber;

            return $"{accountNumber.Substring(0, 4)}-{accountNumber.Substring(4, 4)}-{accountNumber.Substring(8, 4)}-{accountNumber.Substring(12, 4)}";
        }
    }
}
