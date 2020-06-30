using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using msngrAPI.Hubs;
using msngrDAL;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.IO;
using Microsoft.AspNetCore.Builder;

namespace msngrAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var server = Configuration["DBServer"] ?? "192.168.99.100";
            var port = Configuration["DBPort"] ?? "1433";
            var user = Configuration["DBUser"] ?? "SA";
            var password = Configuration["DBPassword"] ?? "str0ngPas5w0rd";
            var databaseName = Configuration["DBName"] ?? "MsngrDB";
            string connectionString = 
                $"Server={server},{port}; Initial Catalog={databaseName}; User ID={user}; Password={password}; Connect Timeout=20";

            //Строка подключения для локальной базы данных. На всякий случай пусть остаётся
            // @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=MsngrDB;" +
            // @"Integrated Security=True;Connect Timeout=20;Encrypt=False;" +
            // @"Connection Timeout=10;" +
            // @"TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

            services.AddScoped<IMessagesRepository>(provider => new MessagesRepository(connectionString));
            services.AddCors();
            services.AddSignalR();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            //Добавление и настройка автогенерации Swagger
            services.AddSwaggerGen(s =>
            {
                s.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "msngrAPI",
                    Version = "0.1",
                    Description = "Тестовое задание на позицию \"Стажёр-разработчик C#\" в компанию \"Кошелёк.ру\"",
                    Contact = new OpenApiContact
                    {
                        Name = "Владимир Семенович",
                        Email = "vladimir.vs.w@mail.ru",
                        Url = new Uri("https://github.com/forintregy"),
                    }
                });
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                s.IncludeXmlComments(xmlPath);
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }
          
            app.UseSwagger();
            app.UseSwaggerUI(s =>
            {
                s.SwaggerEndpoint("/swagger/v1/swagger.json", "msngrAPI v0.1");
            });
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseCors(builder => builder
                    .WithOrigins("null")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
            );
            app.UseSignalR(options=>
            {
                options.MapHub<MessagesHub>("/Hubs/Messages");
                
            });
            app.UseHttpsRedirection();
            app.UseMvc();
            //Вызов метода проверки наличия базы данных
            MessagesDBPreparation.PrepareDB(app);
        }
    }
}
