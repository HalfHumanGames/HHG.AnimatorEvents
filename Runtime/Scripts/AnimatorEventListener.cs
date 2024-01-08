using System.Collections.Generic;
using UnityEngine;

namespace HHG.AnimatorEvents
{
	public class AnimatorEventListener : MonoBehaviour {

		public Animator Animator;
		[SerializeField] private List<AnimatorEvent> events = new List<AnimatorEvent>();
		private List<AnimatorEvent> uncachedEvents; // Not initialzied since initialzied in Awake
		private Dictionary<int, List<AnimatorEvent>> eventsByFullPathHash = new Dictionary<int, List<AnimatorEvent>>();
		private Dictionary<int, int> invocatonCountByFullPathHash = new Dictionary<int, int>();
		private Dictionary<int, AnimatorStateInfo> previousStateInfos = new Dictionary<int, AnimatorStateInfo>();

		private void Awake() {
			GetAnimator();
			uncachedEvents = new List<AnimatorEvent>(events);
		}

		public void GetAnimator() {
			if (Animator == null) {
				Animator = GetComponent<Animator>();
			}
		}

		public void AddEvent(AnimatorEvent animatorEvent) {
			events.Add(animatorEvent);
			if (uncachedEvents != null) {
				uncachedEvents.Add(animatorEvent);
			}
		}

		private void LateUpdate() {
			TryCacheAnimatorEvents();
			TryExecuteAnimatorEvents();
		}
		
		private void TryCacheAnimatorEvents() {
			for (int i = 0; i < uncachedEvents.Count; i++) {
				for (int j = 0; j < uncachedEvents[i].States.Count; j++) {
					AnimatorStateReference state = uncachedEvents[i].States[j];
					int layer = state.LayerIndex;
					AnimatorStateInfo stateInfo = Animator.GetCurrentAnimatorStateInfo(layer);
					if (stateInfo.IsName(state.StateName)) {
						int fullPathHash = stateInfo.fullPathHash;
						if (!eventsByFullPathHash.ContainsKey(fullPathHash)) {
							eventsByFullPathHash.Add(fullPathHash, new List<AnimatorEvent>());
						}
						eventsByFullPathHash[fullPathHash].Add(uncachedEvents[i]);
						uncachedEvents[i].States.RemoveAt(j);
						if (uncachedEvents[i].States.Count == 0) {
							uncachedEvents.RemoveAt(i--);
							break;
						}
					}
				}
			}
		}

		private void TryExecuteAnimatorEvents() {
			int layerCount = Animator.layerCount;
			for (int layer = 0; layer < layerCount; layer++) {

				AnimatorStateInfo currentStateInfo = Animator.GetCurrentAnimatorStateInfo(layer);
				int currentStateFullPathHash = currentStateInfo.fullPathHash;

				// Check previous state first, if different
				if (previousStateInfos.ContainsKey(layer)) {

					AnimatorStateInfo previousStateInfo = previousStateInfos[layer];
					int previousStateFullPathHash = previousStateInfo.fullPathHash;

					bool isNewState = currentStateFullPathHash != previousStateFullPathHash;
					if (isNewState && eventsByFullPathHash.ContainsKey(previousStateFullPathHash)) {
						foreach (AnimatorEvent evt in eventsByFullPathHash[previousStateFullPathHash]) {
							if (CanInvokeAnimatorEventForPreviousState(layer, previousStateFullPathHash, evt)) {
								evt.Event?.Invoke();
							}
						}
					}
				}

				// Check current state next
				if (eventsByFullPathHash.ContainsKey(currentStateFullPathHash)) {
					foreach (AnimatorEvent evt in eventsByFullPathHash[currentStateFullPathHash]) {
						if (CanInvokeAnimatorEventForCurrentState(layer, currentStateFullPathHash, evt)) {
							evt.Event?.Invoke();
						}
					}
				}

				if (!previousStateInfos.ContainsKey(layer)) {
					previousStateInfos.Add(layer, default);
				}
				previousStateInfos[layer] = currentStateInfo;
			}
		}

		private bool CanInvokeAnimatorEventForPreviousState(int layer, int fullPathHash, AnimatorEvent evt) {
			AnimatorStateInfo previousStateInfo = previousStateInfos[layer];
			bool hasInvoked = invocatonCountByFullPathHash[fullPathHash] > 0;
			if (evt.Mode == AnimatorEvent.InvokeMode.Once && hasInvoked) {
				return false;
			}
			int loopCount = (int) previousStateInfo.normalizedTime / 1;
			bool invokedThisLoop = invocatonCountByFullPathHash[fullPathHash] > loopCount;
			if (invokedThisLoop) {
				return false;
			}
			bool canInvoke = loopCount >= invocatonCountByFullPathHash[fullPathHash];
			if (canInvoke) {
				invocatonCountByFullPathHash[fullPathHash]++;
			}
			return canInvoke;
		}

		private bool CanInvokeAnimatorEventForCurrentState(int layer, int fullPathHash, AnimatorEvent evt) {
			AnimatorStateInfo currentStateInfo = Animator.GetCurrentAnimatorStateInfo(layer);
			bool isFirstLoopInState = currentStateInfo.normalizedTime < 1;
			if (isFirstLoopInState) {
				bool isNewState = !previousStateInfos.ContainsKey(layer) || previousStateInfos[layer].fullPathHash != fullPathHash;
				if (isNewState) {
					invocatonCountByFullPathHash.Clear();
					invocatonCountByFullPathHash.Add(fullPathHash, 0);
				}
			}

			// This happens sometimes (not sure why) so I'm adding this just in case
			if (!invocatonCountByFullPathHash.ContainsKey(fullPathHash)) {
				invocatonCountByFullPathHash.Clear();
				invocatonCountByFullPathHash.Add(fullPathHash, 0);
			}

			bool hasInvoked = invocatonCountByFullPathHash[fullPathHash] > 0;
			if (evt.Mode == AnimatorEvent.InvokeMode.Once && hasInvoked) {
				return false;
			}
			int loopCount = (int) currentStateInfo.normalizedTime / 1;
			bool invokedThisLoop = invocatonCountByFullPathHash[fullPathHash] > loopCount;
			if (invokedThisLoop) {
				return false;
			}
			float totalTime = evt.UseNormalizedTime ?
				currentStateInfo.normalizedTime :
				currentStateInfo.normalizedTime * currentStateInfo.length;
			float currentTime = evt.UseNormalizedTime ?
				totalTime % 1 : totalTime % currentStateInfo.length;
			bool canInvoke = currentTime > evt.Time || loopCount > invocatonCountByFullPathHash[fullPathHash];
			if (canInvoke) {
				invocatonCountByFullPathHash[fullPathHash]++;
			}
			return canInvoke;
		}

		private void OnValidate() {
			GetAnimator();
		}

		private void Reset() {
			GetAnimator();
		}
	}

}
