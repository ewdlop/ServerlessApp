using System;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

public static class CosmosChangeFeedFunction
{

      public class ETagFactory
      {
          private static readonly ConcurrentDictionary<string, string> ETagStore = new();
      
          public static string GenerateETag<T>(T item, string itemId)
          {
              if (item == null)
              {
                  throw new ArgumentNullException(nameof(item), "Item cannot be null");
              }
      
              // Convert item to JSON
              string json = JsonSerializer.Serialize(item);
      
              // Compute SHA-256 hash
              using (SHA256 sha256 = SHA256.Create())
              {
                  byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
                  string etag = Convert.ToBase64String(hashBytes);
      
                  // Store ETag with itemId
                  ETagStore[itemId] = etag;
                  return etag;
              }
        }
    
        public static string GetETag(string itemId)
        {
            return ETagStore.TryGetValue(itemId, out string etag) ? etag : null;
        }
    
        public static void RemoveETag(string itemId)
        {
            ETagStore.TryRemove(itemId, out _);
        }
    
        public static void PrintTrackedETags()
        {
            Console.WriteLine("Tracked ETags:");
            foreach (var entry in ETagStore)
            {
                Console.WriteLine($"Item ID: {entry.Key}, ETag: {entry.Value}");
            }
        }
    }


    private static readonly string EndpointUri = "https://your-cosmosdb.documents.azure.com:443/";
    private static readonly string PrimaryKey = "your-primary-key";
    private static readonly string DatabaseId = "YourDatabase";
    private static readonly string ETagContainerId = "ETagTracking";
    private static CosmosClient _cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
    private static Container _etagContainer = _cosmosClient.GetContainer(DatabaseId, ETagContainerId);

    [FunctionName("UpdateETagsOnChange")]
    public static async Task Run(
        [CosmosDBTrigger(
            databaseName: "YourDatabase",
            containerName: "YourContainer",
            LeaseContainerName = "leases",
            Connection = "CosmosDBConnectionString",
            CreateLeaseContainerIfNotExists = true)] IReadOnlyList<dynamic> input,
        ILogger log)
    {
        if (input != null && input.Count > 0)
        {
            List<Task> tasks = new();
            foreach (var doc in input)
            {
                string itemId = doc.id.ToString();
                string newEtag = ETagFactory.GenerateETag(doc);
                var etagRecord = new { id = itemId, etag = newEtag };
                tasks.Add(_etagContainer.UpsertItemAsync(etagRecord, new PartitionKey(itemId)));
                log.LogInformation($"ETag updated for item {itemId}: {newEtag}");
            }
            await Task.WhenAll(tasks);
        }
    }
}
