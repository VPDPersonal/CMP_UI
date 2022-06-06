using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

using BehaviourType = UI.Button.BehaviourType;
using LongClickTransition = UI.Button.LongClickTransition;

// Code by VPDInc
// Email: vpd-2000@yandex.ru
// Version: 1.0 (06.06.2022)
namespace UI.Editor
{
    
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Button), true)]
    public sealed class ButtonEditor : SelectableEditor
    {
        private readonly GUIContent _transitionForLongClickLabel = new("Transition");

        #region Fields
        private SerializedProperty _type;
        
        private SerializedProperty _longClickTransition;
        private SerializedProperty _timeLongClick;
        
        private SerializedProperty _needMultiClickCount;
        private SerializedProperty _timeMultiClick;
        
        private SerializedProperty _clicked;
        #endregion

        protected override void OnEnable()
        {
            base.OnEnable();
            
            _type = serializedObject.FindProperty(nameof(_type));
            
            _longClickTransition = serializedObject.FindProperty(nameof(_longClickTransition));
            _timeLongClick = serializedObject.FindProperty(nameof(_timeLongClick));
            
            _needMultiClickCount = serializedObject.FindProperty(nameof(_needMultiClickCount));
            _timeMultiClick = serializedObject.FindProperty(nameof(_timeMultiClick));
            
            _clicked = serializedObject.FindProperty(nameof(_clicked));
        }

        #region Draw functions
        protected override void DrawFields()
        {
            EditorGUILayout.PropertyField(_type);
            EditorGUILayout.Space();

            switch (GetClickType(_type))
            {
                case BehaviourType.Click: base.DrawFields(); break;
                case BehaviourType.LongClick: DrawLongClick(); break;
                case BehaviourType.MultiClick: DrawMultiClick(); break;
                default: throw new ArgumentOutOfRangeException();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_clicked);
        }

        private void DrawMultiClick()
        {
            DrawInteractable();
            EditorGUILayout.PropertyField(_needMultiClickCount);
            EditorGUILayout.PropertyField(_timeMultiClick);
        }

        private void DrawLongClick()
        {
            DrawInteractable();
            EditorGUILayout.PropertyField(_longClickTransition, _transitionForLongClickLabel);
                    
            ++EditorGUI.indentLevel;
            {
                if (GetTransitionForLongClick(_longClickTransition) == LongClickTransition.Filling)
                    EditorGUILayout.PropertyField(TargetGraphicProperty);
            }
            --EditorGUI.indentLevel;
                    
            EditorGUILayout.PropertyField(_timeLongClick);
                    
            var graphic = (TargetGraphicProperty.objectReferenceValue as Graphic) ??
                (target as Selectable)?.GetComponent<Graphic>();
            if (graphic as Image == null)
                EditorGUILayout.HelpBox("You must have a Image target in order to use a sprite swap transition.", MessageType.Warning);
        }
        #endregion
        
        private static LongClickTransition GetTransitionForLongClick(SerializedProperty transitionForLongClick) =>
            (LongClickTransition)transitionForLongClick.enumValueIndex;
        
        private static BehaviourType GetClickType(SerializedProperty clickType) =>
            (BehaviourType)clickType.enumValueIndex;
    }
}
