using FlightMarkers.Utilities;
using System;
using System.Collections.Generic;
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

        private Ray _centerOfThrust = new Ray(Vector3.zero, Vector3.zero);
        private Ray _centerOfLift = new Ray(Vector3.zero, Vector3.zero);
        private Ray _bodyLift = new Ray(Vector3.zero, Vector3.zero);
        private Ray _drag = new Ray(Vector3.zero, Vector3.zero);
        private Ray _combinedLift = new Ray(Vector3.zero, Vector3.zero);
        private readonly CenterOfLiftQuery _centerOfLiftQuery = new CenterOfLiftQuery();
        private readonly CenterOfThrustQuery _centerOfThrustQuery = new CenterOfThrustQuery();

        private static readonly Ray _zeroRay = new Ray(Vector3.zero, Vector3.zero);

        private const float CenterOfLiftCutoff = 0.1f;
        private const float BodyLiftCutoff = 0.1f;
        private const float DragCutoff = 0.1f;
        private const float SphereScale = 0.5f;
        private const float ArrowLength = 4.0f;


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

        private void OnRenderObjectEvent()
        {
            if (Camera.current != Camera.main || MapView.MapIsEnabled) return;

            if (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA && vessel == FlightGlobals.ActiveVessel)
                return;

            if (vessel != FlightGlobals.ActiveVessel)
            {
                if (Vector3.Distance(FlightGlobals.ActiveVessel.transform.position, vessel.transform.position) >
                    PhysicsGlobals.Instance.VesselRangesDefault.orbit.unload)
                {
                    MarkersEnabled = false;
                    return;
                }
            }

            Profiler.BeginSample("FlightMarkersRenderDraw");

            DrawTools.DrawSphere(vessel.CoM, XKCDColors.Yellow, 1.0f * SphereScale);

            DrawTools.DrawSphere(vessel.rootPart.transform.position, XKCDColors.Green, 0.25f);

            _drag = FindDrag(vessel.rootPart);

            DrawTools.DrawSphere(_drag.origin, XKCDColors.Red, 0.8f * SphereScale);
            DrawTools.DrawArrow(_drag.origin, _drag.direction * ArrowLength, XKCDColors.Red);

            //if (vessel.staticPressurekPa > 0f)
            //{
            //    _centerOfLift = FindCenterOfLift(vessel.rootPart, vessel.srf_velocity, vessel.altitude, vessel.staticPressurekPa,
            //        vessel.atmDensity);

            //}
            //else
            //{
            //    _centerOfLift = _zeroRay;
            //}

            //if (!_centerOfLift.direction.IsSmallerThan(CenterOfLiftCutoff))
            //{
            //    DrawTools.DrawSphere(_centerOfLift.origin, XKCDColors.Blue, 0.9f * SphereScale);
            //    DrawTools.DrawArrow(_centerOfLift.origin, _centerOfLift.direction * ArrowLength, XKCDColors.Blue);
            //}

            //_centerOfThrust = FindCenterOfThrust(vessel.rootPart);

            //if (_centerOfThrust.direction != Vector3.zero)
            //{
            //    DrawTools.DrawSphere(_centerOfThrust.origin, XKCDColors.Magenta, 0.95f * SphereScale);
            //    DrawTools.DrawArrow(_centerOfThrust.origin, _centerOfThrust.direction * ArrowLength, XKCDColors.Magenta);
            //}

            //if (vessel.staticPressurekPa > 0f)
            //{
            //    _bodyLift = FindBodyLift(vessel.rootPart);
            //}
            //else
            //{
            //    _bodyLift = _zeroRay;
            //}

            //if (!_bodyLift.direction.IsSmallerThan(BodyLiftCutoff))
            //{
            //    DrawTools.DrawSphere(_bodyLift.origin, XKCDColors.Cyan, 0.85f * SphereScale);
            //    DrawTools.DrawArrow(_bodyLift.origin, _bodyLift.direction * ArrowLength, XKCDColors.Cyan);
            //}

            //_drag = FindDrag(vessel.rootPart);

            //if (!_drag.direction.IsSmallerThan(DragCutoff))
            //{
            //    DrawTools.DrawSphere(_drag.origin, XKCDColors.Red, 0.8f * SphereScale);
            //    DrawTools.DrawArrow(_drag.origin, _drag.direction * ArrowLength, XKCDColors.Red);
            //}
            




            //var thrustProviders = vessel.FindPartModulesImplementing<IThrustProvider>();
            //_centerOfThrust = thrustProviders.Count > 0 ? FindCenterOfThrust(thrustProviders) : _zeroRay;

            //if (_centerOfThrust.direction != Vector3.zero)
            //{
            //    DrawTools.DrawSphere(_centerOfThrust.origin, XKCDColors.Magenta, 0.95f * SphereScale);
            //    DrawTools.DrawArrow(_centerOfThrust.origin, _centerOfThrust.direction * ArrowLength, XKCDColors.Magenta);
            //}

            //if (vessel.rootPart.staticPressureAtm > 0.0f)
            //{
            //    var liftProviders = vessel.FindPartModulesImplementing<ILiftProvider>();
            //    _centerOfLift = liftProviders.Count > 0 ? FindCenterOfLift(liftProviders) : _zeroRay;

            //    _bodyLift = FindBodyLift();

            //    _drag = FindDrag();
            //}
            //else
            //{
            //    _centerOfLift = _zeroRay;
            //    _bodyLift = _zeroRay;
            //    _drag = _zeroRay;
            //}

            //if (_combineLift)
            //{
            //    _combinedLift.origin = Vector3.zero;
            //    _combinedLift.direction = Vector3.zero;
            //    var count = 0;

            //    if (!_centerOfLift.direction.IsSmallerThan(CenterOfLiftCutoff))
            //    {
            //        _combinedLift.origin += _centerOfLift.origin;
            //        _combinedLift.direction += _centerOfLift.direction;
            //        count++;
            //    }

            //    if (!_bodyLift.direction.IsSmallerThan(BodyLiftCutoff))
            //    {
            //        _combinedLift.origin += _bodyLift.origin;
            //        _combinedLift.direction += _bodyLift.direction;
            //        count++;
            //    }

            //    _combinedLift.origin /= count;
            //    _combinedLift.direction /= count;

            //    DrawTools.DrawSphere(_combinedLift.origin, XKCDColors.Purple, 0.9f * SphereScale);
            //    DrawTools.DrawArrow(_combinedLift.origin, _combinedLift.direction * ArrowLength, XKCDColors.Purple);
            //}
            //else
            //{
            //    if (!_centerOfLift.direction.IsSmallerThan(CenterOfLiftCutoff))
            //    {
            //        DrawTools.DrawSphere(_centerOfLift.origin, XKCDColors.Blue, 0.9f * SphereScale);
            //        DrawTools.DrawArrow(_centerOfLift.origin, _centerOfLift.direction * ArrowLength, XKCDColors.Blue);
            //    }

            //    if (!_bodyLift.direction.IsSmallerThan(BodyLiftCutoff))
            //    {
            //        DrawTools.DrawSphere(_bodyLift.origin, XKCDColors.Cyan, 0.85f * SphereScale);
            //        DrawTools.DrawArrow(_bodyLift.origin, _bodyLift.direction * ArrowLength, XKCDColors.Cyan);
            //    }
            //}

            //if (!_drag.direction.IsSmallerThan(DragCutoff))
            //{
            //    DrawTools.DrawSphere(_drag.origin, XKCDColors.Red, 0.8f * SphereScale);
            //    DrawTools.DrawArrow(_drag.origin, _drag.direction * ArrowLength, XKCDColors.Red);
            //}

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


        public Ray FindCenterOfLift(Part rootPart, Vector3 refVel, double refAlt, double refStp, double refDens)
        {
            var centerOfLift = Vector3.zero;
            var directionOfLift = Vector3.zero;
            var totalLift = 0f;

            RecurseCenterOfLift(rootPart, refVel,
                ref centerOfLift, ref directionOfLift, ref totalLift,
                refAlt, refStp, refDens);

            if (Mathf.Approximately(totalLift, 0f))
                return new Ray(Vector3.zero, Vector3.zero);

            var scale = 1f / totalLift;
            return new Ray(centerOfLift * scale, directionOfLift * scale);
        }


        private void RecurseCenterOfLift(Part part, Vector3 refVel, ref Vector3 centerOfLift,
            ref Vector3 directionOfLift, ref float totalLift, double refAlt, double refStp, double refDens)
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

                centerOfLift += _centerOfLiftQuery.pos * _centerOfLiftQuery.lift;
                directionOfLift += _centerOfLiftQuery.dir * _centerOfLiftQuery.lift;
                totalLift += _centerOfLiftQuery.lift;
            }

            count = part.children.Count;
            for (var i = 0; i < count; i++)
            {
                RecurseCenterOfLift(part.children[i], refVel, ref centerOfLift, ref directionOfLift, ref totalLift,
                    refAlt, refStp, refDens);
            }
        }


        public Ray FindCenterOfThrust(Part rootPart)
        {
            var centerOfThrust = Vector3.zero;
            var directionOfThrust = Vector3.zero;
            var totalThrust = 0f;

            RecurseCenterOfThrust(rootPart, ref centerOfThrust, ref directionOfThrust, ref totalThrust);

            if (Mathf.Approximately(totalThrust, 0f))
                return new Ray(Vector3.zero, Vector3.zero);

            var scale = 1f / totalThrust;
            return new Ray(centerOfThrust * scale, directionOfThrust * scale);
        }


        private void RecurseCenterOfThrust(Part part, ref Vector3 centerOfThrust, ref Vector3 directionOfThrust, ref float totalThrust)
        {
            var count = part.Modules.Count;
            while (count-- > 0)
            {
                var module = part.Modules[count] as IThrustProvider;
                if (module == null || !((ModuleEngines)module).isOperational)
                    continue;

                _centerOfThrustQuery.Reset();

                module.OnCenterOfThrustQuery(_centerOfThrustQuery);

                centerOfThrust += _centerOfThrustQuery.pos * _centerOfThrustQuery.thrust;
                directionOfThrust += _centerOfThrustQuery.dir * _centerOfThrustQuery.thrust;
                totalThrust += _centerOfThrustQuery.thrust;
            }

            count = part.children.Count;
            for (var i = 0; i < count; i++)
            {
                RecurseCenterOfThrust(part.children[i], ref centerOfThrust, ref directionOfThrust, ref totalThrust);
            }
        }


        public Ray FindBodyLift(Part rootPart)
        {
            var bodyLiftPosition = new Vector3();
            var bodyLiftDirection = new Vector3();
            var totalBodyLift = 0f;

            RecurseBodyLift(rootPart, ref bodyLiftPosition, ref bodyLiftDirection, ref totalBodyLift);

            if (Mathf.Approximately(totalBodyLift, 0f))
                return new Ray(Vector3.zero, Vector3.zero);

            var scale = 1f / totalBodyLift;
            return new Ray(bodyLiftPosition * scale, bodyLiftDirection * scale);
        }


        private void RecurseBodyLift(Part part, ref Vector3 bodyLiftPosition, ref Vector3 bodyLiftDirection,
            ref float totalBodyLift)
        {
            bodyLiftPosition += (part.transform.position + part.transform.rotation * part.bodyLiftLocalPosition) * part.bodyLiftLocalVector.magnitude;
            bodyLiftDirection += (part.transform.localRotation * part.bodyLiftLocalVector) * part.bodyLiftLocalVector.magnitude;
            totalBodyLift += part.bodyLiftLocalVector.magnitude;

            var count = part.children.Count;
            for (var i = 0; i < count; i++)
            {
                RecurseBodyLift(part.children[i], ref bodyLiftPosition, ref bodyLiftDirection, ref totalBodyLift);
            }
        }


        public Ray FindDrag(Part rootPart)
        {
            var dragPosition = new Vector3();
            var dragDirection = new Vector3();
            var dragTotal = 0f;

            RecurseDrag(rootPart, ref dragPosition, ref dragDirection, ref dragTotal);

            if (Mathf.Approximately(dragTotal, 0f))
                return new Ray(Vector3.zero, Vector3.zero);

            var scale = 1f / dragTotal;
            return new Ray(dragPosition * scale, dragDirection * scale);
        }


        private void RecurseDrag(Part part, ref Vector3 dragPosition , ref Vector3 dragDirection , ref float dragTotal )
        {
            dragPosition += part.transform.position * part.dragScalar;
            dragDirection += -part.dragVectorDir * part.dragScalar;
            dragTotal += part.dragScalar;

            var count = part.Modules.Count;
            while (count-- > 0)
            {
                var module = part.Modules[count] as ModuleLiftingSurface;
                if (module == null) continue;

                dragPosition += module.transform.position * module.dragScalar;
                dragDirection += module.dragForce;
                dragTotal += module.dragScalar;
            }

            count = part.children.Count;
            for (var i = 0; i < count; i++)
            {
                RecurseDrag(part.children[i], ref dragPosition, ref dragDirection, ref dragTotal);
            }
        }
    }
}
