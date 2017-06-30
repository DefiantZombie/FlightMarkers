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

            if (vessel.staticPressurekPa > 0f)
            {
                _centerOfLift = FindCenterOfLift(vessel.srf_velocity, vessel.altitude, vessel.staticPressurekPa,
                    vessel.atmDensity);

            }
            else
            {
                _centerOfLift = _zeroRay;
            }

            if (_centerOfLift.direction.IsSmallerThan(CenterOfLiftCutoff))
                return;

            DrawTools.DrawSphere(_centerOfLift.origin, XKCDColors.Blue, SphereScale);
            DrawTools.DrawArrow(_centerOfLift.origin, _centerOfLift.direction * ArrowLength, XKCDColors.Blue);
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


        public Ray FindCenterOfLift(Vector3 refVel, double refAlt, double refStp, double refDens)
        {
            var centerOfLift = Vector3.zero;
            var directionOfLift = Vector3.zero;
            var totalLift = 0f;

            FindCenterOfLiftRecurse(vessel.rootPart, refVel,
                ref centerOfLift, ref directionOfLift, ref totalLift,
                refAlt, refStp, refDens);

            if (Mathf.Approximately(totalLift, 0f))
                return new Ray(Vector3.zero, Vector3.zero);

            var scale = 1f / totalLift;
            return new Ray(centerOfLift * scale, directionOfLift * scale);
        }


        private void FindCenterOfLiftRecurse(Part part, Vector3 refVel, ref Vector3 centerOfLift,
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
                FindCenterOfLiftRecurse(part.children[i], refVel, ref centerOfLift, ref directionOfLift, ref totalLift,
                    refAlt, refStp, refDens);
            }
        }

        #region OLD MATHS
        private Ray FindCenterOfLift(IList<ILiftProvider> providers)
        {
            var refVel = vessel.lastVel;
            var refAlt = vessel.altitude;
            var refStp = FlightGlobals.getStaticPressure(refAlt);
            var refTemp = FlightGlobals.getExternalTemperature(refAlt);
            var refDens = FlightGlobals.getAtmDensity(refStp, refTemp);

            var centerOfLift = Vector3.zero;
            var directionOfLift = Vector3.zero;
            var lift = 0f;

            for (var i = 0; i < providers.Count; i++)
            {
                if (!providers[i].IsLifting) continue;

                _centerOfLiftQuery.Reset();
                _centerOfLiftQuery.refVector = refVel;
                _centerOfLiftQuery.refAltitude = refAlt;
                _centerOfLiftQuery.refStaticPressure = refStp;
                _centerOfLiftQuery.refAirDensity = refDens;

                providers[i].OnCenterOfLiftQuery(_centerOfLiftQuery);

                centerOfLift += _centerOfLiftQuery.pos * _centerOfLiftQuery.lift;
                directionOfLift += _centerOfLiftQuery.dir * _centerOfLiftQuery.lift;
                lift += _centerOfLiftQuery.lift;
            }

            if (lift < float.Epsilon) return new Ray(Vector3.zero, Vector3.zero);

            var m = 1f / lift;
            centerOfLift *= m;
            directionOfLift *= m;

            return new Ray(centerOfLift, directionOfLift);
        }


        private Ray FindCenterOfThrust(IList<IThrustProvider> providers)
        {
            var centerOfThrust = Vector3.zero;
            var directionOfThrust = Vector3.zero;
            var thrust = 0f;

            for (var i = 0; i < providers.Count; i++)
            {
                if (!((ModuleEngines)providers[i]).isOperational) continue;

                _centerOfThrustQuery.Reset();

                providers[i].OnCenterOfThrustQuery(_centerOfThrustQuery);

                centerOfThrust += _centerOfThrustQuery.pos * _centerOfThrustQuery.thrust;
                directionOfThrust += _centerOfThrustQuery.dir * _centerOfThrustQuery.thrust;
                thrust += _centerOfThrustQuery.thrust;
            }

            if (thrust < float.Epsilon) return _zeroRay;

            var m = 1f / thrust;
            centerOfThrust *= m;
            directionOfThrust *= m;

            return new Ray(centerOfThrust, directionOfThrust);
        }


        private Ray FindBodyLift()
        {
            var bodyLiftPosition = Vector3.zero;
            var bodyLiftDirection = Vector3.zero;
            var lift = 0f;

            for (var i = 0; i < vessel.parts.Count; i++)
            {
                var part = vessel.parts[i];

                bodyLiftPosition += (part.transform.position + part.transform.rotation * part.bodyLiftLocalPosition)
                    * part.bodyLiftLocalVector.magnitude;
                bodyLiftDirection += (part.transform.localRotation * part.bodyLiftLocalVector)
                    * part.bodyLiftLocalVector.magnitude;
                lift += part.bodyLiftLocalVector.magnitude;
            }

            if (lift < float.Epsilon) return _zeroRay;

            var m = 1f / lift;
            bodyLiftPosition *= m;
            bodyLiftDirection *= m;

            return new Ray(bodyLiftPosition, bodyLiftDirection);
        }


        private Ray FindDrag()
        {
            var dragPosition = Vector3.zero;
            var dragDirection = Vector3.zero;
            var drag = 0f;

            for (var i = 0; i < vessel.parts.Count; i++)
            {
                var part = vessel.parts[i];
                var liftModule = part.Modules.GetModule<ModuleLiftingSurface>();

                if (liftModule)
                {
                    if (liftModule.useInternalDragModel)
                    {
                        dragPosition += (part.transform.position + part.transform.rotation * part.CoPOffset) * liftModule.dragScalar;
                        dragDirection += (part.transform.localRotation * liftModule.dragForce) * liftModule.dragScalar;
                        drag += liftModule.dragScalar;

                        continue;
                    }
                }

                dragPosition += (part.transform.position + part.transform.rotation * part.CoPOffset) * part.dragScalar;
                dragDirection += (part.transform.localRotation * part.dragVectorDirLocal) * part.dragScalar;
                drag += part.dragScalar;
            }

            if (drag < float.Epsilon) return _zeroRay;

            var m = 1f / drag;
            dragPosition *= m;
            dragDirection *= m;

            return new Ray(dragPosition, dragDirection);
        }
        #endregion
    }
}
