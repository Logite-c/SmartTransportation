using System.Collections.Generic;
using Colossal.UI.Binding;

namespace SmartTransportation.Domain
{
    public class RouteInfos : List<RouteInfo> , IJsonWritable
    {
        public void Write(IJsonWriter writer)
        {
            writer.ArrayBegin(Count);
            foreach (RouteInfo routeInfo in this)
            {
                routeInfo.Write(writer);
            }
            writer.ArrayEnd();
        }
    }
}