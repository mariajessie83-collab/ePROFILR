
using Server.Data;
using Server.Services;

namespace Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddScoped<Dbconnections>();
            builder.Services.AddScoped<TeacherService>();
            builder.Services.AddScoped<AdminService>();
            builder.Services.AddScoped<IncidentReportService>();
            builder.Services.AddScoped<StudentProfileCaseRecordService>();
            builder.Services.AddScoped<CallSlipReportService>();
            builder.Services.AddScoped<CallSlipService>();
            builder.Services.AddScoped<SMSService>();
            builder.Services.AddScoped<YakapFormService>();
            builder.Services.AddScoped<YakapPdfService>();
            builder.Services.AddScoped<ParentConferencePdfService>();
            builder.Services.AddScoped<StudentCaseRecordPdfService>();
            builder.Services.AddScoped<AnnexBPdfService>();
builder.Services.AddScoped<AnnexAPdfService>();
            builder.Services.AddScoped<EscalationService>();

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.MaxDepth = 64;
                });
            
            // Increase request size limit to handle large base64 images (up to 50MB)
            builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 52428800; // 50MB
            });
            
            builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = 52428800; // 50MB
            });
            
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder
                    .SetIsOriginAllowed(_ => true)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
                });
            });
            var app = builder.Build();

 
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowAll");
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
