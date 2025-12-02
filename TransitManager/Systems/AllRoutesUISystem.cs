
using Colossal.Logging;
using Colossal.UI.Binding;
using Game.UI;
using SmartTransportation.Bridge;
using SmartTransportation.Extensions;
using System;
using SmartTransportation.Domain;

namespace SmartTransportation.Systems
{
    public partial class AllRoutesUISystem : ExtendedUISystemBase
    {
        private ILog _log;

        private RawValueBinding _routeInfosBinding;
        private RouteInfos _routeInfos = new();
        private DisabledTransportTypes _disabledTransportTypes = new();

        protected override void OnCreate()
        {
            base.OnCreate();

            _log = LogManager.GetLogger(
                $"{nameof(SmartTransportation)}.{nameof(AllRoutesUISystem)}");

            // Routes binding using RawValueBinding
            AddBinding(_routeInfosBinding = new RawValueBinding(
                Mod.modName,
                "routeInfos",
                WriteRouteInfos
            ));

            // Disabled transport types binding using RawValueBinding
            AddBinding(new RawValueBinding(
                Mod.modName,
                "disabledTransportTypes",
                WriteDisabledTransportTypes
            ));

            AddBinding(new TriggerBinding<string, int, string>(
                Mod.modName,
                "setRouteRuleForRoute",
                SetRouteRuleForRouteFromUI
            ));
        }
        
        private void WriteRouteInfos(IJsonWriter writer)
        {
            _routeInfos.Clear();
            var routes = ManageRouteBridge.GetRoutesForUI();
            if (routes != null)
            {
                foreach (var r in routes)
                {
                    _routeInfos.Add(new RouteInfo(
                        r.routeNumber,
                        r.routeName ?? string.Empty,
                        r.transportType ?? string.Empty,
                        r.ruleName ?? string.Empty,
                        r.ruleId
                    ));
                }
            }
            _routeInfos.Write(writer);
        }

        private void WriteDisabledTransportTypes(IJsonWriter writer)
        {
            _disabledTransportTypes = DisabledTransportTypes.FromSettings(Mod.m_Setting);
            _disabledTransportTypes.Write(writer);
        }
        
        private void SetRouteRuleForRouteFromUI(string transportType, int routeNumber, string ruleId)
        {
            if (string.IsNullOrWhiteSpace(transportType))
            {
                _log.Warn("Transport type cannot be empty");
                return;
            }

            if (routeNumber <= 0)
            {
                _log.Warn($"Invalid route number: {routeNumber}");
                return;
            }

            if (string.IsNullOrWhiteSpace(ruleId))
            {
                _log.Info("Rule ID cannot be empty");
                return;
            }

            try
            {
                ManageRouteBridge.SetRouteRuleForRoute(
                    transportType,
                    routeNumber,
                    ruleId
                );
                
                _routeInfosBinding.Update();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to set rule {ruleId} for {transportType} route {routeNumber}");
            }
        }
        
    }
}