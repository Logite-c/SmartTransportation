using Colossal.Entities;
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
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;

namespace SmartTransportation.Bridge
{
    public partial class ManageRouteSystem : GameSystemBase
    {
        private EntityQuery entityQuery;
        private bool firstUpdate = false;

        private static readonly Dictionary<int, string> RuleNames = new()
        {
            { -1, "Disabled" },
            { (int)TransportType.Bus, TransportType.Bus.ToString()},
            { (int)TransportType.Train, TransportType.Train.ToString()},
            { (int)TransportType.Tram, TransportType.Tram.ToString()},
            { (int)TransportType.Subway, TransportType.Subway.ToString()},
            { 51, Mod.m_Setting.name_Custom1 },
            { 52, Mod.m_Setting.name_Custom2 },
            { 53, Mod.m_Setting.name_Custom3 },
            { 54, Mod.m_Setting.name_Custom4 },
            { 55, Mod.m_Setting.name_Custom5 },
        };


        protected override void OnCreate()
        {
            base.OnCreate();

            entityQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
            ComponentType.ReadOnly<RouteNumber>(),
            ComponentType.ReadOnly<TransportLine>(),
            ComponentType.ReadOnly<PrefabRef>()
                }
            });

            RequireForUpdate(entityQuery);
        }

        private Entity GetRouteEntityFromId(int routeId, TransportType transportType)
        {
            var entities = entityQuery.ToEntityArray(Allocator.Temp);
            Mod.log.Info($"Entities: {entities.Length}");
            try
            {
                foreach (var ent in entities)
                {
                    PrefabRef prefab = EntityManager.GetComponentData<PrefabRef>(ent);
                    TransportLine transportLine = EntityManager.GetComponentData<TransportLine>(ent);
                    RouteNumber routeNumber = EntityManager.GetComponentData<RouteNumber>(ent);
                    TransportLineData transportLineData = EntityManager.GetComponentData<TransportLineData>(prefab.m_Prefab);

                    if (routeNumber.m_Number == routeId &&
                        transportLineData.m_TransportType == transportType)
                    {
                        return ent;
                    }
                }
            }
            finally
            {
                entities.Dispose();
            }

            return Entity.Null; // Not found
        }

        public void SetRouteRule(Entity routeEntity, int routeRuleId)
        {
            var routeRule = new RouteRule(routeRuleId);

            //Mod.log.Info($"RouteEntity: {routeEntity}");
            if (EntityManager.HasComponent<RouteRule>(routeEntity))
            {
                EntityManager.SetComponentData(routeEntity, routeRule);
            }
            else
            {
                EntityManager.AddComponentData(routeEntity, routeRule);
            }
        }

        public (int, string) GetRouteRule(Entity routeEntity)
        {
            int ruleId = -1;
            // First, try to get a custom rule from the RouteRule component
            if (EntityManager.TryGetComponent<RouteRule>(routeEntity, out RouteRule routeRule))
            {
                ruleId = routeRule.customRule;
            } else
            {
                // Try to get prefab info for transport type fallback
                if (EntityManager.TryGetComponent<PrefabRef>(routeEntity, out PrefabRef prefab))
                {
                    var transportLineData = EntityManager.GetComponentData<TransportLineData>(prefab.m_Prefab);
                    TransportType transportType = transportLineData.m_TransportType;

                    // Check if this transport type is disabled in settings
                    bool isDisabled = transportType switch
                    {
                        TransportType.Bus => Mod.m_Setting.disable_bus,
                        TransportType.Tram => Mod.m_Setting.disable_Tram,
                        TransportType.Subway => Mod.m_Setting.disable_Subway,
                        TransportType.Train => Mod.m_Setting.disable_Train,
                        _ => true
                    };

                    if (!isDisabled)
                    {
                        int defaultId = (int)transportType;
                        string defaultName = RuleNames.TryGetValue(defaultId, out var name) ? name : transportType.ToString();

                        return (defaultId, defaultName);
                    }
                }
            }

            if (RuleNames.TryGetValue(ruleId, out var ruleName))
            {
                return (ruleId, ruleName);
            } else
            {
                return default;
            }
        }



        public (int, string)[] GetRouteRules(Entity routeEntity)
        {
            if (!EntityManager.TryGetComponent<PrefabRef>(routeEntity, out PrefabRef prefab))
                return Array.Empty<(int, string)>(); // Invalid route, return empty

            if (!EntityManager.HasComponent<TransportLineData>(prefab.m_Prefab))
                return Array.Empty<(int, string)>(); // No transport data

            TransportLineData transportLineData = EntityManager.GetComponentData<TransportLineData>(prefab.m_Prefab);
            TransportType transportType = transportLineData.m_TransportType;

            // Check if this transport type is disabled
            bool isDisabled = transportType switch
            {
                TransportType.Bus => Mod.m_Setting.disable_bus,
                TransportType.Tram => Mod.m_Setting.disable_Tram,
                TransportType.Subway => Mod.m_Setting.disable_Subway,
                TransportType.Train => Mod.m_Setting.disable_Train,
                _ => true
            };

            // Build a list of valid rule entries (could be filtered by type if needed)
            var rules = RuleNames
                .Where(kv => kv.Key == -1 || kv.Key == (int)transportType || (kv.Key >= 51))
                .OrderBy(kv => kv.Key)
                .Select(kv => (kv.Key, kv.Value))
                .ToArray();

            if (isDisabled)
            {
                rules = RuleNames
                .Where(kv => kv.Key == -1)
                .OrderBy(kv => kv.Key)
                .Select(kv => (kv.Key, kv.Value))
                .ToArray();
            }

            return rules;
        }


        protected override void OnUpdate()
        {
            //Entity routeEntity = GetRouteEntityFromId(2, TransportType.Bus);
            ////SetRouteRule(routeEntity, 1);
            //Mod.log.Info($"SetRouteRule set");
            //if (firstUpdate)
            //{
            //    Mod.log.Info($"GetRouteRuleInfo: {GetRouteRule(routeEntity)}");
            //
            //    var routeNames = GetRouteRules(routeEntity);
            //    foreach (var (id, name) in routeNames)
            //    {
            //        Mod.log.Info($"Route ID: {id}, Name: {name}");
            //    }
            //    //this.Enabled = false;
            //}
            //
            //firstUpdate = true;

            //this.Enabled = false;
        }
    }
}