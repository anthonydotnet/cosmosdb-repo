using System.Linq;
using Xunit;

namespace DangEasy.CosmosDb.Repository.Test.Integration
{
    public class When_Getting : BaseTestFixture
    {
        [Fact]
        public void GetById_Has_Valid_Result()
        {
            // setup
            var document = _repository.CreateAsync(_profile).Result;

            var res = _repository.GetByIdAsync(document.Id).Result;
            Assert.NotNull(res);
        }


        [Fact]
        public void GetAll_Has_Valid_Result()
        {
            // setup
            _repository.CreateAsync(_profile).Wait();
            _repository.CreateAsync(new Models.Profile { FirstName = "Milo" }).Wait();

            var res = _repository.GetAllAsync().Result;

            Assert.Equal(2, res.Count());
        }


        [Fact]
        public void QueryBySql_Has_Valid_Result()
        {
            // setup
            _repository.CreateAsync(_profile).Wait();
            _repository.CreateAsync(new Models.Profile { FirstName = "Milo" }).Wait();

            var res = _repository.QueryAsync("SELECT * FROM Profile p WHERE p.firstName = 'Milo'").Result;

            var resAsList = res.ToList();
            Assert.Equal("Milo", resAsList.First().FirstName);
        }


        [Fact]
        public void QueryByPredicate_Has_Valid_Result()
        {
            // setup
            _repository.CreateAsync(_profile).Wait();
            _repository.CreateAsync(new Models.Profile { FirstName = "Milo" }).Wait();

            var res = _repository.QueryAsync(x => x.FirstName == "Milo").Result;

            var resAsList = res.ToList();
            var count = resAsList.Count;
            Assert.Equal(1, count);
            Assert.Equal("Milo", resAsList.First().FirstName);
        }
    }
}
