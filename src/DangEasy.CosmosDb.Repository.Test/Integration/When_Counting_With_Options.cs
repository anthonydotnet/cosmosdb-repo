using System;
using Xunit;
using Microsoft.Azure.Documents.Client;

namespace DangEasy.CosmosDb.Repository.Test.Integration
{
    public class When_Counting_With_Options : BaseTestFixture
    {
        public When_Counting_With_Options(): base(new RequestOptions(), new FeedOptions { EnableCrossPartitionQuery = true })
        {

        }


        [Fact]
        public void Count_Has_Valid_Result()
        {
            // setup
            _repository.CreateAsync(_profile).Wait();
            _repository.CreateAsync(new Models.Profile { FirstName = "Milo" }).Wait();

            var res = _repository.CountAsync().Result;

            Assert.Equal(2, res);
        }


        [Fact]
        public void CountByQuery_Has_Valid_Result()
        {
            // setup
            _repository.CreateAsync(_profile).Wait();
            _repository.CreateAsync(new Models.Profile { FirstName = "Milo" }).Wait();

            var res = _repository.CountAsync("SELECT * FROM Profile p WHERE p.firstName = 'Milo'").Result;

            Assert.Equal(1, res);
        }


        [Fact]
        public void CountByPredicate_Has_Valid_Result()
        {
            // setup
            _repository.CreateAsync(_profile).Wait();
            _repository.CreateAsync(new Models.Profile { FirstName = "Milo" }).Wait();

            var res = _repository.CountAsync(x => x.FirstName == "Milo").Result;

            Assert.True(res == 1);
        }
    }
}
