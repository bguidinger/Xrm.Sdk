namespace Microsoft.Pfe.Xrm.Sdk.Converters
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Metadata;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Linq;

    public class EntityConverter : JsonConverter<Entity>
    {
        private readonly EntityMetadata _metadata;

        public EntityConverter(EntityMetadata metadata)
        {
            _metadata = metadata;
        }
        public override Entity ReadJson(JsonReader reader, Type objectType, Entity existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);

            var collection = new EntityCollection();

            var entityId = new Guid(obj[_metadata.PrimaryIdAttribute].ToString());
            var entity = new Entity(_metadata.LogicalName, entityId);

            foreach (var token in obj.Properties())
            {
                var attribute = _metadata.Attributes.SingleOrDefault(x => x.LogicalName == token.Name);
                if (attribute != null)
                {
                    entity[attribute.LogicalName] = (string)token.Value;
                }
            }

            return entity;
        }

        public override void WriteJson(JsonWriter writer, Entity value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
