using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Timeline;
#endif

namespace BennyKok.TimelineAction
{
    [TrackClipType(typeof(ActionAsset))]
    [TrackBindingType(typeof(ActionBehaviour))]
    public class ActionTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var p = base.CreateTrackMixer(graph, go, inputCount);

            var ab = ActionBehaviourUtils.GetDesiredBehaviour(go, this);
            foreach (var c in GetClips())
            {
                (c.asset as ActionAsset).actionBehaviour = ab;
            }

            return p;
        }
    }

#if UNITY_EDITOR
    [CustomTimelineEditor(typeof(ActionTrack))]
    public class ActionTrackEditor : TrackEditor
    {

    }
#endif
}