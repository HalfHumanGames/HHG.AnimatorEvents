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

        public List<string> Tags = new List<string>();
        public InvokeMode Mode;
        public bool UseNormalizedTime;
        public float Time;
        public UnityEvent Event = new UnityEvent();

        public AnimatorEvent Clone()
        {
            return new AnimatorEvent
            {
                Tags = new List<string>(Tags),
                Mode = Mode,
                UseNormalizedTime = UseNormalizedTime,
                Time = Time,
                Event = Event
            };
        }
    }
}
