using System;

namespace HHG.AnimatorEvents.Runtime
{
    [Serializable]
    public struct AnimatorStateReference
    {
        public int LayerIndex;
        public string StateName;

        public AnimatorStateReference(int layerIndex, string stateName)
        {
            LayerIndex = layerIndex;
            StateName = stateName;
        }
    }
}
