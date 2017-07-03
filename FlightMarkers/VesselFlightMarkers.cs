using FlightMarkers.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable UnusedMember.Local


namespace FlightMarkers
{
    public class VesselFlightMarkers : VesselModule
    {
        public static Dictionary<Vessel, VesselFlightMarkers> VesselModules;

        public event Action<bool> OnFlightMarkersChanged;
        public event Action<bool> OnCombineLiftChanged;

        private ArrowData _centerOfThrust;
        private ArrowData _centerOfLift;
        private ArrowData _bodyLift;
        private ArrowData _drag;
        private readonly CenterOfLiftQuery _centerOfLiftQuery = new CenterOfLiftQuery();
        private readonly CenterOfThrustQuery _centerOfThrustQuery = new CenterOfThrustQuery();
        private readonly WeightedVectorAverager _positionAverager = new WeightedVectorAverager();
        private readonly WeightedVectorAverager _directionAverager = new WeightedVectorAverager();

        private static readonly ArrowData _zeroArrowData = new ArrowData(Vector3.zero, Vector3.zero, 0f);

        private const float CenterOfLiftCutoff = 10f; // 0.1f
        private const float BodyLiftCutoff = 15f; // 0.1f
        private const float DragCutoff = 10f; // 0.1f
        private const float SphereScale = 0.5f;
        private const float ArrowLength = 4.0f;


        public struct ArrowData
        {
            public Vector3 Position { get; }
            public Vector3 Direction { get; }
            public float Total { get; }


            public ArrowData(Vector3 position, Vector3 direction, float total)
            {
                Position = position;
                Direction = direction;
                Total = total;
            }
        }


        private bool _markersEnabled;
        public bool MarkersEnabled
        {
            get { return _markersEnabled; }
            set
            {
                _markersEnabled = value;

                OnFlightMarkersChanged?.Invoke(value);

                if (value)
                    FlightMarkers.OnRenderObjectEvent += OnRenderObjectEvent;
                else
                    FlightMarkers.OnRenderObjectEvent -= OnRenderObjectEvent;
            }
        }


        private bool _combineLift = true;
        public bool CombineLift
        {
            get { return _combineLift; }
            set
            {
                _combineLift = value;

                OnCombineLiftChanged?.Invoke(value);
            }
        }


        private bool _hidden;
        public bool Hidden
        {
            get { return _hidden; }
            set
            {
                _hidden = value;

                if (value)
                {
                    FlightMarkers.OnRenderObjectEvent -= OnRenderObjectEvent;
                }
                else
                {
                    if (_markersEnabled)
                        FlightMarkers.OnRenderObjectEvent += OnRenderObjectEvent;
                }
            }
        }


        protected override void OnAwake()
        {
            if (VesselModules == null)
                VesselModules = new Dictionary<Vessel, VesselFlightMarkers>();
        }


        protected override void OnStart()
        {
            if (HighLogic.LoadedScene != GameScenes.FLIGHT) return;

            Logging.DebugLog($"[{vessel.GetName()}]VesselFlightMarkers.OnStart()");

            if (VesselModules.ContainsKey(vessel))
                VesselModules[vessel] = this;
            else
                VesselModules.Add(vessel, this);

            GameEvents.onFlightReady.Add(OnFlightReady);
        }


        private void OnFlightReady()
        {
            Logging.DebugLog($"[{vessel?.GetName()}]VesselFlightMarkers.OnFlightReady()");

            OnFlightMarkersChanged?.Invoke(_markersEnabled);
            OnCombineLiftChanged?.Invoke(_combineLift);
        }


        public Vector3 TestOrigin;
        public Vector3 TestDirection;
        public bool TestEnabled = false;

        private readonly VectorAverager _positionAvg = new VectorAverager();
        private readonly VectorAverager _directionAvg = new VectorAverager();


        [Flags]
        private enum LiftFlag
        {
            None,
            SurfaceLift,
            BodyLift
        }


        private const LiftFlag CombineFlags = LiftFlag.SurfaceLift | LiftFlag.BodyLift;


