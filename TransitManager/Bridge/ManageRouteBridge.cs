
using System;
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

        /// <summary>
        /// Retrieves all custom rules currently stored in the ManageRouteSystem.
        /// </summary>
        /// <returns>
        /// An array of tuples containing:
        /// - ruleId: The unique identifier of the rule
        /// - ruleName: The name of the rule
        /// - occupancy: Target occupancy percentage
        /// - stdTicket: Standard ticket price
        /// - maxTicketInc: Maximum allowed ticket price increase
        /// - maxTicketDec: Maximum allowed ticket price discount
        /// - maxVehAdj: Maximum vehicle adjustment
        /// - minVehAdj: Minimum vehicle adjustment
        /// </returns>
        public static (
            Colossal.Hash128 ruleId,
            string ruleName,
            int occupancy,
            int stdTicket,
            int maxTicketInc,
            int maxTicketDec,
            int maxVehAdj,
            int minVehAdj
        )[] GetCustomRules()
        {
            return ManageRouteSystem.GetCustomRules();
        }


        /// <summary>
        /// Updates the properties of an existing custom rule identified by the given ruleId.
        /// </summary>
        /// <param name="ruleId">The unique identifier of the custom rule to update.</param>
        /// <param name="ruleName">The name to assign to the rule.</param>
        /// <param name="occupancy">Target occupancy percentage for the route.</param>
        /// <param name="stdTicket">Standard ticket price for the route.</param>
        /// <param name="maxTicketInc">Maximum allowed ticket price increase.</param>
        /// <param name="maxTicketDec">Maximum allowed ticket price discount.</param>
        /// <param name="maxVehAdj">Maximum percentage of vehicle adjustment.</param>
        /// <param name="minVehAdj">Minimum percentage of vehicle adjustment.</param>
        public static void SetCustomRule(
            Colossal.Hash128 ruleId,
            string ruleName,
            int occupancy,
            int stdTicket,
            int maxTicketInc,
            int maxTicketDec,
            int maxVehAdj,
            int minVehAdj)
        {
            ManageRouteSystem.SetCustomRule(ruleId, ruleName, occupancy, stdTicket, maxTicketInc, maxTicketDec, maxVehAdj, minVehAdj);
        }


        /// <summary>
        /// Adds a new custom rule with default values to the ManageRouteSystem.
        /// The rule will be assigned a unique ruleId that does not conflict with built-in rules.
        /// </summary>
        /// <returns>The unique ruleId (Hash128) assigned to the newly created custom rule.</returns>
        public static Colossal.Hash128 AddCustomRule()
        {
            return ManageRouteSystem.AddCustomRule();
        }

        /// <summary>
        /// Removes a custom rule identified by the given ruleId from the ManageRouteSystem.
        /// </summary>
        /// <param name="ruleId">The unique identifier of the custom rule to remove.</param>
        public static void RemoveCustomRule(Colossal.Hash128 ruleId)
        {
            ManageRouteSystem.RemoveCustomRule(ruleId);
        }

        /// <summary>
        /// Retrieves a custom rule by its ruleId from the ManageRouteSystem.
        /// </summary>
        /// <param name="ruleId">The unique ID of the custom rule to retrieve.</param>
        /// <returns>
        /// A tuple containing:
        /// - ruleId: The rule's unique identifier
        /// - ruleName: The name of the rule
        /// - occupancy: The target occupancy percentage
        /// - stdTicket: The standard ticket price
        /// - maxTicketInc: Maximum allowed ticket price increase
        /// - maxTicketDec: Maximum allowed ticket price discount
        /// - maxVehAdj: Maximum vehicle adjustment
        /// - minVehAdj: Minimum vehicle adjustment
        /// </returns>
        public static (
            Colossal.Hash128 ruleId,
            string ruleName,
            int occupancy,
            int stdTicket,
            int maxTicketInc,
            int maxTicketDec,
            int maxVehAdj,
            int minVehAdj
        ) GetCustomRule(Colossal.Hash128 ruleId)
        {
            return ManageRouteSystem.GetCustomRule(ruleId);
        }

        /// <summary>
        /// Returns simple route info for UI consumption.
        /// </summary>
        public static (int routeNumber, string routeName, string transportType, string ruleName, Colossal.Hash128 ruleId)[] GetRoutesForUI()
        {
            var routes = ManageRouteSystem.GetRoutesForUI();
            if (routes == null || routes.Length == 0)
                return Array.Empty<(int, string, string, string, Colossal.Hash128)>();

            var result = new (int, string, string, string, Colossal.Hash128)[routes.Length];
            for (int i = 0; i < routes.Length; i++)
            {
                var r = routes[i];
                result[i] = (r.routeNumber, r.routeName, r.transportType, r.ruleName, r.ruleId);
            }

            return result;
        }

        public static void SetRouteRuleForRoute(string transportType, int routeNumber, string ruleIdString)
        {
            if (ManageRouteSystem == null)
            {
                return;
            }

            Colossal.Hash128? ruleId = null;

            if (!string.IsNullOrEmpty(ruleIdString))
            {
                try
                {
                    ruleId = new Colossal.Hash128(ruleIdString);
                }
                catch
                {
                    // bad ruleIdString, treat as Disabled
                    ruleId = null;
                }
            }

            ManageRouteSystem.SetRouteRuleForRoute(transportType, routeNumber, ruleId);
        }
    }

}
