using System.Collections.Generic;
using Colossal.UI.Binding;

namespace SmartTransportation.Domain
{
    public class CustomRules : List<CustomRule>, IJsonWritable
    {
        public void Write(IJsonWriter writer)
        {
            writer.ArrayBegin(Count);
            foreach (CustomRule rule in this)
            {
                rule.Write(writer);
            }
            writer.ArrayEnd();
        }
    }
}