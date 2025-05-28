using LagoVista.CloudStorage.Models;
using LagoVista.Core.Models;
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
using Azure.Data.Tables;
using Prometheus;

namespace LagoVista.CloudStorage.Storage
{

    public enum StoragePeriod
    {
        All,
        Month,
        Quarter,
        Year
    }

    public abstract class TableStorageBase<TEntity> : IDisposable where TEntity : TableStorageEntity
    {
        TableClient _tableClient;

        IAdminLogger _logger;
        String _srvrPath;
        String _accountName;
        String _accountKey;

        String _tableName = null;

        protected static readonly Histogram GetMetric = Metrics.CreateHistogram("nuviot_tablestorage_get", "Elapsed time for Azure Table Storage get.",
          new HistogramConfiguration
          {
                      // Here you specify only the names of the labels.
                      LabelNames = new[] { "entity" },
              Buckets = Histogram.ExponentialBuckets(0.250, 2, 8)
          });

        protected static readonly Histogram QueryMetric = Metrics.CreateHistogram("nuviot_tablestorage_query", "Elapsed time for Azure Table Storage query.",
          new HistogramConfiguration
          {
              // Here you specify only the names of the labels.
              LabelNames = new[] { "entity" },
              Buckets = Histogram.ExponentialBuckets(0.250, 2, 8)
          });
        
        protected static readonly Histogram CreateMetric = Metrics.CreateHistogram("nuviot_tablestorage_create", "Elapsed time for Azure Table Storage Crete/Insert.",
          new HistogramConfiguration
          {
              // Here you specify only the names of the labels.
              LabelNames = new[] { "entity" },
              Buckets = Histogram.ExponentialBuckets(0.250, 2, 8)
          });

        protected static readonly Histogram UpdateMetric = Metrics.CreateHistogram("nuviot_tablestorage_update", "Elapsed time for Azure Table Storage Update.",
          new HistogramConfiguration
          {
                      // Here you specify only the names of the labels.
                      LabelNames = new[] { "entity" },
              Buckets = Histogram.ExponentialBuckets(0.250, 2, 8)
          });

        protected static readonly Histogram DeleteMetric = Metrics.CreateHistogram("nuviot_tablestorage_delete", "Elapsed time for Azure Table Storage Delete.",
          new HistogramConfiguration
          {
              // Here you specify only the names of the labels.
              LabelNames = new[] { "entity" },
              Buckets = Histogram.ExponentialBuckets(0.250, 2, 8)
          });

        protected static readonly Counter WarningMetric = Metrics.CreateCounter("nuviot_tablestorage_warning", "Number of warnings for table storage operation.",
          new CounterConfiguration
          {
              // Here you specify only the names of the labels.
              LabelNames = new[] { "entity", "operation", "warning_type" },
          });

        protected static readonly Counter ErrorMetric = Metrics.CreateCounter("nuviot_tablestorage_error", "Number of errors for table storage operation.",
          new CounterConfiguration
          {
              // Here you specify only the names of the labels.
              LabelNames = new[] { "entity", "operation", "error_type" },
          });

        private static Dictionary<string, TableClient> _tableClients = new Dictionary<string, TableClient>();


        public TableStorageBase(String accountName, string accountKey, IAdminLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); 
            
            _accountKey = accountKey ;
            _accountName = accountName; 

            if (String.IsNullOrEmpty(accountName)) throw new ArgumentNullException(nameof(accountName));
            if (String.IsNullOrEmpty(accountKey)) throw new ArgumentNullException(nameof(accountKey)); 
         
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

            var tableName = GetTableName();

            var connectionString = $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={accountKey}";
            _tableClient = new TableClient(connectionString, tableName);
          
            _srvrPath = $"https://{_accountName}.table.core.windows.net/{GetTableName()}";
        }

        public void SetTableName(string tableName)
        {
            _tableName = tableName;
            _srvrPath = $"https://{_accountName}.table.core.windows.net/{GetTableName()}";
        }

        public virtual StoragePeriod GetStoragePeriod()
        {
            return StoragePeriod.All;
        }

