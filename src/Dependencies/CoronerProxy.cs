using BepInEx.Bootstrap;
using GameNetcodeStuff;
using System;
using System.Reflection;

namespace Scopophobia.Dependencies
{
    internal static class CoronerProxy
    {
        public const string PLUGIN_GUID = "com.elitemastereric.coroner";
        private const string CORONER_SCOPO_GUID = "Turkeysteaks.coroner.scopophobia";
        private const string KEY = "DeathEnemyShyGuy";

        public static bool Enabled => Chainloader.PluginInfos.ContainsKey(PLUGIN_GUID);
        public static bool CoronerScopoFound => Chainloader.PluginInfos.ContainsKey(CORONER_SCOPO_GUID);
        public static bool Ready => _initialized && Enabled && _shyGuyCause != null && _setCauseMethod != null;
        private static bool _initialized;

        public static object? _shyGuyCause;

        private static MethodInfo? _registerMethod;
        private static MethodInfo? _isRegisteredMethod;
        private static MethodInfo? _setCauseMethod;
        private static MethodInfo? _getCauseByKeyMethod;

        public static void Initialize()
        {
            if (_initialized || !Enabled || CoronerScopoFound)
                return;

            _initialized = true;

            try
            {
                var coronerAssembly = Chainloader.PluginInfos[PLUGIN_GUID].Instance.GetType().Assembly;

                if (coronerAssembly == null)
                    return;

                var apiType = coronerAssembly.GetType("Coroner.API");
                var causeType = coronerAssembly.GetType("Coroner.AdvancedCauseOfDeath");

                if (apiType == null || causeType == null)
                    return;

                _registerMethod = apiType.GetMethod("Register", [typeof(string)]);
                _isRegisteredMethod = apiType.GetMethod("IsRegistered", BindingFlags.Public | BindingFlags.Static);
                _getCauseByKeyMethod = apiType.GetMethod("GetCauseOfDeathByKey", BindingFlags.Public | BindingFlags.Static);

                _setCauseMethod = apiType.GetMethod("SetCauseOfDeath",BindingFlags.Public | BindingFlags.Static,null,[typeof(PlayerControllerB), causeType],null);

                if (_registerMethod == null || _setCauseMethod == null || _isRegisteredMethod == null)
                    return;

                bool registered = (bool)_isRegisteredMethod.Invoke(null, [KEY]);

                if (!registered)
                {
                    _shyGuyCause = _registerMethod.Invoke(null, [KEY]);
                }
                else if (_getCauseByKeyMethod != null)
                {
                    _shyGuyCause = _getCauseByKeyMethod.Invoke(null, [KEY]);
                }
            }
            catch (Exception e)
            {
                ScopophobiaPlugin.Instance.LogWarningExtended($"Failed to initialize Coroner compatibility: {e}");
            }
        }

        public static void SetDeathCause(int playerId)
        {
            if (_shyGuyCause == null || _setCauseMethod == null)
                return;

            var player = StartOfRound.Instance.allPlayerScripts[playerId];

            try
            {
                _setCauseMethod.Invoke(null,[player,_shyGuyCause]);
            }
            catch (Exception e)
            {
                ScopophobiaPlugin.Instance.LogWarningExtended($"Failed to set Coroner death cause: {e}");
            }
        }
    }
}