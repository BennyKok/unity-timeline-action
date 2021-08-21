using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Linq;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Timeline;
#endif

namespace BennyKok.TimelineAction
{
    public class ActionMarker : Marker, INotification
    {
        public PropertyName id => action.GetHashCode();

        public ActionData action = new ActionData();

        [NonSerialized] public ActionBehaviour actionBehaviour;

#if UNITY_EDITOR
        public string GetEditorDisplay() => name; //string.IsNullOrWhiteSpace(action.actionName) ? name : action.actionName;
#endif
    }


#if UNITY_EDITOR

    [CustomEditor(typeof(ActionMarker), true)]
    public class ActionMarkerInspector : ActionInspector
    {
        public override ActionBehaviour GetActionBehaviour()
        {
            var t = (target as ActionMarker);
            var useGlobal = false;
            if (t.parent is MarkerTrack tt)
            {
                //If this is a global track, we use the base game object as binding
                useGlobal = tt.timelineAsset.markerTrack == tt;
            }
            return ActionBehaviourUtils.GetDesiredBehaviour(director.gameObject, t.parent, useGlobal);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var marker = target as ActionMarker;

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            {
                var newName = EditorGUILayout.DelayedTextField("Name", marker.name);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(marker, "Set marker name");
                    marker.name = newName;
                }
            }

            if (marker.parent && marker.parent.timelineAsset != TimelineEditor.inspectedAsset)
                return;

            if (IsActionEditorValid())
            {
                DrawAction(serializedObject.FindProperty("action"));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }


    [CustomTimelineEditor(typeof(ActionMarker))]
    public class ActionMarkerEditor : MarkerEditor
    {
        private static GUIStyle _centerBoldLabel;

        private GUIStyle CenterBoldLabel
        {
            get
            {
                if (_centerBoldLabel == null)
                {
                    _centerBoldLabel = new GUIStyle(EditorStyles.miniBoldLabel);
                    _centerBoldLabel.alignment = TextAnchor.UpperCenter;
                    _centerBoldLabel.wordWrap = true;
                }

                return _centerBoldLabel;
            }
        }

        public override void DrawOverlay(IMarker marker, MarkerUIStates uiState, MarkerOverlayRegion region)
        {
            // if (string.IsNullOrWhiteSpace((marker as ActionMarker).action.actionName)){
            var target = (marker as ActionMarker).GetEditorDisplay();
            var targetSize = CenterBoldLabel.CalcSize(new GUIContent(target));

            var rect = new Rect(region.markerRegion);
            rect.y += uiState == MarkerUIStates.Collapsed ? rect.height : 10;
            rect.x -= targetSize.x / 2 - rect.width / 2;
            rect.width = targetSize.x;
            rect.height = targetSize.y;

            GUI.Label(rect, target, CenterBoldLabel);
            // }

            base.DrawOverlay(marker, uiState, region);
        }

        public override MarkerDrawOptions GetMarkerOptions(IMarker marker)
        {
            var options = base.GetMarkerOptions(marker);
            ActionMarker emitter = (ActionMarker)marker;
            options.tooltip = emitter.GetEditorDisplay();
            return options;
        }
    }
#endif
}