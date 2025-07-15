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
        public int version = 2;

        public RouteRule(Colossal.Hash128 customRule)
        {
            this.customRule = customRule;
        }

        public Colossal.Hash128 customRule = default;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(version);
            writer.Write(customRule);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out version);
            if (version < 2)
            {
                reader.Read(out int customRuleInt);
                customRule = new Colossal.Hash128((uint)customRuleInt, 0, 0, 0); // customRule was an int in version 1
            }
            else
            {
                reader.Read(out customRule);
            }
        }
    }
}
