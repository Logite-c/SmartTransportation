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
    public partial class ManageRouteBridge : GameSystemBase
    {
        private EntityQuery entityQuery;
        private Entity GetRouteEntityFromId(int routeId, TransportType transportType)
        {
            entityQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadOnly<RouteNumber>(),
                    ComponentType.ReadOnly<TransportLine>(),
                    ComponentType.ReadOnly<PrefabRef>()
                }
            });
            RequireForUpdate(entityQuery);

            var entities = entityQuery.ToEntityArray(Allocator.Temp);

            foreach (var ent in entities)
            {
                PrefabRef prefab;
                TransportLine transportLine;
                TransportLineData transportLineData;
                RouteNumber routeNumber;

                transportLine = EntityManager.GetComponentData<TransportLine>(ent);
                prefab = EntityManager.GetComponentData<PrefabRef>(ent);
                routeNumber = EntityManager.GetComponentData<RouteNumber>(ent);
                transportLineData = EntityManager.GetComponentData<TransportLineData>(prefab.m_Prefab);

                if (routeNumber.m_Number == routeId &&
                    transportLineData.m_TransportType == transportType)
                {
                    return ent;
                }
            }
            
            entities.Dispose();
            return Entity.Null; // Not found
        }

        public void setRouteRule(TransportType transportType, int routeId, int routeRuleId, bool disable)
        {
            var routeRule = new RouteRule(routeRuleId, disable);

            Entity routeEntity = GetRouteEntityFromId(routeId, transportType);

            if (EntityManager.HasComponent<RouteRule>(routeEntity))
            {
                EntityManager.SetComponentData(routeEntity, routeRule);
            }
            else
            {
                EntityManager.AddComponentData(routeEntity, routeRule);
            }
        }

        protected override void OnUpdate()
        {
            setRouteRule(TransportType.Bus, 1, 0, true);
            this.Enabled = false;
        }
    }
}