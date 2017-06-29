using System.Diagnostics;
using Debug = UnityEngine.Debug;


namespace FlightMarkers.Utilities
{
    public class Logging
    {
        private const string Tag = "FlightMarkers";


        [Conditional("DEBUG")]
        public static void DebugLog(string msg)
        {
            Log(msg);
        }


        public static void Log(string msg)
        {
            Debug.Log($"[{Tag}] {msg}");
        }
    }
}
