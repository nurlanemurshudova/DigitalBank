
using Business.Abstract;
using Business.Concrete;
using DataAccess.Context;
using DataAccess.UnitOfWork;
using DigitalBankApi.Models;
using Entities.Concrete.TableModels.Membership;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace DigitalBankApi
{
    namespace DigitalBankApi
    {
        public class Program
        {
            public static void Main(string[] args)
            {
                var builder = WebApplication.CreateBuilder(args);

                builder.Services.AddControllers();

                builder.Services.AddDbContext<ApplicationDbContext>()
                    .AddIdentity<ApplicationUser, ApplicationRole>()
                    .AddEntityFrameworkStores<ApplicationDbContext>();

                builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
                builder.Services.AddScoped<ITransactionService, TransactionManager>();
                builder.Services.AddScoped<IMessageService, MessageManager>();
                builder.Services.AddScoped<INotificationService, NotificationManager>();
                builder.Services.AddScoped<IStripeService, StripeManager>();
                // Stripe Configuration
                builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
                // ⭐ JWT Authentication
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
                });

  
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowMVC", policy =>
                    {
                        policy.WithOrigins("https://localhost:7220")
                              .AllowAnyMethod()
                              .AllowAnyHeader()
                              .AllowCredentials();
                    });
                });

                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DigitalBank API", Version = "v1" });

                    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        In = ParameterLocation.Header,
                        Description = "Token: Bearer {token}",
                        Name = "Authorization",
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer"
                    });

                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
                });

                var app = builder.Build();

                if (app.Environment.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                    app.UseSwagger();
                    app.UseSwaggerUI(c =>
                    {
                        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DigitalBank API V1");
                    });
                }

                app.UseHttpsRedirection();

                app.UseCors("AllowMVC"); 

                app.UseAuthentication();
                app.UseAuthorization();

                app.MapControllers();

                app.Run();
            }
        }
    }
}
