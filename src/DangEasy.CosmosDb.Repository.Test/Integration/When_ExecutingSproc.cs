using Xunit;
using System;
using Microsoft.Azure.Documents;

namespace DangEasy.CosmosDb.Repository.Test.Integration
{
    public class When_ExecutingSproc : BaseTestFixture, IDisposable
    {
        const string SprocName = "my_sproc";

        public When_ExecutingSproc()
        {
            //   var res = _repository.CreateAsync(_profile).Result;
            // force the database to be created
            _repository.GetAllAsync().Wait();
        }


        [Fact]
        public void Response_Is_Expected()
        {
            // setup
            var sproc = new StoredProcedure
            {
                Id = SprocName,
                Body = @"function (name){
                          var response = getContext().getResponse();
                          response.setBody('Hello');
                       }"
            };

            _client.CreateStoredProcedureAsync(_collectionUri, sproc).Wait();

            var res = _repository.ExecuteStoredProcedureAsync<string>(SprocName).Result;

            Assert.Equal("Hello", res);
        }


        [Fact]
        public void With_Params_Response_Is_Expected()
        {
            // setup
            var sproc = new StoredProcedure
            {
                Id = SprocName,
                Body = @"function (name){
                          var response = getContext().getResponse();
                          response.setBody('Hello ' + name);
                       }"
            };

            _client.CreateStoredProcedureAsync(_collectionUri, sproc).Wait();

            var res = _repository.ExecuteStoredProcedureAsync<string>(SprocName, "Milo", "Bill", "Karl", "Stephen").Result;

            Assert.Equal("Hello Milo", res);
        }


        [Fact]
        public void No_Response_Is_Expected()
        {
            // setup
            var sproc = new StoredProcedure
            {
                Id = SprocName,
                Body = @"function (){
                          // do something, then don't return
                       }"
            };

            _client.CreateStoredProcedureAsync(_collectionUri, sproc).Wait();

            var res = _repository.ExecuteStoredProcedureAsync<string>(SprocName).Result;

            Assert.True(res == string.Empty);
        }


        [Fact]
        public void Null_Response_Is_Expected()
        {
            // setup
            var sproc = new StoredProcedure
            {
                Id = SprocName,
                Body = @"function (){
                          // do something, then return null
                            var response = getContext().getResponse();
                            response.setBody(null);
                       }"
            };

            _client.CreateStoredProcedureAsync(_collectionUri, sproc).Wait();

            var res = _repository.ExecuteStoredProcedureAsync<string>(SprocName).Result;

            Assert.True(res == null);
        }


        [Fact]
        public void NonExistent_Sproc_Causes_Exception()
        {
            Assert.Throws<AggregateException>(() => _repository.ExecuteStoredProcedureAsync<string>(SprocName).Result);
        }
    }
}
