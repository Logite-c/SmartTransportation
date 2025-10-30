using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.UI.Widgets;
using System.Collections.Generic;
using Unity.Entities;

namespace SmartTransportation
{
    [FileLocation(nameof(SmartTransportation))]
    [SettingsUIGroupOrder(BusGroup, TramGroup, SubwayGroup, TrainGroup, ShipGroup, AirplaneGroup, FerryGroup, TaxiGroup, CustomGroup, SettingsGroup, ChirpSettings)]
    [SettingsUIShowGroupName(BusGroup, TramGroup, SubwayGroup, TrainGroup, ShipGroup, AirplaneGroup, FerryGroup, CustomGroup, TaxiGroup, SettingsGroup, ChirpSettings)]

    public class Setting : ModSetting
    {
        public const string TransitSection = "TransitSection";
        public const string TaxiSection = "TaxiSection";
        public const string CustomSection = "CustomSection";
        public const string SettingsSection = "SettingsSection";
        public const string BusGroup = "BusGroup";
        public const string TramGroup = "TramGroup";
        public const string SubwayGroup = "SubwayGroup";
        public const string TrainGroup = "TrainGroup";
        public const string TaxiGroup = "TaxiGroup";
        public const string CustomGroup = "CustomGroup";
        public const string SettingsGroup = "SettingsGroup";
        public const string ShipGroup = "ShipGroup";
        public const string AirplaneGroup = "AirplaneGroup";
        public const string FerryGroup = "FerryGroup";
        public const string ChirpSettings = "ChirpSettings";


        public Setting(IMod mod) : base(mod)
        {
            if (target_occupancy_bus == 0) SetDefaults();
        }

        public override void SetDefaults()
        {
            disable_bus = false;
            target_occupancy_bus = 30;
            max_ticket_increase_bus = 20;
            max_ticket_discount_bus = 40;
            disable_Tram = false;
            target_occupancy_Tram = 40;
            max_ticket_increase_Tram = 30;
            max_ticket_discount_Tram = 20;
            disable_Subway = false;
            target_occupancy_Subway = 50;
            max_ticket_increase_Subway = 40;
            max_ticket_discount_Subway = 20;
            disable_Train = false;
            target_occupancy_Train = 60;
            max_ticket_increase_Train = 40;
            max_ticket_discount_Train = 20;
            standard_ticket_bus = 8;
            //disable_Taxi = false;
            //target_occupancy_Taxi = 70;
            //max_ticket_increase_Taxi = 100;
            //max_ticket_discount_Taxi = 20;
            //standard_ticket_Taxi = 20;
            standard_ticket_Tram = 8;
            standard_ticket_Subway = 9;
            standard_ticket_Train = 10;
            waiting_time_weight = 1f;
            threshold = 10;
            debug = false;
            updateFreq = UpdateFreqEnum.min45;
            max_vahicles_adj_bus = 0;
            min_vahicles_adj_bus = 25;
            max_vahicles_adj_Tram = 0;
            min_vahicles_adj_Tram = 30;
            max_vahicles_adj_Subway = 0;
            min_vahicles_adj_Subway = -20;
            max_vahicles_adj_Train = 0;
            min_vahicles_adj_Train = 30;

            disable_Ship = false;
            target_occupancy_Ship = 65;
            max_ticket_increase_Ship = 40;
            max_ticket_discount_Ship = 20;
            standard_ticket_Ship = 12;
            max_vahicles_adj_Ship = 50;
            min_vahicles_adj_Ship = 30;

            disable_Airplane = false;
            target_occupancy_Airplane = 70;
            max_ticket_increase_Airplane = 60;
            max_ticket_discount_Airplane = 30;
            standard_ticket_Airplane = 20;
            max_vahicles_adj_Airplane = 30;
            min_vahicles_adj_Airplane = 40;

            disable_Ferry = false;
            target_occupancy_Ferry = 40;
            max_ticket_increase_Ferry = 30;
            max_ticket_discount_Ferry = 30;
            standard_ticket_Ferry = 8;
            max_vahicles_adj_Ferry = 30;
            min_vahicles_adj_Ferry = 40;

            disable_chirps = false;
            busy_stop_enter_pct = 70;
            busy_stop_exit_pct = 55;

        }

