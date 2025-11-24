using Colossal.Logging;
using Colossal.UI.Binding;
using Game.UI;
using Newtonsoft.Json;
using SmartTransportation.Bridge;
using SmartTransportation.Extensions;
using System;
using System.Linq;
using System.Text;
using Unity.Entities;

namespace SmartTransportation.Systems
{
    // If you have an ExtendedUISystem type, replace UISystemBase with that:
    // public class SmartTransportationUISystem : ExtendedUISystem
    public partial class SmartTransportationUISystem : ExtendedUISystemBase
    {
        private ILog _log;
        private GetterValueBinding<string> _customRulesJsonBinding;
        private GetterValueBinding<string> _routesJsonBinding;  // NEW
        private TriggerBinding<string> _setRouteRuleForRouteTrigger;

        private sealed class RouteRuleSelectionDto
        {
            [JsonProperty("routeNumber")]
            public int RouteNumber { get; set; }

            [JsonProperty("transportType")]
            public string TransportType { get; set; }

            [JsonProperty("ruleId")]
            public string RuleId { get; set; }
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            _log = LogManager.GetLogger(
                $"{nameof(SmartTransportation)}.{nameof(SmartTransportationUISystem)}");

            // 1. Custom rules JSON binding
            _customRulesJsonBinding = new GetterValueBinding<string>(
                "smartTransportation",
                "customRulesJson",
                GetCustomRulesJson
            );
            AddBinding(_customRulesJsonBinding);

            // 2. Routes JSON binding
            _routesJsonBinding = new GetterValueBinding<string>(
                "smartTransportation",
                "routesJson",
                GetRoutesJson          // ← new method below
            );
            AddBinding(_routesJsonBinding);

            // 3. Trigger from AddCustomRulePanel.tsx
            AddBinding(new TriggerBinding<string>(
                "smartTransportation",
                "addCustomRule",        // trigger("smartTransportation", "addCustomRule", ...)
                AddCustomRuleFromUI,    // ← new handler below
                null
            ));

            // NEW: trigger for route rule selection
            _setRouteRuleForRouteTrigger = new TriggerBinding<string>(
                "smartTransportation",
                "setRouteRuleForRoute",
                SetRouteRuleForRouteFromUI
            );
            AddBinding(_setRouteRuleForRouteTrigger);
        }

        private void SetRouteRuleForRouteFromUI(string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json))
                {
                    _log.Info("SetRouteRuleForRouteFromUI called with empty payload.");
                    return;
                }

                var dto = JsonConvert.DeserializeObject<RouteRuleSelectionDto>(json);
                if (dto == null)
                {
                    _log.Error($"SetRouteRuleForRouteFromUI: Failed to deserialize payload: {json}");
                    return;
                }

                var transportType = dto.TransportType ?? string.Empty;
                var routeNumber = dto.RouteNumber;
                var ruleIdString = dto.RuleId ?? string.Empty;

                ManageRouteBridge.SetRouteRuleForRoute(
                    transportType,
                    routeNumber,
                    ruleIdString
                );

