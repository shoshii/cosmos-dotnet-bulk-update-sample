using System;
using System.Linq;
using System.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace CosmosBulkUpdateSample.UnitTests.Services
{
    public class CosmosBulkUpdateTest
    {
        private readonly ITestOutputHelper output;
        // The Azure Cosmos DB endpoint for running this sample.
        public static readonly string EndpointUri = ConfigurationManager.AppSettings["EndPointUri"];

        // The primary key for the Azure Cosmos account.
        public static readonly string PrimaryKey = ConfigurationManager.AppSettings["PrimaryKey"];

        // The Cosmos client instance
        private CosmosClient cosmosClient;

        // The database we will create
        private Database database;

        // The container we will create.
        private Container container;

        // The name of the database and container we will create
        private string databaseId = "UserDatabase";
        private string containerId = "UserContainer";

        public CosmosBulkUpdateTest(ITestOutputHelper output)
        {
            this.output = output;
            output.WriteLine("Beginning test to endpoint: {0}...\n", ConfigurationManager.AppSettings["EndPointUri"]);
            this.cosmosClient = new CosmosClient(
                ConfigurationManager.AppSettings["EndPointUri"], ConfigurationManager.AppSettings["PrimaryKey"],
                new CosmosClientOptions() { AllowBulkExecution = true });
        }

        public void Dispose()
        {
            // teardown
            //Dispose of CosmosClient
            this.cosmosClient.Dispose();
        }

        #region Sample_TestCode
        [Fact, Trait("Operation", "Insert")]
        public async void testBulkInsert()
        {
            this.database = await createDatabase();
            this.container = await createContainer();
            int recordNum = 10000;
            await bulkInsert(recordNum);
            List<string> pkeys = await getPartitionKeys();
            output.WriteLine("pkeys: {0}", JsonConvert.SerializeObject(pkeys));
            int recordCount = await getCount();
            output.WriteLine("count: {0}", recordCount);
            Assert.Equal(recordNum, recordCount);

            DatabaseResponse res = await terminateDB();
        }
        #endregion

        #region Sample_TestCode
        [Fact, Trait("Operation", "Update")]
        public async void testBulkUpdate()
        {
            this.database = await createDatabase();
            this.container = await createContainer();
            await bulkInsert(10000);
            string expectedEmail = "shoshii@example.com";
            int updated = await bulkUpdate(expectedEmail);
            output.WriteLine("updated: {0}", updated);

            // assertion
            using FeedIterator<User> queryResultSetIterator = this.container.GetItemQueryIterator<User>("SELECT * FROM c");
            int actualUpdated = 0;
            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<User> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (User _user in currentResultSet)
                {
                    if (_user.Email == expectedEmail)
                    {
                        actualUpdated += 1;
                    }
                    if (_user.Country == "Japan")
                    {
                        Assert.Equal(expectedEmail, _user.Email);
                    } else {
                        Assert.NotEqual(expectedEmail, _user.Email);
                    }
                }
            }
            Assert.Equal(updated, actualUpdated);

            DatabaseResponse res = await terminateDB();
        }
        #endregion

        private async Task<int> bulkUpdate(string email)
        {
            int updatedCount = 0;
            PatchItemRequestOptions options = new()
            {
                FilterPredicate = "FROM c WHERE c.Country = 'Japan'"
            };

            List<PatchOperation> operations = new ()
            {
                PatchOperation.Replace($"/Email", email),
            };
            Container container = this.database.GetContainer(containerId);
            List<Task> tasks = new List<Task>();
            using FeedIterator<User> queryResultSetIterator = container.GetItemQueryIterator<User>("SELECT * FROM c");
            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<User> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (User _user in currentResultSet)
                {
                    if (_user.Country != "Japan") {
                        continue;
                    }
                    
                    updatedCount += 1;
                    tasks.Add(container.PatchItemAsync<User>(
                        id: _user.Id,
                        partitionKey: new PartitionKey(_user.Country),
                        patchOperations: operations,
                        requestOptions: options
                    )
                    .ContinueWith(itemResponse =>
                    {
                        if (!itemResponse.IsCompletedSuccessfully)
                        {
                            AggregateException innerExceptions = itemResponse.Exception.Flatten();
                            if (innerExceptions.InnerExceptions.FirstOrDefault(innerEx => innerEx is CosmosException) is CosmosException cosmosException)
                            {
                                output.WriteLine($"Received {cosmosException.StatusCode} ({cosmosException.Message}).");
                            }
                            else
                            {
                                output.WriteLine($"Exception {innerExceptions.InnerExceptions.FirstOrDefault()}.");
                            }
                        }
                    }));
                }
            }

            // Wait until all are done
            await Task.WhenAll(tasks);
            return updatedCount;
        }

        private async Task<bool> bulkInsert(int count)
        {
            ICollection<User> usersToInsert = new List<User>();
            int amountToInsert = count;
            for (int idx = 0; idx < amountToInsert; idx++)
            {
                User user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    LastName = Faker.Name.Last(),
                    FirstName = Faker.Name.First(),
                    Grade = Faker.Enum.Random<MemberShipGrade>(),
                    Followers = Faker.RandomNumber.Next(0, 1000000),
                    Area = $"{Faker.Address.Country()}, {Faker.Address.City()}",
                    Country = $"{Faker.Address.Country()}",
                    Bio = String.Join(" ", Faker.Lorem.Sentences(3))
                };
                user.Email = Faker.Internet.Email(user.LastName);
                usersToInsert.Add(user);
            }
            Container container = this.database.GetContainer(containerId);
            List<Task> tasks = new List<Task>(amountToInsert);
            foreach (User user in usersToInsert)
            {
                tasks.Add(container.CreateItemAsync(user, new PartitionKey(user.Country))
                    .ContinueWith(itemResponse =>
                    {
                        if (!itemResponse.IsCompletedSuccessfully)
                        {
                            AggregateException innerExceptions = itemResponse.Exception.Flatten();
                            if (innerExceptions.InnerExceptions.FirstOrDefault(innerEx => innerEx is CosmosException) is CosmosException cosmosException)
                            {
                                output.WriteLine($"Received {cosmosException.StatusCode} ({cosmosException.Message}).");
                            }
                            else
                            {
                                output.WriteLine($"Exception {innerExceptions.InnerExceptions.FirstOrDefault()}.");
                            }
                        }
                    }));
            }

            // Wait until all are done
            await Task.WhenAll(tasks);
            return true;
        }

        private async Task<int> getCount()
        {
            int count = 0;
            using FeedIterator<User> queryResultSetIterator = this.container.GetItemQueryIterator<User>("SELECT * FROM c");
            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<User> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (User _user in currentResultSet)
                {
                    count += 1;
                }
            }
            return count;
        }

        private async Task<List<string>> getPartitionKeys()
        {
            List<string> partitionKeys = new List<string>();
            QueryDefinition queryDefinition = new QueryDefinition("SELECT DISTINCT c.Country FROM c");
            using FeedIterator<User> queryResultSetIterator = this.container.GetItemQueryIterator<User>(queryDefinition);
            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<User> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (User _user in currentResultSet)
                {
                    partitionKeys.Add(_user.Country);
                    //output.WriteLine("\tRead {0}\n", _user);
                }
            }
            return partitionKeys;
        }

        private async Task<DatabaseResponse> terminateDB()
        {
            output.WriteLine("Deleting Database: {0}\n", this.database.Id);
            DatabaseResponse databaseResourceResponse = await this.database.DeleteAsync();
            return databaseResourceResponse;
        }
        private async Task<Database> createDatabase()
        {
            // Create a new database
            output.WriteLine("Creating Database: {0}\n", databaseId);
            return await this.cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
        }

        private async Task<Container> createContainer()
        {
            output.WriteLine("Creating Container: {0}\n", containerId);
            return await database.CreateContainerIfNotExistsAsync(containerId, "/Country");
        }
    }
}