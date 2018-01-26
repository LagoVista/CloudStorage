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
using LagoVista.Core;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.CloudStorage.Exceptions;
using LagoVista.Core.Models.UIMetaData;
using System.Diagnostics;

namespace LagoVista.CloudStorage.Storage
{

    public abstract class TableStorageBase<TEntity> : IDisposable where TEntity : TableStorageEntity
    {
        CloudTable _table;
        CloudTableClient _tableClient;

        IAdminLogger _logger;
        String _srvrPath;
        String _accountName;
        String _accountKey;

        public TableStorageBase(String accountName, string accountKey, IAdminLogger logger)
        {
            _logger = logger;
            var credentials = new StorageCredentials(accountName, accountKey);
            var storageAccount = new CloudStorageAccount(credentials, true);
            _tableClient = storageAccount.CreateCloudTableClient();
            _table = _tableClient.GetTableReference(GetTableName());

            _srvrPath = $"https://{accountName}.table.core.windows.net/{GetTableName()}";

            _accountKey = accountKey;
            _accountName = accountName;
        }

        public TableStorageBase(IAdminLogger adminLogger)
        {
            _logger = adminLogger;

        }

        public void SetConnection(String accountName, string accountKey)
        {
            _accountKey = accountKey;
            if (String.IsNullOrEmpty(_accountKey))
            {
                var ex = new InvalidOperationException($"Invalid or missing account key information on {GetType().Name}");
                _logger.AddException($"{GetType().Name}_SetConnection", ex);
                throw ex;
            }

            _accountName = accountName;
            if (String.IsNullOrEmpty(_accountName))
            {
                var ex = new InvalidOperationException($"Invalid or missing account name information on {GetType().Name}");
                _logger.AddException($"{GetType().Name}_SetConnection", ex);
                throw ex;
            }

            var credentials = new StorageCredentials(_accountName, _accountKey);
            var storageAccount = new CloudStorageAccount(credentials, true);
            _tableClient = storageAccount.CreateCloudTableClient();
            _table = _tableClient.GetTableReference(GetTableName());

            _srvrPath = $"https://{_accountName}.table.core.windows.net/{GetTableName()}";
        }

        protected virtual string GetTableName()
        {
            return typeof(TEntity).Name;
        }

        private bool Initialized { get; set; }

        public virtual async Task InitAsync()
        {
            if (_table == null)
            {
                var ex = new InvalidOperationException($"_table Instance not created on {GetType().Name}, either set connection parameters in constructor or with SetConnection");
                _logger.AddException($"{GetType().Name}_InitAsync", ex);
                throw ex;
            }

            if (_tableClient == null)
            {
                var ex = new InvalidOperationException($"_tableClient Instance not created on {GetType().Name}, either set connection parameters in constructor or with SetConnection");
                _logger.AddException($"{GetType().Name}_InitAsync", ex);
                throw ex;
            }

            lock (this)
            {
                if (Initialized)
                {
                    return;
                }
            }

            await _table.CreateIfNotExistsAsync();
            Initialized = true;
        }

