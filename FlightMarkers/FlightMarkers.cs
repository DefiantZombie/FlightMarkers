using FlightMarkers.Utilities;
using KSP.Localization;
using System;
using System.Collections.Generic;
using UnityEngine;
// ReSharper disable UnusedMember.Local


namespace FlightMarkers
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FlightMarkers : MonoBehaviour
    {
        public enum Strings
        {
            FlightMarkersOn,
            FlightMarkersOff,
            CombineLiftOn,
            CombineLiftOff
        }

        public static Dictionary<Strings, string> LocalStrings;
        public static FlightMarkers Instance { get; private set; }
        public static event Action OnUpdateEvent = delegate { };
        public static event Action OnRenderObjectEvent = delegate { };
         

        private void Awake()
        {
            if (Instance != null)
            {
                DestroyImmediate(this);
                return;
            }

            LocalStrings = new Dictionary<Strings, string>();
            OnLanguageSwitched();

            Instance = this;
        }


        private void Start()
        {
            GameEvents.onLanguageSwitched.Add(OnLanguageSwitched);
            GameEvents.onHideUI.Add(OnHideUI);
            GameEvents.onShowUI.Add(OnShowUI);
        }


        private void OnLanguageSwitched()
        {
            LocalStrings[Strings.FlightMarkersOn] = Localizer.Format("#SSC_FM_000001",
                Localizer.GetStringByTag("#SSC_FM_000002"));

            LocalStrings[Strings.FlightMarkersOff] = Localizer.Format("#SSC_FM_000001",
                Localizer.GetStringByTag("#SSC_FM_000003"));

            LocalStrings[Strings.CombineLiftOn] = Localizer.Format("#SSC_FM_000004",
                Localizer.GetStringByTag("#SSC_FM_000002"));

            LocalStrings[Strings.CombineLiftOff] = Localizer.Format("#SSC_FM_000004",
                Localizer.GetStringByTag("#SSC_FM_000003"));
        }


        private void OnHideUI()
        {
            Logging.DebugLog("FlightMarkers.OnHideUI()");

            foreach (var module in VesselFlightMarkers.VesselModules.Values)
            {
                module.Hidden = true;
            }
        }


        private void OnShowUI()
        {
            Logging.DebugLog("FlightMarkers.OnShowUI()");

            foreach (var module in VesselFlightMarkers.VesselModules.Values)
            {
                module.Hidden = false;
            }
        }


        private void Update()
        {
            OnUpdateEvent();
        }


        private void OnRenderObject()
        {
            OnRenderObjectEvent();
        }


        // ReSharper disable once InconsistentNaming
        private void OnGUI()
        {
            DrawTools.NewFrame();
        }


        private void OnDestroy()
        {
            enabled = false;

            GameEvents.onLanguageSwitched.Remove(OnLanguageSwitched);
            GameEvents.onHideUI.Remove(OnHideUI);
            GameEvents.onShowUI.Remove(OnShowUI);

            OnUpdateEvent = delegate { };
            OnRenderObjectEvent = delegate { };
            Instance = null;
        }
    }
}
