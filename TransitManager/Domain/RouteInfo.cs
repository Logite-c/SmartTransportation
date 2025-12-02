
using Colossal.UI.Binding;

namespace SmartTransportation.Domain
{
    

    public class RouteInfo : IJsonWritable
    {
        public int routeNumber { get; set; }
        public string routeName { get; set; }
        public string transportType { get; set; }
        public string ruleName { get; set; }
        public Colossal.Hash128 ruleId { get; set; }
        
        public RouteInfo(int routeNumber, string routeName, string transportType, string ruleName, Colossal.Hash128 ruleId)
        {
            this.routeNumber = routeNumber;
            this.routeName = routeName;
            this.transportType = transportType;
            this.ruleName = ruleName;
            this.ruleId = ruleId;
        }

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(Mod.Name + ".RouteInfo");
            writer.PropertyName("routeNumber");
            writer.Write(routeNumber);
            writer.PropertyName("routeName");
            writer.Write(routeName);
            writer.PropertyName("transportType");
            writer.Write(transportType);
            writer.PropertyName("ruleName");
            writer.Write(ruleName);
            writer.PropertyName("ruleId");
            writer.Write(ruleId.ToString());
            writer.TypeEnd();
        }
    }
}