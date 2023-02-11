---
languages:
- csharp
products:
- azure
- azure-cosmos-db
- dotnet
page_type: sample
description: "This sample shows you how to use the Azure Cosmos DB service to bulk update data from a .NET console application."
---

**I created this repo being inspired by the [cosmos-dotnet-getting-started](https://github.com/Azure-Samples/cosmos-dotnet-getting-started)**

# Bulk updating data in Azure Cosmos DB with using a .NET console app
This sample shows you how to bulk update data in Azure Cosmos DB NoSQL API from a .NET console application.

## Running this sample

1. Before you can run this sample, you must have the following prerequisites:
	- An active Azure Cosmos DB account - If you don't have an account, refer to the [Create a database account](https://docs.microsoft.com/azure/cosmos-db/create-sql-api-dotnet#create-a-database-account) article.

1. Clone this repository using Git for Windows (http://www.git-scm.com/), or download the zip file.

```
git clone https://github.com/shoshii/cosmos-dotnet-bulk-update-sample
cd cosmos-dotnet-bulk-update-sample
```


1. Restore the project

```
cd CosmosBulkUpdateSample
dotnet restore
```

1. Retrieve the URI and PRIMARY KEY (or SECONDARY KEY) values from the Keys blade of your Azure Cosmos DB account in the Azure portal. For more information on obtaining endpoint & keys for your Azure Cosmos DB account refer to [View, copy, and regenerate access keys and passwords](https://docs.microsoft.com/en-us/azure/cosmos-db/manage-account#keys)

If you don't have an account, see [Create a database account](https://docs.microsoft.com/azure/cosmos-db/create-sql-api-dotnet#create-a-database-account) to set one up.

1. In the **App.config** file, located in the src directory, find **EndPointUri** and **PrimaryKey** and replace the placeholder values with the values obtained for your account.

    <add key="EndPointUri" value="~your Azure Cosmos DB endpoint here~" />
    <add key="PrimaryKey" value="~your auth key here~" />

1. You can now run and debug the application locally by `dotnet test`

```
dotnet test --logger "console;verbosity=detailed"
# execute specific tests
dotnet test --logger "console;verbosity=detailed" --filter FullyQualifiedName~testBulkUpdate
```

## About the code
The code included in this sample is intended to get you quickly started with a .NET console application that bulk updates data in Azure Cosmos DB.

## More information

- [Introducing Bulk support in the .NET SDK](https://devblogs.microsoft.com/cosmosdb/introducing-bulk-support-in-the-net-sdk/)
- [Azure Cosmos DB Partial Document Update: Getting Started](https://learn.microsoft.com/en-us/azure/cosmos-db/partial-document-update-getting-started?tabs=dotnet)
- [Bulk import data to Azure Cosmos DB for NoSQL account by using the .NET SDK](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/tutorial-dotnet-bulk-import)
- [Azure Cosmos DB .NET SDK Reference Documentation](https://docs.microsoft.com/dotnet/api/overview/azure/cosmosdb?view=azure-dotnet)
