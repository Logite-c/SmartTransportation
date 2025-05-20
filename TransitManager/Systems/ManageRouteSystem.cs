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

        public void setRouteRule(TransportType transportType, int routeId, int routeRuleId, bool disable)
        {
            var routeRule = new RouteRule(routeRuleId, disable);

            Entity routeEntity = GetRouteEntityFromId(routeId, transportType);
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

        public int getRouteRuleId(TransportType transportType, int routeId)
        {
            Entity routeEntity = GetRouteEntityFromId(routeId, transportType);
            RouteRule routeRule;

            if (EntityManager.TryGetComponent<RouteRule>(routeEntity, out routeRule))
            {
                return routeRule.customRule;
            } else
            {
               return 0; // Default rule ID
            }
        }

        public bool getRouteDisabled(TransportType transportType, int routeId)
        {
            Entity routeEntity = GetRouteEntityFromId(routeId, transportType);
            RouteRule routeRule;

            if (EntityManager.TryGetComponent<RouteRule>(routeEntity, out routeRule))
            {
                return routeRule.disabled;
            }
            else
            {
                return false;
            }
        }

        public (int, string)[] getRouteRuleNames()
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
            //setRouteRule(TransportType.Bus, 2, 1, true);
            //
            //if(firstUpdate)
            //{
            //    Mod.log.Info($"RouteRule: {getRouteRuleId(TransportType.Bus, 1)}");
            //    Mod.log.Info($"RouteDisabled: {getRouteDisabled(TransportType.Bus, 1)}");
            //    var routeNames = getRouteRuleNames();
            //    foreach (var (id, name) in routeNames)
            //    {
            //        Mod.log.Info($"Route ID: {id}, Name: {name}");
            //    }
            //    //this.Enabled = false;
            //}
            //
            //firstUpdate = true;

            this.Enabled = false;
        }
    }
}