        private void OnRenderObjectEvent()
        {
            if (Camera.current != Camera.main || MapView.MapIsEnabled) return;

            if (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA &&
                vessel == FlightGlobals.ActiveVessel)
                return;

            if (vessel != FlightGlobals.ActiveVessel)
            {
                if (Vector3.Distance(FlightGlobals.ActiveVessel.transform.position, vessel.transform.position) >
                    PhysicsGlobals.Instance.VesselRangesDefault.subOrbital.unload)
                {
                    MarkersEnabled = false;
                    return;
                }
            }

            Profiler.BeginSample("FlightMarkersRenderDraw");

            DrawTools.DrawSphere(vessel.CoM, XKCDColors.Yellow, 1.0f * SphereScale);

            DrawTools.DrawSphere(vessel.rootPart.transform.position, XKCDColors.Green, 0.25f);

            _centerOfThrust = FindCenterOfThrust(vessel.rootPart);
            if (_centerOfThrust.Direction != Vector3.zero)
            {
                DrawTools.DrawSphere(_centerOfThrust.Position, XKCDColors.Magenta, 0.95f * SphereScale);
                DrawTools.DrawArrow(_centerOfThrust.Position, _centerOfThrust.Direction.normalized * ArrowLength, XKCDColors.Magenta);
            }

            if (vessel.staticPressurekPa > 0f)
            {
                _centerOfLift = FindCenterOfLift(vessel.rootPart, vessel.srf_velocity, vessel.altitude,
                    vessel.staticPressurekPa, vessel.atmDensity);
                _bodyLift = FindBodyLift(vessel.rootPart);
                _drag = FindDrag(vessel.rootPart);

                var activeLift = LiftFlag.None;
                if (_centerOfLift.Total > CenterOfLiftCutoff) activeLift |= LiftFlag.SurfaceLift;
                if (_bodyLift.Total > BodyLiftCutoff) activeLift |= LiftFlag.BodyLift;

                var drawCombined = _combineLift && (activeLift & CombineFlags) == CombineFlags;

                if (drawCombined)
                {
                    _positionAvg.Reset();
                    _directionAvg.Reset();

                    if ((activeLift & LiftFlag.SurfaceLift) == LiftFlag.SurfaceLift)
                    {
                        _positionAvg.Add(_centerOfLift.Position);
                        _directionAvg.Add(_centerOfLift.Direction);
                    }

                    if ((activeLift & LiftFlag.BodyLift) == LiftFlag.BodyLift)
                    {
                        _positionAvg.Add(_bodyLift.Position);
                        _directionAvg.Add(_bodyLift.Direction);
                    }

                    DrawTools.DrawSphere(_positionAvg.Get(), XKCDColors.Purple, 0.9f * SphereScale);
                    DrawTools.DrawArrow(_positionAvg.Get(), _directionAvg.Get().normalized * ArrowLength, XKCDColors.Purple);
                }
                else
                {
                    if ((activeLift & LiftFlag.SurfaceLift) == LiftFlag.SurfaceLift)
                    {
                        DrawTools.DrawSphere(_centerOfLift.Position, XKCDColors.Blue, 0.9f * SphereScale);
                        DrawTools.DrawArrow(_centerOfLift.Position, _centerOfLift.Direction.normalized * ArrowLength, XKCDColors.Blue);
                    }

                    if ((activeLift & LiftFlag.BodyLift) == LiftFlag.BodyLift)
                    {
                        DrawTools.DrawSphere(_bodyLift.Position, XKCDColors.Cyan, 0.85f * SphereScale);
                        DrawTools.DrawArrow(_bodyLift.Position, _bodyLift.Direction.normalized * ArrowLength, XKCDColors.Cyan);
                    }
                }

                if(_drag.Total > DragCutoff)
                {
                    DrawTools.DrawSphere(_drag.Position, XKCDColors.Red, 0.8f * SphereScale);
                    DrawTools.DrawArrow(_drag.Position, _drag.Direction.normalized * ArrowLength, XKCDColors.Red);
                }
            }

            Profiler.EndSample();
        }


        public void ToggleFlightMarkers()
        {
            MarkersEnabled = !MarkersEnabled;
        }


        public void ToggleCombineLift()
        {
            CombineLift = !CombineLift;
        }


        private void OnDestroy()
        {
            Logging.DebugLog($"[{vessel?.GetName()}]VesselFlightMarkers.OnDestroy()");

            if (vessel != null && VesselModules.ContainsKey(vessel))
                VesselModules.Remove(vessel);

            GameEvents.onFlightReady.Remove(OnFlightReady);

            FlightMarkers.OnRenderObjectEvent -= OnRenderObjectEvent;
        }


        public ArrowData FindCenterOfLift(Part rootPart, Vector3 refVel, double refAlt, double refStp, double refDens)
        {
            _positionAverager.Reset();
            _directionAverager.Reset();

            RecurseCenterOfLift(rootPart, refVel, refAlt, refStp, refDens);

            return Mathf.Approximately(_positionAverager.GetTotalWeight(), 0f) ? _zeroArrowData :
                new ArrowData(_positionAverager.Get(), _directionAverager.Get(), _positionAverager.GetTotalWeight());
        }


        private void RecurseCenterOfLift(Part part, Vector3 refVel, double refAlt, double refStp, double refDens)
        {
            var count = part.Modules.Count;
            while (count-- > 0)
            {
                var module = part.Modules[count] as ILiftProvider;
                if (module == null)
                    continue;

                _centerOfLiftQuery.Reset();
                _centerOfLiftQuery.refVector = refVel;
                _centerOfLiftQuery.refAltitude = refAlt;
                _centerOfLiftQuery.refStaticPressure = refStp;
                _centerOfLiftQuery.refAirDensity = refDens;

                module.OnCenterOfLiftQuery(_centerOfLiftQuery);

                _positionAverager.Add(_centerOfLiftQuery.pos, _centerOfLiftQuery.lift);
                _directionAverager.Add(_centerOfLiftQuery.dir, _centerOfLiftQuery.lift);
            }

            count = part.children.Count;
            for (var i = 0; i < count; i++)
            {
                RecurseCenterOfLift(part.children[i], refVel, refAlt, refStp, refDens);
            }
        }


