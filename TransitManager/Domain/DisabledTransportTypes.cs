using Colossal.UI.Binding;

namespace SmartTransportation.Domain
{
    public class DisabledTransportTypes : IJsonWritable
    {
        public bool Bus { get; set; }
        public bool Tram { get; set; }
        public bool Subway { get; set; }
        public bool Train { get; set; }
        public bool Ship { get; set; }
        public bool Airplane { get; set; }
        public bool Ferry { get; set; }

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(Mod.Name + ".DisabledTransportTypes");
            writer.PropertyName("Bus");
            writer.Write(Bus);
            writer.PropertyName("Tram");
            writer.Write(Tram);
            writer.PropertyName("Subway");
            writer.Write(Subway);
            writer.PropertyName("Train");
            writer.Write(Train);
            writer.PropertyName("Ship");
            writer.Write(Ship);
            writer.PropertyName("Airplane");
            writer.Write(Airplane);
            writer.PropertyName("Ferry");
            writer.Write(Ferry);
            writer.TypeEnd();
        }

        public static DisabledTransportTypes FromSettings(Setting settings)
        {
            return new DisabledTransportTypes
            {
                Bus = settings.disable_bus,
                Tram = settings.disable_Tram,
                Subway = settings.disable_Subway,
                Train = settings.disable_Train,
                Ship = settings.disable_Ship,
                Airplane = settings.disable_Airplane,
                Ferry = settings.disable_Ferry
            };
        }
    }
}
