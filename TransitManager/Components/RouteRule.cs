using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Colossal.Serialization.Entities;
using Game.Agents;

namespace SmartTransportation.Components
{
    public struct RouteRule : IComponentData, IQueryTypeParameter, ISerializable
    {
        public int version = 1;

        public RouteRule(int customRule, bool disabled)
        {
            this.customRule = customRule;
            this.disabled = disabled;
        }

        public int customRule = default;
        public bool disabled = false;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(version);
            writer.Write(customRule);
            writer.Write(disabled);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out version);
            reader.Read(out customRule);
            reader.Read(out disabled);
        }
    }
}
