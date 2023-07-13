using JobHub.Cache;
using JobHub.Service;
using JobHub.Tool;
using ServiceStack;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 注册appsettings配置读取类
builder.Services.AddSingleton(new AppSetting(builder.Configuration));
// 注册日志服务
builder.Services.AddSingleton<LLog, LLog>();
// 注册redis管理服务
builder.Services.AddSingleton<RedisManager, RedisManager>();
// 注册任务调度服务
builder.Services.AddSingleton<JobProcessor, JobProcessor>();


var app = builder.Build();
// 获取 JobProcessor 以触发其创建
var jobProcessor = app.Services.GetRequiredService<JobProcessor>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
