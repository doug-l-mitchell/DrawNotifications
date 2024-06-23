using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace RGO
{
    public class GetVersion
    {
        private readonly ILogger<GetVersion> _logger;

        public GetVersion(ILogger<GetVersion> logger)
        {
            _logger = logger;
        }

        [Function("GetVersion")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("1.0.0");
        }
    }
}
