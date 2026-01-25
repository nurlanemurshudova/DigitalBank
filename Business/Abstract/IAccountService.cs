using Core.Results.Abstract;
using Entities.Concrete.Dtos.Membership;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Abstract
{
    public interface IAccountService
    {
        Task<IResult> RegisterUserAsync(Register model);

        Task<IDataResult<LoginResponse>> UserLoginAsync(Login model);

        Task<IDataResult<LoginResponse>> AdminLoginAsync(Login model);

    }
}
