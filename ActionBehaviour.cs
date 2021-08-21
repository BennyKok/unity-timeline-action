using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Timeline;
#endif

namespace BennyKok.TimelineAction
{
#if UNITY_EDITOR
    [CustomEditor(typeof(ActionBehaviour), true)]
    public class ActionBehaviourEditor : Editor
    {
        private PlayableDirector director;

        private SerializedProperty directorProperty;

        private void OnEnable()
        {
            var t = target as ActionBehaviour;
            directorProperty = serializedObject.FindProperty("director");
            t.TryGetComponent<PlayableDirector>(out director);
        }
        public override void OnInspectorGUI()
        {
            var t = target as ActionBehaviour;

            if (director && director.gameObject != t.gameObject)
            {
                t.TryGetComponent<PlayableDirector>(out director);
            }

            //We skip the director field if the current gameobject has it
            serializedObject.Update();
            if (!director)
            {
                // EditorGUILayout.Space();
                EditorGUILayout.LabelField("Target Director", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(directorProperty);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Properties", EditorStyles.miniLabel);
            }
            DrawPropertiesExcluding(serializedObject, "m_Script", "director");
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif

    public abstract class ActionBehaviour : MonoBehaviour, INotificationReceiver
    {
        [NonSerialized] public Dictionary<string, Func<bool>> actions;

        [HideInInspector] public PlayableDirector director;
        private TimelineAsset timelineAsset;

        private string scopeName;

        [NonSerialized] public bool inActionGroup;

        protected virtual void Awake()
        {
            if (!director)
            {
                director = GetComponent<PlayableDirector>();

                if (director)
                    timelineAsset = director.playableAsset as TimelineAsset;
            }

            InitActions();
        }

        public void InitActions()
        {
            if (actions == null)
                actions = new Dictionary<string, Func<bool>>();

            actions.Clear();
            scopeName = null;

            OnRegisterActions();
        }

        public void SetActionScope(string scopeName)
        {
            this.scopeName = scopeName;

            if (!string.IsNullOrEmpty(scopeName) && !this.scopeName.EndsWith("/"))
            {
                this.scopeName += "/";
            }
        }

        public void RegisterAction(string name, Func<bool> action, bool noScope = false)
        {
            if (!noScope && string.IsNullOrEmpty(scopeName))
            {
                string name1 = this.GetType().Name;
                int length = name1.LastIndexOf("Action");
                string tempName;
                if (length != -1)
                    tempName = name1.Substring(0, length);
                else tempName = name1;

                SetActionScope(tempName);
            }
            actions.Add(scopeName + name, action);
        }

        public void RegisterAction(Action action)
        {
            RegisterAction(action.Method.Name, () =>
            {
                action();
                return true;
            });
        }

        public void RegisterAction(Func<bool> action)
        {
            RegisterAction(action.Method.Name, action);
        }


        public void RegisterAction(string name, Action action)
        {
            RegisterAction(name, () =>
            {
                action();
                return true;
            });
        }

        protected abstract void OnRegisterActions();

        public virtual Func<bool> GetFunction(string name)
        {
            actions.TryGetValue(name, out var func);
            return func;
        }

        public bool RunAction(string name)
        {
            Func<bool> func = GetFunction(name);
            if (func == null)
            {
                Debug.LogWarning($"Action {name} not found.");
                return false;
            }
            return func.Invoke();
        }

        public void ResolveAction(ActionData data, PlayableDirector director)
        {
            if (string.IsNullOrWhiteSpace(data.actionName)) return;

            if (RunAction(data.actionName))
                ResolveResultBehaviour(data.succussBehaviour, director);
            else
                ResolveResultBehaviour(data.failBehaviour, director);
        }

        public void ResolveResultBehaviour(ActionResultBehaviour behaviour, PlayableDirector director)
        {
            switch (behaviour.behaviourType)
            {
                case ActionResultBehaviour.ResultBehaviourType.JumpToMarker:
                    director.Stop();
                    director.time = (behaviour.jumpToMarker as ActionMarker).time;
                    director.Evaluate();
                    director.Play();
                    break;
                case ActionResultBehaviour.ResultBehaviourType.Pause:
                    director.Pause();
                    break;
                case ActionResultBehaviour.ResultBehaviourType.Stop:
                    director.Stop();
                    break;
                case ActionResultBehaviour.ResultBehaviourType.Resume:
                    director.Resume();
                    break;
            }
        }

        public void OnNotify(Playable origin, INotification notification, object context)
        {
            if (inActionGroup) return;

            var originDirector = origin.GetGraph().GetResolver() as PlayableDirector;

            if (notification is ActionMarker marker)
            {
                // Debug.Log($"Triggering {marker.name} at {name}");
                marker.actionBehaviour = this;
                ResolveAction(marker.action, originDirector);
            }
        }
    }
}