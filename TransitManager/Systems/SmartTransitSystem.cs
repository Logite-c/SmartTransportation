using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game;
using Game.Agents;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Pathfind;
using Game.Policies;
using Game.Prefabs;
using Game.Prefabs.Effects;
using Game.Routes;
using Game.Simulation;
using Game.UI.InGame;
using Game.Vehicles;
using SmartTransportation.Bridge;
using SmartTransportation.Components;
using SmartTransportation.Localization;
using SmartTransportation.Systems;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using UnityEngine.Rendering;
using static Game.Input.UIInputActionCollection;
using static Game.Prefabs.TriggerPrefabData;
using static Game.Rendering.OverlayRenderSystem;
using static Game.Rendering.Utilities.State;
using static Game.UI.InGame.VehiclesSection;
using static UnityEngine.GraphicsBuffer;
using RouteModifierInitializeSystem = SmartTransportation.Systems.RouteModifierInitializeSystem;

namespace SmartTransportation
{
    public partial class SmartTransitSystem : GameSystemBase
    {
        private Dictionary<Entity, TransportLine> _transportToData = new Dictionary<Entity, TransportLine>();

        private EntityQuery _query;
        private Entity m_TicketPricePolicy;
        private Entity m_VehicleCountPolicy;
        private EntityQuery m_ConfigQuery;
        private PrefabSystem m_PrefabSystem;
        private PoliciesUISystem m_PoliciesUISystem;
        [ReadOnly]
        private ComponentLookup<VehicleTiming> m_VehicleTimings;
        [ReadOnly]
        private ComponentLookup<PathInformation> m_PathInformations;
        [ReadOnly]
        public BufferLookup<RouteModifierData> m_RouteModifierDatas;
        [ReadOnly]
        public ComponentLookup<PolicySliderData> m_PolicySliderDatas;

        // --- CustomChirps alert state ----------------------------------------------
        // Track which STOPs we’ve already alerted for (avoids spam). We clear when load drops.
        private readonly Dictionary<Entity, bool> _busyStopAlerted = new Dictionary<Entity, bool>();

        // ---- CustomChirps capacity-based alert settings ----------------------------
        // % of a typical vehicle's capacity that must be waiting at a stop to alert.
        // e.g., 0.70 => chirp when waiting >= 70% of capacity.
        private float BusyStopEnterPct = Mod.m_Setting.busy_stop_enter_pct/100f;
        // Hysteresis clear level (e.g., 0.55 => clear once waiting < 55% of capacity)
        private float BusyStopExitPct = Mod.m_Setting.busy_stop_exit_pct/100f;
        private int maxAlertsPerCycle = 1; // implement a per-cycle limit to avoid overwhelming the player

        // Optional: gate all chirps with one toggle.
        private bool ChirpsEnabled = !Mod.m_Setting.disable_chirps;


        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_ConfigQuery = this.GetEntityQuery(ComponentType.ReadOnly<UITransportConfigurationData>());
            this.m_PrefabSystem = this.World.GetOrCreateSystemManaged<PrefabSystem>();
            this.m_PoliciesUISystem = this.World.GetOrCreateSystemManaged<PoliciesUISystem>();
            m_VehicleTimings = SystemAPI.GetComponentLookup<VehicleTiming>(true);
            m_PathInformations = SystemAPI.GetComponentLookup<PathInformation>(true);  
            m_RouteModifierDatas = SystemAPI.GetBufferLookup<RouteModifierData>(true);
            m_PolicySliderDatas = SystemAPI.GetComponentLookup<PolicySliderData>(true);

