using LagoVista.CloudStorage.Models;
using LagoVista.Core.Models;
using LagoVista.Core.PlatformSupport;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using LagoVista.Core.Validation;
using LagoVista.Core.Exceptions;

namespace LagoVista.Core.CloudStorage.Storage
{
    public abstract class TableStorageBase<TEntity> : IDisposable where TEntity : TableStorageEntity
    {
        private static CloudTable _table;
        private static CloudTableClient _tableClient;

        ILogger _logger;
        String _srvrPath;
        String _accountName;
        String _accountKey;

        public TableStorageBase(String accountName, string accountKey, ILogger logger)
        {
            _logger = logger;
            var credentials = new StorageCredentials(accountName, accountKey);
            var storageAccount = new CloudStorageAccount(credentials, true);
            _tableClient = storageAccount.CreateCloudTableClient();
            _table = _tableClient.GetTableReference(typeof(TEntity).Name);

            _srvrPath = $"https://{accountName}.table.core.windows.net/{typeof(TEntity).Name}";

            _accountKey = accountKey;
            _accountName = accountName;

        }

        private bool Initialized { get; set; }

        public async Task InitAsync()
        {
            lock (this)
            {
                if (Initialized)
                {
                    return;
                }
            }

            await _table.CreateIfNotExistsAsync();
            Initialized = true;
            _logger.Log(PlatformSupport.LogLevel.Warning, GetType().FullName, "Table Created If Need-by");
        }

        protected async Task<TableResult> Execute(TableOperation op)
        {
            var result = await _table.ExecuteAsync(op);

            if (result == null)
            {
                _logger.Log(PlatformSupport.LogLevel.Error, "TokenRepo_Excute", "Null Response Code");
                throw new Exception($"Null response code from table operation");
            }
            else if (result.HttpStatusCode < 200 || result.HttpStatusCode > 299)
            {
                _logger.Log(PlatformSupport.LogLevel.Error, "TokenRepo_Excute", "Non-Success Status Code", new System.Collections.Generic.KeyValuePair<string, string>("StatusCode", result.HttpStatusCode.ToString()));
                throw new Exception($"Error response code from table operation");
            }
            else
            {
                return result;
            }
        }

        private System.Net.Http.HttpClient CreateRequest(String fullResourcePath = "")
        {
            var requestDate = DateTime.UtcNow.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

            var request = new HttpClient();
            request.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            request.DefaultRequestHeaders.Add("DataServiceVersion", "3.0;");
            request.DefaultRequestHeaders.Add("MaxDataServiceVersion", "3.0;NetFx");
            request.DefaultRequestHeaders.Add("x-ms-client-request-id", Guid.NewGuid().ToString());
            request.DefaultRequestHeaders.Add("x-ms-date", requestDate);
            request.DefaultRequestHeaders.Add("x-ms-version", "2014-02-14");

            return request;
        }

        private string GetContentMD5String(string json)
        {
            var jsonBuffer = UTF8Encoding.UTF8.GetBytes(json);

            using (var md5Hasher = MD5.Create())
            {
                var hashedByteBuffer = md5Hasher.ComputeHash(jsonBuffer, 0, jsonBuffer.Length);
                var md5Sting = new StringBuilder(hashedByteBuffer.Length);
                for (var i = 0; i < hashedByteBuffer.Length; i++)
                {
                    md5Sting.Append(hashedByteBuffer[i].ToString("X2"));
                }

                return md5Sting.ToString();
            }
        }

        private byte[] GetContentMD5(string json)
        {
            var jsonBuffer = UTF8Encoding.UTF8.GetBytes(json);

            using (var md5Hasher = MD5.Create())
            {
                return md5Hasher.ComputeHash(jsonBuffer, 0, jsonBuffer.Length);
            }
        }

        private AuthenticationHeaderValue GetAuthHeader(HttpClient request, string method, string contentType = "", byte[] contentMd5 = null, string fullResourcePath = "")
        {
            var resource = $"/{_accountName}/{typeof(TEntity).Name}{fullResourcePath}";
            var contentMd5Str = contentMd5 == null ? String.Empty : System.Convert.ToBase64String(contentMd5);
            var date = request.DefaultRequestHeaders.GetValues("x-ms-date").FirstOrDefault();
            // Verb
            var canonicalizedString = $"{method}\n";
            canonicalizedString += $"{contentMd5Str}\n";
            canonicalizedString += $"{contentType}\n";
            canonicalizedString += $"{date}\n";
            canonicalizedString += resource;

            using (var hasher = new HMACSHA256(Convert.FromBase64String(_accountKey)))
            {
                // Authorization header
                var hmacBuffer = hasher.ComputeHash(System.Text.Encoding.UTF8.GetBytes(canonicalizedString));
                var signature = System.Convert.ToBase64String(hmacBuffer);

                return new AuthenticationHeaderValue("SharedKey", $"{_accountName}:{signature}");
            }
        }

