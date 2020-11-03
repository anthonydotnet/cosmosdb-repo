using System;
using System.IO;
using System.Linq;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using Xunit;
using DangEasy.CosmosDb.Repository.Test.Models;

namespace DangEasy.CosmosDb.Repository.Test.Integration
{
    public class BaseTestFixture : IDisposable
    {
        IConfiguration _config = new ConfigurationBuilder()
                                       .SetBasePath(Directory.GetCurrentDirectory())
                                       .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();

        protected DocumentClient _client;
        protected string _databaseName;
        protected DocumentDbRepository<Profile> _repository;
        protected Profile _profile;
        protected Uri _collectionUri;

        public BaseTestFixture(RequestOptions requestOptions = null, FeedOptions feedOptions = null)
        {
            InitRepository(requestOptions, feedOptions);

            // ensure db is deleted before starting
            DeleteDatabase();

            // set up model
            _profile = new Profile
            {
                Id = "Model.1",
                FirstName = "Anthony",
                LastName = "Dang",
                Age = 39,
                Created = new DateTime(2019, 1, 1),
                Updated = new DateTime(2019, 1, 1),
                Location = new Location
                {
                    City = "Sydney",
                    Latitude = 33.8688,
                    Longitude = 151.2093
                }
            };
        }


        public void Dispose()
        {
            DeleteDatabase();
        }


        private void InitRepository(RequestOptions requestOptions, FeedOptions feedOptions)
        {
            string endpointUrl = _config["AppSettings:EndpointUrl"];
            string authorizationKey = _config["AppSettings:AuthorizationKey"];
            _databaseName = _config["AppSettings:DatabaseName"];

            Assert.NotNull(endpointUrl);
            Assert.NotNull(authorizationKey);
            Assert.NotNull(_databaseName);

            _collectionUri = UriFactory.CreateDocumentCollectionUri(_databaseName, typeof(Profile).Name);
            _client = new DocumentClient(new Uri(endpointUrl), authorizationKey);
            _repository = new DocumentDbRepository<Profile>(_client, _databaseName);
        }


        private void DeleteDatabase()
        {
            var database = _client.CreateDatabaseQuery()
                                    .Where(db => db.Id == _databaseName)
                                    .ToArray()
                                    .FirstOrDefault();

            if (database != null)
            {
                Console.WriteLine($"Deleting {_databaseName}");

                _client.DeleteDatabaseAsync(UriFactory.CreateDatabaseUri(_databaseName)).Wait();
            }
        }
    }
}
