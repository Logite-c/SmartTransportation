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
        private struct RouteConfig
        {
            public int OccupancyTarget;
            public int MaxTicketDiscount;
            public int MaxTicketIncrease;
            public int StandardTicketPrice;
            public float MinVehiclesAdj;
            public float MaxVehiclesAdj;
        }
        private struct RouteData
        {
            public int CurrentVehicles;
            public int EmptyVehicles;
            public int TotalPassengers;
            public int TotalWaiting;
            public int PassengerCapacityPerVehicle;
            public int MaxStopWaiting;
            public Entity BusiestStop;
            public float CurrentCapacityRatio;
        }

        private EntityQuery _query;
        private EntityQuery m_ConfigQuery;

        private Entity m_TicketPricePolicy;
        private Entity m_VehicleCountPolicy;

        private PrefabSystem m_PrefabSystem;
        private PoliciesUISystem m_PoliciesUISystem;
        private ManageRouteSystem m_ManageRouteSystem;

        [ReadOnly] private ComponentLookup<VehicleTiming> m_VehicleTimings;
        [ReadOnly] private ComponentLookup<PathInformation> m_PathInformations;
        [ReadOnly] public BufferLookup<RouteModifierData> m_RouteModifierDatas;
        [ReadOnly] public ComponentLookup<PolicySliderData> m_PolicySliderDatas;

        // Cached ComponentLookups for performance optimization
        [ReadOnly] private ComponentLookup<TransportLine> m_TransportLines;
        [ReadOnly] private ComponentLookup<PrefabRef> m_PrefabRefs;
        [ReadOnly] private ComponentLookup<RouteNumber> m_RouteNumbers;
        [ReadOnly] private ComponentLookup<TransportLineData> m_TransportLineDatas;
        [ReadOnly] private ComponentLookup<PublicTransportVehicleData> m_PublicTransportVehicleDatas;
        [ReadOnly] private ComponentLookup<TrainEngineData> m_TrainEngineDatas;
        [ReadOnly] private ComponentLookup<WaitingPassengers> m_WaitingPassengers;
        [ReadOnly] private ComponentLookup<Connected> m_Connecteds;
        [ReadOnly] private ComponentLookup<RouteRule> m_RouteRules;
        [ReadOnly] private BufferLookup<VehicleModel> m_VehicleModels;
        [ReadOnly] private BufferLookup<RouteVehicle> m_RouteVehicles;
        [ReadOnly] private BufferLookup<RouteWaypoint> m_RouteWaypoints;
        [ReadOnly] private BufferLookup<RouteSegment> m_RouteSegments;
        [ReadOnly] private BufferLookup<RouteModifier> m_RouteModifiers;
        [ReadOnly] private BufferLookup<Passenger> m_Passengers;
        [ReadOnly] private BufferLookup<VehicleCarriageElement> m_VehicleCarriageElements;

        // CustomChirps alert state
        private readonly Dictionary<Entity, bool> _busyStopAlerted = new Dictionary<Entity, bool>();

        // Alert settings
        private float BusyStopEnterPct => Mod.m_Setting.busy_stop_enter_pct / 100f;
        private float BusyStopExitPct => Mod.m_Setting.busy_stop_exit_pct / 100f;
        private int maxAlertsPerCycle = 1;
        private bool ChirpsEnabled => !Mod.m_Setting.disable_chirps;


        protected override void OnCreate()
        {
            base.OnCreate();
            m_ConfigQuery = GetEntityQuery(ComponentType.ReadOnly<UITransportConfigurationData>());
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_PoliciesUISystem = World.GetOrCreateSystemManaged<PoliciesUISystem>();
            m_ManageRouteSystem = World.GetOrCreateSystemManaged<ManageRouteSystem>();

            m_VehicleTimings = SystemAPI.GetComponentLookup<VehicleTiming>(true);
            m_PathInformations = SystemAPI.GetComponentLookup<PathInformation>(true);
            m_RouteModifierDatas = SystemAPI.GetBufferLookup<RouteModifierData>(true);
            m_PolicySliderDatas = SystemAPI.GetComponentLookup<PolicySliderData>(true);

            m_TransportLines = SystemAPI.GetComponentLookup<TransportLine>(true);
            m_PrefabRefs = SystemAPI.GetComponentLookup<PrefabRef>(true);
            m_RouteNumbers = SystemAPI.GetComponentLookup<RouteNumber>(true);
            m_TransportLineDatas = SystemAPI.GetComponentLookup<TransportLineData>(true);
            m_PublicTransportVehicleDatas = SystemAPI.GetComponentLookup<PublicTransportVehicleData>(true);
            m_TrainEngineDatas = SystemAPI.GetComponentLookup<TrainEngineData>(true);
            m_WaitingPassengers = SystemAPI.GetComponentLookup<WaitingPassengers>(true);
            m_Connecteds = SystemAPI.GetComponentLookup<Connected>(true);
            m_RouteRules = SystemAPI.GetComponentLookup<RouteRule>(true);
            m_VehicleModels = SystemAPI.GetBufferLookup<VehicleModel>(true);
            m_RouteVehicles = SystemAPI.GetBufferLookup<RouteVehicle>(true);
            m_RouteWaypoints = SystemAPI.GetBufferLookup<RouteWaypoint>(true);
            m_RouteSegments = SystemAPI.GetBufferLookup<RouteSegment>(true);
            m_RouteModifiers = SystemAPI.GetBufferLookup<RouteModifier>(true);
            m_Passengers = SystemAPI.GetBufferLookup<Passenger>(true);
            m_VehicleCarriageElements = SystemAPI.GetBufferLookup<VehicleCarriageElement>(true);

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
            return 262144 / (int)Mod.m_Setting.updateFreq;
        }

        protected override void OnGameLoaded(Context serializationContext)
        {
            if (this.m_ConfigQuery.IsEmptyIgnoreFilter)
                return;

            var prefab = this.m_PrefabSystem.GetSingletonPrefab<UITransportConfigurationPrefab>(this.m_ConfigQuery);
            this.m_TicketPricePolicy = this.m_PrefabSystem.GetEntity((PrefabBase)prefab.m_TicketPricePolicy);
            this.m_VehicleCountPolicy = this.m_PrefabSystem.GetEntity((PrefabBase)prefab.m_VehicleCountPolicy);
        }

        public float CalculateStableDuration(TransportLineData transportLineData, DynamicBuffer<RouteWaypoint> routeWaypoint, DynamicBuffer<RouteSegment> routeSegment)
        {
            int startIndex = 0;
            for (int index = 0; index < routeWaypoint.Length; ++index)
            {
                if (m_VehicleTimings.HasComponent(routeWaypoint[index].m_Waypoint))
                {
                    startIndex = index;
                    break;
                }
            }

            float stableDuration = 0.0f;
            for (int index = 0; index < routeWaypoint.Length; ++index)
            {
                int2 currentIndices = (int2)(startIndex + index);
                currentIndices.y++;
                currentIndices = math.select(currentIndices, currentIndices - routeWaypoint.Length, currentIndices >= routeWaypoint.Length);

                Entity waypointEntity = routeWaypoint[currentIndices.y].m_Waypoint;

                if (m_PathInformations.TryGetComponent(routeSegment[currentIndices.x].m_Segment, out PathInformation pathInfo))
                    stableDuration += pathInfo.m_Duration;

                if (m_VehicleTimings.HasComponent(waypointEntity))
                    stableDuration += transportLineData.m_StopDuration;
            }
            return stableDuration;
        }

        public static int CalculateVehicleCountFromAdjustment(float policyAdjustment, float interval, float duration, BufferLookup<RouteModifierData> routeModifierDatas, Entity vehicleCountPolicy, ComponentLookup<PolicySliderData> policySliderDatas)
        {
            RouteModifier modifier = new RouteModifier();
            if (routeModifierDatas.HasBuffer(vehicleCountPolicy))
            {
                DynamicBuffer<RouteModifierData> modifiers = routeModifierDatas[vehicleCountPolicy];
                foreach (RouteModifierData modifierData in modifiers)
                {
                    if (modifierData.m_Type == RouteModifierType.VehicleInterval)
                    {
                        float modifierDelta = RouteModifierInitializeSystem.RouteModifierRefreshData.GetModifierDelta(modifierData, policyAdjustment, vehicleCountPolicy, policySliderDatas);
                        RouteModifierInitializeSystem.RouteModifierRefreshData.AddModifierData(ref modifier, modifierData, modifierDelta);
                        break;
                    }
                }
            }
            interval += modifier.m_Delta.x;
            interval += interval * modifier.m_Delta.y;
            return TransportLineSystem.CalculateVehicleCount(interval, duration);
        }

        protected override void OnUpdate()
        {
            // 1. Initialize Route Manager System
            if (m_ManageRouteSystem == null)
            {
                m_ManageRouteSystem = this.World.GetOrCreateSystemManaged<ManageRouteSystem>();
            }

            // 2. Update ComponentLookups
            m_VehicleTimings.Update(this);
            m_PathInformations.Update(this);
            m_RouteModifierDatas.Update(this);
            m_PolicySliderDatas.Update(this);

            m_TransportLines.Update(this);
            m_PrefabRefs.Update(this);
            m_RouteNumbers.Update(this);
            m_TransportLineDatas.Update(this);
            m_PublicTransportVehicleDatas.Update(this);
            m_TrainEngineDatas.Update(this);
            m_WaitingPassengers.Update(this);
            m_Connecteds.Update(this);
            m_RouteRules.Update(this);
            m_VehicleModels.Update(this);
            m_RouteVehicles.Update(this);
            m_RouteWaypoints.Update(this);
            m_RouteSegments.Update(this);
            m_RouteModifiers.Update(this);
            m_Passengers.Update(this);
            m_VehicleCarriageElements.Update(this);

            // 3. Query all Transport Line Entities
            using var transports = _query.ToEntityArray(Allocator.Temp);

            // if (Mod.m_Setting.debug)
            // {
            //     Mod.log.Info($"[SmartTransit] OnUpdate Start. Total Routes Found: {transports.Length}");
            // }

            int alertsPostedThisCycle = 0;

            // 4. Iterate through each route and process logic
            foreach (var routeEntity in transports)
            {
                ProcessRoute(routeEntity, ref alertsPostedThisCycle);
            }
        }

        private RouteData GetRouteData(Entity routeEntity, TransportLineData transportLineData)
        {
            RouteData data = new RouteData();

            // 1. Capacity Calculation
            if (m_VehicleModels.TryGetBuffer(routeEntity, out var vehicleModels) && vehicleModels.Length > 0)
            {
                Entity primaryPrefab = Entity.Null;
                for (int i = 0; i < vehicleModels.Length; i++)
                {
                    if (vehicleModels[i].m_PrimaryPrefab != Entity.Null)
                    {
                        primaryPrefab = vehicleModels[i].m_PrimaryPrefab;
                        break;
                    }
                }
                if (primaryPrefab == Entity.Null) primaryPrefab = vehicleModels[0].m_PrimaryPrefab;

                if (primaryPrefab != Entity.Null && m_PublicTransportVehicleDatas.TryGetComponent(primaryPrefab, out var publicTransportVehicleData))
                {
                    int calculatedCapacity = publicTransportVehicleData.m_PassengerCapacity;
                    int engineCount = 1;

                    if (m_TrainEngineDatas.TryGetComponent(primaryPrefab, out var trainEngineData))
                    {
                        engineCount = trainEngineData.m_Count.x;
                        if (m_VehicleCarriageElements.TryGetBuffer(primaryPrefab, out var vehicleCarriage))
                        {
                            for (int i = 0; i < vehicleCarriage.Length; i++)
                            {
                                var carriage = vehicleCarriage[i];
                                if (m_PublicTransportVehicleDatas.HasComponent(carriage.m_Prefab))
                                {
                                    var ptvd = m_PublicTransportVehicleDatas[carriage.m_Prefab];
                                    calculatedCapacity += carriage.m_Count.x * ptvd.m_PassengerCapacity;
                                }
                            }
                        }
                    }

                    if (engineCount > 0) calculatedCapacity *= engineCount;
                    data.PassengerCapacityPerVehicle = calculatedCapacity;
                }
                else
                {
                    // if(Mod.m_Setting.debug) Mod.log.Info($"[DEBUG] Capacity Calc Failed. PrimaryPrefab is Null or Missing Data. Models count: {vehicleModels.Length}");
                }
            }
            if (data.PassengerCapacityPerVehicle == 0) data.PassengerCapacityPerVehicle = 0;

            // 2. Vehicle Statistics
            if (m_RouteVehicles.HasBuffer(routeEntity))
            {
                var vehicles = m_RouteVehicles[routeEntity];
                data.CurrentVehicles = vehicles.Length;

                foreach (var vehicle in vehicles)
                {
                    if (m_Passengers.TryGetBuffer(vehicle.m_Vehicle, out var passengers))
                    {
                        int count = passengers.Length;
                        data.TotalPassengers += count;
                        if (count == 0) data.EmptyVehicles++;
                    }
                    else
                    {
                        data.EmptyVehicles++;
                    }
                }
            }

            // 3. Stop Statistics
            if (m_RouteWaypoints.HasBuffer(routeEntity))
            {
                var waypoints = m_RouteWaypoints[routeEntity];
                foreach (var wp in waypoints)
                {
                    if (m_WaitingPassengers.TryGetComponent(wp.m_Waypoint, out var waiting))
                    {
                        data.TotalWaiting += waiting.m_Count;

                        if (waiting.m_Count > data.MaxStopWaiting)
                        {
                            data.MaxStopWaiting = waiting.m_Count;
                            if (m_Connecteds.TryGetComponent(wp.m_Waypoint, out var connected))
                            {
                                data.BusiestStop = connected.m_Connected;
                            }
                        }
                    }
                }
            }

            // 4. Capacity Ratio
            int totalCapacity = data.CurrentVehicles * data.PassengerCapacityPerVehicle;
            if (totalCapacity > 0)
            {
                data.CurrentCapacityRatio = (float)(data.TotalPassengers + data.TotalWaiting) / totalCapacity;
            }

            return data;
        }

        // [Main Logic] Analyzes a single route and applies changes (Price, Vehicles, Alerts)
        private void ProcessRoute(Entity routeEntity, ref int alertsPostedThisCycle)
        {
            var transportLine = m_TransportLines[routeEntity];
            var prefabRef = m_PrefabRefs[routeEntity];
            var routeNumber = m_RouteNumbers[routeEntity];
            var transportLineData = m_TransportLineDatas[prefabRef.m_Prefab];

            // if (Mod.m_Setting.debug)
            //     Mod.log.Info($"--- Processing Route #{routeNumber.m_Number} ({transportLineData.m_TransportType}) ---");

            bool hasCustomRule = m_RouteRules.TryGetComponent(routeEntity, out var routeRule);

            if (hasCustomRule && routeRule.customRule == default)
            {
                // if (Mod.m_Setting.debug) Mod.log.Info($"   -> Skipped: Invalid Custom Rule.");
                return;
            }

            RouteConfig config = GetRouteConfig(transportLineData.m_TransportType, hasCustomRule, routeRule);

            if (config.OccupancyTarget == 0)
            {
                // if (Mod.m_Setting.debug) Mod.log.Info($"   -> Skipped: Occupancy Target is 0 (Disabled in settings).");
                return;
            }

            RouteData data = GetRouteData(routeEntity, transportLineData);

            // if (Mod.m_Setting.debug)
            // {
            //     Mod.log.Info($"   -> Data: Vehicles={data.CurrentVehicles} (Empty: {data.EmptyVehicles}), CapPerVehicle={data.PassengerCapacityPerVehicle}, Passengers={data.TotalPassengers}, Waiting={data.TotalWaiting}");
            // }

            if (data.CurrentVehicles == 0)
            {
                //  if (Mod.m_Setting.debug) Mod.log.Info($"   -> Skipped: No vehicles active on route.");
                return;
            }


            DynamicBuffer<RouteWaypoint> waypoints = m_RouteWaypoints[routeEntity];
            DynamicBuffer<RouteSegment> routeSegments = m_RouteSegments[routeEntity];
            DynamicBuffer<RouteModifier> routeModifier = m_RouteModifiers[routeEntity];

            float defaultVehicleInterval = transportLineData.m_DefaultVehicleInterval;
            float vehicleInterval = defaultVehicleInterval;

            RouteUtils.ApplyModifier(ref vehicleInterval, routeModifier, RouteModifierType.VehicleInterval);

            float stableDuration = CalculateStableDuration(transportLineData, waypoints, routeSegments);

            float weightedCapacityRatio = 0f;
            int totalCapacity = data.CurrentVehicles * data.PassengerCapacityPerVehicle;
            if (totalCapacity > 0)
            {
                weightedCapacityRatio = (data.TotalPassengers + (data.TotalWaiting * Mod.m_Setting.waiting_time_weight)) / (float)totalCapacity;
            }

            // if (Mod.m_Setting.debug)
            // {
            //     Mod.log.Info($"   -> Calc: TotalCap={totalCapacity}, WeightedRatio={weightedCapacityRatio:F2}, StableDuration={stableDuration}");
            // }

            // Alert System
            if (ChirpsEnabled && CustomChirpsBridge.IsAvailable && data.BusiestStop != Entity.Null && alertsPostedThisCycle < maxAlertsPerCycle)
            {
                if (!EntityManager.Exists(data.BusiestStop))
                {
                    if (_busyStopAlerted.ContainsKey(data.BusiestStop)) _busyStopAlerted.Remove(data.BusiestStop);
                }
                else
                {
                    bool isStopBusy = data.MaxStopWaiting >= (data.PassengerCapacityPerVehicle * BusyStopEnterPct);
                    bool isRouteOverloaded = weightedCapacityRatio > (config.OccupancyTarget + 2 * Mod.m_Setting.threshold) / 100f;

                    bool shouldAlert = isStopBusy || isRouteOverloaded;
                    _busyStopAlerted.TryGetValue(data.BusiestStop, out bool alreadyAlerted);

                    if (shouldAlert && !alreadyAlerted)
                    {
                        if (isStopBusy)
                        {
                            string lineLabel = T2WStrings.T("chirp.line_label",
                                ("type", transportLineData.m_TransportType.ToString()),
                                ("number", routeNumber.m_Number.ToString()));

                            string msg = T2WStrings.T("chirp.stop_busy",
                                ("line_label", lineLabel),
                                ("waiting", data.MaxStopWaiting.ToString()));

                            CustomChirpsBridge.PostChirp(msg, DepartmentAccountBridge.Transportation, data.BusiestStop, T2WStrings.T("chirp.mod_name"));
                            // if (Mod.m_Setting.debug) Mod.log.Info($"   -> ALERT: Chirp posted for busy stop.");
                            alertsPostedThisCycle++;
                        }
                        _busyStopAlerted[data.BusiestStop] = true;
                    }
                    else if (!shouldAlert && alreadyAlerted)
                    {
                        if (data.MaxStopWaiting <= (data.PassengerCapacityPerVehicle * BusyStopExitPct) && !isRouteOverloaded)
                        {
                            _busyStopAlerted.Remove(data.BusiestStop);
                        }
                    }
                }
            }


            int ticketPrice = transportLine.m_TicketPrice;
            int currentVehicles = data.CurrentVehicles;
            int oldTicketPrice = ticketPrice;

            PolicySliderData policySliderData = m_PolicySliderDatas[m_VehicleCountPolicy];

            int maxVehicles = CalculateVehicleCountFromAdjustment(policySliderData.m_Range.max, defaultVehicleInterval, stableDuration, m_RouteModifierDatas, m_VehicleCountPolicy, m_PolicySliderDatas);
            int minVehicles = CalculateVehicleCountFromAdjustment(policySliderData.m_Range.min, defaultVehicleInterval, stableDuration, m_RouteModifierDatas, m_VehicleCountPolicy, m_PolicySliderDatas);

            int setVehicles = TransportLineSystem.CalculateVehicleCount(vehicleInterval, stableDuration);
            int oldVehicles = setVehicles;

            if (hasCustomRule)
            {
                minVehicles = (int)Math.Round(minVehicles * (1 - config.MinVehiclesAdj / 100f));
            }
            else
            {
                maxVehicles = (int)Math.Round(maxVehicles * (1 + config.MaxVehiclesAdj / 100f));
                minVehicles = (int)Math.Round(minVehicles * (1 - config.MinVehiclesAdj / 100f));
            }

            if (minVehicles < 1) minVehicles = 1;

            float targetRatio = config.OccupancyTarget / 100f;
            float threshold = Mod.m_Setting.threshold / 100f;

            // if (Mod.m_Setting.debug)
            // {
            //     Mod.log.Info($"   -> Limits: MinVeh={minVehicles}, MaxVeh={maxVehicles}, TargetRatio={targetRatio:F2}, Threshold={threshold:F2}");
            // }

            // ==================================================================================
            // Case A: Demand is too low (Reduce Vehicles/Price)
            // ==================================================================================
            if (weightedCapacityRatio < (targetRatio - threshold))
            {
                setVehicles--; // Decrease vehicle count
                // if (Mod.m_Setting.debug) Mod.log.Info($"   -> Action: Demand LOW. Decreasing vehicle.");

                // 1. Price is too high -> return to standard
                if (ticketPrice > config.StandardTicketPrice)
                {
                    ticketPrice--;
                    // if (Mod.m_Setting.debug) Mod.log.Info($"   -> Action: Price is too high. Returning to standard: {ticketPrice}");
                }
                // 2. Very low demand -> discount price
                else if (weightedCapacityRatio < (targetRatio - 2 * threshold))
                {
                    int minPriceLimit = (int)Math.Round((100 - config.MaxTicketDiscount) * config.StandardTicketPrice / 100f);

                    if (ticketPrice > minPriceLimit)
                    {
                        ticketPrice--;
                        // if (Mod.m_Setting.debug) Mod.log.Info($"   -> Action: Very LOW demand. Discounting Price to {ticketPrice}");
                    }
                }
            }
            // ==================================================================================
            // Case B: Demand is too high (Increase Vehicles/Price)
            // ==================================================================================
            else if (weightedCapacityRatio > (targetRatio + threshold))
            {
                setVehicles++; // Increase vehicle count
                // if (Mod.m_Setting.debug) Mod.log.Info($"   -> Action: Demand HIGH. Increasing vehicle.");

                // 1. Price is too low -> return to standard (Fix for 0 price issue)
                if (ticketPrice < config.StandardTicketPrice)
                {
                    ticketPrice++;
                    // if (Mod.m_Setting.debug) Mod.log.Info($"   -> Action: Price too low ({ticketPrice}). Raising towards standard ({config.StandardTicketPrice}).");
                }
                // 2. Very high demand -> increase price (surge pricing)
                else if (weightedCapacityRatio > (targetRatio + 2 * threshold))
                {
                    int maxPriceLimit = (int)Math.Round((100 + config.MaxTicketIncrease) * config.StandardTicketPrice / 100f);

                    if (ticketPrice < maxPriceLimit)
                    {
                        ticketPrice++;
                        // if (Mod.m_Setting.debug) Mod.log.Info($"   -> Action: Very HIGH demand. Increasing Price to {ticketPrice}");
                    }
                }
                // 3. Price is standard but demand is high -> start increasing
                else if (ticketPrice == config.StandardTicketPrice)
                {
                    ticketPrice++;
                    // if (Mod.m_Setting.debug) Mod.log.Info($"   -> Action: Raising price above standard: {ticketPrice}");
                }
            }
            else
            {
                //  if (Mod.m_Setting.debug) Mod.log.Info($"   -> Action: Demand STABLE. No changes.");
            }

            if (setVehicles > maxVehicles) setVehicles = maxVehicles;
            if (setVehicles < minVehicles) setVehicles = minVehicles;

            if (data.EmptyVehicles / (float)currentVehicles > 0.3f)
            {
                setVehicles = oldVehicles;
                // if (Mod.m_Setting.debug) Mod.log.Info($"   -> Abort: Too many empty vehicles ({data.EmptyVehicles}/{currentVehicles}). Reverting vehicle count.");
            }

            if (config.StandardTicketPrice == 0) ticketPrice = 0;

            // Apply Changes
            if (oldVehicles != setVehicles || oldTicketPrice != ticketPrice)
            {
                int isFree = ticketPrice > 0 ? 1 : 0;
                m_PoliciesUISystem.SetPolicy(routeEntity, m_TicketPricePolicy, isFree != 0, (float)ticketPrice);

                float newInterval = 100f / (stableDuration / (defaultVehicleInterval * setVehicles));
                m_PoliciesUISystem.SetPolicy(routeEntity, m_VehicleCountPolicy, true, newInterval);

                //  if (Mod.m_Setting.debug) 
                //     Mod.log.Info($"   -> APPLIED: Vehicles {oldVehicles}->{setVehicles}, Price {oldTicketPrice}->{ticketPrice}");
            }
        }

        private RouteConfig GetRouteConfig(TransportType type, bool hasCustomRule, RouteRule routeRule)
        {
            RouteConfig config = new RouteConfig();

            if (hasCustomRule)
            {
                var ruleData = m_ManageRouteSystem.GetCustomRule(routeRule.customRule);
                config.OccupancyTarget = ruleData.Item3;
                config.StandardTicketPrice = ruleData.Item4;
                config.MaxTicketIncrease = ruleData.Item5;
                config.MaxTicketDiscount = ruleData.Item6;
                config.MaxVehiclesAdj = ruleData.Item7;
                config.MinVehiclesAdj = ruleData.Item8;
            }
            else
            {
                switch (type)
                {
                    case TransportType.Bus:
                        config.OccupancyTarget = Mod.m_Setting.target_occupancy_bus;
                        config.StandardTicketPrice = Mod.m_Setting.standard_ticket_bus;
                        config.MaxTicketIncrease = Mod.m_Setting.max_ticket_increase_bus;
                        config.MaxTicketDiscount = Mod.m_Setting.max_ticket_discount_bus;
                        config.MaxVehiclesAdj = Mod.m_Setting.max_vahicles_adj_bus;
                        config.MinVehiclesAdj = Mod.m_Setting.min_vahicles_adj_bus;
                        break;
                    case TransportType.Tram:
                        config.OccupancyTarget = Mod.m_Setting.target_occupancy_Tram;
                        config.StandardTicketPrice = Mod.m_Setting.standard_ticket_Tram;
                        config.MaxTicketIncrease = Mod.m_Setting.max_ticket_increase_Tram;
                        config.MaxTicketDiscount = Mod.m_Setting.max_ticket_discount_Tram;
                        config.MaxVehiclesAdj = Mod.m_Setting.max_vahicles_adj_Tram;
                        config.MinVehiclesAdj = Mod.m_Setting.min_vahicles_adj_Tram;
                        break;
                    case TransportType.Subway:
                        config.OccupancyTarget = Mod.m_Setting.target_occupancy_Subway;
                        config.StandardTicketPrice = Mod.m_Setting.standard_ticket_Subway;
                        config.MaxTicketIncrease = Mod.m_Setting.max_ticket_increase_Subway;
                        config.MaxTicketDiscount = Mod.m_Setting.max_ticket_discount_Subway;
                        config.MaxVehiclesAdj = Mod.m_Setting.max_vahicles_adj_Subway;
                        config.MinVehiclesAdj = Mod.m_Setting.min_vahicles_adj_Subway;
                        break;
                    case TransportType.Train:
                        config.OccupancyTarget = Mod.m_Setting.target_occupancy_Train;
                        config.StandardTicketPrice = Mod.m_Setting.standard_ticket_Train;
                        config.MaxTicketIncrease = Mod.m_Setting.max_ticket_increase_Train;
                        config.MaxTicketDiscount = Mod.m_Setting.max_ticket_discount_Train;
                        config.MaxVehiclesAdj = Mod.m_Setting.max_vahicles_adj_Train;
                        config.MinVehiclesAdj = Mod.m_Setting.min_vahicles_adj_Train;
                        break;
                    case TransportType.Ship:
                        config.OccupancyTarget = Mod.m_Setting.target_occupancy_Ship;
                        config.StandardTicketPrice = Mod.m_Setting.standard_ticket_Ship;
                        config.MaxTicketIncrease = Mod.m_Setting.max_ticket_increase_Ship;
                        config.MaxTicketDiscount = Mod.m_Setting.max_ticket_discount_Ship;
                        config.MaxVehiclesAdj = Mod.m_Setting.max_vahicles_adj_Ship;
                        config.MinVehiclesAdj = Mod.m_Setting.min_vahicles_adj_Ship;
                        break;
                    case TransportType.Airplane:
                        config.OccupancyTarget = Mod.m_Setting.target_occupancy_Airplane;
                        config.StandardTicketPrice = Mod.m_Setting.standard_ticket_Airplane;
                        config.MaxTicketIncrease = Mod.m_Setting.max_ticket_increase_Airplane;
                        config.MaxTicketDiscount = Mod.m_Setting.max_ticket_discount_Airplane;
                        config.MaxVehiclesAdj = Mod.m_Setting.max_vahicles_adj_Airplane;
                        config.MinVehiclesAdj = Mod.m_Setting.min_vahicles_adj_Airplane;
                        break;
                    case TransportType.Ferry:
                        config.OccupancyTarget = Mod.m_Setting.target_occupancy_Ferry;
                        config.StandardTicketPrice = Mod.m_Setting.standard_ticket_Ferry;
                        config.MaxTicketIncrease = Mod.m_Setting.max_ticket_increase_Ferry;
                        config.MaxTicketDiscount = Mod.m_Setting.max_ticket_discount_Ferry;
                        config.MaxVehiclesAdj = Mod.m_Setting.max_vahicles_adj_Ferry;
                        config.MinVehiclesAdj = Mod.m_Setting.min_vahicles_adj_Ferry;
                        break;
                    default:
                        config.OccupancyTarget = 0;
                        config.StandardTicketPrice = 0;
                        break;
                }
            }
            return config;
        }
    }
}