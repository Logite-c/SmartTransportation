using Colossal.Logging;
using Colossal.UI.Binding;
using Game.UI;
using Newtonsoft.Json;
using SmartTransportation.Bridge;
using SmartTransportation.Extensions;
using System;
using Colossal;

namespace SmartTransportation.Systems
{
    public partial class AddCustomRuleUISystem : ExtendedUISystemBase
    {
        private ILog _log;

        protected override void OnCreate()
        {
            base.OnCreate();

            _log = LogManager.GetLogger(
                $"{nameof(SmartTransportation)}.{nameof(AddCustomRuleUISystem)}");

            // Trigger from AddCustomRulePanel.tsx
            AddBinding(new TriggerBinding<AddCustomRule>(
                "smartTransportation",
                "addCustomRule",
                AddCustomRuleFromUI,
                new GenericUIReader<AddCustomRule>()
            ));
        }

        // Shape of the JSON payload sent from AddCustomRulePanel.tsx
        public class AddCustomRule
        {
            public string ruleName { get; set; } = string.Empty;
            public int occupancy { get; set; }
            public int stdTicket { get; set; }
            public int maxTicketInc { get; set; }
            public int maxTicketDec { get; set; }
            public int maxVehAdj { get; set; }
            public int minVehAdj { get; set; }
        }

        private void AddCustomRuleFromUI(AddCustomRule dto)
        {
            try
            {
                Hash128 ruleId = ManageRouteBridge.AddCustomRule();
                ManageRouteBridge.SetCustomRule(
                    ruleId,
                    dto.ruleName ?? string.Empty,
                    dto.occupancy,
                    dto.stdTicket,
                    dto.maxTicketInc,
                    dto.maxTicketDec,
                    dto.maxVehAdj,
                    dto.minVehAdj
                );
            }
            catch (Exception ex)
            {
                _log?.Error(ex, $"Error in {nameof(AddCustomRuleFromUI)}");
            }
            
        }
    }
}