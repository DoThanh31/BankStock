using System.Globalization;
using BankStock.Web;
using BankStock.Web.Controllers.Api;
using BankStock.Web.Data;
using BankStock.Web.Filters;
using BankStock.Web.Localization;
using BankStock.Web.ModelBinding;
using BankStock.Web.Models;
using BankStock.Web.Services;
using BankStock.Web.Validation;
using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Localization services
builder.Services.AddLocalization();

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
    {
        // input type="number" luôn gửi dấu chấm thập phân, độc lập culture vi-VN/en-US.
        options.ModelBinderProviders.Insert(0, new InvariantDecimalModelBinderProvider());

        // Filter toàn cục: vết audit mọi action, bắt exception chưa xử lý (ProblemDetails / JSON), nhớ page size bằng cookie.
        options.Filters.Add<LogActionFilter>();
        options.Filters.Add<GlobalExceptionFilter>();
        options.Filters.Add<PageSizeCookieFilter>();
    })
    .AddJsonOptions(options =>
    {
        // Enum trong JSON (API, AngularJS) dạng chuỗi: "Buy", "Pending"...
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        // JSON sai kiểu → lỗi chung, không lộ tên kiểu .NET nội bộ trong ProblemDetails.
        options.AllowInputFormatterExceptionMessages = false;
    })
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(SharedResource));
    });

// Adapter cho attribute validation tùy biến ([LotSize]) để có thông báo đa ngôn ngữ + validation phía client.
builder.Services.AddSingleton<IValidationAttributeAdapterProvider, AppValidationAttributeAdapterProvider>();

// Thông báo lỗi model binding (sai kiểu số, bỏ trống...) theo ngôn ngữ hiện tại.
builder.Services.AddOptions<Microsoft.AspNetCore.Mvc.MvcOptions>()
    .Configure<IStringLocalizerFactory>((options, factory) =>
    {
        var localizer = factory.Create(typeof(SharedResource));
        var messages = options.ModelBindingMessageProvider;
        messages.SetValueMustBeANumberAccessor(field => localizer["ValNumber", field]);
        messages.SetAttemptedValueIsInvalidAccessor((value, field) => localizer["ValInvalidValue", value, field]);
        messages.SetValueIsInvalidAccessor(value => localizer["ValInvalidValue", value, string.Empty]);
        messages.SetValueMustNotBeNullAccessor(_ => localizer["ValValueRequired"]);
        messages.SetMissingBindRequiredValueAccessor(_ => localizer["ValValueRequired"]);
    });

// Web API: lỗi (400/401/403/404/409/500) trả ProblemDetails (RFC 7807) kèm traceId.
builder.Services.AddProblemDetails();

// CORS chỉ cho các origin trong cấu hình Cors:AllowedOrigins (mặc định rỗng = chặn mọi origin khác).
builder.Services.AddCors(options => options.AddPolicy(ApiDefaults.CorsPolicy, policy =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    policy.WithOrigins(origins)
        .WithMethods(HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete)
        .WithHeaders("Content-Type", "RequestVerificationToken", "X-Requested-With")
        .WithExposedHeaders("Location")
        .AllowCredentials();
}));

// Swagger (OpenAPI) cho /api/v1; chỉ bật ở Development.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(ApiDefaults.Version, new OpenApiInfo
    {
        Title = "BankStock API",
        Version = ApiDefaults.Version,
        Description = "REST API giả lập giao dịch cổ phiếu ngân hàng. Xác thực bằng cookie đăng nhập; " +
                      "request ghi cần header RequestVerificationToken lấy từ GET /api/v1/antiforgery/token."
    });
    // Chỉ đưa controller [ApiController] vào tài liệu (bỏ qua action MVC có attribute route).
    options.DocInclusionPredicate((_, api) =>
        api.ActionDescriptor.EndpointMetadata.OfType<Microsoft.AspNetCore.Mvc.ApiControllerAttribute>().Any());
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml"));
    options.AddSecurityDefinition(ApiSecurityOperationFilter.CookieScheme, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Cookie,
        Name = ".AspNetCore.Identity.Application",
        Description = "Cookie đăng nhập (đăng nhập ở /Account/Login trên cùng trình duyệt)."
    });
    options.AddSecurityDefinition(ApiSecurityOperationFilter.AntiforgeryScheme, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "RequestVerificationToken",
        Description = "Token chống CSRF lấy từ GET /api/v1/antiforgery/token (sau khi đăng nhập)."
    });
    options.OperationFilter<ApiSecurityOperationFilter>();
});

// Session: RecentlyViewedStocks, bộ lọc các trang danh sách, giỏ lệnh nháp (dữ liệu không nhạy cảm).
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = AppCookies.Session;
    options.Cookie.Path = AppCookies.RootPath;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});
builder.Services.AddHttpContextAccessor();

