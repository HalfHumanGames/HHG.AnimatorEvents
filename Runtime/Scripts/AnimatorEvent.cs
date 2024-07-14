using HHG.Common.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace HHG.AnimatorEvents.Runtime
{
    [Serializable]
    public class AnimatorEvent : ICloneable<AnimatorEvent>
    {
        public enum InvokeMode
        {
            Always,
            Once,
        }

        public List<AnimatorStateReference> States = new List<AnimatorStateReference>();
        public InvokeMode Mode;
        public bool UseNormalizedTime;
        public float Time;
        public UnityEvent Event = new UnityEvent();

        public AnimatorEvent Clone()
        {
            return new AnimatorEvent
            {
                States = new List<AnimatorStateReference>(States),
                Mode = Mode,
                UseNormalizedTime = UseNormalizedTime,
                Time = Time,
                Event = Event
            };
        }
    }
}
