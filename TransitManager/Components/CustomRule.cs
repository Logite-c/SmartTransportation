using Colossal.Serialization.Entities;
using Game.Agents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;

namespace SmartTransportation.Components
{
    public struct CustomRule : IComponentData, IQueryTypeParameter, ISerializable
    {
        public int version = 1;
        public int ruleId;
        public FixedString64Bytes ruleName;
        public int occupancy;
        public int stdTicket;
        public int maxTicketInc;
        public int maxTicketDec; 
        public int maxVehAdj;
        public int minVehAdj;

        public CustomRule(int ruleId, FixedString64Bytes ruleName, int occupancy, int stdTicket, int maxTicketInc, int maxTicketDec, int maxVehAdj, int minVehAdj){
            this.ruleId = ruleId;
            this.ruleName = ruleName;
            this.occupancy = occupancy;
            this.stdTicket = stdTicket;
            this.maxTicketInc = maxTicketInc;
            this.maxTicketDec = maxTicketDec;
            this.maxVehAdj = maxVehAdj;
            this.minVehAdj = minVehAdj;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(version);
            writer.Write(ruleId);
            writer.Write(ruleName.ToString());
            writer.Write(occupancy);
            writer.Write(stdTicket);
            writer.Write(maxTicketInc);
            writer.Write(maxTicketDec);
            writer.Write(maxVehAdj);
            writer.Write(minVehAdj);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out version);
            reader.Read(out ruleId);
            reader.Read(out string ruleNameString); // Read as string
            ruleName = ruleNameString;              // Assign to FixedString64Bytes
            reader.Read(out occupancy);
            reader.Read(out stdTicket);
            reader.Read(out maxTicketInc);
            reader.Read(out maxTicketDec);
            reader.Read(out maxVehAdj);
            reader.Read(out minVehAdj);

        }
    }
}
