using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.Animations;
using UnityEditor.AnimatedValues;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

using Transition = UnityEngine.UI.Selectable.Transition;

// Code by VPDInc
// Email: vpd-2000@yandex.com
// Version: 1.0.0
namespace UI.Editor
{
    [CustomEditor(typeof(Selectable), true)]
    public class SelectableEditor : UnityEditor.Editor
    {
        #region Consts
        private const float ArrowThickness = 2.5f;
        private const float ArrowHeadSize = 1.2f;
        private const string ShowNavigationKey = "SelectableEditor.ShowNavigation";
        #endregion

        #region Readonly fields
        private readonly GUIContent _visualizeNavigation = EditorGUIUtility.TrTextContent("Visualize", "Show navigation flows between selectable UI elements.");
        private readonly AnimBool _showColorTint = new();
        private readonly AnimBool _showSpriteTransition = new();
        private readonly AnimBool _showAnimTransition = new();
        #endregion

        #region Static readonly
        private static readonly List<SelectableEditor> _editors = new();
        private static bool _showNavigation;
        #endregion

        #region Fields
        private SerializedProperty _script;
        private SerializedProperty _interactableProperty;
        private SerializedProperty _targetGraphicProperty;
        private SerializedProperty _transitionProperty;
        private SerializedProperty _colorBlockProperty;
        private SerializedProperty _spriteStateProperty;
        private SerializedProperty _animTriggerProperty;
        private SerializedProperty _navigationProperty;

        // Whenever adding new SerializedProperties to the Selectable and SelectableEditor
        // Also update this guy in OnEnable. This makes the inherited classes from Selectable not require a CustomEditor.
        private string[] _propertyPathToExcludeForChildClasses;
        #endregion

        #region Fields
        protected SerializedProperty Script => _script;
        protected SerializedProperty InteractableProperty => _interactableProperty;
        protected SerializedProperty TargetGraphicProperty => _targetGraphicProperty;
        protected SerializedProperty TransitionProperty => _transitionProperty;
        protected SerializedProperty ColorBlockProperty => _colorBlockProperty;
        protected SerializedProperty SpriteStateProperty => _spriteStateProperty;
        protected SerializedProperty AnimTriggerProperty => _animTriggerProperty;
        protected SerializedProperty NavigationProperty => _navigationProperty;
        #endregion

        protected virtual void OnEnable()
        {
            _script = serializedObject.FindProperty("m_Script");
            _interactableProperty = serializedObject.FindProperty("m_Interactable");
            _targetGraphicProperty = serializedObject.FindProperty("m_TargetGraphic");
            _transitionProperty = serializedObject.FindProperty("m_Transition");
            _colorBlockProperty = serializedObject.FindProperty("m_Colors");
            _spriteStateProperty = serializedObject.FindProperty("m_SpriteState");
            _animTriggerProperty = serializedObject.FindProperty("m_AnimationTriggers");
            _navigationProperty = serializedObject.FindProperty("m_Navigation");

            _propertyPathToExcludeForChildClasses = new[]
            {
                _script.propertyPath,
                _navigationProperty.propertyPath,
                _transitionProperty.propertyPath,
                _colorBlockProperty.propertyPath,
                _spriteStateProperty.propertyPath,
                _animTriggerProperty.propertyPath,
                _interactableProperty.propertyPath,
                _targetGraphicProperty.propertyPath,
            };

            var transition = GetTransition(_transitionProperty);
            _showColorTint.value = transition == Transition.ColorTint;
            _showSpriteTransition.value = transition == Transition.SpriteSwap;
            _showAnimTransition.value = transition == Transition.Animation;

            _showColorTint.valueChanged.AddListener(Repaint);
            _showSpriteTransition.valueChanged.AddListener(Repaint);

            _editors.Add(this);
            RegisterStaticOnSceneGUI();

            _showNavigation = EditorPrefs.GetBool(ShowNavigationKey);
        }

        protected virtual void OnDisable()
        {
            _showColorTint.valueChanged.RemoveListener(Repaint);
            _showSpriteTransition.valueChanged.RemoveListener(Repaint);

            _editors.Remove(this);
            RegisterStaticOnSceneGUI();
        }
        
        public sealed override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawFields();

            // We do this here to avoid requiring the user to also write a Editor for their Selectable-derived classes.
            // This way if we are on a derived class we dont draw anything else, otherwise draw the remaining properties.
            ChildClassPropertiesGUI();
            serializedObject.ApplyModifiedProperties();
        }

        #region Draw fields
        protected virtual void DrawFields()
        {
            DrawInteractable();
            DrawTransition();
            
            EditorGUILayout.Space();
            DrawNavigation();
        }

        protected void DrawInteractable() =>
            EditorGUILayout.PropertyField(_interactableProperty);

