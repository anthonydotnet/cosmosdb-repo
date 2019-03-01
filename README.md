Simple repository pattern for [Cosmos DB](https://azure.microsoft.com/en-us/services/cosmos-db/) for dotnet Standard 2.0. 

[![Nuget](https://img.shields.io/badge/nuget-0.1.0-blue.svg?maxAge=3600)](https://www.nuget.org/packages/DangEasy.CosmosDB.Repository)

Originally forked from [https://github.com/Crokus/cosmosdb-repo](https://github.com/Crokus/cosmosdb-repo) 


# Installation

Use NuGet to install the [package](https://www.nuget.org/packages/DangEasy.CosmosDB.Repository/).

```
PM> Install-Package DangEasy.CosmosDB.Repository
```

# Getting started

## Step 1: Get Azure CosmosDB client

Before you can play with CosmosDB you need to create the Azure Client by passing your endopointUrl and  authorizationKey (primary).

```csharp
internal class Program
{
    public DocumentClient Client { get; set; }

	private static void Main(string[] args)
	{		
		// get the Azure DocumentDB client
		Client = init.GetClient(<EndpointUrl>, <AuthorizationKey>);

        // Run demo
        var p = new Program();
        p.MainAsync().Wait();
	}
}    
```

## Step 2: Create Repository and use it with your POCO objects

With the client in place create a repository providing database name and collection name. If you do not pass in a collection name, then the name of the Type will be used. In the example below the collection will be called Person. 

Now it's really easy to do the CRUD operations:

```csharp
public async Task MainAsync(string[] args)
{
	// create repository for persons
	var repo = new DocumentDbRepository<Person>(Client, <MyDatabaseName>);

	// create a new person
	Person matt = new Person
	{
		FirstName = "m4tt",
		LastName = "TBA",
		BirthDayDateTime = new DateTime(1990, 10, 10),
		PhoneNumbers =
			new Collection<PhoneNumber>
			{
				new PhoneNumber {Number = "555", Type = "Mobile"},
				new PhoneNumber {Number = "777", Type = "Landline"}
			}
	};

	// add person to database's collection (if collection doesn't exist it will be created and named as class name -it's a convenction, that can be configured during initialization of the repository)
	matt = await repo.AddOrUpdateAsync(matt);

	// create another person
	Person jack = new Person
	{
		FirstName = "Jack",
		LastName = "Smith",
		BirthDayDateTime = new DateTime(1990, 10, 10),
		PhoneNumbers = new Collection<PhoneNumber>()
	};

	// add jack to collection
	jack = await repo.AddOrUpdateAsync(jack);

	// update first name
	matt.FirstName = "Matt";

	// add last name
	matt.LastName = "Smith";

	// remove landline phone number
	matt.PhoneNumbers.RemoveAt(1);

	// should update person
	await repo.AddOrUpdateAsync(matt);

	// get Matt by his Id
	Person justMatt = await repo.GetByIdAsync(matt.Id);

	// ... or by his first name
	Person firstMatt = await repo.FirstOrDefaultAsync(p => p.FirstName.Equals("matt", StringComparison.OrdinalIgnoreCase));

	// query all the smiths
	var smiths = (await repo.WhereAsync(p => p.LastName.Equals("Smith", StringComparison.OrdinalIgnoreCase))).ToList();
	
	// count all persons
    var personsCount = await repo.CountAsync();

    // count all jacks
    var jacksCount = await repo.CountAsync(p => p.FirstName == "Jack");
	
	// remove matt from collection
	await repo.RemoveAsync(matt.Id);

	// remove jack from collection
	await repo.RemoveAsync(jack.Id);
}
```

Full example can be found [here](https://github.com/anthonydotnet/cosmosdb-repo/blob/master/src/DangEasy.CosmosDB.Repository.Samples/Program.cs).

# License

cosmosdb-repo is provided under the MIT license.
