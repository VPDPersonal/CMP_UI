using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

using Behaviour = VPDUI.ImprovedButton.Behaviour;
using LongClickTransition = VPDUI.ImprovedButton.LongClickTransition;

// Code by VPDInc
// Email: vpd-2000@yandex.com
// Version: 1.1.0
namespace VPDUI.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ImprovedButton), true)]
    public sealed class ButtonEditor : SelectableEditor
    {
        private readonly GUIContent _transitionForLongClickLabel = new("Transition");

        #region Fields
        private SerializedProperty _behaviours;
        
        private SerializedProperty _longClickTransition;
        private SerializedProperty _timeLongClick;
        
        private SerializedProperty _needMultiClickCount;
        private SerializedProperty _timeMultiClick;
        
        private SerializedProperty _clicked;
        #endregion

        protected override void OnEnable()
        {
            base.OnEnable();
            
            _behaviours = serializedObject.FindProperty(nameof(_behaviours));
            
            _longClickTransition = serializedObject.FindProperty(nameof(_longClickTransition));
            _timeLongClick = serializedObject.FindProperty(nameof(_timeLongClick));
            
            _needMultiClickCount = serializedObject.FindProperty(nameof(_needMultiClickCount));
            _timeMultiClick = serializedObject.FindProperty(nameof(_timeMultiClick));
            
            _clicked = serializedObject.FindProperty(nameof(_clicked));
        }

        #region Draw functions
        protected override void DrawFields()
        {
            EditorGUILayout.PropertyField(_behaviours);
            EditorGUILayout.Space();

            for (var i = 0; i < _behaviours.arraySize; i++)
            {
                switch (GetClickType(_behaviours.GetArrayElementAtIndex(i)))
                {
                    case Behaviour.Click: base.DrawFields(); break;
                    case Behaviour.LongClick: DrawLongClick(); break;
                    case Behaviour.MultiClick: DrawMultiClick(); break;
                    default: throw new ArgumentOutOfRangeException();
                }
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
            if (!(graphic as Image))
                EditorGUILayout.HelpBox("You must have a Image target in order to use a sprite swap transition.", MessageType.Warning);
        }
        #endregion
        
        private static LongClickTransition GetTransitionForLongClick(SerializedProperty transitionForLongClick) =>
            (LongClickTransition)transitionForLongClick.enumValueIndex;
        
        private static Behaviour GetClickType(SerializedProperty clickType) =>
            (Behaviour)clickType.enumValueIndex;
    }
}
