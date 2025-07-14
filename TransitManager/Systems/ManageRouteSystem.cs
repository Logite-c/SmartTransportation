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
            //Mod.log.Info($"Entities: {entities.Length}");
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

        public (FixedString64Bytes, int, int, int, int, int, int) GetCustomRule(int ruleId)
        {
            EntityQuery query = EntityManager.CreateEntityQuery(typeof(CustomRule));
            var rules = query.ToComponentDataArray<CustomRule>(Allocator.Temp);

            foreach (var r in rules)
            {
                if (r.ruleId == ruleId)
                {
                    return (r.ruleName, r.occupancy, r.stdTicket, r.maxTicketInc, r.maxTicketDec, r.maxVehAdj, r.minVehAdj);
                }
            }

            return default;
        }


        public void SetCustomRule(int ruleId, FixedString64Bytes ruleName, int occupancy, int stdTicket, int maxTicketInc, int maxTicketDec, int maxVehAdj, int minVehAdj)
        {
            EntityQuery query = EntityManager.CreateEntityQuery(typeof(CustomRule));
            var rules = query.ToComponentDataArray<CustomRule>(Allocator.Temp);

            foreach (var rule in rules)
            {
                if (rule.ruleId == ruleId)
                {
                    Entity entity = query.ToEntityArray(Allocator.Temp)[Array.IndexOf(rules.ToArray(), rule)];
                    EntityManager.SetComponentData(entity, new CustomRule(ruleId, ruleName, occupancy, stdTicket, maxTicketInc, maxTicketDec, maxVehAdj, minVehAdj));
                    return;
                }
            }

            var newRuleEntity = EntityManager.CreateEntity(typeof(CustomRule));
            EntityManager.SetComponentData(newRuleEntity, new CustomRule(ruleId, ruleName, occupancy, stdTicket, maxTicketInc, maxTicketDec, maxVehAdj, minVehAdj));
        }


        public static void RemoveCustomRule(int ruleId)
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(typeof(CustomRule));
            var entities = query.ToEntityArray(Allocator.Temp);
            var rules = query.ToComponentDataArray<CustomRule>(Allocator.Temp);

            for (int i = 0; i < rules.Length; i++)
            {
                if (rules[i].ruleId == ruleId)
                {
                    entityManager.DestroyEntity(entities[i]);
                    break;
                }
            }
        }


        public (int, FixedString64Bytes, int, int, int, int, int, int)[] GetCustomRules()
        {
            EntityQuery query = EntityManager.CreateEntityQuery(typeof(CustomRule));
            var rules = query.ToComponentDataArray<CustomRule>(Allocator.Temp);

            return rules
                .Select(r => (r.ruleId, r.ruleName, r.occupancy, r.stdTicket, r.maxTicketInc, r.maxTicketDec, r.maxVehAdj, r.minVehAdj))
                .ToArray();
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

            if (!firstUpdate)
            {
                // Test adding/setting custom rules
                SetCustomRule(101, "Rule One", 45, 10, 25, 15, 20, 5);
                SetCustomRule(102, "Rule Two", 60, 12, 30, 10, 10, 10);
                SetCustomRule(103, "Rule Three", 50, 11, 20, 20, 15, 8);

                Mod.log.Info("=== All Custom Rules After Adding ===");
                foreach (var (id, name, occ, ticket, inc, dec, maxAdj, minAdj) in GetCustomRules())
                {
                    Mod.log.Info($"ID: {id}, Name: {name}, Occupancy: {occ}, StdTicket: {ticket}, MaxInc: {inc}, MaxDec: {dec}, MaxAdj: {maxAdj}, MinAdj: {minAdj}");
                }

                // Test fetching a specific rule
                var (ruleName, occ2, ticket2, inc2, dec2, maxAdj2, minAdj2) = GetCustomRule(102);
                Mod.log.Info($"--- Retrieved Rule 102 ---");
                Mod.log.Info($"Name: {ruleName}, Occupancy: {occ2}, StdTicket: {ticket2}, MaxInc: {inc2}, MaxDec: {dec2}, MaxAdj: {maxAdj2}, MinAdj: {minAdj2}");

                // Test removing a rule
                RemoveCustomRule(101);

                Mod.log.Info("=== All Custom Rules After Removing Rule 101 ===");
                foreach (var (id, name, occ, ticket, inc, dec, maxAdj, minAdj) in GetCustomRules())
                {
                    Mod.log.Info($"ID: {id}, Name: {name}, Occupancy: {occ}, StdTicket: {ticket}, MaxInc: {inc}, MaxDec: {dec}, MaxAdj: {maxAdj}, MinAdj: {minAdj}");
                }

                firstUpdate = true;
            }

            this.Enabled = false; // Prevent further updates
        }

    }
}