        protected virtual string GetTableName()
        {
            if(!String.IsNullOrEmpty(_tableName))
            {
                return _tableName;
            }

            var name = typeof(TEntity).Name;
            switch(GetStoragePeriod())
            {
                case StoragePeriod.Month:
                    return $"m{DateTime.UtcNow.ToString("yyyyMM")}{name}";
                case StoragePeriod.Quarter:
                    var quarter = (DateTime.UtcNow.Month - 1) / 3 + 1;
                    return $"q{DateTime.UtcNow.ToString("yyyy")}{quarter}{name}";
                case StoragePeriod.Year:
                    return $"y{DateTime.UtcNow.ToString("yyyy")}{name}";
            }

            return name;
        }

        private bool Initialized { get; set; }

        DateTime? _initDate;

        public virtual async Task InitAsync()
        {
            lock (this)
            {
                if (Initialized && _initDate.HasValue && _initDate == DateTime.UtcNow.Date)
                {
                    return;
                }
            }

            try
            {

                var connectionString = $"DefaultEndpointsProtocol=https;AccountName={_accountName};AccountKey={_accountKey}";
                _tableClient = new TableClient(connectionString, GetTableName());
                _srvrPath = $"https://{_accountName}.table.core.windows.net/{GetTableName()}";

                var sw = Stopwatch.StartNew();
                await _tableClient.CreateIfNotExistsAsync();

                _logger.Trace($"[TableStorageBase<{typeof(TEntity).Name}>__InitAsync__{typeof(TEntity).Name}] Create If not Exists {sw.ElapsedMilliseconds}ms");

                _initDate = DateTime.UtcNow.Date;
                Initialized = true;
            }
            catch (Exception ex)
            {
                _logger.AddException($"[TableStorageBase<{typeof(TEntity).Name}>__InitAsync__{typeof(TEntity).Name}]", ex, GetTableName().ToKVP("tableName"));
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
            var sw = Stopwatch.StartNew();
            var operationUri = new Uri($"{_srvrPath}{fullResourcePath}");

            using (var metric = GetMetric.WithLabels(typeof(TEntity).Name).NewTimer())
            using (var request = CreateRequest(fullResourcePath))
            {
                request.DefaultRequestHeaders.Authorization = GetAuthHeader(request, "GET", fullResourcePath: fullResourcePath);

                using (var response = await request.GetAsync(operationUri))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        _logger.Trace($"[TableStorageBase<{typeof(TEntity).Name}>_Get_Full ResourcePath] {operationUri} {sw.ElapsedMilliseconds}ms");
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
                _logger.AddError($"[TableStorageBase<{typeof(TEntity).Name}>_GetRAWJSONAsync]", "emptyRowKey", new KeyValuePair<string, string>("tableName", GetTableName()));
                throw new Exception("Row key must be present get an entity.");
            }

            if (String.IsNullOrEmpty(partitionKey))
            {
                _logger.AddError($"[TableStorageBase<{typeof(TEntity).Name}>_GetRAWJSONAsync]", "emptyPartitionKey", new KeyValuePair<string, string>("tableName", GetTableName()));
                throw new Exception($"Partition key must be present to get an entity, row key {rowKey} was provided.");
            }

            var fullResourcePath = $"(PartitionKey='{partitionKey}',RowKey='{rowKey}')";
            var operationUri = new Uri($"{_srvrPath}{fullResourcePath}");
            _logger.Trace($"[TableStorageBase<{typeof(TEntity).Name}>__GetRAWJSONAsync] {operationUri}");

            using (var metric = GetMetric.WithLabels(typeof(TEntity).Name).NewTimer())
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

                    _logger.AddError($"TableStorageBase<{typeof(TEntity).Name}>_GetRawJSONAsync", "failureResponseCode",
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
                _logger.AddError($"[TableStorageBase<{typeof(TEntity).Name}>_GetAsync]", "Request was made to get table storage entity without a row key.", new KeyValuePair<string, string>("tableName", GetTableName()));
                throw new Exception("Row is required to load a table storage entity.");
            }

            if (String.IsNullOrEmpty(partitionKey))
            {
                _logger.AddError($"TableStorageBase<{typeof(TEntity).Name}>_GetAsync", "emptyPartitionKey", new KeyValuePair<string, string>("tableName", GetTableName()));
                throw new Exception("Partition key is required to load a table storage entity.");
            }

            var fullResourcePath = $"(PartitionKey='{partitionKey}',RowKey='{rowKey}')";

            var record = await Get(fullResourcePath);
            if (record == null)
            {
                if (throwOnNotFound)
                {
                    throw new RecordNotFoundException(GetTableName(), $"Row Key = {rowKey}, Parition Key = {partitionKey}");
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
                _logger.AddError($"[TableStorageBase<{typeof(TEntity).Name}>_GetAsync]", "recordNotFound", new KeyValuePair<string, string>("tableName", GetTableName()), new KeyValuePair<string, string>("rowKey", rowKey));
                throw new Exception("Row Key Must be Present to get Record by Row.");
            }

            var record = (await GetByFilterAsync(FilterOptions.Create("RowKey", FilterOptions.Operators.Equals, rowKey))).FirstOrDefault();

            if (record == null)
            {
                ErrorMetric.WithLabels(typeof(TEntity).Name, "GetAsync", "RecordNotFound");
                if (throwOnNotFound)
                {
                    ErrorMetric.WithLabels(typeof(TEntity).Name, "GetAsync", "RecordNotFound");
                    throw new RecordNotFoundException(GetTableName(), $"RowKey = {rowKey}, Parition Key = -not supplied-");
                }
                else
                {
                    return null;
                }
            }

            return record;
        }

        public async Task InsertAsync(TEntity entity, bool logException = true)
        {
            var sw = Stopwatch.StartNew();

            if (entity == null)
            {
                _logger.AddError($"[TableStorageBase<{typeof(TEntity).Name}>_InsertAsync]", "NULL value provided.", GetTableName().ToKVP("tableName"));
                throw new Exception($"Null Value Provided for InsertAsync({typeof(TEntity).Name}).");
            }

            if (entity is IValidateable)
            {
                var result = Validator.Validate(entity as IValidateable);
                if (!result.Successful)
                {
                    WarningMetric.WithLabels(typeof(TEntity).Name, "InsertAsync", "Validation");
                    throw new ValidationException("Invalid Datea.", result.Errors);
                }
            }

            await InitAsync();

            if (String.IsNullOrEmpty(entity.RowKey))
            {
                _logger.AddError($"TableStorageBase<{typeof(TEntity).Name}>_InsertAsync]", "emptyRowKey", GetTableName().ToKVP("tableName"));
                throw new Exception("Row key must be present to insert or replace an entity.");
            }

            if (String.IsNullOrEmpty(entity.PartitionKey))
            {
                _logger.AddError($"TableStorageBase<{typeof(TEntity).Name}>_InsertAsync]", "emptyPartitionKey", GetTableName().ToKVP("tableName"));
                throw new Exception($"Partition Keys must be present to insert or replace an entity, Row Key = {entity.RowKey}.");
            }

            var json = JsonConvert.SerializeObject(entity);

            var jsonContent = new StringContent(json);
            jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            jsonContent.Headers.ContentMD5 = GetContentMD5(json);
            using(var insertMetric = CreateMetric.WithLabels(typeof(TEntity).Name))
            using (var request = CreateRequest())
            {
                var authHeader = GetAuthHeader(request, "POST", "application/json", contentMd5: jsonContent.Headers.ContentMD5);
                request.DefaultRequestHeaders.Authorization = authHeader;

                using (var response = await request.PostAsync(_srvrPath, jsonContent))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        if (logException)
                        {
                            ErrorMetric.WithLabels(typeof(TEntity).Name, "InsertAsync", response.StatusCode.ToString());

                            _logger.AddError($"TableStorageBase<{typeof(TEntity).Name}>_InsertAsync", "failureResponseCode", GetTableName().ToKVP("tableName"), response.ReasonPhrase.ToKVP("reasonPhrase"));
                        }

                        throw new Exception($"Non success response from server: {response.ReasonPhrase}");
                    }
                    else
                        _logger.Trace($"[TableStorageBase<{typeof(TEntity).Name}>__InsertAsync] {sw.ElapsedMilliseconds} ms");
                }
            }
        }

