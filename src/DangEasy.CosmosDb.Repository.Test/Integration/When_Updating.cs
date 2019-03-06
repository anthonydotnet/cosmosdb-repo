using Xunit;
using Newtonsoft.Json;
using DangEasy.CosmosDb.Repository.Test.Models;
using System;

namespace DangEasy.CosmosDb.Repository.Test.Integration
{
    public class When_Updating : BaseTestFixture
    {
        [Fact]
        public void Exiting_Document_Has_Valid_Result()
        {
            // setup
            var doc = _repository.CreateAsync(_profile).Result;

            var modifiedModel = JsonConvert.DeserializeObject<Profile>(JsonConvert.SerializeObject(doc)); // deep copy
            modifiedModel.Age = 40;
            modifiedModel.LastName = "DANG";
            modifiedModel.Location.City = "London";

            var res = _repository.UpdateAsync(modifiedModel).Result;

            Assert.Equal(doc.Id, res.Id);
            Assert.Equal(modifiedModel.Age, res.Age);
            Assert.Equal(modifiedModel.LastName, res.LastName);
            Assert.Equal(modifiedModel.Location.City, res.Location.City);
        }


        [Fact]
        public void NonExisting_Document_Causes_Exception()
        {
            // setup
            var nonExistingModel = new Profile
            {
                Id = "123"
            };

            Assert.Throws<AggregateException>(() => _repository.UpdateAsync(nonExistingModel).Result);
        }
    }
}
