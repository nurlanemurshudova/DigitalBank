using Business.Abstract;
using Business.Concrete;
using DataAccess.Context;
using DataAccess.UnitOfWork;
using DigitalBankUI.Hubs;
using Entities.Concrete.Dtos;
using Entities.Concrete.TableModels.Membership;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace DigitalBankUI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<ApplicationDbContext>(options =>

            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



            builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()

            .AddEntityFrameworkStores<ApplicationDbContext>()

            .AddDefaultTokenProviders();
            //builder.Services.AddDbContext<ApplicationDbContext>()
            //    .AddIdentity<ApplicationUser, ApplicationRole>()
            //    .AddEntityFrameworkStores<ApplicationDbContext>()
            //    .AddDefaultTokenProviders();

            builder.Services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequiredLength = 5;
                options.User.RequireUniqueEmail = true;
            });

            // JWT Authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
                    ),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (!string.IsNullOrEmpty(context.Token))
                        {
                            return Task.CompletedTask;
                        }

                        var path = context.HttpContext.Request.Path;

                        if (path.StartsWithSegments("/Dashboard"))
                        {
                            var adminToken = context.Request.Cookies["AdminAuthToken"];
                            if (!string.IsNullOrEmpty(adminToken))
                            {
                                context.Token = adminToken;
                            }
                        }
                        else
                        {
                            var userToken = context.Request.Cookies["AuthToken"];
                            if (!string.IsNullOrEmpty(userToken))
                            {
                                context.Token = userToken;
                            }
                        }

                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();

                        if (context.Request.Path.StartsWithSegments("/Home/PaymentSuccess") ||
                            context.Request.Path.StartsWithSegments("/Home/PaymentCancel"))
                        {
                            return Task.CompletedTask;
                        }

                        if (context.Request.Path.StartsWithSegments("/Dashboard"))
                        {
                            context.Response.Redirect("/Dashboard/Account/Login");
                        }
                        else
                        {
                            context.Response.Redirect("/Account/Login");
                        }

                        return Task.CompletedTask;
                    }
                };
            });

            builder.Services.AddHttpClient();
            builder.Services.AddSignalR();
            builder.Services.AddControllersWithViews();

            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<ITransactionService, TransactionManager>();
            builder.Services.AddScoped<IMessageService, MessageManager>();
            builder.Services.AddScoped<INotificationService, NotificationManager>();
            builder.Services.AddScoped<IUserProfileService, UserProfileManager>();
            builder.Services.AddScoped<IAccountService, AccountManager>();
            builder.Services.AddScoped<IUserProfileService, UserProfileManager>();



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
                    logger.LogError(ex, "Xəta baş verdi");
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
            app.MapHub<NotificationHub>("/hubs/notification");
            app.Run();
        }
    }
}