using System.Collections.Generic;
using UnityEngine.Events;

namespace HHG.AnimatorEvents
{
    public class AnimatorEvent
    {
        public List<AnimatorStateReference> States = new List<AnimatorStateReference>();
        public InvokeMode Mode;
        public bool UseNormalizedTime;
        public float Time;
        public UnityEvent Event;

        public enum InvokeMode
        {
            Always,
            Once,
        }
    }
}
