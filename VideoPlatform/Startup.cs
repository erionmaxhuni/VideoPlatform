using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Nest;
using System.Text;
using VideoPlatform.Data;
using VideoPlatform.Models;
using VideoPlatform.Services;

namespace VideoPlatform;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
        MyAllowedOrigins = "AllowedOrigins";
    }

    public IConfiguration Configuration { get; }
    public String MyAllowedOrigins { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {

        services.AddControllers();

        services.AddSwaggerGen(setup => {
            var jwtSecurityScheme = new OpenApiSecurityScheme
            {
                BearerFormat = "JWT",
                Name = "JWT Authentication",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                Description = "Put **_ONLY_** your JWT Bearer token on textbox below!",

                Reference = new OpenApiReference
                {
                    Id = JwtBearerDefaults.AuthenticationScheme,
                    Type = ReferenceType.SecurityScheme
                }
            };

            setup.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);

            setup.AddSecurityRequirement(new OpenApiSecurityRequirement {
                    { jwtSecurityScheme, Array.Empty<string>() }
                });

        });


        services.AddDbContext<DbConnection>(options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
        services.AddCors((options) => {
            options.AddPolicy(name: MyAllowedOrigins,
                              policy => {
                                  policy
                                  .WithOrigins(
                                     Configuration.GetValue<String>("OriginsAllowed"))
                                   .AllowAnyHeader()
                                   .AllowAnyMethod();
                              });
            options.AddPolicy(name: "DevOrigins",
                                policy => {
                                    policy.AllowAnyMethod().AllowAnyOrigin().AllowAnyHeader();
                                });
        });
        services.AddScoped<TokenService>();
        services.AddScoped<PasswordService>();

        services.AddScoped<IValidator<UserPost>, UserValidator>();
        services.AddScoped<IValidator<VideoPost>, VideoValidator>();
        services.AddScoped<IValidator<CommentPost>, CommentValidator>();
        services.AddScoped<IValidator<CategoryDto>, CategoryValidator>();

        services.AddAuthentication(options => {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
            .AddJwtBearer(options => {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = "Elefanti-Video",
                    ValidAudience = "Elefanti-Video",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration.GetValue<string>("JWTKey"))) // The same key as the one that generate the token
                };
            });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Elefanti Video"));
        }

   //     app.UseHttpsRedirection();

        app.UseRouting();

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(
                    Path.Combine(env.ContentRootPath, "assets")),
            RequestPath = "/api/assets"
        });

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseCors(MyAllowedOrigins);

        app.UseEndpoints(endpoints => {
            endpoints.MapControllers();
        });
    }
}