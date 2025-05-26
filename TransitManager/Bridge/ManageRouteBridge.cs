
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
        public static void SetRouteRule(Entity routeEntity, int routeRuleId)
        {
            ManageRouteSystem.SetRouteRule(routeEntity, routeRuleId);
        }

        /// <summary>
        /// Gets the rule info for a given route.
        /// </summary>
        public static (int, string) GetRouteRule(Entity routeEntity)
        {
            return ManageRouteSystem.GetRouteRule(routeEntity);
        }

        /// <summary>
        /// Returns all available route rule names.
        /// </summary>
        public static (int, string)[] GetRouteRules(Entity routeEntity)
        {
            return ManageRouteSystem.GetRouteRules(routeEntity);
        }
    }
}
