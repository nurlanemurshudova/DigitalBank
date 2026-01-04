using Business.Abstract;
using Business.Concrete;
using DataAccess.Context;
using DataAccess.UnitOfWork;
using DigitalBankUI.Hubs;
using Entities.Concrete.TableModels.Membership;
using Microsoft.AspNetCore.Identity;

namespace DigitalBankUI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDbContext<ApplicationDbContext>()
            .AddIdentity<ApplicationUser, ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            builder.Services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequiredLength = 5;

                options.User.RequireUniqueEmail = true;
            });
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/Login";
                options.ExpireTimeSpan = TimeSpan.FromHours(1);
                options.SlidingExpiration = true;

                options.Events.OnRedirectToLogin = context =>
                {
                    string requestedPath = context.Request.Path.ToString();

                    if (requestedPath.StartsWith("/Dashboard", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.Redirect("/Dashboard/Account/Login?ReturnUrl=" + context.Request.Path);
                        context.Response.StatusCode = 302;
                    }
                    else
                    {
                        context.Response.Redirect(context.RedirectUri);
                        context.Response.StatusCode = 302;
                    }
                    return Task.CompletedTask;
                };
            });

            builder.Services.AddSignalR();
            builder.Services.AddControllersWithViews();

            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>(); 
            builder.Services.AddScoped<ITransactionService, TransactionManager>();
            builder.Services.AddScoped<IMessageService,MessageManager>();


            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
                    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

                    string[] roles = { "Admin", "SubAdmin", "User" };

                    foreach (var role in roles)
                    {
                        if (!await roleManager.RoleExistsAsync(role))
                        {
                            await roleManager.CreateAsync(new ApplicationRole { Name = role });
                        }
                    }

                    var adminEmail = "admin@gmail.com";
                    var adminUser = await userManager.FindByEmailAsync(adminEmail);

                    if (adminUser == null)
                    {
                        var admin = new ApplicationUser
                        {
                            UserName = adminEmail,
                            Email = adminEmail,
                            EmailConfirmed = true,
                            FirstName = "System",
                            LastName = "Admin"
                        };

                        var result = await userManager.CreateAsync(admin, "Admin123*");

                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(admin, "Admin");
                        }
                    }
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "xəta");
                }
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "areas",
                    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
            app.MapHub<ChatHub>("/hubs/chat");
            app.Run();
        }
    }
}
