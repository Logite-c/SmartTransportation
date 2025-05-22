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

        public RouteRule(int customRule)
        {
            this.customRule = customRule;
        }

        public int customRule = default;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(version);
            writer.Write(customRule);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out version);
            reader.Read(out customRule);
        }
    }
}
