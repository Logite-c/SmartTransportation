using Game;
using Game.Prefabs;
using Game.Routes;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Colossal.Serialization.Entities;
using Game.Companies;
using Colossal.Entities;
using Game.Common;
using Game.Vehicles;
using Game.Prefabs.Effects;
using UnityEngine.Rendering;
using Game.Simulation;
using System.ComponentModel.Design;
using Game.Agents;
using Game.Buildings;
using static Game.Rendering.Utilities.State;
using static Game.UI.InGame.VehiclesSection;
using Game.Policies;
using static Game.Input.UIInputActionCollection;
using static UnityEngine.GraphicsBuffer;
using Game.UI.InGame;
using static Game.Rendering.OverlayRenderSystem;
using Game.Pathfind;
using Unity.Mathematics;
using Game.Citizens;
using Game.City;

namespace SmartTransportation
{
    public partial class SmartTaxiSystem : GameSystemBase
    {
        private EntityQuery _query2;
        private EntityQuery _query3;
        private CitySystem m_CitySystem;
        public BufferLookup<CityModifier> m_CityModifiers;
        private EntityQuery m_ConfigQuery;
        private PrefabSystem m_PrefabSystem;
        private PoliciesUISystem m_PoliciesUISystem;

        private float avg_passengers_per_taxi = 1.2f;

        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_ConfigQuery = this.GetEntityQuery(ComponentType.ReadOnly<UITransportConfigurationData>());
            m_CityModifiers = SystemAPI.GetBufferLookup<CityModifier>(false);
            m_CitySystem = this.World.GetOrCreateSystemManaged<CitySystem>();
            m_PoliciesUISystem = this.World.GetOrCreateSystemManaged<PoliciesUISystem>();
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // One day (or month) in-game is '262144' ticks
            return 262144 /(int) Mod.m_Setting.updateFreq;
        }

        protected override void OnGameLoaded(Context serializationContext)
        {
            if (this.m_ConfigQuery.IsEmptyIgnoreFilter)
                return;

            //this.m_TicketPricePolicy = this.m_PrefabSystem.GetEntity((PrefabBase)this.m_PrefabSystem.GetSingletonPrefab<UITransportConfigurationPrefab>(this.m_ConfigQuery).m_TicketPricePolicy);
            //this.m_VehicleCountPolicy = this.m_PrefabSystem.GetEntity((PrefabBase)this.m_PrefabSystem.GetSingletonPrefab<UITransportConfigurationPrefab>(this.m_ConfigQuery).m_VehicleCountPolicy);
        }

        protected override void OnUpdate()
        {
            Mod.log.Info("Hello");
            
            _query2 = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadWrite<Game.Vehicles.Taxi>(),
                }
            });
            
            RequireForUpdate(_query2);
            
            _query3 = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadOnly<TaxiRequest>(),
                }
            });
            
            RequireForUpdate(_query3);
            
            var requests = _query3.ToEntityArray(Allocator.Temp);
            var taxis = _query2.ToEntityArray(Allocator.Temp);
            
            //int standardTaxiFee = Mod.m_Setting.standard_ticket_Taxi;
            //float occupancy = (1.2f*requests.Length)/(float)taxis.Length;
            //float newFee = (float)standardTaxiFee;
            //
            //if (occupancy > Mod.m_Setting.target_occupancy_Taxi + Mod.m_Setting.threshold)
            //{
            //    newFee++;
            //}
            //if (occupancy < Mod.m_Setting.target_occupancy_Taxi - Mod.m_Setting.threshold)
            //{
            //    newFee--;
            //}

            //m_PoliciesUISystem.ModifyPolicy(this.m_CityModifiers[m_CitySystem.City], policy, active, adjustment);
            ////CityUtils.ApplyModifier(ref newFee, this.m_CityModifiers[m_CitySystem.City], CityModifierType.TaxiStartingFee);
            //DynamicBuffer<CityModifier> modifiers;
            //if (EntityManager.TryGetBuffer<CityModifier>(m_CitySystem.City, false, out modifiers))
            //{
            //    if (modifiers.Length > (int)CityModifierType.TaxiStartingFee)
            //    {
            //        CityModifier modifier = modifiers[(int)CityModifierType.TaxiStartingFee];
            //        modifier.m_Delta.x = newFee;
            //        modifiers[(int)CityModifierType.TaxiStartingFee] = modifier;
            //        Mod.log.Info($"save: {modifiers[(int)CityModifierType.TaxiStartingFee].m_Delta}");
            //    }
            //}
            //
            //DynamicBuffer<CityModifier> modifiers = m_CityModifiers.TryGetBuffer[m_CitySystem.City];
            //
            //if (modifiers.Length > (int)CityModifierType.TaxiStartingFee)
            //{
            //    CityModifier modifier = modifiers[(int)CityModifierType.TaxiStartingFee];
            //    modifiers.RemoveAt((int)CityModifierType.TaxiStartingFee);
            //    modifier.m_Delta.x = newFee;
            //    modifiers.Insert((int)CityModifierType.TaxiStartingFee, modifier);
            //}
            //
            //m_CityModifiers[m_CitySystem.City] = modifiers;
            
            // if (Mod.m_Setting.debug)
            //{
            //    Mod.log.Info($"Number of Taxis: {taxis.Length}, Number of passengers waiting:{requests.Length}, Taxi Occupancy:{occupancy}: New Fee: {newFee}");
            //}

            //foreach (var taxi in taxis)
            //{
            //    Game.Vehicles.Taxi taxi_vehicle;
            //    if (EntityManager.TryGetComponent<Game.Vehicles.Taxi>(taxi, out taxi_vehicle))
            //    {
            //        taxi_vehicle.m_NextStartingFee = (ushort)newFee;
            //        EntityManager.SetComponentData(taxi, taxi_vehicle);
            //    }
            //}
            //
            //foreach (var stand in stands)
            //{
            //    TaxiStand taxi_stand;
            //    if (EntityManager.TryGetComponent<TaxiStand>(stand, out taxi_stand))
            //    {
            //        taxi_stand.m_StartingFee = (ushort)newFee;
            //        EntityManager.SetComponentData(stand, taxi_stand);
            //    }
            //}
        }
    }
}