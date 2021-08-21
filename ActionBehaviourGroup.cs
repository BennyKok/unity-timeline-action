using System.Linq;
using System.Collections.Generic;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BennyKok.TimelineAction
{
    public class ActionBehaviourGroup : ActionBehaviour
    {
        public List<ActionBehaviour> actionBehaviours;

        protected override void OnRegisterActions()
        {
            foreach (var entry in actionBehaviours)
            {
                if (entry)
                {
                    entry.InitActions();
                    // Debug.Log(entry.actions.Count);
                    foreach (var subAction in entry.actions)
                    {
                        RegisterAction(subAction.Key, subAction.Value, true);
                    }

                    if (Application.isPlaying)
                    {
                        //in playmode, we can disable these component
                        entry.inActionGroup = true;
                    }
                }
            }
            // Debug.Log(actions.Count);
        }

        public void Reset()
        {
            actionBehaviours = GetComponents<ActionBehaviour>().ToList();
            actionBehaviours.Remove(this);
            InitActions();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ActionBehaviourGroup), true)]
    public class ActionBehaviourGroupEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("actionBehaviours"));

            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                // Debug.Log("Changed");
                (target as ActionBehaviourGroup).InitActions();
            }

            if (GUILayout.Button("Update Reference"))
            {
                Undo.RecordObject(target, "Update Group Actions Reference");
                (target as ActionBehaviourGroup).Reset();
            }
        }

    }
#endif
}