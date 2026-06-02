using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TaskManager.Swagger
{
    public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider _provider;

        public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
        {
            _provider = provider;
        }

        public void Configure(SwaggerGenOptions options)
        {
            foreach (var description in _provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName, new OpenApiInfo
                {
                    Title = "TaskManager API",
                    Version = description.ApiVersion.ToString(),
                    Description = "Task management API",
                    TermsOfService = new Uri("https://example.com/terms"),
                });
            }

            var xmlFilename = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".xml");
            if (File.Exists(xmlFilename))
            {
                options.IncludeXmlComments(xmlFilename);
            }
        }
    }
}
