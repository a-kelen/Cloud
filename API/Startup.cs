using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Persistence;
using Application.Util;
using MediatR;
using Application.Interfaces;
using Infrastructures;
using AutoMapper;
using Swashbuckle.AspNetCore.Swagger;
using FluentValidation.AspNetCore;
using static Application.ComponentCQ.Commands.Create;
using MediatR.Extensions.FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.CookiePolicy;
using API.Common;
using AspNetCore.SpaServices.Extensions.Vue;
using VueCliMiddleware;
using Microsoft.Extensions.FileProviders;
using System.IO;

namespace API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {            
            services.AddCors(opt =>
            {
                opt.AddPolicy("CorsPolicy", policy =>
                {
                    policy.WithOrigins("http://localhost:8080")
                          .AllowCredentials()
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .WithExposedHeaders("WWW-Authenticate")
                          .AllowCredentials();
                });
            });

            services.AddControllers();
            services.TryAddSingleton<ISystemClock, SystemClock>();
            services.AddDbContext<DataContext>(opt =>
            {
                opt.UseSqlServer(
                    //Configuration.GetConnectionString("LocalConnection"),
                    "Data Source=localhost;Initial Catalog=diplom;User Id=sa;Password=diplom_123123Aa;Integrated Security=false",
                    s => {
                        s.MigrationsAssembly("API");
                        s.EnableRetryOnFailure(
                           maxRetryCount: 3,
                           maxRetryDelay: TimeSpan.FromSeconds(30),
                           errorNumbersToAdd: null);
                    });
            });

            var builder = services.AddIdentityCore<User>();
            var identityBuilder = new IdentityBuilder(builder.UserType, builder.Services);
            identityBuilder.AddRoles<Role>();
            identityBuilder.AddEntityFrameworkStores<DataContext>();

            identityBuilder.AddSignInManager<SignInManager<User>>();
            services.AddAuthorizationCore();

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("eiwlksnbafevlasdfbkjasdrdfavsdf"));
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(opt =>
                {
                    opt.RequireHttpsMetadata = false;
                    opt.SaveToken = true;
                    opt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = key,
                        ValidateAudience = false,
                        ValidateIssuer = false,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                    opt.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = ctx =>
                        {
                            var accessToken = ctx.Request.Query["access_token"];
                            var path = ctx.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/chat")))
                                ctx.Token = accessToken;

                            return Task.CompletedTask;
                        }
                    };
                }).AddCookie(options =>
                {
                    options.Cookie.SameSite = SameSiteMode.None;
                    options.Cookie.HttpOnly = false;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
                });



            services.AddSwaggerGen(swagger =>
            {
                swagger.SwaggerDoc("v1", new OpenApiInfo { Title = "Diplom API" });
                swagger.CustomSchemaIds(x => x.FullName);
                swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\"",
                });
                swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                            new string[] {}

                    }
                });
            });

            services.AddOptions();
            var ass1 = AppDomain.CurrentDomain.Load("Application");
            services.AddMediatR(ass1);



            services.AddFluentValidation(new[] { typeof(ValidatorExtensions).Assembly });

            services.AddAutoMapper(typeof(ValidatorExtensions));

            services.AddScoped<iJWTGenerator, JWTGenerator>();
            services.AddScoped<iUserAccessor, UserAccessor>();

            

            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "publish/ClientApp/dist";

            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseCustomExceptionHandler();
            app.UseRouting();
            app.Use(async (context, next) =>
            {
                var token = context.Request.Cookies["token"];
                if (!string.IsNullOrEmpty(token))
                    context.Request.Headers.Add("Authorization", "Bearer " + token);

                await next();
            });
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseCors("CorsPolicy");
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Diplom API");
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
            });
            app.UseStaticFiles();
           
            app.UseSpaStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseSpa(spa =>
            {
                logger.LogInformation($"[[[[[ {spa.Options.SourcePath} ]]]]]");
                logger.LogInformation($"[[[[[ {spa.Options.DefaultPage} ]]]]]");
                logger.LogInformation($"[[[[[ {env.ContentRootPath} ]]]]]");
                logger.LogInformation($"[[[[[ {env.WebRootPath} ]]]]]");
                logger.LogInformation($"[[[[[ {env.ApplicationName} ]]]]]");

                spa.Options.SourcePath = "ClientApp";
                if (env.IsDevelopment())
                {
                    spa.UseVueCli(npmScript: "serve");
                } else
                {
                    //spa.Options.DefaultPageStaticFileOptions = spaStaticFileOptions;
                }
            });


        }
    }
}
