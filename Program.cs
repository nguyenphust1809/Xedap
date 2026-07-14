using Xedap;
using Xedap.Areas.Admin.Repository;
using Xedap.Hubs; 
using Xedap.Models;
using Xedap.Models.Momo;
using Xedap.Repository;
using Xedap.Services;
using Xedap.Services.Embedding;
using Xedap.Services.Llm;
using Xedap.Services.Momo; // cho UseNetTopologySuite()
using Xedap.Services.Tools;
using Xedap.Services.Vector;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using OpenAI.Chat;
using OpenAI.Embeddings;
using Xedap.Interfaces;
using Xedap.Services;
var builder = WebApplication.CreateBuilder(args);

// ================== DATABASE ==================
builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("PostgresConnection"),
        o => o.UseNetTopologySuite() // ✅ Bắt buộc để map cột "location" (PostGIS Point)
    ));
builder.Services.AddScoped<IProductService, ProductService>();

// ================== EMAIL SERVICE ==================
builder.Services.AddSingleton<IEmailSender, EmailSender>();

// ================== CONFIG ==================
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}
var configuration = builder.Configuration.Get<AppConfig>() ?? new AppConfig();
builder.Services.AddSingleton(configuration);
builder.Services.AddHttpClient();

// ================== OPENAI / LLM ==================
if (configuration.Provider == "OpenAI")
{
    builder.Services.AddSingleton<ILlmChatProvider, OpenAIChatProvider>();
    builder.Services.AddSingleton<IEmbeddingProvider, OpenAIEmbeddingProvider>();

    builder.Services.AddSingleton(new ChatClient(configuration.OpenAI.ChatModel, configuration.OpenAI.ApiKey));
    builder.Services.AddSingleton(new EmbeddingClient(configuration.OpenAI.EmbedModel, configuration.OpenAI.ApiKey));
}
else
{
    builder.Services.AddSingleton<ILlmChatProvider, OllamaChatProvider>();
    builder.Services.AddSingleton<IEmbeddingProvider, OllamaEmbeddingProvider>();
}

// ================== RAG PIPELINE ==================
builder.Services.AddSingleton<IQdrantClient, QdrantHttpClient>();
builder.Services.AddSingleton<RagPipeline>();
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 1024L * 1024L * 200L; //200MB
});
builder.Services.AddSingleton<SearchDocsTool>(); // tool service

// ================== MVC + SWAGGER ==================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "RAG API",
        Version = "v1"
    });
});

builder.Services.AddSignalR(); // 👈 Thêm dòng này để đăng ký SignalR

builder.Services.AddControllersWithViews();

// ================== SESSION + IDENTITY ==================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.IsEssential = true;
});
//connect momo api
builder.Services.Configure<MomoOptionModel>(builder.Configuration.GetSection("MomoAPI"));
builder.Services.AddScoped<IMomoService, MomoService>();
builder.Services.AddIdentity<AppUserModel, IdentityRole>()
    .AddEntityFrameworkStores<DataContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 4;
    options.User.RequireUniqueEmail = true;
});

var app = builder.Build();

// ================== MIDDLEWARE ==================
app.UseStatusCodePagesWithRedirects("/Home/Error?statuscode={0}");
app.UseSession();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ================== SWAGGER ==================
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "RAG API v1");
    c.RoutePrefix = "swagger";
});

// ================== ROUTES ==================
app.MapControllerRoute(
    name: "predict",
    pattern: "Predict/{action=Index}/{id?}",
    defaults: new { controller = "Predict", action = "Index" });
// Areas
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Product}/{action=Index}/{id?}");

// Category
app.MapControllerRoute(
    name: "category",
    pattern: "category/{Slug?}",
    defaults: new { controller = "Category", action = "Index" });

// Brand
app.MapControllerRoute(
    name: "brand",
    pattern: "Brand/{Slug?}",
    defaults: new { controller = "Brand", action = "Index" });

// Default
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 👇 Đăng ký ChatHub tại đây
app.MapHub<ChatHub>("/chathub");

// ================== SEED DATABASE ==================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DataContext>();
    SeedData.SeedingData(context);
}

// ================== RUN ==================
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUserModel>>();

    var user = await userManager.FindByNameAsync("phuadmin");

    if (user != null)
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, "Admin@123");
        Console.WriteLine($"Reset password: {result.Succeeded}");

        foreach (var error in result.Errors)
        {
            Console.WriteLine(error.Description);
        }
            }
}
app.Run();
