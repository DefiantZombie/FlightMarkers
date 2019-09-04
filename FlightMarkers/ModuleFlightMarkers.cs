using UnityEngine;
// ReSharper disable UnusedMember.Local


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
		protected bool highlightEnabled = false;

		protected const string ArrowShader = "Particles/Alpha Blended";
		protected const int ArrowLayer = 0;
		protected const float LineLength = 1.0f;

		protected Color _color = XKCDColors.Yellow;
		protected Material _material;

		protected GameObject _arrowObject;
		protected LineRenderer _lineStart;
		protected LineRenderer _lineEnd;

        [KSPEvent(active = true, advancedTweakable = true,
            externalToEVAOnly = false, guiActive = true,
            guiActiveEditor = false, guiActiveUncommand = true,
            guiActiveUnfocused = true, guiName = "FM Test",
            isPersistent = false, name = "Test",
            requireFullControl = false, unfocusedRange = 100f)]
        public void Test()
        {
			if(_material == null)
			{
				_material = new Material(Shader.Find(ArrowShader))
				{
					renderQueue = 3000
				};
			}

			Part controlPart = vessel.GetReferenceTransformPart();
			if(controlPart == null)
			{
				UnityEngine.Debug.LogError($"Control part not found");
				return;
			}

			highlightEnabled = !highlightEnabled;

			controlPart.highlightColor = XKCDColors.AquaBlue;
			controlPart.highlightType = Part.HighlightType.AlwaysOn;

			controlPart.Highlight(highlightEnabled);

			if (highlightEnabled)
			{
				_color.a = 0.75f;

				_arrowObject = new GameObject("FlightMarker Control Arrow");
				_arrowObject.transform.parent = controlPart.transform;
				_arrowObject.transform.localPosition = Vector3.zero;
				_arrowObject.layer = ArrowLayer;

				_lineStart = NewLine(_arrowObject);
				_lineStart.positionCount = 2;
				_lineStart.startColor = _color;
				_lineStart.endColor = _color;
				_lineStart.startWidth = 0.1f;
				_lineStart.endWidth = 0.1f;
				_lineStart.SetPosition(0, Vector3.zero);
				_lineStart.SetPosition(1, controlPart.transform.forward * (LineLength - 0.2f));
				_lineStart.enabled = true;

				_lineEnd = NewLine(_arrowObject);
				_lineEnd.positionCount = 2;
				_lineEnd.startColor = _color;
				_lineEnd.endColor = _color;
				_lineEnd.startWidth = 0.2f;
				_lineEnd.endWidth = 0.0f;
				_lineEnd.SetPosition(0, controlPart.transform.forward * (LineLength - 0.2f));
				_lineEnd.SetPosition(1, controlPart.transform.forward * LineLength);
				_lineEnd.enabled = true;
			}
			else
			{
				DestroyImmediate(_arrowObject);
				_lineStart = null;
				_lineEnd = null;
			}
		}

		protected LineRenderer NewLine(GameObject parent = null)
		{
			var obj = new GameObject("FlightMarkers LineRenderer object");
			var lr = obj.AddComponent<LineRenderer>();
			obj.transform.parent = parent == null ? gameObject.transform : parent.transform;
			obj.transform.localPosition = Vector3.zero;
			obj.layer = ArrowLayer;
			lr.material = _material;
			lr.useWorldSpace = false;
			return lr;
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
				VesselFlightMarkers.VesselModules[vessel]?.HighlightPart(next);
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