        private async Task<TEntity> Get(String fullResourcePath)
        {
            var operationUri = new Uri($"{_srvrPath}{fullResourcePath}");
            var request = CreateRequest(fullResourcePath);
            request.DefaultRequestHeaders.Authorization = GetAuthHeader(request, "GET", fullResourcePath: fullResourcePath);

            var response = await request.GetAsync(operationUri);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TEntity>(json);
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            throw new Exception($"Non success response from server: {response.RequestMessage}");

        }

        public async Task<TEntity> GetAsync(string partitionKey, string rowKey)
        {
            await InitAsync();

            if (String.IsNullOrEmpty(rowKey) || String.IsNullOrEmpty(partitionKey))
            {
                throw new Exception("Row and Partition Keys must be present to insert or replace an entity.");
            }

            var resource = $"()";
            var query = $"?$filter=PartitionKey eq '{partitionKey}'";
            var operationUri = new Uri($"{_srvrPath}{resource}{query}");


            var fullResourcePath = $"(PartitionKey='{partitionKey}',RowKey='{rowKey}')";

            return await Get(fullResourcePath);
        }

        public async Task<TEntity> GetAsync(string rowKey)
        {
            await InitAsync();

            if (String.IsNullOrEmpty(rowKey))
            {
                throw new Exception("Row Key Must be Present to get Record by Row.");
            }

            return (await GetByFilterAsync(FilterOptions.Create("RowKey", FilterOptions.Operators.Equals, rowKey))).FirstOrDefault();
        }

        public async Task InsertAsync(TEntity entity)
        {
            if (entity is IValidateable)
            {
                var result = Validator.Validate(entity as IValidateable);
                if (!result.IsValid)
                {
                    throw new ValidationException("Invalid Datea.", result.Errors);
                }
            }

            await InitAsync();

            if (String.IsNullOrEmpty(entity.RowKey) || String.IsNullOrEmpty(entity.PartitionKey))
            {
                throw new Exception("Row and Partition Keys must be present to insert or replace an entity.");
            }

            var json = JsonConvert.SerializeObject(entity);

            var request = CreateRequest();
            var jsonContent = new StringContent(json);
            jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            jsonContent.Headers.ContentMD5 = GetContentMD5(json);

            var authHeader = GetAuthHeader(request, "POST", "application/json", contentMd5: jsonContent.Headers.ContentMD5);
            request.DefaultRequestHeaders.Authorization = authHeader;
            var response = await request.PostAsync(_srvrPath, jsonContent);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Non success response from server: {response.RequestMessage}");
            }
        }

        public async Task RemoveAsync(string partitionKey, string rowKey, string etag = "*")
        {
            await InitAsync();

            if (String.IsNullOrEmpty(rowKey) || String.IsNullOrEmpty(partitionKey))
            {
                throw new InvalidOperationException("Row and Partition Keys must be present to insert or replace an entity.");
            }

            var fullResourcePath = $"(PartitionKey='{partitionKey}',RowKey='{rowKey}')";
            var operationUri = new Uri($"{_srvrPath}{fullResourcePath}");
            var request = CreateRequest(fullResourcePath);

            request.DefaultRequestHeaders.Authorization = GetAuthHeader(request, "DELETE", fullResourcePath: fullResourcePath);
            request.DefaultRequestHeaders.Add("If-Match", etag);

            await request.DeleteAsync(operationUri);
        }

        public Task RemoveAsync(TEntity entity)
        {
            return RemoveAsync(entity.PartitionKey, entity.RowKey, entity.ETag);
        }