// Scoped business services
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IRecentlyViewedStocksService, RecentlyViewedStocksService>();
builder.Services.AddScoped<IListQueryStateService, ListQueryStateService>();
builder.Services.AddScoped<IEmailSender, LoggingEmailSender>();

// Admin area
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IBankAdminService, BankAdminService>();
builder.Services.AddScoped<IStockAdminService, StockAdminService>();
builder.Services.AddScoped<IPriceHistoryAdminService, PriceHistoryAdminService>();
builder.Services.AddScoped<IFinancialReportAdminService, FinancialReportAdminService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddScoped<IWalletAdminService, WalletAdminService>();
builder.Services.AddScoped<IOrderAdminService, OrderAdminService>();

// Investor area
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IWatchlistService, WatchlistService>();
builder.Services.AddScoped<IOrderDraftService, OrderDraftService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Server=(localdb)\\mssqllocaldb;Database=BankStockDb;Trusted_Connection=True;MultipleActiveResultSets=true"));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password policy
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // Lockout policy
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User policy
    options.User.RequireUniqueEmail = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Admin khóa tài khoản / đổi role thì đổi security stamp; cookie cũ hết hiệu lực trong tối đa 1 phút.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(1);
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    // Mặc định cookie lấy PathBase (/en, /vi) làm Path; cố định "/" để đăng nhập dùng chung mọi tiền tố culture.
    options.Cookie.Path = AppCookies.RootPath;
    // Không "ghi nhớ": cookie phiên trình duyệt, vé hết hạn sau 2 giờ không hoạt động (trượt khi còn dùng).
    options.ExpireTimeSpan = AppCookies.AuthSessionLifetime;
    options.SlidingExpiration = true;

    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";

    // "Ghi nhớ đăng nhập": cookie bền vững với ExpiresUtc = lúc đăng nhập + 14 ngày (vẫn trượt theo SlidingExpiration).
    options.Events.OnSigningIn = context =>
    {
        if (context.Properties.IsPersistent)
        {
            context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.Add(AppCookies.RememberMeLifetime);
        }
        return Task.CompletedTask;
    };

    // API (/api/...) trả 401/403 thay vì chuyển hướng sang trang đăng nhập.
    options.Events.OnRedirectToLogin = context => RedirectOrStatus(context, StatusCodes.Status401Unauthorized);
    options.Events.OnRedirectToAccessDenied = context => RedirectOrStatus(context, StatusCodes.Status403Forbidden);
});

// Cookie antiforgery / TempData cũng mặc định theo PathBase → cố định "/" (xem UrlCultureMiddleware).
builder.Services.AddAntiforgery(options => options.Cookie.Path = AppCookies.RootPath);
builder.Services.Configure<Microsoft.AspNetCore.Mvc.CookieTempDataProviderOptions>(options => options.Cookie.Path = AppCookies.RootPath);

var app = builder.Build();

// Configure the HTTP request pipeline.
// Culture trên URL (/vi/..., /en/...): tách tiền tố thành PathBase trước mọi middleware khác.
app.UseMiddleware<UrlCultureMiddleware>();

// /api trả lỗi dạng ProblemDetails (JSON); trang MVC giữ trang lỗi HTML thân thiện.
if (!app.Environment.IsDevelopment())
{
    app.UseWhen(IsApiRequest, api => api.UseExceptionHandler());
    app.UseWhen(context => !IsApiRequest(context), web => web.UseExceptionHandler("/Home/Error"));
    app.UseHsts();
}

app.UseWhen(IsApiRequest, api => api.UseStatusCodePages());
app.UseWhen(context => !IsApiRequest(context), web => web.UseStatusCodePagesWithReExecute("/Home/StatusCodeError/{0}"));

app.UseHttpsRedirection();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint($"/swagger/{ApiDefaults.Version}/swagger.json", $"BankStock API {ApiDefaults.Version}");
        options.DocumentTitle = "BankStock API";
    });
}

// Request Localization — thứ tự ưu tiên: tiền tố URL (/en/...) → ?culture= → cookie đã chọn → Accept-Language.
var supportedCultures = UrlCulture.SupportedCultures.Select(name => new CultureInfo(name)).ToList();

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(UrlCulture.DefaultCulture),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures,
    RequestCultureProviders =
    [
        new UrlCultureRequestCultureProvider(),
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    ]
};

app.UseRequestLocalization(localizationOptions);

app.UseRouting();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

if (app.Environment.IsDevelopment())
{
    await SeedData.InitializeAsync(app.Services);
}

app.Run();

static Task RedirectOrStatus(Microsoft.AspNetCore.Authentication.RedirectContext<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions> context, int statusCode)
{
    if (IsApiRequest(context.HttpContext))
    {
        // Body ProblemDetails do UseStatusCodePages ghi.
        context.Response.StatusCode = statusCode;
        return Task.CompletedTask;
    }

    context.Response.Redirect(context.RedirectUri);
    return Task.CompletedTask;
}

static bool IsApiRequest(HttpContext context) => context.Request.Path.StartsWithSegments("/api");
