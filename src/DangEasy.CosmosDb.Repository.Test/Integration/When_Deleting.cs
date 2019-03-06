using System;
using Xunit;

namespace DangEasy.CosmosDb.Repository.Test.Integration
{
    public class When_Deleting : BaseTestFixture
    {
        [Fact]
        public void Existing_Document_Has_Valid_Result()
        {
            // setup
            var document = _repository.CreateAsync(_profile).Result;

            var res = _repository.DeleteAsync(document.Id).Result;

            Assert.True(res);

            var retrievedModel = _repository.GetByIdAsync(document.Id).Result;
            Assert.Null(retrievedModel);
        }


        [Fact]
        public void NonExisting_Document_Causes_Exception()
        {
            Assert.Throws<AggregateException>(() => _repository.DeleteAsync(123).Result);
        }
    }
}
