using UnityEngine;


namespace FlightMarkers.Utilities
{
    public class VectorAverager
    {
        private Vector3 _sum = Vector3.zero;
        private uint _count;


        public void Add(Vector3 v)
        {
            _sum += v;
            _count++;
        }


        public Vector3 Get()
        {
            if (_count > 0)
                return _sum / _count;

            return Vector3.zero;
        }


        public void Reset()
        {
            _sum = Vector3.zero;
            _count = 0;
        }
    }
}
