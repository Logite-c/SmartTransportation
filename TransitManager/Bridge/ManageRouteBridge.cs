
using Unity.Collections;
using Unity.Entities;

namespace SmartTransportation.Bridge
{
    public static class ManageRouteBridge
    {
        private static ManageRouteSystem manageRouteSystem; 

        private static ManageRouteSystem ManageRouteSystem => manageRouteSystem ??= World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ManageRouteSystem>();

        /// <summary>
        /// Sets a rule for a given transport route.
        /// </summary>
        public static void SetRouteRule(Entity routeEntity, Colossal.Hash128 routeRuleId)
        {
            ManageRouteSystem.SetRouteRule(routeEntity, routeRuleId);
        }

        /// <summary>
        /// Gets the rule info for a given route.
        /// </summary>
        public static (Colossal.Hash128, string) GetRouteRule(Entity routeEntity)
        {
            return ManageRouteSystem.GetRouteRule(routeEntity);
        }

        /// <summary>
        /// Returns all available route rule names.
        /// </summary>
        public static (Colossal.Hash128, string)[] GetRouteRules(Entity routeEntity)
        {
            return ManageRouteSystem.GetRouteRules(routeEntity);
        }

        public static (Colossal.Hash128, string, int, int, int, int, int, int)[] GetCustomRules()
        {
            return ManageRouteSystem.GetCustomRules();
        }

        public static void SetCustomRule(Colossal.Hash128 ruleId, string ruleName, int occupancy, int stdTicket, int maxTicketInc, int maxTicketDec, int maxVehAdj, int minVehAdj)
        {
            ManageRouteSystem.SetCustomRule(ruleId, ruleName, occupancy, stdTicket, maxTicketInc, maxTicketDec, maxVehAdj, minVehAdj);
        }

        public static void AddCustomRule(string ruleName, int occupancy, int stdTicket, int maxTicketInc, int maxTicketDec, int maxVehAdj, int minVehAdj)
        {
            ManageRouteSystem.AddCustomRule(ruleName, occupancy, stdTicket, maxTicketInc, maxTicketDec, maxVehAdj, minVehAdj);
        }

        public static void RemoveCustomRule(Colossal.Hash128 ruleId)
        {
            ManageRouteSystem.RemoveCustomRule(ruleId);
        }

        public static (FixedString64Bytes, int, int, int, int, int, int) GetCustomRule(Colossal.Hash128 ruleId)
        {
            return ManageRouteSystem.GetCustomRule(ruleId);
        }
    }
}
