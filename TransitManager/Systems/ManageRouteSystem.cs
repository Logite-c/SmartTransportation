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
using static Unity.Collections.Unicode;

namespace SmartTransportation.Bridge
{
    public partial class ManageRouteSystem : GameSystemBase
    {
        private EntityQuery entityQuery;
        private const int disabled_int_id = 999; // Used for disabled routes
        private bool firstUpdate = false;

        public static readonly Dictionary<Colossal.Hash128, string> RuleNames = new()
        {
            { new Colossal.Hash128((uint)disabled_int_id,0,0,0), "Disabled" },
            { new Colossal.Hash128((uint)TransportType.Bus,0,0,0), TransportType.Bus.ToString()},
            { new Colossal.Hash128((uint)TransportType.Train,0,0,0), TransportType.Train.ToString()},
            { new Colossal.Hash128((uint)TransportType.Tram,0,0,0), TransportType.Tram.ToString()},
            { new Colossal.Hash128((uint)TransportType.Subway,0,0,0), TransportType.Subway.ToString()},
            { new Colossal.Hash128((uint)TransportType.Ship,0,0,0), TransportType.Ship.ToString()},
            { new Colossal.Hash128((uint)TransportType.Airplane,0,0,0), TransportType.Airplane.ToString()},

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

        private void SyncDefaultRulesFromSettings()
        {
            foreach (var (ruleId, ruleName) in RuleNames)
            {
                int occ = 0, ticket = 0, inc = 0, dec = 0, maxAdj = 0, minAdj = 0;

                if (ruleName == "Disabled")
                {
                    // All values remain zero
                }
                else if (ruleName == "Bus")
                {
                    occ = Mod.m_Setting.target_occupancy_bus;
                    ticket = Mod.m_Setting.standard_ticket_bus;
                    inc = Mod.m_Setting.max_ticket_increase_bus;
                    dec = Mod.m_Setting.max_ticket_discount_bus;
                    maxAdj = Mod.m_Setting.max_vahicles_adj_bus;
                    minAdj = Mod.m_Setting.min_vahicles_adj_bus;
                }
                else if (ruleName == "Tram")
                {
                    occ = Mod.m_Setting.target_occupancy_Tram;
                    ticket = Mod.m_Setting.standard_ticket_Tram;
                    inc = Mod.m_Setting.max_ticket_increase_Tram;
                    dec = Mod.m_Setting.max_ticket_discount_Tram;
                    maxAdj = Mod.m_Setting.max_vahicles_adj_Tram;
                    minAdj = Mod.m_Setting.min_vahicles_adj_Tram;
                }
                else if (ruleName == "Subway")
                {
                    occ = Mod.m_Setting.target_occupancy_Subway;
                    ticket = Mod.m_Setting.standard_ticket_Subway;
                    inc = Mod.m_Setting.max_ticket_increase_Subway;
                    dec = Mod.m_Setting.max_ticket_discount_Subway;
                    maxAdj = Mod.m_Setting.max_vahicles_adj_Subway;
                    minAdj = Mod.m_Setting.min_vahicles_adj_Subway;
                }
                else if (ruleName == "Train")
                {
                    occ = Mod.m_Setting.target_occupancy_Train;
                    ticket = Mod.m_Setting.standard_ticket_Train;
                    inc = Mod.m_Setting.max_ticket_increase_Train;
                    dec = Mod.m_Setting.max_ticket_discount_Train;
                    maxAdj = Mod.m_Setting.max_vahicles_adj_Train;
                    minAdj = Mod.m_Setting.min_vahicles_adj_Train;
                }
                else if (ruleName == "Ship")
                {
                    occ = Mod.m_Setting.target_occupancy_Ship;
                    ticket = Mod.m_Setting.standard_ticket_Ship;
                    inc = Mod.m_Setting.max_ticket_increase_Ship;
                    dec = Mod.m_Setting.max_ticket_discount_Ship;
                    maxAdj = Mod.m_Setting.max_vahicles_adj_Ship;
                    minAdj = Mod.m_Setting.min_vahicles_adj_Ship;
                }
                else if (ruleName == "Airplane")
                {
                    occ = Mod.m_Setting.target_occupancy_Airplane;
                    ticket = Mod.m_Setting.standard_ticket_Airplane;
                    inc = Mod.m_Setting.max_ticket_increase_Airplane;
                    dec = Mod.m_Setting.max_ticket_discount_Airplane;
                    maxAdj = Mod.m_Setting.max_vahicles_adj_Airplane;
                    minAdj = Mod.m_Setting.min_vahicles_adj_Airplane;
                }

                else
                {
                    continue; // Unknown built-in name
                }

                // Check if the rule already exists
                var (_, existingName, _, _, _, _, _, _) = GetCustomRule(ruleId);
                if (!string.IsNullOrEmpty(existingName))
                {
                    // Update
                    SetCustomRule(ruleId, ruleName, occ, ticket, inc, dec, maxAdj, minAdj);
                }
                else
                {
                    // Create
                    Entity entity = EntityManager.CreateEntity(typeof(CustomRule));
                    var rule = new CustomRule(ruleId, ruleName, occ, ticket, inc, dec, maxAdj, minAdj);

                    EntityManager.SetComponentData(entity, rule);
                }
            }

            Mod.log.Info("[ManageRouteSystem] Synced all default + disabled route rules.");
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

        public void SetRouteRule(Entity routeEntity, Colossal.Hash128 routeRuleId)
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

        public (Colossal.Hash128, string) GetRouteRule(Entity routeEntity)
        {
            Colossal.Hash128 ruleId = default;
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
                        TransportType.Ship => Mod.m_Setting.disable_Ship,
                        TransportType.Airplane => Mod.m_Setting.disable_Airplane,
                        _ => true
                    };


                    if (!isDisabled)
                    {
                        Colossal.Hash128 defaultId = new Colossal.Hash128((uint)transportType,0,0,0);
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



        public (Colossal.Hash128, string)[] GetRouteRules(Entity routeEntity)
        {
            if (!EntityManager.TryGetComponent<PrefabRef>(routeEntity, out PrefabRef prefab))
                return Array.Empty<(Colossal.Hash128, string)>(); // Invalid route, return empty

            if (!EntityManager.HasComponent<TransportLineData>(prefab.m_Prefab))
                return Array.Empty<(Colossal.Hash128, string)>(); // No transport data

            var transportLineData = EntityManager.GetComponentData<TransportLineData>(prefab.m_Prefab);
            var transportType = transportLineData.m_TransportType;

            // Check if this transport type is disabled in the mod settings
            bool isDisabled = transportType switch
            {
                TransportType.Bus => Mod.m_Setting.disable_bus,
                TransportType.Tram => Mod.m_Setting.disable_Tram,
                TransportType.Subway => Mod.m_Setting.disable_Subway,
                TransportType.Train => Mod.m_Setting.disable_Train,
                TransportType.Ship => Mod.m_Setting.disable_Ship,
                TransportType.Airplane => Mod.m_Setting.disable_Airplane,
                _ => true
            };

            var result = new List<(Colossal.Hash128, string)>();

            // If transport type is disabled, only return the "Disabled" option
            if (isDisabled)
            {
                var disabledId = new Colossal.Hash128((uint)disabled_int_id, 0, 0, 0);
                if (RuleNames.TryGetValue(disabledId, out var disabledName))
                {
                    result.Add((disabledId, disabledName));
                }
                return result.ToArray();
            }

            // 1. Add built-in rule for the transport type
            var defaultId = new Colossal.Hash128((uint)transportType, 0, 0, 0);
            if (RuleNames.TryGetValue(defaultId, out var defaultName))
            {
                result.Add((defaultId, defaultName));
            }

            // 2. Add custom rules (excluding built-in ones from RuleNames)
            var customRules = GetCustomRules();
            foreach (var (ruleId, ruleName, _, _, _, _, _, _) in customRules)
            {
                if (RuleNames.ContainsKey(ruleId))
                    continue; // Skip built-in rule

                result.Add((ruleId, ruleName.ToString()));
            }

            return result.ToArray();
        }


        public (Colossal.Hash128 ruleId, string, int, int, int, int, int, int) GetCustomRule(Colossal.Hash128 ruleId)
        {
            EntityQuery query = EntityManager.CreateEntityQuery(typeof(CustomRule));
            var rules = query.ToComponentDataArray<CustomRule>(Allocator.Temp);

            foreach (var r in rules)
            {
                if (r.ruleId == ruleId)
                {
                    return (r.ruleId, r.ruleName.ToString(), r.occupancy, r.stdTicket, r.maxTicketInc, r.maxTicketDec, r.maxVehAdj, r.minVehAdj);
                }
            }

            return default;
        }


        public void SetCustomRule(Colossal.Hash128 ruleId, FixedString64Bytes ruleName, int occupancy, int stdTicket, int maxTicketInc, int maxTicketDec, int maxVehAdj, int minVehAdj)
        {
            EntityQuery query = EntityManager.CreateEntityQuery(typeof(CustomRule));
            var entities = query.ToEntityArray(Allocator.Temp);
            var rules = query.ToComponentDataArray<CustomRule>(Allocator.Temp);

            try
            {
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

                        return;
                    }
                }

            }
            finally
            {
                entities.Dispose();
                rules.Dispose();
            }
        }


        public Colossal.Hash128 AddCustomRule()
        {
            var newRuleEntity = EntityManager.CreateEntity(typeof(CustomRule));
            CustomRule customRule = new CustomRule("Unnamed", 0, 0, 0, 0, 0, 0);
            EntityManager.SetComponentData(newRuleEntity,customRule);

            return customRule.ruleId; // Return the generated ruleId
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


        public (Colossal.Hash128, string, int, int, int, int, int, int)[] GetCustomRules()
        {
            // Dispose the query after use (or cache it in OnCreate and reuse).
            using var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<CustomRule>());

            // This returns a NativeArray<CustomRule>; it must be disposed.
            using var rules = query.ToComponentDataArray<CustomRule>(Allocator.Temp);

            var result = new (Colossal.Hash128, string, int, int, int, int, int, int)[rules.Length];

            // Fill a managed array, then return it (after the NativeArray is disposed).
            for (int i = 0; i < rules.Length; i++)
            {
                var r = rules[i];
                result[i] = (
                    r.ruleId,
                    r.ruleName.ToString(), // copy FixedString to managed string
                    r.occupancy,
                    r.stdTicket,
                    r.maxTicketInc,
                    r.maxTicketDec,
                    r.maxVehAdj,
                    r.minVehAdj
                );
            }

            return result;
        }




        protected override void OnUpdate()
        {
            if (firstUpdate) return;

            SyncDefaultRulesFromSettings();

            //Entity routeEntity = GetRouteEntityFromId(1, TransportType.Bus);
            //if (routeEntity == Entity.Null)
            //{
            //    Mod.log.Warn("[TEST] Could not find Bus route with ID 1.");
            //    this.Enabled = false;
            //    return;
            //}
            //
            //Mod.log.Info("=== STARTING ManageRouteSystem TEST ===");
            //
            //// 1. Add Custom Rules (use AddCustomRule + SetCustomRule)
            //var alphaId = AddCustomRule();
            //SetCustomRule(alphaId, "Alpha", 40, 10, 20, 10, 25, 5);
            //
            //var betaId = AddCustomRule();
            //SetCustomRule(betaId, "Beta", 60, 15, 30, 15, 20, 10);
            //
            //Mod.log.Info("[TEST] Added Custom Rules");
            //
            //// 2. Get All Custom Rules
            //var allCustomRules = GetCustomRules();
            //foreach (var (id, name, occ, ticket, inc, dec, maxAdj, minAdj) in allCustomRules)
            //{
            //    Mod.log.Info($"[TEST] CustomRule - ID: {id}, Name: {name}, Occ: {occ}, StdTicket: {ticket}, MaxInc: {inc}, MaxDec: {dec}, MaxAdj: {maxAdj}, MinAdj: {minAdj}");
            //}
            //
            //// 3. SetRouteRule: Assign the first custom rule to the route
            //var (testRuleId, _, _, _, _, _, _, _) = allCustomRules.Last();
            //SetRouteRule(routeEntity, testRuleId);
            //Mod.log.Info($"[TEST] SetRouteRule to custom rule ID: {testRuleId}");
            //
            //// 4. GetRouteRule: Confirm assignment
            //var (assignedRuleId, assignedName) = GetRouteRule(routeEntity);
            //Mod.log.Info($"[TEST] GetRouteRule => ID: {assignedRuleId}, Name: {assignedName}");
            //
            //// 5. GetRouteRules: List all valid rules for the route
            //var routeRules = GetRouteRules(routeEntity);
            //foreach (var (id, name) in routeRules)
            //{
            //    Mod.log.Info($"[TEST] Valid RouteRule => ID: {id}, Name: {name}");
            //}
            //
            //// 6. GetCustomRule: Get the full data of the assigned custom rule
            //var (_, rName, rOcc, rTicket, rInc, rDec, rMax, rMin) = GetCustomRule(testRuleId);
            //Mod.log.Info($"[TEST] GetCustomRule => Name: {rName}, Occ: {rOcc}, StdTicket: {rTicket}, MaxInc: {rInc}, MaxDec: {rDec}, MaxAdj: {rMax}, MinAdj: {rMin}");
            //
            //// 7. Update the custom rule
            //SetCustomRule(testRuleId, "Alpha Updated", 55, 12, 22, 8, 18, 6);
            //var (_, uName, uOcc, uTicket, uInc, uDec, uMax, uMin) = GetCustomRule(testRuleId);
            //Mod.log.Info($"[TEST] Updated CustomRule => Name: {uName}, Occ: {uOcc}, StdTicket: {uTicket}, MaxInc: {uInc}, MaxDec: {uDec}, MaxAdj: {uMax}, MinAdj: {uMin}");
            //
            //// 8. Remove the custom rule
            //RemoveCustomRule(testRuleId);
            //Mod.log.Info($"[TEST] Removed CustomRule with ID: {testRuleId}");
            //
            //// 9. Confirm removal
            //var afterRemoval = GetCustomRules();
            //Mod.log.Info("[TEST] Remaining Custom Rules:");
            //foreach (var (id, name, occ, ticket, inc, dec, maxAdj, minAdj) in afterRemoval)
            //{
            //    Mod.log.Info($"[TEST] Remaining => ID: {id}, Name: {name}");
            //}
            //
            //Mod.log.Info("=== END OF ManageRouteSystem TEST ===");

            firstUpdate = true;

            this.Enabled = false;
        }

    }
}