        public ArrowData FindCenterOfThrust(Part rootPart)
        {
            _positionAverager.Reset();
            _directionAverager.Reset();

            RecurseCenterOfThrust(rootPart);

            return Mathf.Approximately(_positionAverager.GetTotalWeight(), 0f) ? _zeroArrowData :
                new ArrowData(_positionAverager.Get(), _directionAverager.Get(), _positionAverager.GetTotalWeight());
        }


        private void RecurseCenterOfThrust(Part part)
        {
            var count = part.Modules.Count;
            while (count-- > 0)
            {
                var module = part.Modules[count] as IThrustProvider;
                if (module == null || !((ModuleEngines)module).isOperational)
                    continue;

                _centerOfThrustQuery.Reset();

                module.OnCenterOfThrustQuery(_centerOfThrustQuery);

                _positionAverager.Add(_centerOfThrustQuery.pos, _centerOfThrustQuery.thrust);
                _directionAverager.Add(_centerOfThrustQuery.dir, _centerOfThrustQuery.thrust);
            }

            count = part.children.Count;
            for (var i = 0; i < count; i++)
            {
                RecurseCenterOfThrust(part.children[i]);
            }
        }


        public ArrowData FindBodyLift(Part rootPart)
        {
            _positionAverager.Reset();
            _directionAverager.Reset();

            RecurseBodyLift(rootPart);

            return Mathf.Approximately(_positionAverager.GetTotalWeight(), 0f) ? _zeroArrowData :
                new ArrowData(_positionAverager.Get(), _directionAverager.Get(), _positionAverager.GetTotalWeight());

            //return new ArrowData(bodyLiftPosition * scale, bodyLiftDirection * scale, bodyLiftTotal / (PhysicsGlobals.BodyLiftMultiplier * 2));
        }


        private void RecurseBodyLift(Part part)
        {
            //bodyLiftPosition += (part.transform.position + part.transform.rotation * part.bodyLiftLocalPosition) * part.bodyLiftLocalVector.magnitude;
            //bodyLiftDirection += (part.transform.localRotation * part.bodyLiftLocalVector) * part.bodyLiftLocalVector.magnitude;
            //bodyLiftTotal += part.bodyLiftLocalVector.magnitude;

            //bodyLiftPosition += part.partTransform.TransformPoint(part.bodyLiftLocalPosition) * part.bodyLiftScalar;
            //bodyLiftDirection += part.partTransform.TransformDirection(part.bodyLiftLocalVector) * part.bodyLiftScalar;
            //bodyLiftTotal += part.bodyLiftScalar;

            var direction = part.transform.TransformDirection(part.bodyLiftLocalVector);
            _positionAverager.Add(part.partTransform.TransformPoint(part.bodyLiftLocalPosition), direction.magnitude);
            _directionAverager.Add(direction, direction.magnitude);

            var count = part.children.Count;
            for (var i = 0; i < count; i++)
            {
                RecurseBodyLift(part.children[i]);
            }
        }


        public ArrowData FindDrag(Part rootPart)
        {
            _positionAverager.Reset();
            _directionAverager.Reset();

            RecurseDrag(rootPart);

            return Mathf.Approximately(_positionAverager.GetTotalWeight(), 0f) ? _zeroArrowData :
                new ArrowData(_positionAverager.Get(), _directionAverager.Get(), _positionAverager.GetTotalWeight());
        }


        private void RecurseDrag(Part part)
        {
            _positionAverager.Add(part.transform.position, part.dragScalar);
            _directionAverager.Add(-part.dragVectorDir, part.dragScalar);

            var count = part.Modules.Count;
            while (count-- > 0)
            {
                var module = part.Modules[count] as ModuleLiftingSurface;
                if (module == null) continue;

                _positionAverager.Add(module.transform.position, module.dragScalar);
                _directionAverager.Add(module.dragForce, module.dragScalar); // keep an eye on this, dragScalar wasn't used in the previous version.
            }

            count = part.children.Count;
            for (var i = 0; i < count; i++)
            {
                RecurseDrag(part.children[i]);
            }
        }


#if DEBUG
        public bool DisplayDebugWindow = false;

        private Rect _debugWindowPos;


        public void OnGUI()
        {
            if (DisplayDebugWindow)
            {
                _debugWindowPos = GUILayout.Window("FlightMarkerDebug".GetHashCode(), _debugWindowPos, DrawDebugWindow,
                    "Flight Markers");
            }
        }


        private void DrawDebugWindow(int id)
        {
            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(5, 5, 3, 0),
                margin = new RectOffset(1, 1, 1, 1),
                stretchWidth = false,
                stretchHeight = false
            };

            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = false
            };

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("X", buttonStyle))
                DisplayDebugWindow = false;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Lift: {_centerOfLift.Total}", labelStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Body Lift: {_bodyLift.Total}", labelStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Drag: {_drag.Total}", labelStyle);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.DragWindow();
        }
#endif
    }
}