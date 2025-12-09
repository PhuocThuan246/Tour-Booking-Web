using Microsoft.EntityFrameworkCore;
using NLog.Extensions.Logging;
using TourBookingWeb.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;

namespace TourBookingWeb
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);



            // Cấu hình Identity (dùng IdentityUser với bảng AspNetUsers)
            builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;               // Có số
                options.Password.RequiredLength = 8;                // Ít nhất 8 ký tự
                options.Password.RequireNonAlphanumeric = true;     // Có ký tự đặc biệt
                options.Password.RequireUppercase = true;           // Có chữ hoa
                options.Password.RequireLowercase = true;
                //options.SignIn.RequireConfirmedEmail = true;    //Cái này tạo môi trường giả lập không cần thêm

            })
            .AddEntityFrameworkStores<TourDBContext>()
            .AddDefaultTokenProviders(); // hỗ trợ xác thực OTP, reset password


            // Add services to the container.
            builder.Services.AddControllersWithViews();
            // Connect database
            var connectionString = builder.Configuration.GetConnectionString("DbConnection");
            builder.Services.AddDbContext<TourDBContext>(options => options.UseSqlServer(connectionString));

            // JWT
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var config = builder.Configuration;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
                    RoleClaimType = ClaimTypes.Role // cần thêm dòng này
                };
            });


            builder.Services.AddAuthorization();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromMinutes(20); // hoặc bao nhiêu tùy ý
                options.SlidingExpiration = false;
            });
            builder.Services.AddSession();
            // Cấu hình Admin
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
            });

            // Cấu hình NLog 
            builder.Services.AddLogging(logging =>
            {
                logging.ClearProviders(); // Xoá các provider mặc định (Console, Debug, v.v.)
                logging.SetMinimumLevel(LogLevel.Warning); // Thiết lập mức log tối thiểu
            });

            // Thêm NLog làm logger provider
            builder.Services.AddSingleton<ILoggerProvider, NLogLoggerProvider>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

                string adminEmail = "admin@tourbooking.com";
                string adminPassword = "Admin@123";

                if (await roleManager.FindByNameAsync("admin") == null)
                {
                    await roleManager.CreateAsync(new IdentityRole("admin"));
                }

                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    adminUser = new IdentityUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(adminUser, adminPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "admin");
                    }
                }
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();
            app.UseSession(); // thêm dòng này
            // Đọc JWT từ cookie → header
            app.Use(async (context, next) =>
            {
                var token = context.Request.Cookies["jwt"];
                if (!string.IsNullOrEmpty(token))
                {
                    context.Request.Headers.Authorization = "Bearer " + token;
                }
                await next();
            });
            // Sử dụng xác thực + phân quyền
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(           // Mặc định cho Areas
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
