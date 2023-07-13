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

// ע��appsettings���ö�ȡ��
builder.Services.AddSingleton(new AppSetting(builder.Configuration));
// ע����־����
builder.Services.AddSingleton<LLog, LLog>();
// ע��redis�������
builder.Services.AddSingleton<RedisManager, RedisManager>();
// ע��������ȷ���
builder.Services.AddSingleton<JobProcessor, JobProcessor>();


var app = builder.Build();
// ��ȡ JobProcessor �Դ����䴴��
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
