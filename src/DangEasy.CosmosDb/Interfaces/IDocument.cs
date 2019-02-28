using System;
using Newtonsoft.Json;

namespace DangEasy.CosmosDb.Interfaces
{
    public interface IDocument
    {
        [JsonProperty("id")]
        string Id { get; set; }
    }
}
