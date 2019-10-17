using UnityEngine;

namespace FlightMarkers
{
	public class ModuleFlightMarkers : PartModule
	{
		[KSPEvent(active = true, advancedTweakable = true,
			externalToEVAOnly = false, guiActive = true,
			guiActiveEditor = false, guiActiveUncommand = true,
			guiActiveUnfocused = true, guiName = "SSC_FM_000001",
			isPersistent = false, name = "ToggleFlightMarkers",
			requireFullControl = false, unfocusedRange = 100f)]
		public void ToggleFlightMarkers()
		{
			VesselFlightMarkers.VesselModules[vessel]?.ToggleFlightMarkers();
		}


		[KSPEvent(active = true, advancedTweakable = true,
			externalToEVAOnly = false, guiActive = true,
			guiActiveEditor = false, guiActiveUncommand = true,
			guiActiveUnfocused = true, guiName = "SSC_FM_000004",
			isPersistent = false, name = "ToggleCombineMarkers",
			requireFullControl = false, unfocusedRange = 100f)]
		public void ToggleCombineLift()
		{
			VesselFlightMarkers.VesselModules[vessel]?.ToggleCombineLift();
		}


#if DEBUG
        [KSPEvent(active = true, advancedTweakable = true,
            externalToEVAOnly = false, guiActive = true,
            guiActiveEditor = false, guiActiveUncommand = true,
            guiActiveUnfocused = true, guiName = "FM Test",
            isPersistent = false, name = "Test",
            requireFullControl = false, unfocusedRange = 100f)]
        public void Test()
        {
		}
#endif


		public override void OnStart(StartState state)
		{
			if (HighLogic.LoadedScene != GameScenes.FLIGHT) return;

			Events["ToggleFlightMarkers"].guiName = FlightMarkers.LocalStrings[FlightMarkers.Strings.FlightMarkersOn];
			Events["ToggleCombineLift"].guiName = FlightMarkers.LocalStrings[FlightMarkers.Strings.CombineLiftOn];

			// ControlFromWhere
			ModuleCommand commandModule = part.FindModuleImplementing<ModuleCommand>();
			if (commandModule != null)
			{
				BaseEvent toggleEvent = commandModule.Events["ToggleControlPointVisual"];
				if (toggleEvent != null)
				{
					toggleEvent.active = true;
					toggleEvent.guiActive = true;
					toggleEvent.guiActiveEditor = true;
				}
				else
				{
					Debug.LogError("[FlightMarkers] ERROR: ToggleControlPointVisual not found");
				}
			}
		}


		public override void OnStartFinished(StartState state)
		{
			if (HighLogic.LoadedScene != GameScenes.FLIGHT) return;

			if (vessel == null) return;

			VesselFlightMarkers.VesselModules[vessel].OnFlightMarkersChanged += OnFlightMarkersChanged;
			VesselFlightMarkers.VesselModules[vessel].OnCombineLiftChanged += OnCombineLiftChanged;

			// ControlFromWhere
			VesselFlightMarkers.VesselModules[vessel].OnHighlightOnSwitchChanged += OnHighlightOnSwitchChanged;

			if (VesselFlightMarkers.VesselModules[vessel].HighlightOnSwitch)
				GameEvents.onVesselReferenceTransformSwitch.Add(OnVesselReferenceTransformSwitch);
		}


		private void OnFlightMarkersChanged(bool markersEnabled)
		{
			Events["ToggleFlightMarkers"].guiName = markersEnabled ? FlightMarkers.LocalStrings[FlightMarkers.Strings.FlightMarkersOff] : FlightMarkers.LocalStrings[FlightMarkers.Strings.FlightMarkersOn];
			Events["ToggleCombineLift"].active = markersEnabled;
		}


		private void OnCombineLiftChanged(bool combineLift)
		{
			Events["ToggleCombineLift"].guiName = combineLift ? FlightMarkers.LocalStrings[FlightMarkers.Strings.CombineLiftOff] : FlightMarkers.LocalStrings[FlightMarkers.Strings.CombineLiftOn];
		}


		#region ControlFromWhere

		[KSPEvent(active = true, advancedTweakable = true,
			guiName = "#SSC_FM_000005", guiActive = true,
			guiActiveEditor = false, isPersistent = false,
			requireFullControl = false)]
		public void FindControlPart()
		{
			VesselFlightMarkers.VesselModules[vessel]?.HighlightPart();
		}

		public void OnVesselReferenceTransformSwitch(Transform prev, Transform next)
		{
			if (prev == next) return;

			if (next == part.GetReferenceTransform())
			{
				if (VesselFlightMarkers.VesselModules.ContainsKey(vessel))
				{
					VesselFlightMarkers.VesselModules[vessel]?.HighlightPart(next);
				}
			}
		}

		protected void OnHighlightOnSwitchChanged(bool value)
		{
			if (value)
				GameEvents.onVesselReferenceTransformSwitch.Add(OnVesselReferenceTransformSwitch);
			else
				GameEvents.onVesselReferenceTransformSwitch.Remove(OnVesselReferenceTransformSwitch);
		}

		#endregion

		private void OnDestroy()
		{
			if (HighLogic.LoadedScene != GameScenes.FLIGHT) return;

			if (vessel == null || !VesselFlightMarkers.VesselModules.ContainsKey(vessel)) return;

			VesselFlightMarkers.VesselModules[vessel].OnFlightMarkersChanged -= OnFlightMarkersChanged;
			VesselFlightMarkers.VesselModules[vessel].OnCombineLiftChanged -= OnCombineLiftChanged;

			// ControlFromWhere
			VesselFlightMarkers.VesselModules[vessel].OnHighlightOnSwitchChanged -= OnHighlightOnSwitchChanged;
			GameEvents.onVesselReferenceTransformSwitch.Remove(OnVesselReferenceTransformSwitch);
		}
	}
}