            _query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadWrite<TransportLine>(),
                    ComponentType.ReadOnly<VehicleModel>(),
                    ComponentType.ReadOnly<RouteNumber>(),
                    ComponentType.ReadOnly<PrefabRef>(),
                }
            });

            RequireForUpdate(_query);
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

            this.m_TicketPricePolicy = this.m_PrefabSystem.GetEntity((PrefabBase)this.m_PrefabSystem.GetSingletonPrefab<UITransportConfigurationPrefab>(this.m_ConfigQuery).m_TicketPricePolicy);
            this.m_VehicleCountPolicy = this.m_PrefabSystem.GetEntity((PrefabBase)this.m_PrefabSystem.GetSingletonPrefab<UITransportConfigurationPrefab>(this.m_ConfigQuery).m_VehicleCountPolicy);
        }
        public float CalculateStableDuration(TransportLineData transportLineData, DynamicBuffer<RouteWaypoint> routeWaypoint, DynamicBuffer<RouteSegment> routeSegment)
        {
            int num = 0;
            for (int index = 0; index < routeWaypoint.Length; ++index)
            {
                if (this.m_VehicleTimings.HasComponent(routeWaypoint[index].m_Waypoint))
                {
                    num = index;
                    break;
                }
            }
            float stableDuration = 0.0f;
            for (int index = 0; index < routeWaypoint.Length; ++index)
            {
                int2 a = (int2)(num + index);
                ++a.y;
                a = math.select(a, a - routeWaypoint.Length, a >= routeWaypoint.Length);
                Entity waypoint = routeWaypoint[a.y].m_Waypoint;
                PathInformation componentData;

                if (this.m_PathInformations.TryGetComponent(routeSegment[a.x].m_Segment, out componentData))
                    stableDuration += componentData.m_Duration;

                if (this.m_VehicleTimings.HasComponent(waypoint))
                    stableDuration += transportLineData.m_StopDuration;
            }
            return stableDuration;
        }

        public static int CalculateVehicleCountFromAdjustment(
        float policyAdjustment,
        float interval,
        float duration,
        BufferLookup<RouteModifierData> routeModifierDatas,
        Entity vehicleCountPolicy,
        ComponentLookup<PolicySliderData> policySliderDatas)
        {
            RouteModifier modifier = new RouteModifier();
            foreach (RouteModifierData modifierData in routeModifierDatas[vehicleCountPolicy])
            {
                if (modifierData.m_Type == RouteModifierType.VehicleInterval)
                {
                    float modifierDelta = RouteModifierInitializeSystem.RouteModifierRefreshData.GetModifierDelta(modifierData, policyAdjustment, vehicleCountPolicy, policySliderDatas);
                    RouteModifierInitializeSystem.RouteModifierRefreshData.AddModifierData(ref modifier, modifierData, modifierDelta);
                    break;
                }
            }
            interval += modifier.m_Delta.x;
            interval += interval * modifier.m_Delta.y;
            return TransportLineSystem.CalculateVehicleCount(interval, duration);
        }
        public static float CalculateAdjustmentFromVehicleCount(
       int vehicleCount,
       float originalInterval,
       float duration,
       DynamicBuffer<RouteModifierData> modifierDatas,
       PolicySliderData sliderData)
        {
            float vehicleInterval = TransportLineSystem.CalculateVehicleInterval(duration, vehicleCount);
            RouteModifier modifier = new RouteModifier();
            foreach (RouteModifierData modifierData in modifierDatas)
            {
                if (modifierData.m_Type == RouteModifierType.VehicleInterval)
                {
                    if (modifierData.m_Mode == ModifierValueMode.Absolute)
                        modifier.m_Delta.x = vehicleInterval - originalInterval;
                    else
                        modifier.m_Delta.y = (-originalInterval + vehicleInterval) / originalInterval;
                    float deltaFromModifier = RouteModifierInitializeSystem.RouteModifierRefreshData.GetDeltaFromModifier(modifier, modifierData);
                    return RouteModifierInitializeSystem.RouteModifierRefreshData.GetPolicyAdjustmentFromModifierDelta(modifierData, deltaFromModifier, sliderData);
                }
            }
            return -1f;
        }



        protected override void OnUpdate()
        {
            ManageRouteSystem manageRouteSystem = this.World.GetOrCreateSystemManaged<ManageRouteSystem>();

            using (var transports = _query.ToEntityArray(Allocator.Temp))
            {
                if (Mod.m_Setting.debug)
                {
                    Mod.log.Info($"Updating {transports.Length} transit routes");
                }

                int numAlerts = 0; // track how many alerts we post this cycle

                foreach (var trans in transports)
                {

                    PrefabRef prefab;
                    TransportLineData transportLineData;
                    TransportLine transportLine;
                    VehicleModel vehicleModel;
                    PublicTransportVehicleData publicTransportVehicleData;
                    RouteNumber routeNumber;

                    transportLine = EntityManager.GetComponentData<TransportLine>(trans);
                    prefab = EntityManager.GetComponentData<PrefabRef>(trans);
                    routeNumber = EntityManager.GetComponentData<RouteNumber>(trans);
                    RouteRule routeRule;

                    transportLineData = EntityManager.GetComponentData<TransportLineData>(prefab.m_Prefab);
                    bool hasCustomRule = EntityManager.TryGetComponent<RouteRule>(trans, out routeRule);

                    if (hasCustomRule && routeRule.customRule == default)
                    {
                        if (Mod.m_Setting.debug)
                        {
                            Mod.log.Info($"Transport Type: {transportLineData.m_TransportType}, Route Number: {routeNumber.m_Number} - Disabled");
                        }
                        continue;
                    }
                    else
                    {
                        //Routes with custom rules will not be disabled by the settings
                        //If some modes are disabled, continue to the next transport line
                        switch (transportLineData.m_TransportType)
                        {
                            case TransportType.Bus:
                                if (Mod.m_Setting.disable_bus)
                                {
                                    continue;
                                }
                                break;
                            case TransportType.Tram:
                                if (Mod.m_Setting.disable_Tram)
                                {
                                    continue;
                                }
                                break;
                            case TransportType.Subway:
                                if (Mod.m_Setting.disable_Subway)
                                {
                                    continue;
                                }
                                break;
                            case TransportType.Train:
                                if (Mod.m_Setting.disable_Train)
                                {
                                    continue;
                                }
                                break;
                            case TransportType.Ship:
                                if (Mod.m_Setting.disable_Ship) continue;
                                break;
                            case TransportType.Airplane:
                                if (Mod.m_Setting.disable_Airplane) continue;
                                break;
                            default:
                                break;
                        }
                    }

                    DynamicBuffer<VehicleModel> vehicleModels;
                    if (EntityManager.TryGetBuffer<VehicleModel>(trans, /*isReadOnly:*/ true, out vehicleModels) && vehicleModels.Length > 0)
                    {
                        // Pick the first valid primary prefab from the buffer
                        Entity primaryPrefab = Entity.Null;
                        for (int i = 0; i < vehicleModels.Length; i++)
                        {
                            var vm = vehicleModels[i];
                            if (vm.m_PrimaryPrefab != Entity.Null)
                            {
                                primaryPrefab = vm.m_PrimaryPrefab;
                                break;
                            }
                        }
                        // If none were explicitly set, fall back to element 0 (may still be Null; we guard below)
                        if (primaryPrefab == Entity.Null)
                            primaryPrefab = vehicleModels[0].m_PrimaryPrefab;

                        if (primaryPrefab == Entity.Null)
                            continue; // nothing usable on this line yet

                        if (EntityManager.TryGetComponent<PublicTransportVehicleData>(primaryPrefab, out publicTransportVehicleData))
                        {
                            DynamicBuffer<RouteVehicle> vehicles = EntityManager.GetBuffer<RouteVehicle>(trans);

                            int passengers = 0;
                            int emptyVehicles = 0;
                            for (int i = 0; i < vehicles.Length; i++)
                            {
                                RouteVehicle vehicle = vehicles[i];
                                if (EntityManager.TryGetBuffer<Passenger>(vehicle.m_Vehicle, true, out var pax))
                                {
                                    if (pax.Length == 0) emptyVehicles++;
                                    passengers += pax.Length;
                                }
                            }

                            int passenger_capacity = publicTransportVehicleData.m_PassengerCapacity;
                            int num2 = 1;
                            if (EntityManager.TryGetComponent<TrainEngineData>(primaryPrefab, out var trainEgineData))
                            {
                                num2 = trainEgineData.m_Count.x;

                                if (EntityManager.TryGetBuffer<VehicleCarriageElement>(primaryPrefab, true, out var vehicleCarriage))
                                {
                                    for (int i = 0; i < vehicleCarriage.Length; i++)
                                    {
                                        var carriage = vehicleCarriage[i];
                                        var ptvd = EntityManager.GetComponentData<PublicTransportVehicleData>(carriage.m_Prefab);
                                        passenger_capacity += carriage.m_Count.x * ptvd.m_PassengerCapacity;
                                    }
                                }
                            }
                            if (num2 > 0)
                            {
                                passenger_capacity *= num2;
                            }

                            DynamicBuffer<RouteWaypoint> waypoints = EntityManager.GetBuffer<RouteWaypoint>(trans);
                            int waiting = 0;

                            // Track the busiest stop on this route
                            int maxStopWaiting = 0;
                            Entity busiestStop = Entity.Null;

                            for (int i = 0; i < waypoints.Length; i++)
                            {
                                RouteWaypoint waypoint = waypoints[i];
                                WaitingPassengers waitingPax;
                                if (EntityManager.TryGetComponent<WaitingPassengers>(waypoint.m_Waypoint, out waitingPax))
                                {
                                    waiting += waitingPax.m_Count;
                                    Connected connected;
                                    if (EntityManager.TryGetComponent<Connected>(waypoint.m_Waypoint, out connected))
                                    {
                                        //Mod.log.Info($"Stop {i}: {waitingPax.m_Count} waiting");
                                        if (waitingPax.m_Count > maxStopWaiting)
                                        {
                                            maxStopWaiting = waitingPax.m_Count;
                                            busiestStop = connected.m_Connected;   // This is the clickable stop entity
                                        }
                                    }
                                        
                                }
                            }


                            if (vehicles.Length == 0)
                            {
                                continue;
                            }

                            DynamicBuffer<RouteSegment> routeSegments = EntityManager.GetBuffer<RouteSegment>(trans);
                            DynamicBuffer<RouteModifier> routeModifier = EntityManager.GetBuffer<RouteModifier>(trans);

                            float defaultVehicleInterval = transportLineData.m_DefaultVehicleInterval;
                            float vehicleInterval = defaultVehicleInterval;
                            RouteUtils.ApplyModifier(ref vehicleInterval, routeModifier, RouteModifierType.VehicleInterval);

                            float stableDuration = CalculateStableDuration(transportLineData, waypoints, routeSegments);

                            //Half weight for waiting passengers, the assumption is that when they board, a similar amount will deboard
                            float capacity = (passengers + waiting * Mod.m_Setting.waiting_time_weight) / ((float)passenger_capacity * vehicles.Length);

                            int ticketPrice = transportLine.m_TicketPrice;
                            int oldTicketPrice = ticketPrice;
                            int currentVehicles = vehicles.Length;
                            PolicySliderData policySliderData = EntityManager.GetComponentData<PolicySliderData>(m_VehicleCountPolicy);
                            int maxVehicles = CalculateVehicleCountFromAdjustment(policySliderData.m_Range.max, defaultVehicleInterval, stableDuration, this.m_RouteModifierDatas, this.m_VehicleCountPolicy, this.m_PolicySliderDatas); ;
                            int minVehicles = CalculateVehicleCountFromAdjustment(policySliderData.m_Range.min, defaultVehicleInterval, stableDuration, this.m_RouteModifierDatas, this.m_VehicleCountPolicy, this.m_PolicySliderDatas); ;
                            int setVehicles = TransportLineSystem.CalculateVehicleCount(vehicleInterval, stableDuration); ;
                            int oldVehicles = setVehicles;
                            int occupancy = 0;
                            int max_discount = 0;
                            int max_increase = 0;
                            int standard_ticket = 0;

                            if (hasCustomRule)
                            {
                                int maxVehiclesAdj = 0;
                                int minVehiclesAdj = 0;
                                string name = default;

                                (_, name, occupancy, standard_ticket, max_increase, max_discount, maxVehiclesAdj, minVehiclesAdj) = manageRouteSystem.GetCustomRule(routeRule.customRule);
                                minVehicles = (int)Math.Round(minVehicles * (1 - minVehiclesAdj / 100f));

                                //Mod.log.Info($"Route:{routeNumber.m_Number}, Type:{transportLineData.m_TransportType}, Custom Rule:{routeRule.customRule}, Standard Ticket Price:{standard_ticket}, Number of Vehicles:{setVehicles}, Max Vehicles:{maxVehicles}, Min Vehicles:{minVehicles}, Empty Vehicles:{emptyVehicles}, Passengers:{passengers}, Waiting Passengers:{waiting}, Occupancy:{capacity}, Target Occupancy:{occupancy/100f}");
                            }
                            else
                            {
                                switch (transportLineData.m_TransportType)
                                {
                                    case TransportType.Bus:
                                        occupancy = Mod.m_Setting.target_occupancy_bus;
                                        max_discount = Mod.m_Setting.max_ticket_discount_bus;
                                        max_increase = Mod.m_Setting.max_ticket_increase_bus;
                                        maxVehicles = (int)Math.Round(maxVehicles * (1 + Mod.m_Setting.max_vahicles_adj_bus / 100f));
                                        minVehicles = (int)Math.Round(minVehicles * (1 - Mod.m_Setting.min_vahicles_adj_bus / 100f));
                                        standard_ticket = Mod.m_Setting.standard_ticket_bus;
                                        break;
                                    case TransportType.Tram:
                                        occupancy = Mod.m_Setting.target_occupancy_Tram;
                                        max_discount = Mod.m_Setting.max_ticket_discount_Tram;
                                        max_increase = Mod.m_Setting.max_ticket_increase_Tram;
                                        maxVehicles = (int)Math.Round(maxVehicles * (1 + Mod.m_Setting.max_vahicles_adj_Tram / 100f));
                                        minVehicles = (int)Math.Round(minVehicles * (1 - Mod.m_Setting.min_vahicles_adj_Tram / 100f));
                                        standard_ticket = Mod.m_Setting.standard_ticket_Tram;
                                        break;
                                    case TransportType.Subway:
                                        occupancy = Mod.m_Setting.target_occupancy_Subway;
                                        max_discount = Mod.m_Setting.max_ticket_discount_Subway;
                                        max_increase = Mod.m_Setting.max_ticket_increase_Subway;
                                        maxVehicles = (int)Math.Round(maxVehicles * (1 + Mod.m_Setting.max_vahicles_adj_Subway / 100f));
                                        minVehicles = (int)Math.Round(minVehicles * (1 - Mod.m_Setting.min_vahicles_adj_Subway / 100f));
                                        standard_ticket = Mod.m_Setting.standard_ticket_Subway;
                                        break;
                                    case TransportType.Train:
                                        occupancy = Mod.m_Setting.target_occupancy_Train;
                                        max_discount = Mod.m_Setting.max_ticket_discount_Train;
                                        max_increase = Mod.m_Setting.max_ticket_increase_Train;
                                        minVehicles = (int)Math.Round(minVehicles * (1 - Mod.m_Setting.min_vahicles_adj_Train / 100f));
                                        maxVehicles = (int)Math.Round(maxVehicles * (1 + Mod.m_Setting.max_vahicles_adj_Train / 100f));
                                        standard_ticket = Mod.m_Setting.standard_ticket_Train;
                                        break;
                                    case TransportType.Ship:
                                        occupancy = Mod.m_Setting.target_occupancy_Ship;
                                        max_discount = Mod.m_Setting.max_ticket_discount_Ship;
                                        max_increase = Mod.m_Setting.max_ticket_increase_Ship;
                                        maxVehicles = (int)Math.Round(maxVehicles * (1 + Mod.m_Setting.max_vahicles_adj_Ship / 100f));
                                        minVehicles = (int)Math.Round(minVehicles * (1 - Mod.m_Setting.min_vahicles_adj_Ship / 100f));
                                        standard_ticket = Mod.m_Setting.standard_ticket_Ship;
                                        break;
                                    case TransportType.Airplane:
                                        occupancy = Mod.m_Setting.target_occupancy_Airplane;
                                        max_discount = Mod.m_Setting.max_ticket_discount_Airplane;
                                        max_increase = Mod.m_Setting.max_ticket_increase_Airplane;
                                        maxVehicles = (int)Math.Round(maxVehicles * (1 + Mod.m_Setting.max_vahicles_adj_Airplane / 100f));
                                        minVehicles = (int)Math.Round(minVehicles * (1 - Mod.m_Setting.min_vahicles_adj_Airplane / 100f));
                                        standard_ticket = Mod.m_Setting.standard_ticket_Airplane;
                                        break;

                                    default:
                                        continue;
                                }
                            }
                            ticketPrice = standard_ticket;

                            if (minVehicles < 1)
                            {
                                minVehicles = 1;
                            }

                            // ---- Busy stop / route alert via CustomChirps --------------------------------
                            if (ChirpsEnabled && CustomChirpsBridge.IsAvailable && busiestStop != Entity.Null && numAlerts < maxAlertsPerCycle)
                            {
                                // Decide if the stop is "busy" using the per-stop queue size
                                bool isBusy = maxStopWaiting >= passenger_capacity* BusyStopEnterPct;

                                // Optional: also consider route-wide crowding vs target occupancy
                                // Busy if we're above target + 2*threshold (same logic style you already use below)
                                bool routeOverTarget = capacity > (occupancy + 2 * Mod.m_Setting.threshold) / 100f;

                                bool shouldAlert = isBusy || routeOverTarget;

                                // Check existing state for THIS stop so we don’t spam
                                _busyStopAlerted.TryGetValue(busiestStop, out bool alreadyAlerted);

                                if (shouldAlert && !alreadyAlerted)
                                {
                                    if (isBusy)
                                    {
                                        string lineLabel = T2WStrings.T(
                                            "chirp.line_label",
                                            ("type", transportLineData.m_TransportType.ToString()),
                                            ("number", routeNumber.m_Number.ToString())
                                        );

                                        // Localized crowded stop message
                                        string msg = T2WStrings.T(
                                            "chirp.stop_busy",
                                            ("line_label", lineLabel),
                                            ("waiting", maxStopWaiting.ToString())
                                        );

                                        CustomChirpsBridge.PostChirp(
                                        text: msg,
                                        department: DepartmentAccountBridge.Transportation,
                                        entity: busiestStop,                // clickable stop entity
                                        customSenderName: T2WStrings.T("chirp.mod_name")    // shows who’s speaking
                                        );

                                        numAlerts++;
                                    }

                                    _busyStopAlerted[busiestStop] = true;   // arm the anti-spam latch
                                }
                                else if (!shouldAlert && alreadyAlerted)
                                {
                                    // Clear the latch when the stop cools down; use hysteresis so it doesn’t flap
                                    if (maxStopWaiting <= passenger_capacity*BusyStopExitPct && !routeOverTarget)
                                    {
                                        _busyStopAlerted.Remove(busiestStop);
                                    }
                                }
                            }


                            //Calculating ticket change. If capacity is within +- 10% points of target occupancy no change
                            // If price was reduced or increased from standard ticket but is within +- 20% points from target occupancy also no change
                            if (capacity < (occupancy - Mod.m_Setting.threshold) / 100f)
                            {
                                setVehicles--;
                                if (ticketPrice < standard_ticket && capacity < (occupancy - 2 * Mod.m_Setting.threshold) / 100f)
                                {
                                    if (ticketPrice > Math.Round((100 - max_discount) * standard_ticket / 100f))
                                    {
                                        ticketPrice--;
                                    }
                                    ////If occupancy is not too low, we don't need to have a very small number of vehicles
                                    //if (setVehicles == minVehicles)
                                    //{
                                    //    setVehicles++;
                                    //}
                                }
                                else if (ticketPrice == standard_ticket)
                                {
                                    ticketPrice--;
                                }
                            }
                            else if (capacity > (occupancy + Mod.m_Setting.threshold) / 100f)
                            {
                                if (ticketPrice > standard_ticket && capacity > (occupancy + 2 * Mod.m_Setting.threshold) / 100f)
                                {
                                    if (ticketPrice < Math.Round((100 + max_increase) * standard_ticket / 100f))
                                    {
                                        ticketPrice++;
                                    }
                                }
                                else if (ticketPrice == standard_ticket)
                                {
                                    ticketPrice++;
                                }
                                setVehicles++;
                            }

                            if (setVehicles > maxVehicles)
                            {
                                setVehicles = maxVehicles;
                            }
                            if (setVehicles < minVehicles)
                            {
                                setVehicles = minVehicles;
                            }

                            //If too many empty vehicles, don't update
                            if (emptyVehicles / (float)oldVehicles > 0.3f)
                            {
                                setVehicles = oldVehicles;
                            }

                            if (standard_ticket == 0)
                            {
                                ticketPrice = 0;
                            }

                            int num1 = ticketPrice > 0 ? 1 : 0;
                            DynamicBuffer<RouteModifierData> buffer = EntityManager.GetBuffer<RouteModifierData>(m_VehicleCountPolicy, true);
                            m_PoliciesUISystem.SetPolicy(trans, m_TicketPricePolicy, num1 != 0, (float)ticketPrice);
                            vehicleInterval = 100f / (stableDuration / (defaultVehicleInterval * setVehicles));
                            //m_PoliciesUISystem.SetPolicy(trans, m_VehicleCountPolicy, true, CalculateAdjustmentFromVehicleCount(setVehicles, transportLineData.m_DefaultVehicleInterval, stableDuration, buffer, policySliderData));
                            m_PoliciesUISystem.SetPolicy(trans, m_VehicleCountPolicy, true, vehicleInterval);

                            if (Mod.m_Setting.debug && (oldVehicles != setVehicles || ticketPrice != oldTicketPrice))
                            {
                                Mod.log.Info($"Route:{routeNumber.m_Number}, Type:{transportLineData.m_TransportType}, Ticket Price:{ticketPrice}, Number of Vehicles:{setVehicles}, Max Vehicles:{maxVehicles}, Min Vehicles:{minVehicles}, Empty Vehicles:{emptyVehicles}, Passengers:{passengers}, Waiting Passengers:{waiting}, Occupancy:{capacity}, Target Occupancy:{occupancy / 100f}");
                            }
                        }
                    }
                }
            }  
        }
    }
}