        protected virtual async Task<TableResult> Execute(TableOperation op)
        {
            var result = await _table.ExecuteAsync(op);

            if (result == null)
            {
                _logger.AddCustomEvent(Core.PlatformSupport.LogLevel.Error, "TableStorageBase_Execute", "Null Response Code");
                throw new Exception($"Null response code from table operation");
            }
            else if (result.HttpStatusCode < 200 || result.HttpStatusCode > 299)
            {
                _logger.AddCustomEvent(Core.PlatformSupport.LogLevel.Error, "TableStorageBase_Execute", "Non-Success Status Code", new System.Collections.Generic.KeyValuePair<string, string>("StatusCode", result.HttpStatusCode.ToString()));
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
            var resource = $"/{_accountName}/{GetTableName()}{fullResourcePath}";
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
            using (var request = CreateRequest(fullResourcePath))
            {
                request.DefaultRequestHeaders.Authorization = GetAuthHeader(request, "GET", fullResourcePath: fullResourcePath);

                using (var response = await request.GetAsync(operationUri))
                {
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
            }
        }


        public async Task<String> GetRAWJSONAsync(String rowKey, string partitionKey)
        {
            await InitAsync();

            if (String.IsNullOrEmpty(rowKey))
            {
                _logger.AddError("TableStorageBase_GetAsync", "emptyRowKey", new KeyValuePair<string, string>("tableName", GetTableName()));
                throw new Exception("Row and Partition Keys must be present to insert or replace an entity.");
            }

            if (String.IsNullOrEmpty(partitionKey))
            {
                _logger.AddError("TableStorageBase_GetAsync", "emptyPartitionKey", new KeyValuePair<string, string>("tableName", GetTableName()));
                throw new Exception("Row and Partition Keys must be present to insert or replace an entity.");
            }


            var fullResourcePath = $"(PartitionKey='{partitionKey}',RowKey='{rowKey}')";
            var operationUri = new Uri($"{_srvrPath}{fullResourcePath}");

            using (var request = CreateRequest(fullResourcePath))
            {
                request.DefaultRequestHeaders.Authorization = GetAuthHeader(request, "GET", fullResourcePath: fullResourcePath);

                using (var response = await request.GetAsync(operationUri))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }

                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new RecordNotFoundException(GetTableName(), rowKey);
                    }

                    _logger.AddError("TableStorageBase_GetRawJSONAsync", "failureResponseCode",
                        new KeyValuePair<string, string>("tableName", GetTableName()),
                        new KeyValuePair<string, string>("reasonPhrase", response.ReasonPhrase),
                        new KeyValuePair<string, string>("rowKey", rowKey),
                        new KeyValuePair<string, string>("partitionKey", partitionKey));

                    throw new Exception($"Non success response from server: {response.RequestMessage}");
                }
            }
        }

        public async Task<TEntity> GetAsync(string partitionKey, string rowKey, bool throwOnNotFound = true)
        {
            await InitAsync();

            if (String.IsNullOrEmpty(rowKey))
            {
                _logger.AddError("TableStorageBase_GetAsync", "emptyRowKey", new KeyValuePair<string, string>("tableName", GetTableName()));
                throw new Exception("Row and Partition Keys must be present to insert or replace an entity.");
            }

            if (String.IsNullOrEmpty(partitionKey))
            {
                _logger.AddError("TableStorageBase_GetAsync", "emptyPartitionKey", new KeyValuePair<string, string>("tableName", GetTableName()));
                throw new Exception("Row and Partition Keys must be present to insert or replace an entity.");
            }


            var fullResourcePath = $"(PartitionKey='{partitionKey}',RowKey='{rowKey}')";

            var record = await Get(fullResourcePath);
            if (record == null)
            {
                if (throwOnNotFound)
                {
                    throw new RecordNotFoundException(GetTableName(), rowKey);
                }
                else
                {
                    return null;
                }
            }

            return record;
        }

        public async Task<TEntity> GetAsync(string rowKey, bool throwOnNotFound = true)
        {
            await InitAsync();

            if (String.IsNullOrEmpty(rowKey))
            {
                _logger.AddError("TableStorageBase", "recordNotFound", new KeyValuePair<string, string>("tableName", GetTableName()), new KeyValuePair<string, string>("rowKey", rowKey));
                throw new Exception("Row Key Must be Present to get Record by Row.");
            }

            var record = (await GetByFilterAsync(FilterOptions.Create("RowKey", FilterOptions.Operators.Equals, rowKey))).FirstOrDefault();

            if (record == null)
            {
                if (throwOnNotFound)
                {
                    throw new RecordNotFoundException(GetTableName(), rowKey);
                }
                else
                {
                    return null;
                }
            }

            return record;
        }

