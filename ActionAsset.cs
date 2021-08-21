using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

#if UNITY_EDITOR
using UnityEditor.Timeline;
using UnityEditor;
#endif

namespace BennyKok.TimelineAction
{
    public class ActionAsset : PlayableAsset, ITimelineClipAsset
    {
        public ActionData startAction = new ActionData();
        public ActionData endAction = new ActionData();

        [Space]
        public ActionData updateAction = new ActionData();

        public ClipCaps clipCaps => ClipCaps.None;

        [NonSerialized] public ActionBehaviour actionBehaviour;

        public string GetErrorDisplay(ActionBehaviour actionBehaviour)
        {
            if (!actionBehaviour)
            {
                return "No Action Behaviour Found";
            }

            if (ActionBehaviourUtils.VerifyActionData(actionBehaviour, startAction) &&
            ActionBehaviourUtils.VerifyActionData(actionBehaviour, endAction) &&
            ActionBehaviourUtils.VerifyActionData(actionBehaviour, updateAction))
            {
                return null;
            }
            else
            {
                return "Contains Invalid Action";
            }
        }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<ActionPlayable>.Create(graph);

            var playableBehaviour = playable.GetBehaviour();
            playableBehaviour.startAction = startAction;
            playableBehaviour.endAction = endAction;
            playableBehaviour.updateAction = updateAction;
            playableBehaviour.actionBehaviour = actionBehaviour;

            return playable;
        }
    }

    public class ActionPlayable : PlayableBehaviour
    {
        public ActionData startAction;
        public ActionData endAction;
        public ActionData updateAction;

        public ActionBehaviour actionBehaviour;

        [NonSerialized] private PlayableDirector originDirector;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (!originDirector)
                originDirector = playable.GetGraph().GetResolver() as PlayableDirector;

            if (startAction != null)
                actionBehaviour.ResolveAction(startAction, originDirector);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var duration = playable.GetDuration();
            var time = playable.GetTime();
            var count = time + info.deltaTime;

            if ((info.effectivePlayState == PlayState.Paused && count > duration) || Mathf.Approximately((float)time, (float)duration))
            {
                // Execute your finishing logic here:
                // Debug.Log("Clip done!");

                if (!originDirector)
                    originDirector = playable.GetGraph().GetResolver() as PlayableDirector;

                if (endAction != null)
                    actionBehaviour.ResolveAction(endAction, originDirector);
            }
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (updateAction != null)
            {
                if (!originDirector)
                    originDirector = playable.GetGraph().GetResolver() as PlayableDirector;
                actionBehaviour.ResolveAction(updateAction, originDirector);
            }
        }
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(ActionAsset), true)]
    public class ActionAssetInspector : ActionInspector
    {
        public override ActionBehaviour GetActionBehaviour()
        {
            return (target as ActionAsset).actionBehaviour;
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (IsActionEditorValid())
            {
                DrawAction(serializedObject.FindProperty("startAction"));
                DrawAction(serializedObject.FindProperty("endAction"));
                DrawAction(serializedObject.FindProperty("updateAction"));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomTimelineEditor(typeof(ActionAsset))]
    public class ActionAssetEditor : ClipEditor
    {
        private ActionBehaviour actionBehaviour;

        public void UpdateState(TrackAsset track)
        {
            if (!actionBehaviour)
            {
                actionBehaviour = ActionBehaviourUtils.GetDesiredBehaviour(TimelineEditor.inspectedDirector.gameObject, track);
            }
        }

        public override void OnCreate(TimelineClip clip, TrackAsset track, TimelineClip clonedFrom)
        {
            if (TimelineEditor.inspectedDirector)
            {
                UpdateState(track);
                ((ActionAsset)clip.asset).actionBehaviour = actionBehaviour;
            }

            base.OnCreate(clip, track, clonedFrom);
        }

        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            var options = base.GetClipOptions(clip);


            if (TimelineEditor.inspectedDirector)
            {
                actionBehaviour = null;
                UpdateState(clip.parentTrack);
                ActionAsset action = (ActionAsset)clip.asset;
                options.errorText = action.GetErrorDisplay(actionBehaviour);
            }

            return options;
        }
    }
#endif
}
