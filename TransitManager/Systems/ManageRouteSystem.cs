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

        public (FixedString64Bytes, int, int, int, int, int, int) GetCustomRule(Colossal.Hash128 ruleId)
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


        public void SetCustomRule(Colossal.Hash128 ruleId, FixedString64Bytes ruleName, int occupancy, int stdTicket, int maxTicketInc, int maxTicketDec, int maxVehAdj, int minVehAdj)
        {
            EntityQuery query = EntityManager.CreateEntityQuery(typeof(CustomRule));
            var entities = query.ToEntityArray(Allocator.Temp);
            var rules = query.ToComponentDataArray<CustomRule>(Allocator.Temp);

            for (int i = 0; i < rules.Length; i++)
            {
                if (rules[i].ruleId == ruleId)
                {
                    var updated = rules[i];
                    updated.ruleName = ruleName;
                    updated.occupancy = occupancy;
                    updated.stdTicket = stdTicket;
                    updated.maxTicketInc = maxTicketInc;
                    updated.maxTicketDec = maxTicketDec;
                    updated.maxVehAdj = maxVehAdj;
                    updated.minVehAdj = minVehAdj;

                    EntityManager.SetComponentData(entities[i], updated);

                    entities.Dispose();
                    rules.Dispose();
                    return;
                }
            }

            Mod.log.Warn($"SetCustomRule failed: No CustomRule found with ruleId {ruleId}");
            entities.Dispose();
            rules.Dispose();
        }


        public void AddCustomRule(FixedString64Bytes ruleName, int occupancy, int stdTicket, int maxTicketInc, int maxTicketDec, int maxVehAdj, int minVehAdj)
        {
            var newRuleEntity = EntityManager.CreateEntity(typeof(CustomRule));
            EntityManager.SetComponentData(newRuleEntity, new CustomRule(ruleName, occupancy, stdTicket, maxTicketInc, maxTicketDec, maxVehAdj, minVehAdj));
        }


        public static void RemoveCustomRule(Colossal.Hash128 ruleId)
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


        public (Colossal.Hash128, FixedString64Bytes, int, int, int, int, int, int)[] GetCustomRules()
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

            //if (!firstUpdate)
            //{
            //    // Step 1: Add new rules
            //    AddCustomRule("Alpha", 40, 10, 20, 10, 25, 5);
            //    AddCustomRule("Beta", 60, 15, 30, 15, 20, 10);
            //
            //    var allRules = GetCustomRules();
            //
            //    Mod.log.Info("=== After Adding Custom Rules ===");
            //    foreach (var (id, name, occ, ticket, inc, dec, maxAdj, minAdj) in allRules)
            //    {
            //        Mod.log.Info($"ID: {id}, Name: {name}, Occ: {occ}, StdTicket: {ticket}, MaxInc: {inc}, MaxDec: {dec}, MaxAdj: {maxAdj}, MinAdj: {minAdj}");
            //
            //    }
            //
            //    if (allRules.Length > 0)
            //    {
            //        // Step 2: Pick the first rule and update it
            //        var (targetId, _, _, _, _, _, _, _) = allRules[0];
            //
            //        Mod.log.Info($"--- Updating Rule with ID: {targetId} ---");
            //        SetCustomRule(targetId, "Alpha (Updated)", 55, 12, 22, 8, 18, 6);
            //
            //        // Step 3: Fetch updated rule
            //        var (updatedName, updatedOcc, updatedTicket, updatedInc, updatedDec, updatedMax, updatedMin) = GetCustomRule(targetId);
            //
            //        Mod.log.Info($"Updated Rule: Name: {updatedName}, Occ: {updatedOcc}, StdTicket: {updatedTicket}, MaxInc: {updatedInc}, MaxDec: {updatedDec}, MaxAdj: {updatedMax}, MinAdj: {updatedMin}");
            //
            //        // Step 4: Remove that rule
            //        RemoveCustomRule(targetId);
            //    }
            //
            //    // Step 5: Log what's left
            //    var finalRules = GetCustomRules();
            //    Mod.log.Info("=== After Removing First Custom Rule ===");
            //    foreach (var (id, name, occ, ticket, inc, dec, maxAdj, minAdj) in finalRules)
            //    {
            //        Mod.log.Info($"ID: {id}, Name: {name}, Occ: {occ}, StdTicket: {ticket}, MaxInc: {inc}, MaxDec: {dec}, MaxAdj: {maxAdj}, MinAdj: {minAdj}");
            //    }
            //
            //    firstUpdate = true;
            //}
            //
            this.Enabled = false; // Disable after one run
        }

    }
}