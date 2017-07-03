using FlightMarkers.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
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
        private readonly VectorAverager _positionAvg = new VectorAverager();
        private readonly VectorAverager _directionAvg = new VectorAverager();
        private readonly WeightedVectorAverager _weightedPositionAvg = new WeightedVectorAverager();
        private readonly WeightedVectorAverager _weightedDirectionAvg = new WeightedVectorAverager();

        private static readonly ArrowData _zeroArrowData = new ArrowData(Vector3.zero, Vector3.zero, 0f);

        private const LiftFlag CombineFlags = LiftFlag.SurfaceLift | LiftFlag.BodyLift;
        private const float CenterOfLiftCutoff = 10f;
        private const float BodyLiftCutoff = 15f;
        private const float DragCutoff = 10f;
        private const float SphereScale = 0.5f;
        private const float ArrowLength = 4.0f;


        private struct ArrowData
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


        [Flags]
        private enum LiftFlag
        {
            None,
            SurfaceLift,
            BodyLift
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


        private ArrowData FindCenterOfLift(Part rootPart, Vector3 refVel, double refAlt, double refStp, double refDens)
        {
            _weightedPositionAvg.Reset();
            _weightedDirectionAvg.Reset();

            RecurseCenterOfLift(rootPart, refVel, refAlt, refStp, refDens);

            return Mathf.Approximately(_weightedPositionAvg.GetTotalWeight(), 0f) ? _zeroArrowData :
                new ArrowData(_weightedPositionAvg.Get(), _weightedDirectionAvg.Get(), _weightedPositionAvg.GetTotalWeight());
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

                _weightedPositionAvg.Add(_centerOfLiftQuery.pos, _centerOfLiftQuery.lift);
                _weightedDirectionAvg.Add(_centerOfLiftQuery.dir, _centerOfLiftQuery.lift);
            }

            count = part.children.Count;
            for (var i = 0; i < count; i++)
            {
                RecurseCenterOfLift(part.children[i], refVel, refAlt, refStp, refDens);
            }
        }


        private ArrowData FindCenterOfThrust(Part rootPart)
        {
            _weightedPositionAvg.Reset();
            _weightedDirectionAvg.Reset();

            RecurseCenterOfThrust(rootPart);

            return Mathf.Approximately(_weightedPositionAvg.GetTotalWeight(), 0f) ? _zeroArrowData :
                new ArrowData(_weightedPositionAvg.Get(), _weightedDirectionAvg.Get(), _weightedPositionAvg.GetTotalWeight());
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

                _weightedPositionAvg.Add(_centerOfThrustQuery.pos, _centerOfThrustQuery.thrust);
                _weightedDirectionAvg.Add(_centerOfThrustQuery.dir, _centerOfThrustQuery.thrust);
            }

            count = part.children.Count;
            for (var i = 0; i < count; i++)
            {
                RecurseCenterOfThrust(part.children[i]);
            }
        }


        private ArrowData FindBodyLift(Part rootPart)
        {
            _weightedPositionAvg.Reset();
            _weightedDirectionAvg.Reset();

            RecurseBodyLift(rootPart);

            return Mathf.Approximately(_weightedPositionAvg.GetTotalWeight(), 0f) ? _zeroArrowData :
                new ArrowData(_weightedPositionAvg.Get(), _weightedDirectionAvg.Get(), _weightedPositionAvg.GetTotalWeight());

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
            _weightedPositionAvg.Add(part.partTransform.TransformPoint(part.bodyLiftLocalPosition), direction.magnitude);
            _weightedDirectionAvg.Add(direction, direction.magnitude);

            var count = part.children.Count;
            for (var i = 0; i < count; i++)
            {
                RecurseBodyLift(part.children[i]);
            }
        }


        private ArrowData FindDrag(Part rootPart)
        {
            _weightedPositionAvg.Reset();
            _weightedDirectionAvg.Reset();

            RecurseDrag(rootPart);

            return Mathf.Approximately(_weightedPositionAvg.GetTotalWeight(), 0f) ? _zeroArrowData :
                new ArrowData(_weightedPositionAvg.Get(), _weightedDirectionAvg.Get(), _weightedPositionAvg.GetTotalWeight());
        }


        private void RecurseDrag(Part part)
        {
            _weightedPositionAvg.Add(part.transform.position, part.dragScalar);
            _weightedDirectionAvg.Add(-part.dragVectorDir, part.dragScalar);

            var count = part.Modules.Count;
            while (count-- > 0)
            {
                var module = part.Modules[count] as ModuleLiftingSurface;
                if (module == null) continue;

                _weightedPositionAvg.Add(module.transform.position, module.dragScalar);
                _weightedDirectionAvg.Add(module.dragForce, module.dragScalar); // keep an eye on this, dragScalar wasn't used in the previous version.
            }

            count = part.children.Count;
            for (var i = 0; i < count; i++)
            {
                RecurseDrag(part.children[i]);
            }
        }
    }
}