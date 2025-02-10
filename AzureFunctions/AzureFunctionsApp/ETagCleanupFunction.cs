using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

public static class ETagCleanupFunction
{
    private static readonly string EndpointUri = "https://your-cosmosdb.documents.azure.com:443/";
    private static readonly string PrimaryKey = "your-primary-key";
    private static readonly string DatabaseId = "YourDatabase";
    private static readonly string ETagContainerId = "ETagTracking";
    private static CosmosClient _cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
    private static Container _etagContainer = _cosmosClient.GetContainer(DatabaseId, ETagContainerId);
  
    /**using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos;**/

    public class ETagFactory
    {
        private static readonly string DatabaseId = "YourDatabase";
        private static readonly string ETagContainerId = "ETagTracking";
        private static CosmosClient _cosmosClient;
        private static Container _etagContainer;
    
        public ETagFactory(CosmosClient client)
        {
            _cosmosClient = client;
            _etagContainer = _cosmosClient.GetContainer(DatabaseId, ETagContainerId);
        }
    
        public static string GenerateETag<T>(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item), "Item cannot be null");
            }
    
            string json = JsonSerializer.Serialize(item);
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
                return Convert.ToBase64String(hashBytes);
            }
        }
    
        public async Task SaveETagAsync(string itemId, string etag)
        {
            var etagRecord = new { id = itemId, etag };
            await _etagContainer.UpsertItemAsync(etagRecord, new PartitionKey(itemId));
        }
    
        public async Task<string> GetETagAsync(string itemId)
        {
            try
            {
                var response = await _etagContainer.ReadItemAsync<dynamic>(itemId, new PartitionKey(itemId));
                return response.Resource.etag;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }
    }

  
    [FunctionName("CleanupETags")]
    public static async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer, ILogger log)
    {
        List<string> expiredIds = new();
        QueryDefinition query = new QueryDefinition("SELECT c.id FROM c WHERE c._ts < @threshold")
            .WithParameter("@threshold", DateTimeOffset.UtcNow.ToUnixTimeSeconds() - (7 * 24 * 60 * 60)); // 7 days

        using FeedIterator<dynamic> iterator = _etagContainer.GetItemQueryIterator<dynamic>(query);
        while (iterator.HasMoreResults)
        {
            FeedResponse<dynamic> response = await iterator.ReadNextAsync();
            foreach (var item in response)
            {
                expiredIds.Add(item.id.ToString());
            }
        }

        List<Task> deleteTasks = new();
        foreach (var id in expiredIds)
        {
            deleteTasks.Add(_etagContainer.DeleteItemAsync<dynamic>(id, new PartitionKey(id)));
            log.LogInformation($"Deleted expired ETag: {id}");
        }

        await Task.WhenAll(deleteTasks);
    }
}