        public async Task InsertAsync(string json)
        {
            var sw = Stopwatch.StartNew();
            using (var insertMetric = CreateMetric.WithLabels(typeof(TEntity).Name))
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
                        ErrorMetric.WithLabels(typeof(TEntity).Name, "InsertAsync", response.StatusCode.ToString());
                        _logger.AddError($"TableStorageBase<{typeof(TEntity).Name}>_InsertAsync]", "failureResponseCode", GetTableName().ToKVP("tableName"), response.ReasonPhrase.ToKVP("reasonPhrase"));
                        throw new Exception($"Non success response from server: {response.ReasonPhrase}");
                    }
                    else
                        _logger.Trace($"[TableStorageBase<{typeof(TEntity).Name}>__InsertAsync] {sw.ElapsedMilliseconds}ms");
                }
            }
        }

        public async Task UpdateAsync(string partitionKey, string rowKey, string json, string etag)
        {
            var sw = Stopwatch.StartNew();
            var fullResourcePath = $"(PartitionKey='{partitionKey}',RowKey='{rowKey}')";
            var operationUri = new Uri($"{_srvrPath}{fullResourcePath}");

            using (var udpateMetric = UpdateMetric.WithLabels(typeof(TEntity).Name))
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

                            ErrorMetric.WithLabels(typeof(TEntity).Name, "UpdateAsync", "Precondition Failed");

                            throw new ContentModifiedException();
                        }
                        else
                        {
                            ErrorMetric.WithLabels(typeof(TEntity).Name, "UpdateAsync", response.StatusCode.ToString());

                            _logger.AddError("TableStorageBase_UpdateAsync(json)", "failureResponseCode", new KeyValuePair<string, string>("tableName", GetTableName()), new KeyValuePair<string, string>("reasonPhrase", response.ReasonPhrase));
                            throw new Exception(response.ReasonPhrase);
                        }
                    }
                    else
                        _logger.Trace($"[TableStorageBase<{typeof(TEntity).Name}>__UpdateAsync] {operationUri} {sw.ElapsedMilliseconds}ms");

                }
            }
        }


        public async Task RemoveAsync(string partitionKey, string rowKey, string etag = "*")
        {
            var sw = Stopwatch.StartNew();
           
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
            

            using(var removeTimer = DeleteMetric.WithLabels(typeof(TEntity).Name))
            using (var request = CreateRequest(fullResourcePath))
            {
                request.DefaultRequestHeaders.Authorization = GetAuthHeader(request, "DELETE", fullResourcePath: fullResourcePath);
                request.DefaultRequestHeaders.Add("If-Match", etag);

                using (var response = await request.DeleteAsync(operationUri))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.Trace($"[TableStorageBase<{typeof(TEntity).Name}>__RemoveAsync] {operationUri} {sw.ElapsedMilliseconds}ms");
                        return;
                    }

                    if (response.StatusCode == HttpStatusCode.PreconditionFailed)
                    {
                        _logger.AddError("TableStorageBase_RemoveAsync(PartitionKey, RowKey)", "contentModified",
                          new KeyValuePair<string, string>("tableName", GetTableName()),
                          new KeyValuePair<string, string>("rowKey", rowKey),
                          new KeyValuePair<string, string>("partitionKey", partitionKey));
                        
                        ErrorMetric.WithLabels(typeof(TEntity).Name, "RemoveAsync", "Precondition Failed");

                        throw new ContentModifiedException();
                    }

                    _logger.AddError("TableStorageBase_RemoveAsync(PartitionKey, RowKey)", "failureResponseCode",
                        new KeyValuePair<string, string>("tableName", GetTableName()),
                        new KeyValuePair<string, string>("reasonPhrase", response.ReasonPhrase),
                        new KeyValuePair<string, string>("rowKey", rowKey),
                        new KeyValuePair<string, string>("partitionKey", partitionKey));

                    ErrorMetric.WithLabels(typeof(TEntity).Name, "RemoveAsync", response.StatusCode.ToString());

                    throw new Exception($"Non success response from server: {response.ReasonPhrase}");
                }
            }
        }

        public async Task RemoveByPartitionKeyAsync(string partitionKey, string etag = "*")
        {
            await InitAsync();

            var records = await GetByParitionIdAsync(partitionKey);
            foreach(var record in records)
            {
                await RemoveAsync(record.PartitionKey, record.RowKey);
            }
        }

        public Task RemoveAsync(TEntity entity, string etag = "*")
        {
            return RemoveAsync(entity.PartitionKey, entity.RowKey, String.IsNullOrEmpty(etag) ? entity.ETag : etag);
        }

        public async Task UpdateAsync(TEntity entity, string etag = "*")
        {
            var sw = Stopwatch.StartNew();

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

            using(var updateTimer = UpdateMetric.WithLabels(typeof(TEntity).Name))
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

                            ErrorMetric.WithLabels(typeof(TEntity).Name, "RemoveAsync", "Precodition__ContentModified");

                            throw new ContentModifiedException();
                        }
                        else
                        {
                            _logger.AddError("TableStorageBase_UpdateAsync(entity)", "failureResponseCode",
                                new KeyValuePair<string, string>("tableName", GetTableName()),
                                new KeyValuePair<string, string>("reasonPhrase", response.ReasonPhrase),
                                new KeyValuePair<string, string>("rowKey", entity.RowKey),
                                new KeyValuePair<string, string>("partitionKey", entity.PartitionKey));
                            ErrorMetric.WithLabels(typeof(TEntity).Name, "RemoveAsync", response.StatusCode.ToString());

                            throw new Exception($"Non success response from server: {response.RequestMessage}");

                        }
                    }
                    else
                        _logger.Trace($"[TableStorageBase<{typeof(TEntity).Name}>__UpdateAsync] {operationUri} {sw.ElapsedMilliseconds} ms");
                }
            }
        }

        public async Task<ListResponse<TEntity>> GetPagedResultsAsync(string partitionKey, ListRequest listRequest, params FilterOptions[] filters)
        {
            var sw = Stopwatch.StartNew();
            await InitAsync();

            if (String.IsNullOrEmpty(partitionKey))
            {
                _logger.AddError("TableStorageBase_GetPagedResults", "emptyPartitionKey", new KeyValuePair<string, string>("tableName", GetTableName()));
                throw new Exception("Row and Partition Keys must be present to insert or replace an entity.");
            }

            var resource = $"()";

            var query = (filters.Length > 0) ? $"{GetFilter(filters.ToList())} and PartitionKey eq '{partitionKey}'" : $"?$filter=(PartitionKey eq '{partitionKey}')";

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

            if (!String.IsNullOrEmpty(listRequest.NextPartitionKey))
            {
                query += $"&NextPartitionKey={listRequest.NextPartitionKey}";
            }

            if (!String.IsNullOrEmpty(listRequest.NextRowKey))
            {
                query += $"&NextRowKey={listRequest.NextRowKey}";
            }

            query += $"&$top={Math.Min(1000, listRequest.PageSize)}";

            var operationUri = new Uri($"{_srvrPath}{resource}{query}");
            
            using(var queryTimer = QueryMetric.WithLabels(typeof(TEntity).Name))
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

                        _logger.Trace($"[TableStorageBase<{typeof(TEntity).Name}>__GetPagedResultsAsync] {sw.ElapsedMilliseconds} ms {operationUri}");

                        return listResponse;
                    }
                    else
                    {
                        _logger.AddError("TableStorageBase_GetPagedResultsAsync(entity)", "failureResponseCode",
                            new KeyValuePair<string, string>("tableName", GetTableName()),
                            new KeyValuePair<string, string>("reasonPhrase", response.ReasonPhrase));

                        ErrorMetric.WithLabels(typeof(TEntity).Name, "GetPagedResultsAsync", response.StatusCode.ToString());

                        throw new Exception($"Non success response from server: {response.ReasonPhrase}");
                    }
                }
            }
        }

        public async Task<ListResponse<TEntity>> GetPagedResultsAsync(ListRequest listRequest, params FilterOptions[] filters)
        {
            var sw = Stopwatch.StartNew();
            await InitAsync();

            var resource = $"()";


            var query = GetFilter(filters.ToList()) ; //Just seed this with something so we can use 

            if (!String.IsNullOrEmpty(listRequest.StartDate))
            {
                var startTime = listRequest.StartDate.ToDateTime();
                query += $"?$filter=RowKey lt '{startTime.ToInverseTicksRowKey()}'";
            }

            if (!String.IsNullOrEmpty(listRequest.EndDate))
            {
                var endTime = listRequest.EndDate.ToDateTime();
                if (query == String.Empty)
                    query += $"?$filter=RowKey gt '{endTime.ToInverseTicksRowKey()}'";
                else
                    query += $" and RowKey gt '{endTime.ToInverseTicksRowKey()}'";
            }

            // We just need to provide something for the query.
            if (query == String.Empty)
            {
                query += $"?$filter=RowKey gt '{DateTime.UtcNow.AddMinutes(30).ToInverseTicksRowKey()}'";
            }

            query += $"&$top={Math.Min(1000,listRequest.PageSize)}";

            if (!String.IsNullOrEmpty(listRequest.NextPartitionKey))
            {
                query += $"&NextPartitionKey={listRequest.NextPartitionKey}";
            }

            if (!String.IsNullOrEmpty(listRequest.NextRowKey))
            {
                query += $"&NextRowKey={listRequest.NextRowKey}";
            }

            var operationUri = new Uri($"{_srvrPath}{resource}{query}");
            _logger.Trace($"[TableStorageBase__GetPagedResultsAsync] {operationUri}");

            using (var queryTimer = QueryMetric.WithLabels(typeof(TEntity).Name))
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

                        _logger.Trace($"[TableStorageBase<{typeof(TEntity).Name}>__GetPagedResultsAsync] {sw.ElapsedMilliseconds} ms {operationUri}");

                        return listResponse;
                    }
                    else
                    {
                        _logger.AddError("TableStorageBase_GetPagedResultsAsync(entity)", "failureResponseCode",
                            new KeyValuePair<string, string>("tableName", GetTableName()),
                            new KeyValuePair<string, string>("reasonPhrase", response.ReasonPhrase));

                        ErrorMetric.WithLabels(typeof(TEntity).Name, "GetPagedResultsAsync", response.StatusCode.ToString());

                        throw new Exception($"Non success response from server: {response.ReasonPhrase}");
                    }
                }
            }
        }

        public async Task InsertOrReplaceAsync(TEntity entity)
        {
            var sw = Stopwatch.StartNew();

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
            

            using (var queryTimer = QueryMetric.WithLabels(typeof(TEntity).Name))
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

                            ErrorMetric.WithLabels(typeof(TEntity).Name, "GetPagedResultsAsync", "Precondition__ContentModified");

                            throw new ContentModifiedException();
                        }
                        else
                        {
                            _logger.AddError("TableStorageBase_InsertOrReplaceAsync(entity)", "failureResponseCode",
                               new KeyValuePair<string, string>("tableName", GetTableName()),
                               new KeyValuePair<string, string>("reasonPhrase", response.ReasonPhrase),
                               new KeyValuePair<string, string>("rowKey", entity.RowKey),
                               new KeyValuePair<string, string>("partitionKey", entity.PartitionKey));

                            ErrorMetric.WithLabels(typeof(TEntity).Name, "GetPagedResultsAsync", response.StatusCode.ToString());

                            throw new Exception(response.ReasonPhrase);
                        }
                    }
                    else
                        _logger.Trace($"[TableStorageBase<{typeof(TEntity).Name}>__GetPagedResultsAsync] {sw.ElapsedMilliseconds}ms {operationUri}");
                }
            }

        }

        public async Task<IEnumerable<TEntity>> GetByParitionIdAsync(String partitionKey, int? count = null, int? skip = null)
        {
            var sw = Stopwatch.StartNew();

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
            

            using (var getTimer = GetMetric.WithLabels(typeof(TEntity).Name))
            using (var request = CreateRequest(resource))
            {
                request.DefaultRequestHeaders.Authorization = GetAuthHeader(request, "GET", fullResourcePath: resource);

                var json = await request.GetStringAsync(operationUri);
                var resultset = JsonConvert.DeserializeObject<TableStorageResultSet<TEntity>>(json);
                _logger.Trace($"[TableStorageBase<{typeof(TEntity).Name}>__GetByParitionIdAsync] {sw.ElapsedMilliseconds} ms {operationUri} ");
                return resultset.ResultSet;
            }
        }

        public async Task<string> GetRawJSONByParitionIdAsync(String partitionKey, int? count = null, int? skip = null)
        {
            var sw = Stopwatch.StartNew();

            await InitAsync();

            var resource = $"()";
            var query = $"?$filter=PartitionKey eq '{partitionKey}'";
            if (count.HasValue)
            {
                query += $"&$top={count.Value}";
            }

            //TODO: Add support for continuation tokens

            /*
            //HACK: Is not effective since the record count could change, need to pass in the last value from the previous mechanism
            if (skip.HasValue)
            {
                query += $"&$skip={skip.Value}";
            }*/

            var operationUri = new Uri($"{_srvrPath}{resource}{query}");


            using (var getTimer = GetMetric.WithLabels(typeof(TEntity).Name))
            using (var request = CreateRequest(resource))
            {
                request.DefaultRequestHeaders.Authorization = GetAuthHeader(request, "GET", fullResourcePath: resource);

                var json = await request.GetStringAsync(operationUri);
                var resultset = JsonConvert.DeserializeObject<TableStorageResultSet<TEntity>>(json);
                _logger.Trace($"[TableStorageBase<{typeof(TEntity).Name}>__GetRawJSONByParitionIdAsync] {sw.ElapsedMilliseconds} ms  {operationUri}");
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
            var sw = Stopwatch.StartNew();
            await InitAsync();

            var resource = $"()";
            var query = GetFilter(filters.ToList());
            var operationUri = new Uri($"{_srvrPath}{resource}{query}");
           

            using (var request = CreateRequest(resource))
            {
                request.DefaultRequestHeaders.Authorization = GetAuthHeader(request, "GET", fullResourcePath: resource);

                var json = await request.GetStringAsync(operationUri);
                var resultset = JsonConvert.DeserializeObject<TableStorageResultSet<TEntity>>(json);

                _logger.Trace($"[TableStorageBase<{typeof(TEntity).Name}>__GetByFilterAsync] {sw.ElapsedMilliseconds} ms {operationUri}");
                return resultset.ResultSet;
            }
        }

        public async Task<String> GetEntitiesJSONByFilterAsync(params FilterOptions[] filters)
        {
            var sw = Stopwatch.StartNew();
            await InitAsync();
         
            var resource = $"()";
            var query = GetFilter(filters.ToList());
            var operationUri = new Uri($"{_srvrPath}{resource}{query}");

            using (var getTimer = GetMetric.WithLabels(typeof(TEntity).Name))
            using (var request = CreateRequest(resource))
            {
                request.DefaultRequestHeaders.Authorization = GetAuthHeader(request, "GET", fullResourcePath: resource);

                _logger.Trace($"[TableStorageBase<{typeof(TEntity).Name}>__GetByFilterAsync] {sw.ElapsedMilliseconds} ms {operationUri}");

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

        public object Filter { get; private set; }

        public Operators Operator { get; private set; }

        public static FilterOptions Create(string field, Operators op, object filter)
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

            if(Filter.GetType() == typeof(bool))
                return $"({Field} {op} {Filter.ToString().ToLower()})";

            return $"({Field} {op} '{Filter}')";
        }

    }
}
