using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;  // Add Swagger namespace
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System;
using System.Net;
using J_Test.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authorization;

namespace J_Test.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserStoryDescriptionController : ControllerBase
    {
        private readonly ILogger<UserStoryDescriptionController> _logger;
        private readonly IConfiguration _configuration;
        private readonly JTestCredentialsContext _context;
        private readonly IDataProtector _jiraTokenProtector;
        private static readonly HttpClient client = new HttpClient();

        private static string EscapeJqlValue(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }

        public UserStoryDescriptionController(
            ILogger<UserStoryDescriptionController> logger,
            IConfiguration configuration,
            JTestCredentialsContext context,
            IDataProtectionProvider dataProtectionProvider)
        {
            _logger = logger;
            _configuration = configuration;
            _context = context;

            _jiraTokenProtector = dataProtectionProvider.CreateProtector(
                "JTest.Jira.ApiToken.v1");
        }

        private bool TryGetAllowedJiraHost(
            string domain,
            out string validatedHost)
        {
            validatedHost = string.Empty;

            if (string.IsNullOrWhiteSpace(domain))
            {
                return false;
            }

            var candidate = domain.Trim();

            // Domain must be a hostname only.
            // Reject schemes, paths, query strings, fragments,
            // user-info and explicit ports.
            if (candidate.Contains("://", StringComparison.OrdinalIgnoreCase) ||
                candidate.Contains('/') ||
                candidate.Contains('\\') ||
                candidate.Contains('?') ||
                candidate.Contains('#') ||
                candidate.Contains('@') ||
                candidate.Contains(':'))
            {
                return false;
            }

            // Reject localhost explicitly.
            if (candidate.Equals(
                "localhost",
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Reject IP literals such as:
            // 127.0.0.1
            // 10.0.0.1
            // 169.254.169.254
            // IPv6 addresses
            if (IPAddress.TryParse(candidate, out _))
            {
                return false;
            }

            // Parse it as an HTTPS hostname.
            if (!Uri.TryCreate(
                    $"https://{candidate}",
                    UriKind.Absolute,
                    out var parsedUri))
            {
                return false;
            }

            var normalizedHost = parsedUri.IdnHost
                .TrimEnd('.')
                .ToLowerInvariant();

            // Read approved Jira hosts from configuration.
            var allowedHosts =
                _configuration
                    .GetSection("Jira:AllowedHosts")
                    .Get<string[]>()
                ?? Array.Empty<string>();

            var isAllowed = allowedHosts.Any(
                host =>
                    !string.IsNullOrWhiteSpace(host) &&
                    normalizedHost.Equals(
                        host.Trim().TrimEnd('.'),
                        StringComparison.OrdinalIgnoreCase));

            if (!isAllowed)
            {
                return false;
            }

            validatedHost = normalizedHost;
            return true;
        }

        // Endpoint to generate test cases from a user story (POST)
        [HttpPost("GenerateTestCases")]
        [SwaggerOperation(Summary = "Generate test cases from a user story", Description = "Generates test cases based on the provided user story.")]
        [SwaggerResponse(200, "Test cases generated successfully", typeof(string))]
        [SwaggerResponse(400, "Bad request, invalid user story format")]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<IActionResult> GenerateTestCases([FromBody] UserStoryRequest request)
        {
            _logger.LogInformation("Received request to generate test cases.");  // Log the test-case generation request.

            if (string.IsNullOrWhiteSpace(request.UserStory))
            {
                _logger.LogWarning("User story is empty.");
                return BadRequest("User story cannot be empty.");
            }

            if (!IsValidUserStory(request.UserStory))
            {
                _logger.LogWarning("Invalid user story format received.");
                return BadRequest($"Invalid user story format: {request.UserStory}");
            }

            // Call the method to generate test cases using the Gemini API
            var response = await GenerateTestCasesFromGemini(request.UserStory);

            if (string.IsNullOrEmpty(response))
            {
                _logger.LogError("Failed to generate test cases.");
                return StatusCode(500, "Failed to generate test cases.");
            }

            return Ok(response); // Return the generated test cases as a response
        }

        // Validate the user story format
        private bool IsValidUserStory(string userStory)
        {
            // A simple check for a valid user story structure
            return userStory.StartsWith("As a", StringComparison.OrdinalIgnoreCase) || userStory.Contains("I want to", StringComparison.OrdinalIgnoreCase) || userStory.Contains("I want an", StringComparison.OrdinalIgnoreCase) || userStory.Contains("so that", StringComparison.OrdinalIgnoreCase) || userStory.Contains("The user", StringComparison.OrdinalIgnoreCase) || userStory.Contains("I want a", StringComparison.OrdinalIgnoreCase);
        }

        // Generate test cases using the Gemini API
        private async Task<string> GenerateTestCasesFromGemini(string userStory)
        {
            try
            {
                var geminiApiKey = _configuration["Gemini:ApiKey"];
                var geminiModel = _configuration["Gemini:Model"];

                if (string.IsNullOrWhiteSpace(geminiApiKey))
                {
                    _logger.LogError("Gemini API key is not configured.");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(geminiModel))
                {
                    _logger.LogError("Gemini model is not configured.");
                    return null;
                }

                string apiUrl =
                    $"https://generativelanguage.googleapis.com/v1beta/models/{geminiModel}:generateContent?key={geminiApiKey}";

                // Create the payload to send to Gemini
                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new
                                {
                                    text =
                                        "Generate functional test cases for the following user story, " +
                                        "including both pass and fail scenarios, and display them in a " +
                                        "table format with columns for test case description, expected " +
                                        $"result, and actual result: {userStory}"
                                }
                            }
                        }
                    }
                };

                var jsonPayload = JsonConvert.SerializeObject(payload);

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var apiResponse = await client.PostAsync(apiUrl, content);

                if (!apiResponse.IsSuccessStatusCode)
                {
                    _logger.LogError($"API request failed with status code: {apiResponse.StatusCode}");
                    return null;
                }

                var responseContent = await apiResponse.Content.ReadAsStringAsync();

                return responseContent; // Return the response (the generated test cases)
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Gemini test-case generation failed. Exception type: {ExceptionType}.",
                    ex.GetType().Name);

                return null;
            }
        }

        [HttpPost("ExportAllUserStories")]
        [SwaggerOperation(Summary = "Export all user stories from Jira", Description = "Fetches Jira issues based on domain, project, and issue type.")]
        [SwaggerResponse(200, "Successfully retrieved Jira issues", typeof(List<IssueDetail>))]
        [SwaggerResponse(400, "Bad request - missing domain, project, or issue type")]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<IActionResult> ExportAllUserStories([FromBody] JiraRequestParams request)
        {
            try
            {
                // Ensure the necessary parameters are provided
                if (request.credentialId <= 0 ||
                    string.IsNullOrWhiteSpace(request.project) ||
                    string.IsNullOrWhiteSpace(request.issueType))
                {
                    return BadRequest(
                        "Please provide credential ID, project, and issue type.");
                }

                var credential = await _context.Details
                    .FirstOrDefaultAsync(d => d.Id == request.credentialId);

                if (credential == null)
                {
                    return BadRequest("Selected Jira credential was not found.");
                }

                if (!TryGetAllowedJiraHost(
                        credential.domain,
                        out var jiraHost))
                {
                    _logger.LogWarning(
                        "Blocked Jira request for credential ID {CredentialId}: " +
                        "destination host is not allowed.",
                        credential.Id);

                    return BadRequest(
                        "The configured Jira domain is not allowed.");
                }

                string jiraApiToken;

                try
                {
                    jiraApiToken =
                        _jiraTokenProtector.Unprotect(
                            credential.apiToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Unable to unprotect Jira API token for credential ID {CredentialId}.",
                        credential.Id);

                    return StatusCode(
                        500,
                        "Unable to use the selected Jira credential.");
                }

                // Use request parameters
                string authValue = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes(
                        $"{credential.username}:{jiraApiToken}"));

                var jql =
                    $"project = \"{EscapeJqlValue(request.project)}\" " +
                    $"AND issuetype = \"{EscapeJqlValue(request.issueType)}\" " +
                    "AND reporter = currentUser() " +
                    "AND summary IS NOT EMPTY";

                var uriBuilder = new UriBuilder
                {
                    Scheme = Uri.UriSchemeHttps,
                    Host = jiraHost,
                    Port = -1,
                    Path = "/rest/api/2/search/jql",
                    Query = $"jql={Uri.EscapeDataString(jql)}" +
                    "&fields=summary,description,assignee"
                };

                var jiraUri = uriBuilder.Uri;

                var requestMessage =
                    new HttpRequestMessage(HttpMethod.Get, jiraUri);
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);

                // Log the Jira request URL only; never log authorization credentials
                _logger.LogInformation(
                    "Sending Jira request for credential ID {CredentialId} " +
                    "to approved Jira host.",
                    credential.Id);

                var apiResponse = await client.SendAsync(requestMessage);

                if (!apiResponse.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Jira API request failed with status code {StatusCode} " +
                        "for credential ID {CredentialId}.",
                        apiResponse.StatusCode,
                        credential.Id);

                    return StatusCode(
                        500,
                        "Failed to fetch Jira issues.");
                }

                var responseContent = await apiResponse.Content.ReadAsStringAsync();
                _logger.LogInformation(
                    "Jira API request completed successfully for credential ID {CredentialId}.",
                    credential.Id);

                var dictProjectIssues = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
                var listAllIssues = new List<IssueDetail>();

                if (dictProjectIssues.ContainsKey("issues"))
                {
                    var issues = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(dictProjectIssues["issues"].ToString());

                    foreach (var issue in issues)
                    {
                        var issueDetail = new IssueDetail();
                        var fields = issue.ContainsKey("fields") ? JsonConvert.DeserializeObject<Dictionary<string, object>>(issue["fields"].ToString()) : null;
                        if (fields != null)
                        {
                            if (fields.ContainsKey("summary"))
                                issueDetail.Summary = fields["summary"].ToString();

                            if (fields.ContainsKey("description"))
                                issueDetail.Description = fields["description"]?.ToString() ?? "No description available";

                            if (fields.ContainsKey("assignee") &&
                                fields["assignee"] != null)
                            {
                                var assignee =
                                    JsonConvert.DeserializeObject<Dictionary<string, object>>(
                                        fields["assignee"].ToString());

                                issueDetail.Reporter =
                                    assignee != null &&
                                    assignee.ContainsKey("displayName")
                                        ? assignee["displayName"].ToString()
                                        : "Unassigned";
                            }
                            else
                            {
                                issueDetail.Reporter = "Unassigned";
                            }
                        }

                        listAllIssues.Add(issueDetail);
                    }
                }

                return Ok(listAllIssues);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while fetching Jira data.");

                return StatusCode(
                    500,
                    "An internal server error occurred.");
            }
        }
    }
}