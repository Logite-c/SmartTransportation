using Colossal.UI.Binding;

namespace SmartTransportation.Domain
{
    public class CustomRule : IJsonWritable
    {
        public Colossal.Hash128 RuleId { get; }
        public string RuleName { get; }
        public int Occupancy { get; }
        public int StdTicket { get; }
        public int MaxTicketInc { get; }
        public int MaxTicketDec { get; }
        public int MaxVehAdj { get; }
        public int MinVehAdj { get; }

        public CustomRule(Colossal.Hash128 ruleId, string ruleName, int occupancy, int stdTicket, int maxTicketInc, int maxTicketDec, int maxVehAdj, int minVehAdj)
        {
            RuleId = ruleId;
            RuleName = ruleName;
            Occupancy = occupancy;
            StdTicket = stdTicket;
            MaxTicketInc = maxTicketInc;
            MaxTicketDec = maxTicketDec;
            MaxVehAdj = maxVehAdj;
            MinVehAdj = minVehAdj;
        }

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(GetType().FullName);
            writer.PropertyName("ruleId");
            writer.Write(RuleId.ToString()); // Convert Hash128 to string
            writer.PropertyName("ruleName");
            writer.Write(RuleName);
            writer.PropertyName("occupancy");
            writer.Write(Occupancy);
            writer.PropertyName("stdTicket");
            writer.Write(StdTicket);
            writer.PropertyName("maxTicketInc");
            writer.Write(MaxTicketInc);
            writer.PropertyName("maxTicketDec");
            writer.Write(MaxTicketDec);
            writer.PropertyName("maxVehAdj");
            writer.Write(MaxVehAdj);
            writer.PropertyName("minVehAdj");
            writer.Write(MinVehAdj);
            writer.TypeEnd();
        }
    }
}