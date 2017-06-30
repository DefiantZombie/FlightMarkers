using UnityEngine;


namespace FlightMarkers.Utilities
{
    public class WeightedVectorAverager
    {
        private Vector3 _sum = Vector3.zero;
        private float _totalWeight;


        public void Add(Vector3 v, float weight)
        {
            _sum += v * weight;
            _totalWeight += weight;
        }


        public Vector3 Get()
        {
            if (_totalWeight > 0f)
                return _sum / _totalWeight;

            return Vector3.zero;
        }


        public float GetTotalWeight()
        {
            return _totalWeight;
        }


        public void Reset()
        {
            _sum = Vector3.zero;
            _totalWeight = 0f;
        }
    }
}