                // Refresh routes view after update
                _routesJsonBinding?.TriggerUpdate();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "SetRouteRuleForRouteFromUI: unexpected exception");
            }
        }

        /// <summary>
        /// Builds a JSON array for the UI:
        /// [
        ///   { "ruleId":"...", "ruleName":"...", "occupancy":..., ... },
        ///   ...
        /// ]
        /// </summary>
        private string GetCustomRulesJson()
        {
            try
            {
                var rules = ManageRouteBridge.GetCustomRules();
                if (rules == null || rules.Length == 0)
                {
                    return "[]";
                }

                // Shape data exactly as the UI expects (camelCase property names)
                var uiRules = rules.Select(r => new
                {
                    ruleId = r.ruleId.ToString(),
                    ruleName = r.ruleName ?? string.Empty,
                    occupancy = r.occupancy,
                    stdTicket = r.stdTicket,
                    maxTicketInc = r.maxTicketInc,
                    maxTicketDec = r.maxTicketDec,
                    maxVehAdj = r.maxVehAdj,
                    minVehAdj = r.minVehAdj
                }).ToArray();

                return JsonConvert.SerializeObject(uiRules);
            }
            catch (Exception ex)
            {
                _log?.Error(ex, "Error building custom rules JSON");
                return "[]";
            }
        }


        /**
         * The problem of the missing route name is resolved here by explicitly
         * including "routeName" in the JSON generation.
         * The "ruleId" is also included, matching the bridge signature: 
         * (int routeNumber, string routeName, string transportType, string ruleName, Colossal.Hash128 ruleId)
         */
        private string GetRoutesJson()
        {
            try
            {
                var routes = ManageRouteBridge.GetRoutesForUI();
                if (routes == null || routes.Length == 0)
                    return "[]";

                var sb = new StringBuilder();
                sb.Append('[');

                for (int i = 0; i < routes.Length; i++)
                {
                    var r = routes[i];
                    if (i > 0)
                        sb.Append(',');

                    sb.Append('{');

                    sb.Append("\"routeNumber\":")
                      .Append(r.routeNumber)
                      .Append(',');

                    sb.Append("\"routeName\":\"")
                      .Append(JsonEscape(r.routeName ?? string.Empty))
                      .Append("\",");

                    sb.Append("\"transportType\":\"")
                      .Append(JsonEscape(r.transportType ?? string.Empty))
                      .Append("\",");

                    sb.Append("\"ruleName\":\"")
                      .Append(JsonEscape(r.ruleName ?? string.Empty))
                      .Append("\",");

                    // Confirmed field from ManageRouteBridge.GetRoutesForUI()
                    sb.Append("\"ruleId\":\"")
                      .Append(r.ruleId.ToString())
                      .Append('"');

                    sb.Append('}');
                }

                sb.Append(']');
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _log?.Error(ex, $"Error building routes JSON in {nameof(GetRoutesJson)}");
                return "[]";
            }
        }


        // Shape of the JSON payload sent from AddCustomRulePanel.tsx
        private class AddCustomRuleDto
        {
            public string ruleName { get; set; } = string.Empty;
            public int occupancy { get; set; }
            public int stdTicket { get; set; }
            public int maxTicketInc { get; set; }
            public int maxTicketDec { get; set; }
            public int maxVehAdj { get; set; }
            public int minVehAdj { get; set; }
        }

        private void AddCustomRuleFromUI(string payloadJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(payloadJson))
                {
                    _log?.Warn($"{nameof(AddCustomRuleFromUI)} called with empty payload.");
                    return;
                }

                var dto = JsonConvert.DeserializeObject<AddCustomRuleDto>(payloadJson);
                if (dto == null)
                {
                    _log?.Warn($"{nameof(AddCustomRuleFromUI)}: deserialized payload is null.");
                    return;
                }

                // 1) Create a new custom rule ID
                var ruleId = ManageRouteBridge.AddCustomRule();

                // 2) Fill it with the values from the UI
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

                // 3) Tell the UI to refresh both panels
                _customRulesJsonBinding?.TriggerUpdate();
                _routesJsonBinding?.TriggerUpdate();
            }
            catch (Exception ex)
            {
                _log?.Error(ex, $"Error in {nameof(AddCustomRuleFromUI)}");
            }
        }



        /// <summary>
        /// Shape of the JSON object the UI sends when adding a custom rule.
        /// Property names must match what the TypeScript side sends.
        /// </summary>
        private class AddCustomRuleRequest
        {
            public string ruleName { get; set; } = string.Empty;
            public int occupancy { get; set; }
            public int stdTicket { get; set; }
            public int maxTicketInc { get; set; }
            public int maxTicketDec { get; set; }
            public int maxVehAdj { get; set; }
            public int minVehAdj { get; set; }
        }


        private static string JsonEscape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var sb = new StringBuilder(value.Length + 8);

            foreach (var c in value)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32)
                        {
                            sb.Append("\\u");
                            sb.Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;
                }
            }

            return sb.ToString();
        }
    }
}