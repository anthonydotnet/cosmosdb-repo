using System;
using DangEasy.CosmosDb.Repository.Test.Models;
using Xunit;

namespace DangEasy.CosmosDb.Repository.Test.Integration
{
    public class When_Creating : BaseTestFixture
    {
        [Fact]
        public void New_Document_Has_Valid_Result()
        {
            var res = _repository.CreateAsync(_profile).Result;
            Assert.NotEmpty(res.Id);
        }

        [Fact]
        public void Duplicate_Document_Causes_Exception()
        {
            var res = _repository.CreateAsync(_profile).Result;

            var duplicate = new Profile
            {
                Id = res.Id
            };

            Assert.Throws<AggregateException>(() => _repository.CreateAsync(duplicate).Result);
        }
    }
}
