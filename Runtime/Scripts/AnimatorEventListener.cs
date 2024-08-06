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
        private Dictionary<int, AnimatorStateInfo> previousStateInfos = new Dictionary<int, AnimatorStateInfo>();

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
            int layerCount = Animator.layerCount;

            for (int layer = 0; layer < layerCount; layer++)
            {
                AnimatorStateInfo currentStateInfo = Animator.GetCurrentAnimatorStateInfo(layer);
                int currentStateFullPathHash = currentStateInfo.fullPathHash;
                int currentStateTagHash = currentStateInfo.tagHash;

                // Check previous state first, if different
                if (previousStateInfos.ContainsKey(layer))
                {
                    AnimatorStateInfo previousStateInfo = previousStateInfos[layer];
                    int previousStateFullPathHash = previousStateInfo.fullPathHash;
                    int prevousStateTagHash = previousStateInfo.tagHash;

                    bool isNewState = currentStateFullPathHash != previousStateFullPathHash;
                    if (isNewState && eventCache.TryGetValue(prevousStateTagHash, out List<AnimatorEvent> events))
                    {
                        foreach (AnimatorEvent evt in events)
                        {
                            if (CanInvokeAnimatorEventForPreviousState(layer, previousStateFullPathHash, evt))
                            {
                                evt.Event?.Invoke();
                            }
                        }
                    }
                }

                // Check current state next
                if (eventCache.TryGetValue(currentStateTagHash, out List<AnimatorEvent> events2))
                {
                    foreach (AnimatorEvent evt in events2)
                    {
                        if (CanInvokeAnimatorEventForCurrentState(layer, currentStateFullPathHash, evt))
                        {
                            evt.Event?.Invoke();
                        }
                    }
                }

                if (!previousStateInfos.ContainsKey(layer))
                {
                    previousStateInfos.Add(layer, default);
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
            if (canInvoke)
            {
                invocatonCountByFullPathHash[fullPathHash] = invocations++;
            }

            return canInvoke;
        }

        private bool CanInvokeAnimatorEventForCurrentState(int layer, int fullPathHash, AnimatorEvent evt)
        {
            AnimatorStateInfo currentStateInfo = Animator.GetCurrentAnimatorStateInfo(layer);
            bool isFirstLoopInState = currentStateInfo.normalizedTime < 1f;
            if (isFirstLoopInState)
            {
                bool isNewState = !previousStateInfos.ContainsKey(layer) || previousStateInfos[layer].fullPathHash != fullPathHash;
                if (isNewState)
                {
                    invocatonCountByFullPathHash.Clear();
                    invocatonCountByFullPathHash.Add(fullPathHash, 0);
                }
            }

            // This happens sometimes (not sure why) so I'm adding this just in case
            if (!invocatonCountByFullPathHash.ContainsKey(fullPathHash))
            {
                invocatonCountByFullPathHash.Clear();
                invocatonCountByFullPathHash.Add(fullPathHash, 0);
            }

            bool hasInvoked = invocatonCountByFullPathHash[fullPathHash] > 0;
            if (evt.Mode == AnimatorEvent.InvokeMode.Once && hasInvoked)
            {
                return false;
            }

            int loopCount = (int)(currentStateInfo.normalizedTime / 1f);
            bool invokedThisLoop = invocatonCountByFullPathHash[fullPathHash] > loopCount;
            if (invokedThisLoop)
            {
                return false;
            }

            float totalTime = evt.UseNormalizedTime ? currentStateInfo.normalizedTime : currentStateInfo.normalizedTime * currentStateInfo.length;
            float currentTime = evt.UseNormalizedTime ? totalTime % 1f : totalTime % currentStateInfo.length;
            bool canInvoke = currentTime > evt.Time || loopCount > invocatonCountByFullPathHash[fullPathHash];
            if (canInvoke)
            {
                invocatonCountByFullPathHash[fullPathHash]++;
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
            previousStateInfos.Clear();
        }
    }

}