        protected void DrawTransition()
        {
             var transition = GetTransition(_transitionProperty);

            var graphic = _targetGraphicProperty.objectReferenceValue as Graphic;
            if (!graphic) graphic = (target as Selectable)?.GetComponent<Graphic>();

            var animator = (target as Selectable)?.GetComponent<Animator>();
            _showColorTint.target = !_transitionProperty.hasMultipleDifferentValues && transition == Transition.ColorTint;
            _showSpriteTransition.target = !_transitionProperty.hasMultipleDifferentValues && transition == Transition.SpriteSwap;
            _showAnimTransition.target = !_transitionProperty.hasMultipleDifferentValues && transition == Transition.Animation;

            EditorGUILayout.PropertyField(_transitionProperty);

            ++EditorGUI.indentLevel;
            {
                if (transition is Transition.ColorTint or Transition.SpriteSwap)
                {
                    EditorGUILayout.PropertyField(_targetGraphicProperty);
                }

                switch (transition)
                {
                    case Transition.ColorTint:
                        if (!graphic)
                            EditorGUILayout.HelpBox("You must have a Graphic target in order to use a color transition.", MessageType.Warning);
                        break;

                    case Transition.SpriteSwap:
                        if (graphic as Image == null)
                            EditorGUILayout.HelpBox("You must have a Image target in order to use a sprite swap transition.", MessageType.Warning);
                        break;
                    
                    case Transition.None: break;
                    case Transition.Animation: break;
                    default: throw new ArgumentOutOfRangeException();
                }

                if (EditorGUILayout.BeginFadeGroup(_showColorTint.faded))
                {
                    EditorGUILayout.PropertyField(_colorBlockProperty);
                }
                EditorGUILayout.EndFadeGroup();

                if (EditorGUILayout.BeginFadeGroup(_showSpriteTransition.faded))
                {
                    EditorGUILayout.PropertyField(_spriteStateProperty);
                }
                EditorGUILayout.EndFadeGroup();

                if (EditorGUILayout.BeginFadeGroup(_showAnimTransition.faded))
                {
                    EditorGUILayout.PropertyField(_animTriggerProperty);

                    if (!animator || !animator.runtimeAnimatorController)
                    {
                        var buttonRect = EditorGUILayout.GetControlRect();
                        buttonRect.xMin += EditorGUIUtility.labelWidth;
                        if (GUI.Button(buttonRect, "Auto Generate Animation", EditorStyles.miniButton))
                        {
                            var controller = GenerateSelectableAnimatorController((target as Selectable)?.animationTriggers, target as Selectable);
                            if (controller)
                            {
                                animator ??= (target as Selectable)?.gameObject.AddComponent<Animator>();
                                AnimatorController.SetAnimatorController(animator, controller);
                            }
                        }
                    }
                }
                EditorGUILayout.EndFadeGroup();
            }
            --EditorGUI.indentLevel;
        }

