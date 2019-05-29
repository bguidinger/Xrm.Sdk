namespace Microsoft.Pfe.Xrm.Sdk
{
    using Microsoft.Identity.Client;
    using Microsoft.Pfe.Xrm.Sdk.Converters;
    using Microsoft.Pfe.Xrm.Sdk.Services;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Messages;
    using Microsoft.Xrm.Sdk.Metadata;
    using Microsoft.Xrm.Sdk.Query;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security;
    using System.Text;
    using System.Threading.Tasks;

    public class WebApi : IWebApi
    {
        private static HttpClient _client;

        private string _baseUrl { get; set; }
        private Dictionary<string, EntityMetadata> _metadata { get; set; }

        public WebApi(string resource, string username, SecureString password)
        {
            var token = GetToken(resource, username, password).Result;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            _client.DefaultRequestHeaders.Add("Accept", "application/json");
            _client.DefaultRequestHeaders.Add("OData-Version", "4.0");
            _client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
            _client.DefaultRequestHeaders.ExpectContinue = false;

            _baseUrl = $"{resource}/api/data/v8.2";
            _metadata = new Dictionary<string, EntityMetadata>();
        }

        private async Task<string> GetToken(string resource, string username, SecureString password)
        {
            var authority = "https://login.microsoftonline.com/organizations";
            var clientId = "2ad88395-b77d-4561-9441-d0e40824f9bc";

            var app = PublicClientApplicationBuilder.Create(clientId).WithAuthority(authority).Build();

            var result = await app.AcquireTokenByUsernamePassword(new string[] { resource + "/.default" }, username, password).ExecuteAsync();

            return result.AccessToken;
        }

        private async Task<EntityMetadata> GetMetadata(string logicalName)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/EntityDefinitions(LogicalName='{logicalName}')?$select=LogicalName,LogicalCollectionName,PrimaryIdAttribute&$expand=Attributes($select=LogicalName,AttributeType)");
            var response = await _client.SendAsync(request);

            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var reader = new StreamReader(stream))
            using (var json = new JsonTextReader(reader))
            {
                var serializer = new JsonSerializer();
                var metadata = serializer.Deserialize<EntityMetadata>(json);
                return metadata;
            }
        }

        public async Task<OrganizationResponse> Send(OrganizationRequest request)
        {
            var message = await ToHttpRequest(request);
            var result = _client.SendAsync(message).Result;
            return ToOrgResponse(request, result);
        }

        private async Task<EntityMetadata> GetEntityMetadata(string logicalName)
        {
            if (_metadata.ContainsKey(logicalName))
            {
                return _metadata[logicalName];
            }

            var metadata = await GetMetadata(logicalName);
            _metadata[logicalName] = metadata;

            return metadata;
        }

        private async Task<HttpRequestMessage> ToHttpRequest(OrganizationRequest orgRequest)
        {
            switch (orgRequest)
            {
                case ExecuteMultipleRequest request:
                {
                    var message = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/$batch");

                    var batchId = $"batch_{Guid.NewGuid().ToString("N")}";
                    var batch = new MultipartContent("mixed", batchId);

                    foreach (var req in request.Requests)
                    {
                        var changesetId = $"changeset_{Guid.NewGuid().ToString("N")}";
                        var changeset = new MultipartContent("mixed", changesetId);
                        changeset.Headers.Remove("Content-Type");
                        changeset.Headers.Add("Content-Type", $"multipart/mixed;boundary={changesetId}");

                        var change = await ToHttpRequest(req);

                        var content = new HttpMessageContent(change);
                        content.Headers.Remove("Content-Type");
                        content.Headers.Add("Content-Type", $"application/http");
                        content.Headers.Add("Content-Transfer-Encoding", "binary");
                        content.Headers.Add("Content-ID", "1");
                        changeset.Add(content);
                        batch.Add(changeset);
                    }
                    message.Content = batch;
                    return message;
                }

                case CreateRequest request:
                {
                    var target = request.Target;
                    var metadata = await GetEntityMetadata(target.LogicalName);
                    var entitySetName = metadata.LogicalCollectionName;
                    var message = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/{entitySetName}");

                    var body = JsonConvert.SerializeObject(request.Target.Attributes.ToDictionary(x => x.Key, x => x.Value));
                    message.Content = new StringContent(body, Encoding.UTF8, "application/json");
                    return message;
                }

                case RetrieveRequest request:
                {
                    var target = request.Target;
                    var metadata = await GetEntityMetadata(target.LogicalName);
                    var entity = metadata.LogicalCollectionName;
                    var query = new QueryExpression(target.LogicalName)
                    {
                        ColumnSet = request.ColumnSet
                    };

                    var visitor = new ODataQueryExpressionVisitor();
                    visitor.Visit(query);

                    return new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/{entity}({target.Id})?{visitor.QueryString}");
                }
                case RetrieveMultipleRequest request:
                {
                    if (request.Query is QueryExpression query)
                    {
                        var metadata = await GetEntityMetadata(query.EntityName);
                        var entity = metadata.LogicalCollectionName;

                        var visitor = new ODataQueryExpressionVisitor();
                        visitor.Visit(query);

                        return new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/{entity}?{visitor.QueryString}");
                    }
                    else
                    {
                        throw new NotSupportedException("Query type is not supported.");
                    }
                }
                case DeleteRequest request:
                {
                    var target = request.Target;
                    var metadata = await GetEntityMetadata(target.LogicalName);
                    var entity = metadata.LogicalCollectionName;
                    return new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/{entity}({target.Id})");
                }
            }
            return null;
        }

        private OrganizationResponse ToOrgResponse(OrganizationRequest orgRequest, HttpResponseMessage result)
        {
            if (result.StatusCode == HttpStatusCode.InternalServerError)
            {
                var body = result.Content.ReadAsStringAsync().Result;
            }

            var json = result.Content.ReadAsStringAsync().Result;

            switch (orgRequest)
            {
                case RetrieveRequest request:
                {
                    var target = request.Target;
                    var metadata = _metadata[target.LogicalName];
                    var entity = JsonConvert.DeserializeObject<Entity>(json, new EntityConverter(metadata));

                    var response = new RetrieveResponse();
                    response.Results["Entity"] = entity;
                    return response;
                }
                case RetrieveMultipleRequest request:
                {
                    if (request.Query is QueryExpression query)
                    {
                        var metadata = _metadata[query.EntityName];
                        var converters = new JsonConverter[] {
                            new EntityConverter(metadata),
                            new EntityCollectionConverter(metadata)
                        };
                        var collection = JsonConvert.DeserializeObject<EntityCollection>(json, converters);

                        var response = new RetrieveMultipleResponse();
                        response.Results["EntityCollection"] = collection;
                        return response;
                    }
                    else
                    {
                        throw new NotSupportedException("Query type is not supported.");
                    }
                }
                case CreateRequest request:
                {
                    var response = new CreateResponse();
                    response.Results["Id"] = new Guid();
                    return response;
                }
                case DeleteRequest request:
                {
                    return null;
                }
            }
            //if (response.RequestMessage.Method == HttpMethod.Get)
            //{
            //    if (response.RequestMessage.RequestUri.AbsolutePath.Contains("("))
            //    {

            //    }
            //    else
            //    {
            //        var serializer = new JsonSerializer();
            //        using (var stream = response.Content.ReadAsStreamAsync().Result)
            //        using (var sr = new StreamReader(stream))
            //        using (var jsonTextReader = new JsonTextReader(sr))
            //        {
            //            return serializer.Deserialize(jsonTextReader);
            //        }

            //    }
            //}
            //else
            //{
            return null;
            //}
        }
    }
}