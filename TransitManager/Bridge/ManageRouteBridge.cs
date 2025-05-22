using Colossal.PSI.Common;
using Game;
using Game.City;
using Game.Events;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Game.SceneFlow;
using Game.Settings;
using Game.UI;
using Game.UI.Localization;
using SmartTransportation.Components;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;

namespace SmartTransportation.Bridge
{
    public static class ManageRouteBridge
    {
        private static ManageRouteSystem manageRouteSystem; 

        public static ManageRouteSystem GetManageRouteSystem(Entity e) => manageRouteSystem ??= World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ManageRouteSystem>();

        /// <summary>
        /// Sets a rule for a given transport route.
        /// </summary>
        public static void SetRouteRule(Entity routeEntity, int routeRuleId)
        {
            manageRouteSystem.SetRouteRule(routeEntity, routeRuleId);
        }

        /// <summary>
        /// Gets the rule info for a given route.
        /// </summary>
        public static (int, string) GetRouteRule(Entity routeEntity)
        {
            return manageRouteSystem.GetRouteRule(routeEntity);
        }

        /// <summary>
        /// Returns all available route rule names.
        /// </summary>
        public static (int, string)[] GetRouteRules(Entity routeEntity)
        {
            return manageRouteSystem.GetRouteRules(routeEntity);
        }
    }
}
