using Colossal.PSI.Common;
using Game;
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

        public static void setRouteRule(TransportType transportType, int routeId, int routeRuleId, bool disable)
        {
            ManageRouteSystem manageRouteSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<ManageRouteSystem>();
            manageRouteSystem.setRouteRule(transportType, routeId, routeRuleId, disable);
        }

        public static (int customRuleId, bool isDisabled) getRouteRuleInfo(TransportType transportType, int routeId)
        {
            ManageRouteSystem manageRouteSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<ManageRouteSystem>();
            return manageRouteSystem.GetRouteRuleInfo(transportType, routeId);
        }

        public static (int, string)[] getRouteRuleNames()
        {
            ManageRouteSystem manageRouteSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<ManageRouteSystem>();
            return manageRouteSystem.getRouteRuleNames();
        }

    }
}