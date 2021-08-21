using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Timeline;
#endif
namespace BennyKok.TimelineAction
{
    [System.Serializable]
    public class ActionData
    {
        public string actionName;
        public ActionResultBehaviour succussBehaviour;
        public ActionResultBehaviour failBehaviour;
    }

    public static class ActionBehaviourUtils
    {
        public static ActionBehaviour GetDesiredBehaviour(GameObject gameObject, TrackAsset track = null, bool useGlobal = false)
        {
            if (track)
            {
                //re-target to track binding
                if (gameObject.TryGetComponent<PlayableDirector>(out var dir))
                {
                    var actionBehaviour = dir.GetGenericBinding(track) as ActionBehaviour;
                    if (actionBehaviour)
                    {
                        gameObject = actionBehaviour.gameObject;
                    }
                    else if (!useGlobal)
                    {
                        gameObject = null;
                    }
                }
            }

            if (!gameObject) return null;

            //Return the group first if there is
            if (gameObject.TryGetComponent<ActionBehaviourGroup>(out var group))
            {
                return group;
            }

            if (gameObject.TryGetComponent<ActionBehaviour>(out var behaviour))
            {
                return behaviour;
            }

            return null;
        }

        public static bool VerifyActionData(ActionBehaviour behaviour, ActionData data)
        {
            if (behaviour.actions == null)
                behaviour.InitActions();

            if (string.IsNullOrWhiteSpace(data.actionName)) return true;

            return behaviour.actions.ContainsKey(data.actionName);
        }
    }

    [System.Serializable]
    public class ActionResultBehaviour
    {
        public ResultBehaviourType behaviourType;

        [HideInInspector] public Marker jumpToMarker;


        public enum ResultBehaviourType
        {
            None, JumpToMarker, Pause, Stop, Resume
        }
    }

#if UNITY_EDITOR
    public abstract class ActionInspector : Editor
    {
        public PlayableDirector director;
        public TimelineAsset timelineAsset;
        private ActionBehaviour actionBehaviour;

        private List<string> allMarkerNames;
        private List<ActionMarker> allMarkers;
        private List<string> actionList;

        protected virtual void OnEnable()
        {
            RefreshState();
        }

        public virtual ActionBehaviour GetActionBehaviour()
        {
            return ActionBehaviourUtils.GetDesiredBehaviour(director.gameObject);
        }

        private void RefreshState()
        {
            director = TimelineEditor.inspectedDirector;
            if (director)
            {
                actionBehaviour = GetActionBehaviour();
                timelineAsset = director.playableAsset as TimelineAsset;

                allMarkers = timelineAsset.markerTrack.GetMarkers().OfType<ActionMarker>().Where(x =>
                {
                    return x.name != "Action Marker";
                }).ToList();
                allMarkerNames = allMarkers.ConvertAll(x => x.name);
                allMarkerNames.Insert(0, "None");

                if (actionBehaviour)
                {
                    if (actionBehaviour.actions == null)
                        actionBehaviour.InitActions();

                    // Debug.Log(actionBehaviour.GetType().Name + actionBehaviour.actions.Count);
                    actionList = actionBehaviour.actions.Keys.ToList();
                    actionList.Insert(0, "None");
                    actionList.Insert(0, "Custom");
                }
            }
        }

        public void DrawAction(SerializedProperty property)
        {
            var actionNameProperty = property.FindPropertyRelative("actionName");
            var succussBehaviourProperty = property.FindPropertyRelative("succussBehaviour");
            var failBehaviourProperty = property.FindPropertyRelative("failBehaviour");

            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 10);
            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUILayout.BeginVertical();
            var l = actionBehaviour ? actionBehaviour.name : "<No Binding>";
            EditorGUILayout.LabelField($"{property.displayName} - {l}", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                var index = actionList == null ? 0 : Mathf.Max(actionList.IndexOf(actionNameProperty.stringValue), 0);

                if (actionList != null)
                {
                    //None
                    if (string.IsNullOrWhiteSpace(actionNameProperty.stringValue))
                        index = 1;

                    EditorGUI.BeginChangeCheck();
                    {
                        index = EditorGUILayout.Popup("Action", index, actionList == null ? null : actionList.ToArray());

                        if (EditorGUI.EndChangeCheck())
                        {
                            if (index > 1)
                                actionNameProperty.stringValue = actionList[index];
                            else if (index == 1)
                                actionNameProperty.stringValue = null;
                            else if (index == 0)
                                actionNameProperty.stringValue = "Custom";
                        }
                    }
                }

                //Custom
                if (index == 0)
                {
                    EditorGUILayout.PropertyField(actionNameProperty);

                    if (actionList != null)
                    {
                        var target = actionNameProperty.stringValue.ToLower();
                        var possible = "";
                        //Skip the first two default option
                        for (int i = 2; i < actionList.Count; i++)
                        {
                            var action = actionList[i];
                            var lowered = action.ToLower();
                            if (lowered.Contains(target) || target.Contains(lowered))
                            {
                                possible = action;
                                break;
                            }
                        }
                        if (!string.IsNullOrWhiteSpace(possible))
                        {
                            EditorGUILayout.HelpBox($"Similar action found {possible}", MessageType.Warning);
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Fix", GUILayout.Width(80)))
                            {
                                actionNameProperty.stringValue = possible;
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(actionNameProperty.stringValue))
                {
                    DrawResultBehaviour(succussBehaviourProperty);
                    DrawResultBehaviour(failBehaviourProperty);
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel = indentLevel;
            EditorGUILayout.Space();
        }

        public void DrawResultBehaviour(SerializedProperty property)
        {
            var jumpToMarkerProperty = property.FindPropertyRelative("jumpToMarker");
            var behaviourTypeProperty = property.FindPropertyRelative("behaviourType");

            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.PropertyField(behaviourTypeProperty, new GUIContent(property.displayName));

                switch ((ActionResultBehaviour.ResultBehaviourType)behaviourTypeProperty.enumValueIndex)
                {
                    case ActionResultBehaviour.ResultBehaviourType.JumpToMarker:
                        if (allMarkers != null)
                        {
                            var index = allMarkers.IndexOf(jumpToMarkerProperty.objectReferenceValue as ActionMarker) + 1;
                            EditorGUI.BeginChangeCheck();
                            {
                                index = EditorGUILayout.Popup("Jump To Marker", index, allMarkerNames.ToArray());

                                if (EditorGUI.EndChangeCheck())
                                    if (index > 0)
                                        jumpToMarkerProperty.objectReferenceValue = allMarkers[index - 1];
                                    else
                                        jumpToMarkerProperty.objectReferenceValue = null;
                            }
                        }
                        else
                        {
                            EditorGUILayout.PropertyField(jumpToMarkerProperty);
                        }
                        break;
                }
            }
            EditorGUILayout.EndVertical();
        }

        public bool IsActionEditorValid()
        {
            if (TimelineEditor.inspectedDirector != director)
                RefreshState();

            // if (!director) return false;
            // if (!actionBehaviour) return false;

            return true;
        }
    }
#endif
}