using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Helpers;
using ProjectQLDCCT.Helpers.Middlewares;
using ProjectQLDCCT.Helpers.Services;
using ProjectQLDCCT.Hubs;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("LmStudio", client =>
{
    client.BaseAddress = new Uri("http://localhost:1234/v1/");
    client.Timeout = Timeout.InfiniteTimeSpan;
});
builder.Services.AddTransient<ILmStudioService, LmStudioService>();

builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddScoped<JwtHelper>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ProjectQLDCCT API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Nhập theo format: Bearer {token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
builder.Services.AddDbContext<QLDCContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy
          .WithOrigins("http://localhost:5173")
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();
    });
});

var jwt = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwt["Key"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt["Issuer"],
        ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var cookieToken = context.Request.Cookies["jwt"];
            if (!string.IsNullOrEmpty(cookieToken))
                context.Token = cookieToken;

            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken) &&
                context.HttpContext.Request.Path.StartsWithSegments("/importHub"))
                context.Token = accessToken;

            return Task.CompletedTask;
        },


        OnChallenge = async context =>
        {
            context.HandleResponse();

            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(@"{ 
            ""success"": false,
            ""message"": ""Vui lòng đăng nhập để sử dụng API""
        }");
        },


        OnForbidden = async context =>
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(@"{ 
            ""success"": false,
            ""message"": ""Bạn không có quyền sử dụng API này""
        }");
        }
    };

});


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", p => p.RequireClaim("id_type_users", "5"));
    options.AddPolicy("CTDT", p => p.RequireClaim("id_type_users", "2"));
    options.AddPolicy("DonVi", p => p.RequireClaim("id_type_users", "3"));
    options.AddPolicy("GVDC", p => p.RequireClaim("id_type_users", "4"));
    options.AddPolicy("User", p => p.RequireClaim("id_type_users", "1"));
});



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReactApp");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<JwtSessionMiddleware>();
app.UseAuthentication();
app.UseMiddleware<JwtSessionMiddleware>();
app.UseAuthorization();

app.UseMiddleware<CustomAuthorizationMiddleware>();


app.MapHub<ImportHub>("/importHub");

app.MapControllers();

app.Run();
