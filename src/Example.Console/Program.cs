using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DangEasy.CosmosDb.Repository;
using Example.Console.Models;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;


namespace Example.Console
{
    internal class Program
    {
        internal Program()
        {
            var builder = new ConfigurationBuilder()
                           .SetBasePath(Directory.GetCurrentDirectory())
                           .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            Configuration = builder.Build();
        }


        public static IConfigurationRoot Configuration { get; set; }

        private static void Main(string[] args)
        {
            // Run demo
            var p = new Program();
            p.MainAsync().Wait();
        }

        internal async Task MainAsync()
        {
            string endpointUrl = Configuration["AppSettings:EndpointUrl"];
            string authorizationKey = Configuration["AppSettings:AuthorizationKey"];
            string databaseName = Configuration["AppSettings:MyDatabaseName"];

            // create the Azure DocumentDB client
            var client = new DocumentClient(new Uri(endpointUrl), authorizationKey);

            // ensure database is deleted
            DeleteDatabase(client, databaseName);

            // create repository 
            var repo = new DocumentDbRepository<Person>(client, databaseName);

            // output all persons in our database, nothing there yet
            await ShowPersonCollection(repo);

            // create a new person
            var matt = new Person
            {
                FirstName = "Matt",
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
            matt = await repo.CreateAsync(matt);

            var mattResult = await repo.GetByIdAsync(matt.Id);
            Debug.Assert(mattResult != null);
            System.Console.WriteLine("GetByIdAsync result: " + mattResult);


            // create another person
            var jack = new Person
            {
                FirstName = "Jack",
                LastName = "Smith",
                BirthDayDateTime = new DateTime(1990, 10, 10),
                PhoneNumbers = new Collection<PhoneNumber>()
            };

            // add jack to collection
            jack = await repo.CreateAsync(jack);
            var jackResult = await repo.GetByIdAsync(matt.Id);
            Debug.Assert(jackResult != null);
            System.Console.WriteLine("GetByIdAsync result: " + jackResult);


            // count all person documents
            var personsCount = await repo.CountAsync();
            Debug.Assert(personsCount == 2);
            System.Console.WriteLine($"personsCount: {personsCount}");

            // output person documents
            await ShowPersonCollection(repo);

            // count all jacks
            var jacksCount = await repo.CountAsync(p => p.FirstName == "Jack");
            Debug.Assert(jacksCount == 1);
            System.Console.WriteLine($"jacksCount: {jacksCount}");


            // modify Matt
            matt.BirthDayDateTime -= new TimeSpan(500, 0, 0, 0);
            matt.PhoneNumbers.RemoveAt(1);
            await repo.UpdateAsync(matt);

            mattResult = await repo.GetByIdAsync(matt.Id);
            Debug.Assert(mattResult.BirthDayDateTime == matt.BirthDayDateTime);
            Debug.Assert(mattResult.PhoneNumbers.First().Number == matt.PhoneNumbers.First().Number);

            // output Matt, now with just one phone number
            await ShowPersonCollection(repo);


            // Get documents by first name
            mattResult = await repo.FirstOrDefaultAsync(p => p.FirstName.Equals("Matt", StringComparison.OrdinalIgnoreCase));
            Debug.Assert(mattResult != null);
            System.Console.WriteLine("First Matt: " + mattResult);


            // query all the Smiths
            var smiths = (await repo.QueryAsync(p => p.LastName.Equals("Smith"))).ToList();
            Debug.Assert(smiths.Count == 1);
            System.Console.WriteLine($"Smith Count: {smiths.Count}");

            // use IQueryable, as for now supported expressions are 'Queryable.Where', 'Queryable.Select' & 'Queryable.SelectMany'
            var allSmithsPhones = (await repo.GetAllAsync()).SelectMany(p => p.PhoneNumbers).Select(p => p.Type).ToList();
            Debug.Assert(allSmithsPhones.Any());
            allSmithsPhones.ForEach(System.Console.WriteLine);



            // remove matt from collection
            await repo.DeleteAsync(matt.Id);

            // remove jack from collection
            await repo.DeleteAsync(jack.Id);

            // should output nothing
            await ShowPersonCollection(repo);

            DeleteDatabase(client, databaseName);
        }

        private static async Task ShowPersonCollection(DocumentDbRepository<Person> repo)
        {
            var persons = await repo.GetAllAsync();

            persons.ToList().ForEach(System.Console.WriteLine);
        }


        private void DeleteDatabase(DocumentClient client, string databaseName)
        {
            System.Console.WriteLine($"Deleting {databaseName}");

            var database = client.CreateDatabaseQuery()
                                    .Where(db => db.Id == databaseName)
                                    .ToArray()
                                    .FirstOrDefault();

            if (database != null)
            {
                client.DeleteDatabaseAsync(UriFactory.CreateDatabaseUri(databaseName)).Wait();
            }
        }
    }
}