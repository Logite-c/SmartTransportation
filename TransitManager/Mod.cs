using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.PSI.Environment;
using Game;
using Game.Modding;
using Game.Prefabs;
using Game.SceneFlow;
using HarmonyLib;
using SmartTransportation.Bridge;
using SmartTransportation.Systems;
using System.IO;
using System.Linq;
using Unity.Entities;

namespace SmartTransportation
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(SmartTransportation)}.{nameof(Mod)}").SetShowsErrorsInUI(false);

        public static Setting m_Setting;
        public static readonly string harmonyID = "SmartTransportation";

        // Mods Settings Folder
        public static string SettingsFolder = Path.Combine(EnvPath.kUserDataPath, "ModsSettings", nameof(SmartTransportation));
        readonly public static int kComponentVersion = 1;
        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            if (!Directory.Exists(SettingsFolder))
            {
                Directory.CreateDirectory(SettingsFolder);
            }

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));
            AssetDatabase.global.LoadSettings(nameof(SmartTransportation), m_Setting, new Setting(this));

            foreach (var modInfo in GameManager.instance.modManager)
            {
                if (modInfo.asset.name.Equals("TransportPolicyAdjuster"))
                {
                    Mod.log.Info($"This mod is not compatible with {modInfo.asset.name}");
                }
            }

            // Disable original systems
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Policies.ModifiedSystem>().Enabled = false;


            updateSystem.UpdateAt<SmartTransportation.Systems.ModifiedSystem>(SystemUpdatePhase.Modification4);
            updateSystem.UpdateAt<SmartTransitSystem>(SystemUpdatePhase.GameSimulation);
            //updateSystem.UpdateAt<SmartTaxiSystem>(SystemUpdatePhase.GameSimulation);

            updateSystem.UpdateAt<ManageRouteSystem>(SystemUpdatePhase.GameSimulation);
            
            

            //Harmony
            var harmony = new Harmony(harmonyID);
            //Harmony.DEBUG = true;
            harmony.PatchAll(typeof(Mod).Assembly);
            var patchedMethods = harmony.GetPatchedMethods().ToArray();
            log.Info($"Plugin {harmonyID} made patches! Patched methods: " + patchedMethods);
            foreach (var patchedMethod in patchedMethods)
            {
                log.Info($"Patched method: {patchedMethod.Module.Name}:{patchedMethod.Name}");
            }

        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
        }
    }
}