        public async Task InsertAsync(TEntity entity)
        {
            if (entity == null)
            {
                _logger.AddError("TableStorageBase_InsertAsync(entity)", "NULL value provided.", GetTableName().ToKVP("tableName"));
                throw new Exception("Null Value Provided for InsertAsync(entity).");
            }

            if (entity is IValidateable)
            {
                var result = Validator.Validate(entity as IValidateable);
                if (!result.Successful)
                {
                    throw new ValidationException("Invalid Datea.", result.Errors);
                }
            }

            await InitAsync();

            if (String.IsNullOrEmpty(entity.RowKey))
            {
                _logger.AddError("TableStorageBase_InsertAsync(entity)", "emptyRowKey", GetTableName().ToKVP("tableName"));
                throw new Exception("Row and Partition Keys must be present to insert or replace an entity.");
            }

            if (String.IsNullOrEmpty(entity.PartitionKey))
            {
                _logger.AddError("TableStorageBase_InsertAsync(entity)", "emptyPartitionKey", GetTableName().ToKVP("tableName"));
                throw new Exception("Row and Partition Keys must be present to insert or replace an entity.");
            }

            var json = JsonConvert.SerializeObject(entity);

            var jsonContent = new StringContent(json);
            jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            jsonContent.Headers.ContentMD5 = GetContentMD5(json);

            using (var request = CreateRequest())
            {
                var authHeader = GetAuthHeader(request, "POST", "application/json", contentMd5: jsonContent.Headers.ContentMD5);
                request.DefaultRequestHeaders.Authorization = authHeader;

                using (var response = await request.PostAsync(_srvrPath, jsonContent))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(response.ReasonPhrase);
                        Console.ResetColor();

                        _logger.AddError("TableStorageBase_InsertAsync(entity)", "failureResponseCode", GetTableName().ToKVP("tableName"), response.ReasonPhrase.ToKVP("reasonPhrase"));

                        throw new Exception($"Non success response from server: {response.ReasonPhrase}");
                    }
                }
            }
        }

        public async Task InsertAsync(string json)
        {
            using (var request = CreateRequest())
            {
                var jsonContent = new StringContent(json);
                jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                jsonContent.Headers.ContentMD5 = GetContentMD5(json);

                var authHeader = GetAuthHeader(request, "POST", "application/json", contentMd5: jsonContent.Headers.ContentMD5);
                request.DefaultRequestHeaders.Authorization = authHeader;
                using (var response = await request.PostAsync(_srvrPath, jsonContent))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.AddError("TableStorageBase_InsertAsync(json)", "failureResponseCode", GetTableName().ToKVP("tableName"), response.ReasonPhrase.ToKVP("reasonPhrase"));
                        throw new Exception($"Non success response from server: {response.ReasonPhrase}");
                    }
                }
            }
        }

        public async Task UpdateAsync(string partitionKey, string rowKey, string json, string etag)
        {
            var fullResourcePath = $"(PartitionKey='{partitionKey}',RowKey='{rowKey}')";
            var operationUri = new Uri($"{_srvrPath}{fullResourcePath}");


            using (var request = CreateRequest(fullResourcePath))
            {
                var jsonContent = new StringContent(json);
                jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                jsonContent.Headers.ContentMD5 = GetContentMD5(json);

                request.DefaultRequestHeaders.Authorization = GetAuthHeader(request, "PUT", "application/json", fullResourcePath: fullResourcePath, contentMd5: jsonContent.Headers.ContentMD5);
                request.DefaultRequestHeaders.Add("If-Match", etag);

                using (var response = await request.PutAsync(operationUri, jsonContent))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == HttpStatusCode.PreconditionFailed)
                        {
                            _logger.AddError("TableStorageBase_UpdateAsync(json)", "contentModified",
                              new KeyValuePair<string, string>("tableName", GetTableName()),
                              new KeyValuePair<string, string>("rowKey", rowKey),
                              new KeyValuePair<string, string>("partitionKey", partitionKey));

                            throw new ContentModifiedException();
                        }
                        else
                        {
                            _logger.AddError("TableStorageBase_UpdateAsync(json)", "failureResponseCode", new KeyValuePair<string, string>("tableName", GetTableName()), new KeyValuePair<string, string>("reasonPhrase", response.ReasonPhrase));
                            throw new Exception(response.ReasonPhrase);
                        }
                    }
                }
            }
        }


        public async Task RemoveAsync(string partitionKey, string rowKey, string etag = "*")
        {
            await InitAsync();

            if (String.IsNullOrEmpty(rowKey))
            {
                _logger.AddError("TableStorageBase_RemoveAsync", "emptyRowKey", new KeyValuePair<string, string>("tableName", GetTableName()));
                throw new Exception("Row and Partition Keys must be present to insert or replace an entity.");
            }

            if (String.IsNullOrEmpty(partitionKey))
            {
                _logger.AddError("TableStorageBase_RemoveAsync", "emptyPartitionKey", new KeyValuePair<string, string>("tableName", GetTableName()));
                throw new Exception("Row and Partition Keys must be present to insert or replace an entity.");
            }

            var fullResourcePath = $"(PartitionKey='{partitionKey}',RowKey='{rowKey}')";
            var operationUri = new Uri($"{_srvrPath}{fullResourcePath}");
            using (var request = CreateRequest(fullResourcePath))
            {

                request.DefaultRequestHeaders.Authorization = GetAuthHeader(request, "DELETE", fullResourcePath: fullResourcePath);
                request.DefaultRequestHeaders.Add("If-Match", etag);

                using (var response = await request.DeleteAsync(operationUri))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        return;
                    }

                    if (response.StatusCode == HttpStatusCode.PreconditionFailed)
                    {
                        _logger.AddError("TableStorageBase_RemoveAsync", "contentModified",
                          new KeyValuePair<string, string>("tableName", GetTableName()),
                          new KeyValuePair<string, string>("rowKey", rowKey),
                          new KeyValuePair<string, string>("partitionKey", partitionKey));

                        throw new ContentModifiedException();
                    }

                    _logger.AddError("TableStorageBase_RemoveAsync", "failureResponseCode",
                        new KeyValuePair<string, string>("tableName", GetTableName()),
                        new KeyValuePair<string, string>("reasonPhrase", response.ReasonPhrase),
                        new KeyValuePair<string, string>("rowKey", rowKey),
                        new KeyValuePair<string, string>("partitionKey", partitionKey));


                    throw new Exception($"Non success response from server: {response.ReasonPhrase}");
                }
            }
        }

        public Task RemoveAsync(TEntity entity, string etag = "*")
        {
            return RemoveAsync(entity.PartitionKey, entity.RowKey, String.IsNullOrEmpty(etag) ? entity.ETag : etag);
        }

        public async Task UpdateAsync(TEntity entity, string etag = "*")
        {
            if (entity is IValidateable)
            {
                var result = Validator.Validate(entity as IValidateable);
                if (!result.Successful)
                {
                    throw new ValidationException("Invalid Date.", result.Errors);
                }
            }

            await InitAsync();

            if (String.IsNullOrEmpty(entity.RowKey))
            {
                _logger.AddError("TableStorageBase_UdpateAsync", "emptyRowKey", new KeyValuePair<string, string>("tableName", GetTableName()));
                throw new Exception("Row and Partition Keys must be present to insert or replace an entity.");
            }

            if (String.IsNullOrEmpty(entity.PartitionKey))
            {
                _logger.AddError("TableStorageBase_UdpateAsync", "emptyPartitionKey", new KeyValuePair<string, string>("tableName", GetTableName()));
                throw new Exception("Row and Partition Keys must be present to insert or replace an entity.");
            }


            var fullResourcePath = $"(PartitionKey='{entity.PartitionKey}',RowKey='{entity.RowKey}')";
            var operationUri = new Uri($"{_srvrPath}{fullResourcePath}");

            var json = JsonConvert.SerializeObject(entity);

            using (var request = CreateRequest(fullResourcePath))
            {
                var jsonContent = new StringContent(json);
                jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                jsonContent.Headers.ContentMD5 = GetContentMD5(json);

                request.DefaultRequestHeaders.Authorization = GetAuthHeader(request, "PUT", "application/json", fullResourcePath: fullResourcePath, contentMd5: jsonContent.Headers.ContentMD5);
                request.DefaultRequestHeaders.Add("If-Match", string.IsNullOrEmpty(etag) ? entity.ETag : etag);

                using (var response = await request.PutAsync(operationUri, jsonContent))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == HttpStatusCode.PreconditionFailed)
                        {
                            _logger.AddError("TableStorageBase_UpdateAsync(entity)", "contentModified",
                                new KeyValuePair<string, string>("tableName", GetTableName()),
                                new KeyValuePair<string, string>("rowKey", entity.RowKey),
                                new KeyValuePair<string, string>("partitionKey", entity.PartitionKey));

                            throw new ContentModifiedException();
                        }
                        else
                        {
                            _logger.AddError("TableStorageBase_UpdateAsync(entity)", "failureResponseCode",
                                new KeyValuePair<string, string>("tableName", GetTableName()),
                                new KeyValuePair<string, string>("reasonPhrase", response.ReasonPhrase),
                                new KeyValuePair<string, string>("rowKey", entity.RowKey),
                                new KeyValuePair<string, string>("partitionKey", entity.PartitionKey));

                            throw new Exception($"Non success response from server: {response.RequestMessage}");

                        }
                    }
                }
            }
        }

        public async Task<ListResponse<TEntity>> GetPagedResultsAsync(string partitionKey, ListRequest listRequest)
        {
            await InitAsync();

            if (String.IsNullOrEmpty(partitionKey))
            {
                _logger.AddError("TableStorageBase_GetPagedResults", "emptyPartitionKey", new KeyValuePair<string, string>("tableName", GetTableName()));
                throw new Exception("Row and Partition Keys must be present to insert or replace an entity.");
            }

            var resource = $"()";

            var query = $"?$filter=PartitionKey eq '{partitionKey}'";

            if (!String.IsNullOrEmpty(listRequest.StartDate))
            {
                var startTime = listRequest.StartDate.ToDateTime();
                query += $" and RowKey lt '{startTime.ToInverseTicksRowKey()}'";
            }

            if (!String.IsNullOrEmpty(listRequest.EndDate))
            {
                var endTime = listRequest.EndDate.ToDateTime();
                query += $" and RowKey gt '{endTime.ToInverseTicksRowKey()}'";
            }

            query += $"&$top={listRequest.PageSize}";

            if (!String.IsNullOrEmpty(listRequest.NextPartitionKey))
            {
                query += $"&NextPartitionKey={listRequest.NextPartitionKey}";
            }

            if (!String.IsNullOrEmpty(listRequest.NextRowKey))
            {
                query += $"&NextRowKey={listRequest.NextRowKey}";
            }

            var operationUri = new Uri($"{_srvrPath}{resource}{query}");
            using (var request = CreateRequest())
            {
                request.DefaultRequestHeaders.Authorization = GetAuthHeader(request, "GET", fullResourcePath: resource);

                using (var response = await request.GetAsync(operationUri))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var resultset = JsonConvert.DeserializeObject<TableStorageResultSet<TEntity>>(json);

                        var listResponse = ListResponse<TEntity>.Create(resultset.ResultSet);
                        foreach (var header in response.Headers)
                        {
                            if (header.Key == "x-ms-continuation-NextPartitionKey") listResponse.NextPartitionKey = header.Value.First();
                            if (header.Key == "x-ms-continuation-NextRowKey") listResponse.NextRowKey = header.Value.First();
                        }

                        listResponse.HasMoreRecords = !String.IsNullOrEmpty(listResponse.NextPartitionKey);
                        listResponse.PageIndex = listRequest.PageIndex;

                        return listResponse;
                    }
                    else
                    {
                        _logger.AddError("TableStorageBase_GetPagedResultsAsync(entity)", "failureResponseCode",
                            new KeyValuePair<string, string>("tableName", GetTableName()),
                            new KeyValuePair<string, string>("reasonPhrase", response.ReasonPhrase));

                        throw new Exception($"Non success response from server: {response.RequestMessage}");
                    }
                }
            }
        }

        public async Task InsertOrReplaceAsync(TEntity entity)
        {
            if (entity is IValidateable)
            {
                var result = Validator.Validate(entity as IValidateable);
                if (!result.Successful)
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

            using (var request = CreateRequest(fullResourcePath))
            {
                var jsonContent = new StringContent(json);
                jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                jsonContent.Headers.ContentMD5 = GetContentMD5(json);
                request.DefaultRequestHeaders.Authorization = GetAuthHeader(request, "PUT", "application/json", fullResourcePath: fullResourcePath, contentMd5: jsonContent.Headers.ContentMD5);
                request.DefaultRequestHeaders.Add("If-Match", entity.ETag);

                using (var response = await request.PutAsync(operationUri, jsonContent))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == HttpStatusCode.PreconditionFailed)
                        {
                            _logger.AddError("TableStorageBase_InsertOrReplaceAsync(entity)", "contentModified",
                                new KeyValuePair<string, string>("tableName", GetTableName()),
                                new KeyValuePair<string, string>("rowKey", entity.RowKey),
                                new KeyValuePair<string, string>("partitionKey", entity.PartitionKey));

                            throw new ContentModifiedException();
                        }
                        else
                        {
                            _logger.AddError("TableStorageBase_InsertOrReplaceAsync(entity)", "failureResponseCode",
                               new KeyValuePair<string, string>("tableName", GetTableName()),
                               new KeyValuePair<string, string>("reasonPhrase", response.ReasonPhrase),
                               new KeyValuePair<string, string>("rowKey", entity.RowKey),
                               new KeyValuePair<string, string>("partitionKey", entity.PartitionKey));

                            throw new Exception(response.ReasonPhrase);
                        }
                    }
                }
            }

        }

        public async Task<IEnumerable<TEntity>> GetByParitionIdAsync(String partitionKey, int? count = null, int? skip = null)
        {
            await InitAsync();

            var resource = $"()";
            var query = $"?$filter=PartitionKey eq '{partitionKey}'";
            if (count.HasValue)
            {
                query += $"$top={count.Value}";
            }

            //HACK: Is not effective since the record count could change, need to pass in the last value from the previous mechanism
            if (skip.HasValue)
            {
                query += $"$skip={skip.Value}";
            }

            var operationUri = new Uri($"{_srvrPath}{resource}{query}");

            using (var request = CreateRequest(resource))
            {
                request.DefaultRequestHeaders.Authorization = GetAuthHeader(request, "GET", fullResourcePath: resource);

                var json = await request.GetStringAsync(operationUri);
                var resultset = JsonConvert.DeserializeObject<TableStorageResultSet<TEntity>>(json);
                return resultset.ResultSet;
            }
        }

        public async Task<string> GetRawJSONByParitionIdAsync(String partitionKey, int? count = null, int? skip = null)
        {
            await InitAsync();

            var resource = $"()";
            var query = $"?$filter=PartitionKey eq '{partitionKey}'";
            if (count.HasValue)
            {
                query += $"&$top={count.Value}";
            }

            //HACK: Is not effective since the record count could change, need to pass in the last value from the previous mechanism
            if (skip.HasValue)
            {
                query += $"&$skip={skip.Value}";
            }

            var operationUri = new Uri($"{_srvrPath}{resource}{query}");

            using (var request = CreateRequest(resource))
            {
                request.DefaultRequestHeaders.Authorization = GetAuthHeader(request, "GET", fullResourcePath: resource);

                var json = await request.GetStringAsync(operationUri);
                var resultset = JsonConvert.DeserializeObject<TableStorageResultSet<TEntity>>(json);
                return JsonConvert.SerializeObject(resultset.ResultSet);
            }
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

            using (var request = CreateRequest(resource))
            {
                request.DefaultRequestHeaders.Authorization = GetAuthHeader(request, "GET", fullResourcePath: resource);

                var json = await request.GetStringAsync(operationUri);
                var resultset = JsonConvert.DeserializeObject<TableStorageResultSet<TEntity>>(json);
                return resultset.ResultSet;
            }
        }

        public async Task<String> GetEntitiesJSONByFilterAsync(params FilterOptions[] filters)
        {
            await InitAsync();

            var resource = $"()";
            var query = GetFilter(filters.ToList());
            var operationUri = new Uri($"{_srvrPath}{resource}{query}");

            using (var request = CreateRequest(resource))
            {
                request.DefaultRequestHeaders.Authorization = GetAuthHeader(request, "GET", fullResourcePath: resource);

                return await request.GetStringAsync(operationUri);
            }
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
