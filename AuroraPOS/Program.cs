using AuroraPOS.Data;
using AuroraPOS.Models;
using AuroraPOS.Permissions;
using AuroraPOS.Services;
using AuroraPOS.ModelsCentral;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;
using AuroraPOS;
using Edi.Captcha;
using WebEssentials.AspNetCore.Pwa;
using Microsoft.AspNetCore.SignalR;
using AuroraPOS.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation()
	.AddNewtonsoftJson(options =>
	options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore)
	.AddViewLocalization();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
	{
		options.Cookie.HttpOnly = true;
		options.Cookie.IsEssential = true;
        options.IdleTimeout = TimeSpan.FromDays(1);
		options.Cookie.SameSite = SameSiteMode.None;
    });
builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddDbContext<DbAlfaCentralContext>();

//AUTH Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();


//AUTH JSON WEB TOKEN
string password = AppConfiguration.Get()["Token:Environment"] == "True" ? AppConfiguration.Get()["super_secret_key"] : AppConfiguration.Get()["Token:Password"];

builder.Services.AddAuthentication(x => {
	//x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	//x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options => {

	options.RequireHttpsMetadata = false;
	options.SaveToken = true;
	options.TokenValidationParameters = new TokenValidationParameters
	{
		ValidateIssuer = false,
		ValidateAudience = false,
		ValidateLifetime = true,
		ValidateIssuerSigningKey = true,
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(password))
	};
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<ICentralService, CentralService>();
builder.Services.AddTransient<IPrintService, PrintService>();
builder.Services.AddTransient<UserResolverService>();
builder.Services.AddTransient<ExtendedAppDbContext>();
builder.Services.AddTransient<IUploadService, UploadService>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
/*builder.Services.AddProgressiveWebApp(new PwaOptions
{
	RegisterServiceWorker = true,
	RegisterWebmanifest = false,  // (Manually register in Layout file)
	Strategy = ServiceWorkerStrategy.NetworkFirst,
	OfflineRoute = "Offline.html"
});*/

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(1);
    options.Cookie.HttpOnly = true;
});
builder.Services.AddSessionBasedCaptcha(option =>
{
    option.Letters = "1234567890";
    option.SessionName = "CaptchaCode";
    option.CodeLength = 4;
});

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddScoped<PrinterService>();

const string defaultCulture = "en-GB";
var supportedCultures = new[]
{
    new CultureInfo(defaultCulture),
    new CultureInfo("es-DO")
};


builder.Services.Configure<RequestLocalizationOptions>(options => {
	options.DefaultRequestCulture = new RequestCulture(supportedCultures[1]);
	options.SupportedCultures = supportedCultures;
	options.SupportedUICultures = supportedCultures;
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	//app.UseExceptionHandler("/Home/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}
app.UseSession();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseRouting();
app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);
app.UseAuthorization();

var cookiePolicyOptions = new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Strict,
	
};
app.MapControllers();
app.MapHub<PrintHub>("/printHub");
app.UseCookiePolicy(cookiePolicyOptions);
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapPost("/POS/Logout", async context =>
{
    context.Response.Redirect("/POS/Login");
});
app.Run();
