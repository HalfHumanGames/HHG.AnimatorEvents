using System;
using System.Collections.Generic;
using UnityEngine;

namespace HHG.AnimatorEvents.Runtime
{
    public class AnimatorEventListener : MonoBehaviour
    {
        public Animator Animator;

        [SerializeField] private List<AnimatorEvent> events = new List<AnimatorEvent>();

        private Dictionary<int, List<AnimatorEvent>> eventCache = new Dictionary<int, List<AnimatorEvent>>();
        private Dictionary<int, int> invocatonCountByFullPathHash = new Dictionary<int, int>();
        private AnimatorStateInfo[] previousStateInfos = new AnimatorStateInfo[0];

        private void Awake()
        {
            GetAnimator();
            RebuildEventCache();
        }

        public void GetAnimator()
        {
            if (Animator == null)
            {
                Animator = GetComponent<Animator>();
            }
        }

        private void RebuildEventCache()
        {
            eventCache.Clear();

            for (int i = 0; i < events.Count; i++)
            {
                for (int j = 0; j < events[i].Tags.Count; j++)
                {
                    string tag = events[i].Tags[j];
                    int hash = Animator.StringToHash(tag);

                    if (!eventCache.ContainsKey(hash))
                    {
                        eventCache[hash] = new List<AnimatorEvent>();
                    }

                    eventCache[hash].Add(events[i]);
                }
            }

            Array.Resize(ref previousStateInfos, Animator.layerCount);
        }

        public void AddEvent(AnimatorEvent animatorEvent)
        {
            events.Add(animatorEvent);
            RebuildEventCache();
        }

        public void RemoveEvent(AnimatorEvent animatorEvent)
        {
            events.Remove(animatorEvent);
            RebuildEventCache();
        }

        private void LateUpdate()
        {
            for (int layer = 0, layerCount = Animator.layerCount; layer < layerCount; layer++)
            {
                AnimatorStateInfo currentStateInfo = Animator.GetCurrentAnimatorStateInfo(layer);
                int currentStateFullPathHash = currentStateInfo.fullPathHash;
                int currentStateTagHash = currentStateInfo.tagHash;

                AnimatorStateInfo previousStateInfo = previousStateInfos[layer];
                int previousStateFullPathHash = previousStateInfo.fullPathHash;
                int previousStateTagHash = previousStateInfo.tagHash;

                bool isNewState = currentStateFullPathHash != previousStateFullPathHash;
                if (isNewState)
                {
                    if (eventCache.TryGetValue(previousStateTagHash, out List<AnimatorEvent> events))
                    {
                        foreach (AnimatorEvent evt in events)
                        {
                            if (CanInvokeAnimatorEventForPreviousState(layer, previousStateFullPathHash, evt))
                            {
                                evt.Event?.Invoke();
                            }
                        }

                        invocatonCountByFullPathHash[previousStateFullPathHash] = 0;
                    }
                }

                if (eventCache.TryGetValue(currentStateTagHash, out List<AnimatorEvent> events2))
                {
                    if (isNewState)
                    {
                        invocatonCountByFullPathHash[currentStateFullPathHash] = 0;
                    }

                    foreach (AnimatorEvent evt in events2)
                    {
                        if (CanInvokeAnimatorEventForCurrentState(currentStateInfo, previousStateInfo, evt))
                        {
                            evt.Event?.Invoke();
                        }
                    }
                }

                previousStateInfos[layer] = currentStateInfo;
            }
        }

        private bool CanInvokeAnimatorEventForPreviousState(int layer, int fullPathHash, AnimatorEvent evt)
        {
            int invocations = invocatonCountByFullPathHash.TryGetValue(fullPathHash, out invocations) ? invocations : 0;
            bool hasInvoked = invocations > 0;
            if (evt.Mode == AnimatorEvent.InvokeMode.Once && hasInvoked)
            {
                return false;
            }

            AnimatorStateInfo previousStateInfo = previousStateInfos[layer];
            int loopCount = (int)(previousStateInfo.normalizedTime / 1f);
            bool invokedThisLoop = invocations > loopCount;
            if (invokedThisLoop)
            {
                return false;
            }

            bool canInvoke = loopCount >= invocations;
            return canInvoke;
        }

        private bool CanInvokeAnimatorEventForCurrentState(AnimatorStateInfo currentStateInfo, AnimatorStateInfo previousStateInfo, AnimatorEvent evt)
        {
            int fullPathHash = currentStateInfo.fullPathHash;
            float normalizedTime = currentStateInfo.normalizedTime;
            bool isFirstLoopInState = normalizedTime <= 1f;

            if (isFirstLoopInState)
            {
                int prevPathHash = previousStateInfo.fullPathHash;
                bool isNewState = prevPathHash != fullPathHash;

                if (isNewState)
                {
                    invocatonCountByFullPathHash.Clear();
                }
            }

            int invocations = invocatonCountByFullPathHash.TryGetValue(fullPathHash, out invocations) ? invocations : 0;
            bool hasInvoked = invocations > 0;

            if (hasInvoked && evt.Mode == AnimatorEvent.InvokeMode.Once)
            {
                return false;
            }

            int loopCount = (int)(normalizedTime / 1f);
            bool invokedThisLoop = invocations > loopCount;

            if (invokedThisLoop)
            {
                return false;
            }

            bool useNormalized = evt.UseNormalizedTime;
            float evtTime = evt.Time;
            float length = currentStateInfo.length;
            float totalTime = useNormalized ? normalizedTime : normalizedTime * length;
            float currentTime = useNormalized ? totalTime % 1f : totalTime % length;
            bool canInvoke = currentTime > evtTime || loopCount > invocations;

            if (canInvoke)
            {
                invocatonCountByFullPathHash[fullPathHash] = invocations + 1;
            }

            return canInvoke;
        }

        private void OnValidate()
        {
            GetAnimator();
        }

        public void Reset()
        {
            GetAnimator();
            invocatonCountByFullPathHash.Clear();
            Array.Fill(previousStateInfos, default);
        }
    }

}