        public async Task UpdateAsync(TEntity entity)
        {
            if (entity is IValidateable)
            {
                var result = Validator.Validate(entity as IValidateable);
                if (!result.IsValid)
                {
                    throw new ValidationException("Invalid Date.", result.Errors);
                }
            }

            await InitAsync();

            if (String.IsNullOrEmpty(entity.RowKey) || String.IsNullOrEmpty(entity.PartitionKey))
            {
                throw new Exception("Row and Partition Keys must be present to insert or replace an entity.");
            }

            var fullResourcePath = $"(PartitionKey='{entity.PartitionKey}',RowKey='{entity.RowKey}')";
            var operationUri = new Uri($"{_srvrPath}{fullResourcePath}");

            var json = JsonConvert.SerializeObject(entity);

            var request = CreateRequest(fullResourcePath);
            var jsonContent = new StringContent(json);
            jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            jsonContent.Headers.ContentMD5 = GetContentMD5(json);

            request.DefaultRequestHeaders.Authorization = GetAuthHeader(request, "PUT", "application/json", fullResourcePath: fullResourcePath, contentMd5: jsonContent.Headers.ContentMD5);
            request.DefaultRequestHeaders.Add("If-Match", entity.ETag);

            var response = await request.PutAsync(operationUri, jsonContent);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.PreconditionFailed)
                {
                    throw new Exception("ContentModified.");
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }
            }
        }

        public async Task InsertOrReplaceAsync(TEntity entity)
        {
            if (entity is IValidateable)
            {
                var result = Validator.Validate(entity as IValidateable);
                if (!result.IsValid)
                {
                    throw new ValidationException("Invalid Date.", result.Errors);
                }
            }

            await InitAsync();

            if (String.IsNullOrEmpty(entity.RowKey) || String.IsNullOrEmpty(entity.PartitionKey))
            {
                throw new Exception("Row and Partition Keys must be present to insert or replace an entity.");
            }

            var fullResourcePath = $"(PartitionKey='{entity.PartitionKey}',RowKey='{entity.RowKey}')";
            var operationUri = new Uri($"{_srvrPath}{fullResourcePath}");

            var json = JsonConvert.SerializeObject(entity);

            var request = CreateRequest(fullResourcePath);
            var jsonContent = new StringContent(json);
            jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            jsonContent.Headers.ContentMD5 = GetContentMD5(json);
            request.DefaultRequestHeaders.Authorization = GetAuthHeader(request, "PUT", "application/json", fullResourcePath: fullResourcePath, contentMd5: jsonContent.Headers.ContentMD5);
            request.DefaultRequestHeaders.Add("If-Match", entity.ETag);

            var response = await request.PutAsync(operationUri, jsonContent);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.PreconditionFailed)
                {
                    throw new Exception("ContentModified.");
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }
            }

        }

        public async Task<IEnumerable<TEntity>> GetByParitionIdAsync(String partitionKey)
        {
            await InitAsync();

            var resource = $"()";
            var query = $"?$filter=PartitionKey eq '{partitionKey}'";
            var operationUri = new Uri($"{_srvrPath}{resource}{query}");

            var request = CreateRequest(resource);
            request.DefaultRequestHeaders.Authorization = GetAuthHeader(request, "GET", fullResourcePath: resource);

            var json = await request.GetStringAsync(operationUri);
            var resultset = JsonConvert.DeserializeObject<TableStorageResultSet<TEntity>>(json);
            return resultset.ResultSet;
        }

        private String GetFilter(List<FilterOptions> filters)
        {
            //TODO Add support for multiple filters using a fluent interface builder
            if (filters.Count == 0)
            {
                return String.Empty;
            }

            var bldr = new StringBuilder();

            bldr.Append("?$filter=");
            var idx = 0;
            foreach (var filter in filters)
            {
                bldr.Append((idx++ > 0) ? " and " : String.Empty);
                bldr.Append(filter);
                idx++;
            }

            return bldr.ToString();
        }


        public async Task<IEnumerable<TEntity>> GetByFilterAsync(params FilterOptions[] filters)
        {
            await InitAsync();

            var resource = $"()";
            var query = GetFilter(filters.ToList());
            var operationUri = new Uri($"{_srvrPath}{resource}{query}");

            var request = CreateRequest(resource);
            request.DefaultRequestHeaders.Authorization = GetAuthHeader(request, "GET", fullResourcePath: resource);

            var json = await request.GetStringAsync(operationUri);
            var resultset = JsonConvert.DeserializeObject<TableStorageResultSet<TEntity>>(json);
            return resultset.ResultSet;
        }

        public void Dispose()
        {

        }
    }

    public class FilterOptions
    {
        public enum Operators
        {
            Equals,
            LessThan,
            GreaterThan
        }

        public String Field { get; private set; }

        public String Filter { get; private set; }

        public Operators Operator { get; private set; }

        public static FilterOptions Create(string field, Operators op, string filter)
        {
            return new FilterOptions()
            {
                Field = field,
                Operator = op,
                Filter = filter
            };
        }

        public override string ToString()
        {
            String op = String.Empty;

            switch (Operator)
            {
                case Operators.Equals: op = "eq"; break;
                case Operators.GreaterThan: op = "gt"; break;
                case Operators.LessThan: op = "lt"; break;
            }

            return $"({Field} {op} '{Filter}')";
        }

    }
}
