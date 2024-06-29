using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace HHG.AnimatorEvents.Runtime
{
    [Serializable]
    public class AnimatorEvent
    {
        public List<AnimatorStateReference> States = new List<AnimatorStateReference>();
        public InvokeMode Mode;
        public bool UseNormalizedTime;
        public float Time;
        public UnityEvent Event = new UnityEvent();

        public enum InvokeMode
        {
            Always,
            Once,
        }
    }
}
