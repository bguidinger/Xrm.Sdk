namespace Microsoft.Pfe.Xrm.Sdk.Converters
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Metadata;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Linq;

    public class EntityCollectionConverter : JsonConverter<EntityCollection>
    {
        private readonly EntityMetadata _metadata;

        public EntityCollectionConverter(EntityMetadata metadata)
        {
            _metadata = metadata;
        }
        public override EntityCollection ReadJson(JsonReader reader, Type objectType, EntityCollection existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);

            var collection = new EntityCollection();
            foreach (var token in obj["value"])
            {
                var entity = token.ToObject<Entity>(serializer);
                collection.Entities.Add(entity);
            }

            return collection;
        }

        public override void WriteJson(JsonWriter writer, EntityCollection value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
