using Newtonsoft.Json;
using System;

namespace DangEasy.CosmosDb.Repository.Test.Models
{
    public class Profile
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("accountId")]
        public string AccountId { get; set; }

        [JsonProperty("firstName")]
        public string FirstName { get; set; }

        [JsonProperty("lastName")]
        public string LastName { get; set; }

        [JsonProperty("age")]
        public int Age { get; set; }

        [JsonProperty("location")]
        public Location Location { get; set; }

        [JsonProperty("created")]
        public DateTime Created { get; set; }

        [JsonProperty("updated")]
        public DateTime Updated { get; set; }
    }
}