        protected void DrawNavigation()
        {
            EditorGUILayout.PropertyField(_navigationProperty);
            
            EditorGUI.BeginChangeCheck();
            var toggleRect = EditorGUILayout.GetControlRect();
            toggleRect.xMin += EditorGUIUtility.labelWidth;
            _showNavigation = GUI.Toggle(toggleRect, _showNavigation, _visualizeNavigation, EditorStyles.miniButton);
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(ShowNavigationKey, _showNavigation);
                SceneView.RepaintAll();
            }
        }
        #endregion

        // Draw the extra SerializedProperties of the child class.
        // We need to make sure that m_PropertyPathToExcludeForChildClasses has all the Selectable properties and in the correct order.
        // TODO: find a nicer way of doing this. (creating a InheritedEditor class that automagically does this)
        private void ChildClassPropertiesGUI()
        {
            if (IsDerivedSelectableEditor()) return;
            DrawPropertiesExcluding(serializedObject, _propertyPathToExcludeForChildClasses);
        }

        private bool IsDerivedSelectableEditor() =>
            GetType() != typeof(SelectableEditor);
        
        private static void RegisterStaticOnSceneGUI()
        {
            SceneView.duringSceneGui -= StaticOnSceneGUI;
            if (_editors.Count > 0)
                SceneView.duringSceneGui += StaticOnSceneGUI;
        }

        private static Transition GetTransition(SerializedProperty transition) =>
            (Transition)transition.enumValueIndex;

        private static AnimatorController GenerateSelectableAnimatorController(AnimationTriggers animationTriggers, Selectable target)
        {
            if (!target) return null;

            // Where should we create the controller?
            var path = GetSaveControllerPath(target);
            if (string.IsNullOrEmpty(path)) return null;

            // figure out clip names
            var normalName = string.IsNullOrEmpty(animationTriggers.normalTrigger) ? "Normal" : animationTriggers.normalTrigger;
            var highlightedName = string.IsNullOrEmpty(animationTriggers.highlightedTrigger) ? "Highlighted" : animationTriggers.highlightedTrigger;
            var pressedName = string.IsNullOrEmpty(animationTriggers.pressedTrigger) ? "Pressed" : animationTriggers.pressedTrigger;
            var selectedName = string.IsNullOrEmpty(animationTriggers.selectedTrigger) ? "Selected" : animationTriggers.selectedTrigger;
            var disabledName = string.IsNullOrEmpty(animationTriggers.disabledTrigger) ? "Disabled" : animationTriggers.disabledTrigger;

            // Create controller and hook up transitions.
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            GenerateTriggerableTransition(normalName, controller);
            GenerateTriggerableTransition(highlightedName, controller);
            GenerateTriggerableTransition(pressedName, controller);
            GenerateTriggerableTransition(selectedName, controller);
            GenerateTriggerableTransition(disabledName, controller);

            AssetDatabase.ImportAsset(path);

            return controller;
        }

        private static string GetSaveControllerPath(Selectable target)
        {
            var defaultName = target.gameObject.name;
            var message = $"Create a new animator for the game object '{defaultName}':";
            return EditorUtility.SaveFilePanelInProject("New Animation Controller", defaultName, "controller", message);
        }

        private static void GenerateTriggerableTransition(string name, AnimatorController controller)
        {
            // Create the clip
            var clip = AnimatorController.AllocateAnimatorClip(name);
            AssetDatabase.AddObjectToAsset(clip, controller);

            // Create a state in the animator controller for this clip
            var state = controller.AddMotion(clip);

            // Add a transition property
            controller.AddParameter(name, AnimatorControllerParameterType.Trigger);

            // Add an any state transition
            var stateMachine = controller.layers[0].stateMachine;
            var transition = stateMachine.AddAnyStateTransition(state);
            transition.AddCondition(AnimatorConditionMode.If, 0, name);
        }

        private static void StaticOnSceneGUI(SceneView view)
        {
            if (!_showNavigation) return;

            var selectables = Selectable.allSelectablesArray;
            foreach (var selectable in selectables)
                if (StageUtility.IsGameObjectRenderedByCamera(selectable.gameObject, Camera.current))
                    DrawNavigationForSelectable(selectable);
        }

        private static void DrawNavigationForSelectable(Selectable selectable)
        {
            if (!selectable) return;
            
            var transform = selectable.transform;
            var active = Selection.transforms.Any(e => e == transform);

            Handles.color = new Color(1.0f, 0.6f, 0.2f, active ? 1.0f : 0.4f);
            DrawNavigationArrow(-Vector2.right, selectable, selectable.FindSelectableOnLeft());
            DrawNavigationArrow(Vector2.up, selectable, selectable.FindSelectableOnUp());

            Handles.color = new Color(1.0f, 0.9f, 0.1f, active ? 1.0f : 0.4f);
            DrawNavigationArrow(Vector2.right, selectable, selectable.FindSelectableOnRight());
            DrawNavigationArrow(-Vector2.up, selectable, selectable.FindSelectableOnDown());
        }

        private static void DrawNavigationArrow(Vector2 direction, Selectable fromObj, Selectable toObj)
        {
            if (fromObj == null || toObj == null) return;
            
            var fromTransform = fromObj.transform;
            var toTransform = toObj.transform;

            var sideDirection = new Vector2(direction.y, -direction.x);
            var fromPoint = fromTransform.TransformPoint(GetPointOnRectEdge(fromTransform as RectTransform, direction));
            var toPoint = toTransform.TransformPoint(GetPointOnRectEdge(toTransform as RectTransform, -direction));
            var fromSize = HandleUtility.GetHandleSize(fromPoint) * 0.05f;
            var toSize = HandleUtility.GetHandleSize(toPoint) * 0.05f;
            fromPoint += fromTransform.TransformDirection(sideDirection) * fromSize;
            toPoint += toTransform.TransformDirection(sideDirection) * toSize;
            var length = Vector3.Distance(fromPoint, toPoint);
            var toTransformRotation = toTransform.rotation;
            var fromTangent = fromTransform.rotation * direction * length * 0.3f;
            var toTangent = toTransformRotation * -direction * length * 0.3f;

            Handles.DrawBezier(fromPoint, toPoint, fromPoint + fromTangent, toPoint + toTangent, Handles.color, null, ArrowThickness);
            Handles.DrawAAPolyLine(ArrowThickness, toPoint, toPoint + toTransformRotation * (-direction - sideDirection) * toSize * ArrowHeadSize);
            Handles.DrawAAPolyLine(ArrowThickness, toPoint, toPoint + toTransformRotation * (-direction + sideDirection) * toSize * ArrowHeadSize);
        }

        private static Vector3 GetPointOnRectEdge(RectTransform rectTransform, Vector2 direction)
        {
            if (!rectTransform) return Vector3.zero;
            
            if (direction != Vector2.zero)
                direction /= Mathf.Max(Mathf.Abs(direction.x), Mathf.Abs(direction.y));
            
            var rect = rectTransform.rect;
            direction = rect.center + Vector2.Scale(rect.size, direction * 0.5f);
            return direction;
        }
    }
}
