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

        public void SetRouteRule(Entity routeEntity, int routeRuleId, bool disable)
        {
            var routeRule = new RouteRule(routeRuleId, disable);

            Mod.log.Info($"RouteEntity: {routeEntity}");
            if (EntityManager.HasComponent<RouteRule>(routeEntity))
            {
                EntityManager.SetComponentData(routeEntity, routeRule);
            }
            else
            {
                EntityManager.AddComponentData(routeEntity, routeRule);
            }
        }

        public (int customRuleId, bool isDisabled) GetRouteRuleInfo(Entity routeEntity)
        {
            if (EntityManager.TryGetComponent<RouteRule>(routeEntity, out RouteRule routeRule))
            {
                return (routeRule.customRule, routeRule.disabled);
            }
            else
            {
                return (0, false); // Default rule ID and not disabled
            }
        }

        public (int, string)[] GetRouteRuleNames()
        {
            return new (int, string)[]
            {
                (0, "No Custom Rule"),
                (1, Mod.m_Setting.name_Custom1),
                (2, Mod.m_Setting.name_Custom2),
                (3, Mod.m_Setting.name_Custom3),
                (4, Mod.m_Setting.name_Custom4),
                (5, Mod.m_Setting.name_Custom5)
            };
        }


        protected override void OnUpdate()
        {
            //Entity routeEntity = GetRouteEntityFromId(2, TransportType.Bus);
            //SetRouteRule(routeEntity, 1, true);
            //Mod.log.Info($"SetRouteRule set");
            //if (firstUpdate)
            //{
            //    Mod.log.Info($"GetRouteRuleInfo: {GetRouteRuleInfo(routeEntity)}");
            //
            //    var routeNames = GetRouteRuleNames();
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