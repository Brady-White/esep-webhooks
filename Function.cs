using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace EsepWebhook
{
    public class Function
    {
        public async Task<APIGatewayProxyResponse> LambdaHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                // Parse the GitHub webhook JSON payload
                var payload = JsonConvert.DeserializeObject<JObject>(request.Body);
                var issueUrl = payload["issue"]?["html_url"]?.ToString();
                var issueTitle = payload["issue"]?["title"]?.ToString();
                var issueBody = payload["issue"]?["body"]?.ToString();

                // Log the extracted issue information
                context.Logger.LogLine($"Issue URL: {issueUrl}");
                context.Logger.LogLine($"Issue Title: {issueTitle}");
                context.Logger.LogLine($"Issue Body: {issueBody}");

                // Post message to Slack
                var slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
                if (!string.IsNullOrEmpty(slackUrl) && !string.IsNullOrEmpty(issueUrl))
                {
                    var slackMessage = new
                    {
                        text = $"New GitHub Issue Created:\n*Title:* {issueTitle}\n*URL:* {issueUrl}\n*Description:* {issueBody}"
                    };

                    var messageContent = JsonConvert.SerializeObject(slackMessage);
                    var requestContent = new StringContent(messageContent, Encoding.UTF8, "application/json");

                    using (var client = new HttpClient())
                    {
                        var response = await client.PostAsync(slackUrl, requestContent);
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            context.Logger.LogLine("Message posted to Slack successfully.");
                        }
                        else
                        {
                            context.Logger.LogLine($"Failed to post message to Slack. Status code: {response.StatusCode}");
                        }
                    }
                }
                else
                {
                    context.Logger.LogLine("SLACK_URL environment variable is not set or issue URL is missing.");
                }

                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Body = "Lambda executed successfully"
                };
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"An error occurred: {ex.Message}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Body = $"Error: {ex.Message}"
                };
            }
        }
    }
}
