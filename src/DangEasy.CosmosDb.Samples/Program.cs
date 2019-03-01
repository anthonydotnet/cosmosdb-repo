using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DangEasy.CosmosDb.Repository;
using DangEasy.CosmosDb.Samples.Model;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;

namespace DangEasy.CosmosDb.Samples
{
    internal class Program
    {
        internal Program()
        {
            var builder = new ConfigurationBuilder()
                           .SetBasePath(Directory.GetCurrentDirectory())
                           .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            Configuration = builder.Build();

            string endpointUrl = Configuration["AppSettings:EndpointUrl"];
            string authorizationKey = Configuration["AppSettings:AuthorizationKey"];

            // get the Azure DocumentDB client
            Client = new DocumentClient(new Uri(endpointUrl), authorizationKey);

        }

        public static DocumentClient Client { get; set; }

        public static IConfigurationRoot Configuration { get; set; }

        private static void Main(string[] args)
        {
            // Run demo
            var p = new Program();
            p.MainAsync().Wait();
        }

        internal async Task MainAsync()
        {
            string databaseName = Configuration["AppSettings:MyDatabaseName"];
            DeleteDatabase(databaseName);

            // create repository for persons and set Person.FullName property as identity field (overriding default Id property name)
            var repo = new DocumentDbRepository<Person>(Client, databaseName);

            // output all persons in our database, nothing there yet
            await PrintPersonCollection(repo);

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
            matt = await repo.CreateAsync(matt);

            // create another person
            Person jack = new Person
            {
                FirstName = "Jack",
                LastName = "Smith",
                BirthDayDateTime = new DateTime(1990, 10, 10),
                PhoneNumbers = new Collection<PhoneNumber>()
            };

            // add jack to collection
            jack = await repo.CreateAsync(jack);

            // should output person and his two phone numbers
            await PrintPersonCollection(repo);

            // change birth date
            matt.BirthDayDateTime -= new TimeSpan(500, 0, 0, 0);

            // remove landline phone number
            matt.PhoneNumbers.RemoveAt(1);

            // should update person
            await repo.UpdateAsync(matt);

            // should output Matt with just one phone number
            await PrintPersonCollection(repo);

            // get Matt by his Id
            Person justMatt = await repo.GetByIdAsync(matt.Id);
            Console.WriteLine("GetByIdAsync result: " + justMatt);

            // ... or by his first name
            Person firstMatt = await repo.FirstOrDefaultAsync(p => p.FirstName.Equals("matt", StringComparison.OrdinalIgnoreCase));
            Console.WriteLine("First: " + firstMatt);

            // query all the smiths
            var smiths = (await repo.WhereAsync(p => p.LastName.Equals("Smith"))).ToList();
            Console.WriteLine(smiths.Count);

            // use IQueryable, as for now supported expressions are 'Queryable.Where', 'Queryable.Select' & 'Queryable.SelectMany'
            var allSmithsPhones =
                (await repo.QueryAsync()).SelectMany(p => p.PhoneNumbers).Select(p => p.Type);
            foreach (var phone in allSmithsPhones)
            {
                Console.WriteLine(phone);
            }

            // count all persons
            var personsCount = await repo.CountAsync();

            // count all jacks
            var jacksCount = await repo.CountAsync(p => p.FirstName == "Jack");

            // remove matt from collection
            await repo.DeleteAsync(matt.Id);

            // remove jack from collection
            await repo.DeleteAsync(jack.Id);

            // should output nothing
            await PrintPersonCollection(repo);

            DeleteDatabase(databaseName);
        }

        private static async Task PrintPersonCollection(DocumentDbRepository<Person> repo)
        {
            IEnumerable<Person> persons = await repo.GetAllAsync();

            persons.ToList().ForEach(Console.WriteLine);
        }


        private void DeleteDatabase(string databaseName)
        {
            var database = Client.CreateDatabaseQuery().Where(db => db.Id == databaseName).ToArray().FirstOrDefault();
            if (database != null)
            {
                Client.DeleteDatabaseAsync(UriFactory.CreateDatabaseUri(databaseName)).Wait();
            }
        }
    }
}