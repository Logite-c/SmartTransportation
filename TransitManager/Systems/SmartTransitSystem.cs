using Colossal.Serialization.Entities;
using Game;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Game.UI.InGame;
using Game.Vehicles;
using SmartTransportation.Bridge;
using SmartTransportation.Components;
using SmartTransportation.Localization;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using RouteModifierInitializeSystem = SmartTransportation.Systems.RouteModifierInitializeSystem;

namespace SmartTransportation
{
    public partial class SmartTransitSystem : GameSystemBase
    {
        private struct RouteConfig
        {// Configuration parameters for each transport type
            public int OccupancyTarget;
            public int MaxTicketDiscount;
            public int MaxTicketIncrease;
            public int StandardTicketPrice;
            public float MinVehiclesAdj;
            public float MaxVehiclesAdj;
        }
        private struct RouteData
        {// Data collected per transport line during update
            public int CurrentVehicles;
            public int EmptyVehicles;
            public int TotalPassengers;
            public int TotalWaiting;
            public int PassengerCapacityPerVehicle;
            public int MaxStopWaiting;
            public Entity BusiestStop;
            public float CurrentCapacityRatio;
        }

        //private Dictionary<Entity, TransportLine> _transportToData = new Dictionary<Entity, TransportLine>();

        private EntityQuery _query;
        private EntityQuery m_ConfigQuery;

        private Entity m_TicketPricePolicy;
        private Entity m_VehicleCountPolicy;

        private PrefabSystem m_PrefabSystem;
        private PoliciesUISystem m_PoliciesUISystem;
        //For Caching ManageRouteSystem class(like pointer?) OnCreate. So we don't need to create it again and again in OnUpdate
        private ManageRouteSystem m_ManageRouteSystem;

        [ReadOnly] private ComponentLookup<VehicleTiming> m_VehicleTimings;
        [ReadOnly] private ComponentLookup<PathInformation> m_PathInformations;
        [ReadOnly] public BufferLookup<RouteModifierData> m_RouteModifierDatas;
        [ReadOnly] public ComponentLookup<PolicySliderData> m_PolicySliderDatas;

        //For Enhancing performance by caching ComponentLookups
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
        // --- CustomChirps alert state ----------------------------------------------
        // Track which STOPs we’ve already alerted for (avoids spam). We clear when load drops.
        private readonly Dictionary<Entity, bool> _busyStopAlerted = new Dictionary<Entity, bool>();

        // ---- CustomChirps capacity-based alert settings ----------------------------
        // % of a typical vehicle's capacity that must be waiting at a stop to alert.
        // e.g., 0.70 => chirp when waiting >= 70% of capacity.
        private float BusyStopEnterPct => Mod.m_Setting.busy_stop_enter_pct / 100f;
        // Hysteresis clear level (e.g., 0.55 => clear once waiting < 55% of capacity)
        private float BusyStopExitPct => Mod.m_Setting.busy_stop_exit_pct / 100f;
        private int maxAlertsPerCycle = 1; // implement a per-cycle limit to avoid overwhelming the player

        // Optional: gate all chirps with one toggle.
        private bool ChirpsEnabled => !Mod.m_Setting.disable_chirps;


