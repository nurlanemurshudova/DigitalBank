using Business.Utilities;
using Entities.Concrete.Dtos.Membership;
using Entities.Concrete.TableModels.Membership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace DigitalBankUI.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }


        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Login(Login model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ViewBag.Error = "Email və ya şifrə yanlışdır"; 
                return View(model);
            }




            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin") || roles.Contains("SubAdmin"))
            {
                ViewBag.Error = "Bu hesabla giriş edilə bilməz";
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                isPersistent: false,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }


            ViewBag.Error = "Email və ya şifrə yanlışdır"; 
            return View(model);
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(Register model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Bu email artıq qeydiyyatdan keçib.");
                return View(model);
            }

            var passwordValidator = HttpContext.RequestServices.GetRequiredService<IPasswordValidator<ApplicationUser>>();
            var fakeUser = new ApplicationUser { UserName = model.Email, Email = model.Email }; 

            var passwordCheck = await passwordValidator.ValidateAsync(_userManager, fakeUser, model.Password);

            if (!passwordCheck.Succeeded)
            {
                foreach (var error in passwordCheck.Errors)
                {
                    ModelState.AddModelError("Password", error.Description);
                }
                return View(model);
            }

            string accountNumber;
            do
            {
                accountNumber = AccountNumberHelper.GenerateAccountNumber();
            }
            while (await _userManager.Users.AnyAsync(u => u.AccountNumber == accountNumber));

            var user = new ApplicationUser
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Address = model.Address,
                Age = model.Age,
                Balance = 0,
                Email = model.Email,
                UserName = model.Email,
                AccountNumber = accountNumber,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                TempData["Success"] = "Qeydiyyat uğurla tamamlandı.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
    }
}