        [SettingsUISection(TransitSection, BusGroup)]
        public bool disable_bus { get; set; }

        [SettingsUISlider(min = 10, max = 90, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, BusGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_bus))]
        public int target_occupancy_bus { get; set; }

        [SettingsUISlider(min = 0, max = 30, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(TransitSection, BusGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_bus))]
        public int standard_ticket_bus { get; set; }

        [SettingsUISlider(min = 0, max = 300, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, BusGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_bus))]
        public int max_ticket_increase_bus { get; set; }

        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, BusGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_bus))]
        public int max_ticket_discount_bus { get; set; }

        [SettingsUISlider(min = -50, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, BusGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_bus))]
        public int max_vahicles_adj_bus { get; set; }

        [SettingsUISlider(min = -50, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, BusGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_bus))]
        public int min_vahicles_adj_bus { get; set; }

        [SettingsUISection(TransitSection, TramGroup)]
        public bool disable_Tram { get; set; }

        [SettingsUISlider(min = 10, max = 90, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, TramGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Tram))]
        public int target_occupancy_Tram { get; set; }

        [SettingsUISlider(min = 0, max = 30, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(TransitSection, TramGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Tram))]
        public int standard_ticket_Tram { get; set; }

        [SettingsUISlider(min = 0, max = 300, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, TramGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Tram))]
        public int max_ticket_increase_Tram { get; set; }

        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, TramGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Tram))]
        public int max_ticket_discount_Tram { get; set; }

        [SettingsUISlider(min = -50, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, TramGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Tram))]
        public int max_vahicles_adj_Tram { get; set; }

        [SettingsUISlider(min = -50, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, TramGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Tram))]
        public int min_vahicles_adj_Tram { get; set; }

        [SettingsUISection(TransitSection, SubwayGroup)]
        public bool disable_Subway { get; set; }

        [SettingsUISlider(min = 10, max = 90, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, SubwayGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Subway))]
        public int target_occupancy_Subway { get; set; }

        [SettingsUISlider(min = 0, max = 30, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(TransitSection, SubwayGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Subway))]
        public int standard_ticket_Subway { get; set; }

        [SettingsUISlider(min = 0, max = 300, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, SubwayGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Subway))]
        public int max_ticket_increase_Subway { get; set; }

        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, SubwayGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Subway))]
        public int max_ticket_discount_Subway { get; set; }

        [SettingsUISlider(min = -50, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, SubwayGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Subway))]
        public int max_vahicles_adj_Subway { get; set; }

        [SettingsUISlider(min = -50, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, SubwayGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Subway))]
        public int min_vahicles_adj_Subway { get; set; }

        [SettingsUISection(TransitSection, TrainGroup)]
        public bool disable_Train { get; set; }

        [SettingsUISlider(min = 10, max = 90, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, TrainGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Train))]
        public int target_occupancy_Train { get; set; }

        [SettingsUISlider(min = 0, max = 30, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(TransitSection, TrainGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Train))]
        public int standard_ticket_Train { get; set; }

        [SettingsUISlider(min = 0, max = 300, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, TrainGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Train))]
        public int max_ticket_increase_Train { get; set; }

        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, TrainGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Train))]
        public int max_ticket_discount_Train { get; set; }

        [SettingsUISlider(min = -50, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, TrainGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Train))]
        public int max_vahicles_adj_Train { get; set; }

        [SettingsUISlider(min = -50, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, TrainGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Train))]
        public int min_vahicles_adj_Train { get; set; }

        [SettingsUISection(TransitSection, ShipGroup)]
        public bool disable_Ship { get; set; }

        [SettingsUISlider(min = 10, max = 90, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, ShipGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Ship))]
        public int target_occupancy_Ship { get; set; }

        [SettingsUISlider(min = 0, max = 30, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(TransitSection, ShipGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Ship))]
        public int standard_ticket_Ship { get; set; }

        [SettingsUISlider(min = 0, max = 300, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, ShipGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Ship))]
        public int max_ticket_increase_Ship { get; set; }

        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, ShipGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Ship))]
        public int max_ticket_discount_Ship { get; set; }

        [SettingsUISlider(min = -50, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, ShipGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Ship))]
        public int max_vahicles_adj_Ship { get; set; }

        [SettingsUISlider(min = -50, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, ShipGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Ship))]
        public int min_vahicles_adj_Ship { get; set; }
        [SettingsUISection(TransitSection, AirplaneGroup)]
        public bool disable_Airplane { get; set; }

        [SettingsUISlider(min = 10, max = 90, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, AirplaneGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Airplane))]
        public int target_occupancy_Airplane { get; set; }

        [SettingsUISlider(min = 0, max = 30, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(TransitSection, AirplaneGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Airplane))]
        public int standard_ticket_Airplane { get; set; }

        [SettingsUISlider(min = 0, max = 300, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, AirplaneGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Airplane))]
        public int max_ticket_increase_Airplane { get; set; }

        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, AirplaneGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Airplane))]
        public int max_ticket_discount_Airplane { get; set; }

        [SettingsUISlider(min = -50, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, AirplaneGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Airplane))]
        public int max_vahicles_adj_Airplane { get; set; }

        [SettingsUISlider(min = -50, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, AirplaneGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Airplane))]
        public int min_vahicles_adj_Airplane { get; set; }

        [SettingsUISection(TransitSection, FerryGroup)]
        public bool disable_Ferry { get; set; }

        [SettingsUISlider(min = 10, max = 90, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, FerryGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Ferry))]
        public int target_occupancy_Ferry { get; set; }

        [SettingsUISlider(min = 0, max = 30, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(TransitSection, FerryGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Ferry))]
        public int standard_ticket_Ferry { get; set; }

        [SettingsUISlider(min = 0, max = 300, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, FerryGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Ferry))]
        public int max_ticket_increase_Ferry { get; set; }

        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, FerryGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Ferry))]
        public int max_ticket_discount_Ferry { get; set; }

        [SettingsUISlider(min = -50, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, FerryGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Ferry))]
        public int max_vahicles_adj_Ferry { get; set; }

        [SettingsUISlider(min = -50, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, FerryGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Ferry))]
        public int min_vahicles_adj_Ferry { get; set; }

        [SettingsUISlider(min = 0, max = 2, step = 0.1f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(SettingsSection, SettingsGroup)]
        public float waiting_time_weight { get; set; }

        [SettingsUISlider(min = 5, max = 25, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(SettingsSection, SettingsGroup)]
        public float threshold { get; set; }

        [SettingsUISection(SettingsSection, SettingsGroup)]
        public UpdateFreqEnum updateFreq { get; set; }

        [SettingsUISection(SettingsSection, SettingsGroup)]
        public bool debug { get; set; }

        [SettingsUISection(SettingsSection, ChirpSettings)]
        public bool disable_chirps { get; set; }

        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(SettingsSection, ChirpSettings)]
        public float busy_stop_enter_pct { get; set; }

        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(SettingsSection, ChirpSettings)]
        public float busy_stop_exit_pct { get; set; }

        public enum UpdateFreqEnum
        {
            min45 = 32,
            min22 = 64,
            min90 = 16
        }

        [SettingsUIButton]
        [SettingsUISection(SettingsSection, SettingsGroup)]
        public bool Button
        {
            set
            {
                SetDefaults();
            }

        }


    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Smart Transportation" },
                { m_Setting.GetOptionTabLocaleID(Setting.TransitSection), "Transit" },
                { m_Setting.GetOptionTabLocaleID(Setting.TaxiSection), "Taxi" },
                { m_Setting.GetOptionTabLocaleID(Setting.CustomSection), "Custom Rules" },
                { m_Setting.GetOptionTabLocaleID(Setting.SettingsSection), "Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.BusGroup), "Bus Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.TramGroup), "Tram Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.SubwayGroup), "Subway Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.TrainGroup), "Train Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.AirplaneGroup), "Airplane Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ShipGroup), "Ship Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.FerryGroup), "Ferry Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.TaxiGroup), "Taxi Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.CustomGroup), "Custom Rules Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.SettingsGroup), "Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ChirpSettings), "Chirps" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.debug)), "Write Transit Information to Log" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.debug)), "Writes information used to make decision on ticket price and frequency for each route, such as transit occupancy and number of vehicles, to log." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.updateFreq)), "Update Frequency (In-game minutes)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.updateFreq)), "How frequent Smart Transportation will evaluate each route and decide to update vehicles or ticket prices. Time is in in-game minutes. Note that if you are using the slow feature from the Realistic Trip mods, this time is based on the vanilla game, with that feature you need to divide this time with the slow time factor to get the actual time with that mod." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.waiting_time_weight)), "Waiting Time Weight" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.waiting_time_weight)), "When this mod calculates target occupancy for each route, it will take into account the passengers that are waiting at a station stop. This weight is applied to the number of passengers waiting when doing this calculation. The assumption is that when those passengers board, some passengers that are already in the vehicle will deboard." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.threshold)), "Target Occupancy Threshold" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.threshold)), "Number of percentage points within the target occupancy that will be considered when checking if the target occupancy has been met." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_bus)), "Disable Bus Dynamic Pricing & Frequency" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.target_occupancy_bus)), "Target Occupancy" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.standard_ticket_bus)), "Standard Ticket Price" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_ticket_increase_bus)), "Max. Ticket Increase" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_ticket_discount_bus)), "Max. Ticket Discount" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_vahicles_adj_bus)), "Max. Vehicle Increase" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.min_vahicles_adj_bus)), "Min. Vehicle Decrease" },

                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_bus)), "Disable bus daynamic pricing and frequency and use standard values for tickets and frequency. " },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.target_occupancy_bus)), "Target occupancy for buses. The mod will change ticket prices and vehicle frequency to try to reach this occupancy target." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.standard_ticket_bus)), "Standard ticket price. Vanilla value is 8." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_ticket_increase_bus)), "Maximum ticket increase. Ticket prices will increase at peak hours to generate more revenue and discourage cims to use transit at rush hours." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_ticket_discount_bus)), "Maximum ticket decrease. Ticket prices will decrease at off-peak hours to attract more passengers." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_vahicles_adj_bus)), "Increase the maximum allowed number of vehicles by this percentage." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.min_vahicles_adj_bus)), "Decrease the minimum allowed number of vehicles by this percentage." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_Tram)), "Disable Tram Dynamic Pricing & Frequency" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.target_occupancy_Tram)), "Target Occupancy" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.standard_ticket_Tram)), "Standard Ticket Price" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_ticket_increase_Tram)), "Max. Ticket Increase" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_ticket_discount_Tram)), "Max. Ticket Discount" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_vahicles_adj_Tram)), "Max. Vehicle Increase" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.min_vahicles_adj_Tram)), "Min. Vehicle Decrease" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_Airplane)), "Disable Airplane Dynamic Pricing & Frequency" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.target_occupancy_Airplane)), "Target Occupancy" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.standard_ticket_Airplane)), "Standard Ticket Price" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_ticket_increase_Airplane)), "Max. Ticket Increase" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_ticket_discount_Airplane)), "Max. Ticket Discount" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_vahicles_adj_Airplane)), "Max. Vehicle Increase" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.min_vahicles_adj_Airplane)), "Min. Vehicle Decrease" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_Ship)), "Disable Ship Dynamic Pricing & Frequency" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.target_occupancy_Ship)), "Target Occupancy" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.standard_ticket_Ship)), "Standard Ticket Price" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_ticket_increase_Ship)), "Max. Ticket Increase" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_ticket_discount_Ship)), "Max. Ticket Discount" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_vahicles_adj_Ship)), "Max. Vehicle Increase" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.min_vahicles_adj_Ship)), "Min. Vehicle Decrease" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_Ferry)), "Disable Ferry Dynamic Pricing & Frequency" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.target_occupancy_Ferry)), "Target Occupancy" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.standard_ticket_Ferry)), "Standard Ticket Price" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_ticket_increase_Ferry)), "Max. Ticket Increase" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_ticket_discount_Ferry)), "Max. Ticket Discount" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_vahicles_adj_Ferry)), "Max. Vehicle Increase" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.min_vahicles_adj_Ferry)), "Min. Vehicle Decrease" },

                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_Tram)), "Disable tram daynamic pricing and frequency and use standard values for tickets and frequency. " },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.target_occupancy_Tram)), "Target occupancy for Trams. The mod will change ticket prices and vehicle frequency to try to reach this occupancy target." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.standard_ticket_Tram)), "Standard ticket price. Vanilla value is 8." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_ticket_increase_Tram)), "Maximum ticket increase. Ticket prices will increase at peak hours to generate more revenue and discourage cims to use transit at rush hours." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_ticket_discount_Tram)), "Maximum ticket decrease. Ticket prices will decrease at off-peak hours to attract more passengers." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_vahicles_adj_Tram)), "Increase the maximum allowed number of vehicles by this percentage." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.min_vahicles_adj_Tram)), "Decrease the minimum allowed number of vehicles by this percentage." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_Subway)), "Disable Subway Dynamic Pricing & Frequency" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.target_occupancy_Subway)), "Target Occupancy" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.standard_ticket_Subway)), "Standard Ticket Price" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_ticket_increase_Subway)), "Max. Ticket Increase" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_ticket_discount_Subway)), "Max. Ticket Discount" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_vahicles_adj_Subway)), "Max. Vehicle Increase" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.min_vahicles_adj_Subway)), "Min. Vehicle Decrease" },

                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_Subway)), "Disable subway daynamic pricing and frequency and use standard values for tickets and frequency. " },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.target_occupancy_Subway)), "Target occupancy for Subways. The mod will change ticket prices and vehicle frequency to try to reach this occupancy target." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.standard_ticket_Subway)), "Standard ticket price. Vanilla value is 8." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_ticket_increase_Subway)), "Maximum ticket increase. Ticket prices will increase at peak hours to generate more revenue and discourage cims to use transit at rush hours." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_ticket_discount_Subway)), "Maximum ticket decrease. Ticket prices will decrease at off-peak hours to attract more passengers." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_vahicles_adj_Subway)), "Increase the maximum allowed number of vehicles by this percentage." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.min_vahicles_adj_Subway)), "Decrease the minimum allowed number of vehicles by this percentage." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_Train)), "Disable Train Dynamic Pricing & Frequency" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.target_occupancy_Train)), "Target Occupancy" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.standard_ticket_Train)), "Standard Ticket Price" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_ticket_increase_Train)), "Max. Ticket Increase" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_ticket_discount_Train)), "Max. Ticket Discount" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_vahicles_adj_Train)), "Max. Vehicle Increase" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.min_vahicles_adj_Train)), "Min. Vehicle Decrease" },

                { m_Setting.GetEnumValueLocaleID(Setting.UpdateFreqEnum.min22), "22" },
                { m_Setting.GetEnumValueLocaleID(Setting.UpdateFreqEnum.min45), "45" },
                { m_Setting.GetEnumValueLocaleID(Setting.UpdateFreqEnum.min90), "90" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Button)), "Reset Settings" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Button)), $"Reset settings to default values" },
                
                // --- SmartTransit chirp-related settings ------------------------------------
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_chirps)), "Disable Chirps" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_chirps)),  "Do not send in-game chirp notifications from Smart Transportation." },
                
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.busy_stop_enter_pct)), "Busy Stop Alert Threshold" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.busy_stop_enter_pct)),  "Trigger a chirp when the busiest stop’s waiting passengers reach this percentage of a typical vehicle’s capacity." },
                
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.busy_stop_exit_pct)),  "Busy Stop Alert Clear Level" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.busy_stop_exit_pct)),   "Clear (stop) the alert once the waiting passengers fall below this percentage of a typical vehicle’s capacity." },


                { "chirp.mod_name",      "SmartTransportation Mod" },
                // Template used in messages to show the line label (type + number)
                { "chirp.line_label",    "{type} Line {number}" },
                
                // When a single stop is crowded relative to vehicle capacity
                // Variables: line_label, waiting, enter_pct, vehicle_cap
                { "chirp.stop_busy",
                  "{line_label}: Stop {LINK_1} is very busy — {waiting} passengers waiting." },
                



            };
        }

        public void Unload()
        {

        }

    }
}