        protected override void OnCreate()
        {
            base.OnCreate();

            // Query to retrieve global transport configuration settings
            m_ConfigQuery = GetEntityQuery(ComponentType.ReadOnly<UITransportConfigurationData>());

            // Retrieve and cache required systems
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_PoliciesUISystem = World.GetOrCreateSystemManaged<PoliciesUISystem>();
            //Setup ManageRouteSystem cache
            m_ManageRouteSystem = World.GetOrCreateSystemManaged<ManageRouteSystem>();

            // Initialize ComponentLookups (using ReadOnly for performance)
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

            // Query to retrieve all transport lines with required components
            _query = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadWrite<TransportLine>(),
                    ComponentType.ReadOnly<VehicleModel>(),
                    ComponentType.ReadOnly<RouteNumber>(),
                    ComponentType.ReadOnly<PrefabRef>(),
                }
            });
            // Ensure OnUpdate is only called when matching entities exist
            RequireForUpdate(_query);
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // One day (or month) in-game is '262144' ticks
            return 262144 / (int)Mod.m_Setting.updateFreq;
        }

        // OnGameLoaded is called after all data is loaded from a save game
        protected override void OnGameLoaded(Context serializationContext)
        {
            //abort if configuration data is missing
            if (this.m_ConfigQuery.IsEmptyIgnoreFilter)
                return;

            // Retrieve singleton prefab and cache policy entities
            var prefab = this.m_PrefabSystem.GetSingletonPrefab<UITransportConfigurationPrefab>(this.m_ConfigQuery);
            this.m_TicketPricePolicy = this.m_PrefabSystem.GetEntity((PrefabBase)prefab.m_TicketPricePolicy);
            this.m_VehicleCountPolicy = this.m_PrefabSystem.GetEntity((PrefabBase)prefab.m_VehicleCountPolicy);
        }

        // Calculates the total stable duration including travel time and stop delays
        public float CalculateStableDuration(TransportLineData transportLineData, DynamicBuffer<RouteWaypoint> routeWaypoint, DynamicBuffer<RouteSegment> routeSegment)
        {
            int startIndex = 0;

            // Find the first waypoint that has VehicleTiming component 
            for (int index = 0; index < routeWaypoint.Length; ++index)
            {
                if (m_VehicleTimings.HasComponent(routeWaypoint[index].m_Waypoint))
                {
                    startIndex = index;
                    break;
                }
            }

            float stableDuration = 0.0f;

            // Add durations for each segment and stop
            for (int index = 0; index < routeWaypoint.Length; ++index)
            {
                // Current waypoint and segment indices with wrap-around
                int2 currentIndices = (int2)(startIndex + index);
                currentIndices.y++;

                // Wrap around logic
                //if (currentIndices>= routeWaypoint.Length) currentIndices -= routeWaypoint.Length;
                currentIndices = math.select(currentIndices, currentIndices - routeWaypoint.Length, currentIndices >= routeWaypoint.Length);

                Entity waypointEntity = routeWaypoint[currentIndices.y].m_Waypoint;

                //Segment duration
                if (m_PathInformations.TryGetComponent(routeSegment[currentIndices.x].m_Segment, out PathInformation pathInfo))
                    stableDuration += pathInfo.m_Duration;

                //Stop duration
                if (m_VehicleTimings.HasComponent(waypointEntity))
                    stableDuration += transportLineData.m_StopDuration;
            }
            return stableDuration;
        }

        // Static helper to calculate exact vehicle count from policy slider adjustment
        public static int CalculateVehicleCountFromAdjustment(
            float policyAdjustment,
            float interval,
            float duration,
            BufferLookup<RouteModifierData> routeModifierDatas,
            Entity vehicleCountPolicy,
            ComponentLookup<PolicySliderData> policySliderDatas)
        {
            RouteModifier modifier = new RouteModifier();

            // Check Policy Modifiers
            if (routeModifierDatas.HasBuffer(vehicleCountPolicy))
            {
                DynamicBuffer<RouteModifierData> modifiers = routeModifierDatas[vehicleCountPolicy];
                foreach (RouteModifierData modifierData in modifiers)
                {
                    // Check for Vehicle Interval type
                    if (modifierData.m_Type == RouteModifierType.VehicleInterval)
                    {
                        // Calculate Modifier Delta from Slider Adjustment
                        float modifierDelta = RouteModifierInitializeSystem.RouteModifierRefreshData.GetModifierDelta(modifierData, policyAdjustment, vehicleCountPolicy, policySliderDatas);

                        // Apply Modifier Delta to Route Modifier
                        RouteModifierInitializeSystem.RouteModifierRefreshData.AddModifierData(ref modifier, modifierData, modifierDelta);
                        break;
                    }
                }
            }

            // Finalize interval with modifier applied
            interval += modifier.m_Delta.x;
            interval += interval * modifier.m_Delta.y;

            // Calculate and return vehicle count
            return TransportLineSystem.CalculateVehicleCount(interval, duration);
        }
        /*public static float CalculateAdjustmentFromVehicleCount(
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
        }*/

        protected override void OnUpdate()
        {
            // 1. Initialize Route Manager System
            // Ensure m_ManageRouteSystem is available (ProcessRoute needs it to check custom rules).
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
            // Convert the entity query to an array to iterate over them.
            using var transports = _query.ToEntityArray(Allocator.Temp);

            // Debug Logging (Only if debug mode is enabled in Mod settings)
            if (Mod.m_Setting.debug)
            {
                Mod.log.Info($"Updating {transports.Length} transit routes");
            }

            // 4. Reset Alert Counter
            // We limit the number of Chirps per update cycle to prevent spamming the player.
            int alertsPostedThisCycle = 0;

            // 5. Iterate through each route and process logic
            foreach (var routeEntity in transports)
            {
                // Delegate all heavy logic to the ProcessRoute function.
                // We pass 'alertsPostedThisCycle' by reference to track global alert limits.
                ProcessRoute(routeEntity, ref alertsPostedThisCycle);
            }
        }

        private RouteData GetRouteData(Entity routeEntity, TransportLineData transportLineData)
        {
            RouteData data = new RouteData();

            // ------------------------
            // 1. Capacity Calculation 
            // ------------------------
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
            }
            if (data.PassengerCapacityPerVehicle == 0) data.PassengerCapacityPerVehicle = 0;

            // ----------------------
            // 2. Vehicle Statistics
            // ----------------------
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
            // -------------------
            // 3. Stop Statistics 
            // -------------------
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

            // ------------------------------
            // 4. Capacity Ratio Calculation 
            // ------------------------------
            int totalCapacity = data.CurrentVehicles * data.PassengerCapacityPerVehicle;
            if (totalCapacity > 0)
            {
                data.CurrentCapacityRatio = (float)(data.TotalPassengers + data.TotalWaiting) / totalCapacity;
            }

            return data;
        }

        // [Main Logic] Analyzes a single route and applies changes (Price, Vehicles, Alerts)
        // This function orchestrates the decision-making process by using data from helper functions.
        private void ProcessRoute(Entity routeEntity, ref int alertsPostedThisCycle)
        {
            // 1. Setup & Basic Components
            // Retrieve essential components needed to identify the route and its type.
            var transportLine = m_TransportLines[routeEntity];
            var prefabRef = m_PrefabRefs[routeEntity];
            var routeNumber = m_RouteNumbers[routeEntity];
            var transportLineData = m_TransportLineDatas[prefabRef.m_Prefab];

            // 2. Configuration Retrieval
            // Check if there is a specific Custom Rule assigned to this route.
            bool hasCustomRule = m_RouteRules.TryGetComponent(routeEntity, out var routeRule);

            // If a Custom Rule exists but is invalid (default), we skip processing based on original logic.
            if (hasCustomRule && routeRule.customRule == default) return;

            // [Helper Call] Get targets and limits (Occupancy, Price, etc.) based on Transport Type or Custom Rule.
            // This replaces the massive 'switch' statement from the original OnUpdate.
            RouteConfig config = GetRouteConfig(transportLineData.m_TransportType, hasCustomRule, routeRule);

            // If the target occupancy is 0 (meaning this transport type is disabled in Mod Settings), stop here.
            if (config.OccupancyTarget == 0) return;


            // 3. Real-time Data Collection
            // [Helper Call] Gather current statistics: passenger count, active vehicles, waiting passengers.
            RouteData data = GetRouteData(routeEntity, transportLineData);

            // If there are no vehicles on the route, we cannot perform calculations.
            if (data.CurrentVehicles == 0) return;


            // 4. Calculations (Duration & Capacity)
            DynamicBuffer<RouteWaypoint> waypoints = m_RouteWaypoints[routeEntity];
            DynamicBuffer<RouteSegment> routeSegments = m_RouteSegments[routeEntity];
            DynamicBuffer<RouteModifier> routeModifier = m_RouteModifiers[routeEntity];

            // Calculate current interval and duration
            float defaultVehicleInterval = transportLineData.m_DefaultVehicleInterval;
            float vehicleInterval = defaultVehicleInterval;

            // Apply modifiers (e.g., from other game systems) to the interval
            RouteUtils.ApplyModifier(ref vehicleInterval, routeModifier, RouteModifierType.VehicleInterval);

            // Calculate the stable duration for the entire route loop
            float stableDuration = CalculateStableDuration(transportLineData, waypoints, routeSegments);

            // Calculate Weighted Capacity Ratio
            // Formula: (Passengers + (Waiting * Weight)) / Total Capacity
            // We give weight to waiting passengers because they are potential demand.
            float weightedCapacityRatio = 0f;
            int totalCapacity = data.CurrentVehicles * data.PassengerCapacityPerVehicle;
            if (totalCapacity > 0)
            {
                weightedCapacityRatio = (data.TotalPassengers + (data.TotalWaiting * Mod.m_Setting.waiting_time_weight)) / (float)totalCapacity;
            }


            // 5. Alert System (Chirps)
            // Post a "Chirp" message if a specific stop is overcrowded or the route is overloaded.
            if (ChirpsEnabled && CustomChirpsBridge.IsAvailable && data.BusiestStop != Entity.Null && alertsPostedThisCycle < maxAlertsPerCycle)
            {
                if (!EntityManager.Exists(data.BusiestStop))//Is the busiest stop valid?
                {
                    // Remove from alerted list if stop no longer exists
                    if (_busyStopAlerted.ContainsKey(data.BusiestStop)) _busyStopAlerted.Remove(data.BusiestStop);
                }
                else
                {
                    // Condition 1: A single stop has too many waiting passengers.
                    bool isStopBusy = data.MaxStopWaiting >= (data.PassengerCapacityPerVehicle * BusyStopEnterPct);

                    // Condition 2: The entire route is over capacity (Target + Buffer).
                    bool isRouteOverloaded = weightedCapacityRatio > (config.OccupancyTarget + 2 * Mod.m_Setting.threshold) / 100f;

                    bool shouldAlert = isStopBusy || isRouteOverloaded;

                    // Check if we already alerted for this specific stop to avoid spam.
                    _busyStopAlerted.TryGetValue(data.BusiestStop, out bool alreadyAlerted);

                    if (shouldAlert && !alreadyAlerted)
                    {
                        if (isStopBusy) // Only post specific stop alerts
                        {
                            string lineLabel = T2WStrings.T("chirp.line_label",
                                ("type", transportLineData.m_TransportType.ToString()),
                                ("number", routeNumber.m_Number.ToString()));

                            string msg = T2WStrings.T("chirp.stop_busy",
                                ("line_label", lineLabel),
                                ("waiting", data.MaxStopWaiting.ToString()));

                            CustomChirpsBridge.PostChirp(msg, DepartmentAccountBridge.Transportation, data.BusiestStop, T2WStrings.T("chirp.mod_name"));

                            alertsPostedThisCycle++;
                        }
                        _busyStopAlerted[data.BusiestStop] = true; // Mark as alerted
                    }
                    else if (!shouldAlert && alreadyAlerted)
                    {
                        // Hysteresis: Clear the alert status only when the crowd significantly drops.
                        if (data.MaxStopWaiting <= (data.PassengerCapacityPerVehicle * BusyStopExitPct) && !isRouteOverloaded)
                        {
                            _busyStopAlerted.Remove(data.BusiestStop);
                        }
                    }
                }


                // 6. Ticket Price & Vehicle Count Adjustment Logic
                int ticketPrice = transportLine.m_TicketPrice;
                int currentVehicles = data.CurrentVehicles;

                // Get policy slider constraints
                PolicySliderData policySliderData = EntityManager.GetComponentData<PolicySliderData>(m_VehicleCountPolicy);

                // Calculate Min/Max allowable vehicles based on the slider range
                int maxVehicles = CalculateVehicleCountFromAdjustment(policySliderData.m_Range.max, defaultVehicleInterval, stableDuration, m_RouteModifierDatas, m_VehicleCountPolicy, m_PolicySliderDatas);
                int minVehicles = CalculateVehicleCountFromAdjustment(policySliderData.m_Range.min, defaultVehicleInterval, stableDuration, m_RouteModifierDatas, m_VehicleCountPolicy, m_PolicySliderDatas);

                // Calculate currently set vehicles based on interval
                int setVehicles = TransportLineSystem.CalculateVehicleCount(vehicleInterval, stableDuration);
                int oldVehicles = setVehicles;

                // Apply Adjustment Percentages from Config (This logic was moved here from the switch statement)
                if (hasCustomRule)
                {
                    // Custom rules only apply reduction to min vehicles in the original logic
                    minVehicles = (int)Math.Round(minVehicles * (1 - config.MinVehiclesAdj / 100f));
                }
                else
                {
                    // Apply global settings adjustments
                    maxVehicles = (int)Math.Round(maxVehicles * (1 + config.MaxVehiclesAdj / 100f));
                    minVehicles = (int)Math.Round(minVehicles * (1 - config.MinVehiclesAdj / 100f));
                }

                // Ensure at least 1 vehicle
                if (minVehicles < 1) minVehicles = 1;


                // [Decision Making] Compare Current Ratio vs Target Ratio
                float targetRatio = config.OccupancyTarget / 100f;
                float threshold = Mod.m_Setting.threshold / 100f;

                // Case A: Demand is too low (Reduce Vehicles/Price)
                if (weightedCapacityRatio < (targetRatio - threshold))
                {
                    setVehicles--; // Decrease vehicle count

                    // If price reduction is allowed and demand is very low
                    if (ticketPrice < config.StandardTicketPrice && weightedCapacityRatio < (targetRatio - 2 * threshold))
                    {
                        // Calculate minimum price limit based on max discount
                        int minPriceLimit = (int)Math.Round((100 - config.MaxTicketDiscount) * config.StandardTicketPrice / 100f);
                        if (ticketPrice > minPriceLimit)
                        {
                            ticketPrice--;
                        }
                    }
                    else if (ticketPrice == config.StandardTicketPrice)
                    {
                        ticketPrice--; // Drop to standard if currently equal
                    }
                }
                // Case B: Demand is too high (Increase Vehicles/Price)
                else if (weightedCapacityRatio > (targetRatio + threshold))
                {
                    // If price increase is allowed and demand is very high
                    if (ticketPrice > config.StandardTicketPrice && weightedCapacityRatio > (targetRatio + 2 * threshold))
                    {
                        // Calculate maximum price limit based on max increase
                        int maxPriceLimit = (int)Math.Round((100 + config.MaxTicketIncrease) * config.StandardTicketPrice / 100f);
                        if (ticketPrice < maxPriceLimit)
                        {
                            ticketPrice++;
                        }
                    }
                    else if (ticketPrice == config.StandardTicketPrice)
                    {
                        ticketPrice++; // Rise to standard if currently equal
                    }
                    setVehicles++; // Increase vehicle count
                }

                // Clamp vehicle count within calculated limits
                if (setVehicles > maxVehicles) setVehicles = maxVehicles;
                if (setVehicles < minVehicles) setVehicles = minVehicles;

                // Safety Check: If too many empty vehicles, revert vehicle count changes to prevent issues
                if (data.EmptyVehicles / (float)currentVehicles > 0.3f)
                {
                    setVehicles = oldVehicles;
                }

                // Handle free transport setting
                if (config.StandardTicketPrice == 0) ticketPrice = 0;


                // 7. Apply Changes to Game Systems
                // Update Ticket Price Policy
                int isFree = ticketPrice > 0 ? 1 : 0;
                m_PoliciesUISystem.SetPolicy(routeEntity, m_TicketPricePolicy, isFree != 0, (float)ticketPrice);

                // Update Vehicle Count Policy (Converted back to Interval)
                float newInterval = 100f / (stableDuration / (defaultVehicleInterval * setVehicles));
                m_PoliciesUISystem.SetPolicy(routeEntity, m_VehicleCountPolicy, true, newInterval);

                // Debug Logging
                if (Mod.m_Setting.debug && (oldVehicles != setVehicles || ticketPrice != transportLine.m_TicketPrice))
                {
                    Mod.log.Info($"Route:{routeNumber.m_Number}, Type:{transportLineData.m_TransportType}, Ticket:{ticketPrice}, Vehicles:{setVehicles} (Min:{minVehicles}/Max:{maxVehicles}), Occupancy:{weightedCapacityRatio:F2}");
                }
            }
        }
        // [Configuration] Retrieves the configuration targets and limits for a specific route.
        // It prioritizes Custom Rules (if assigned); otherwise, it falls back to global Mod Settings.
        private RouteConfig GetRouteConfig(TransportType type, bool hasCustomRule, RouteRule routeRule)
        {
            // Use C# 9.0 syntax (or 'new RouteConfig()' if using older version)
            RouteConfig config = new RouteConfig();

            // 1. Check if a Custom Rule is assigned to this route via ManageRouteSystem
            if (hasCustomRule)
            {
                // Retrieve custom rule data tuple from the system
                // Tuple Structure: (id, name, occupancy, stdTicket, maxInc, maxDec, maxVehAdj, minVehAdj)
                var ruleData = m_ManageRouteSystem.GetCustomRule(routeRule.customRule);

                config.OccupancyTarget = ruleData.Item3;      // Target Occupancy (%)
                config.StandardTicketPrice = ruleData.Item4;  // Standard Ticket Price
                config.MaxTicketIncrease = ruleData.Item5;    // Max Price Increase (%)
                config.MaxTicketDiscount = ruleData.Item6;    // Max Price Discount (%)
                config.MaxVehiclesAdj = ruleData.Item7;       // Max Vehicle Adjustment (%)
                config.MinVehiclesAdj = ruleData.Item8;       // Min Vehicle Adjustment (%)
            }
            else
            {
                // 2. Apply default settings based on Transport Type (Global Mod Settings)
                // This replaces the large switch statement in the original OnUpdate loop.
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

                    case TransportType.Ship: // Passenger Ship
                        config.OccupancyTarget = Mod.m_Setting.target_occupancy_Ship;
                        config.StandardTicketPrice = Mod.m_Setting.standard_ticket_Ship;
                        config.MaxTicketIncrease = Mod.m_Setting.max_ticket_increase_Ship;
                        config.MaxTicketDiscount = Mod.m_Setting.max_ticket_discount_Ship;
                        config.MaxVehiclesAdj = Mod.m_Setting.max_vahicles_adj_Ship;
                        config.MinVehiclesAdj = Mod.m_Setting.min_vahicles_adj_Ship;
                        break;

                    case TransportType.Airplane: // Passenger Airplane
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
                        // If the transport type is not supported or disabled, return default (0 target).
                        // ProcessRoute will ignore routes with OccupancyTarget == 0.
                        config.OccupancyTarget = 0;
                        config.StandardTicketPrice = 0;
                        break;
                }
            }

            return config;
        }
    }

}