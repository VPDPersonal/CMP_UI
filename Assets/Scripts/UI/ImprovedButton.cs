using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.EventSystems;

// Code by VPDInc
// Email: vpd-2000@yandex.com
// Version: 1.0.0
namespace UI
{
    [AddComponentMenu("UI/Improved button", 30)]
    public sealed class ImprovedButton : Selectable, IPointerClickHandler
    {
        #region Enums
        public enum BehaviourType
        {
            Click,
            LongClick,
            MultiClick,
        }
        
        public enum LongClickTransition
        {
            None,
            Filling
        }
        #endregion

        #region Inspector fields
        [SerializeField] private BehaviourType _type;
        
        [SerializeField] private LongClickTransition _longClickTransition;
        [SerializeField] [Min(0)] private float _timeLongClick = 1;

        [SerializeField] [Min(2)] private int _needMultiClickCount = 2;
        [SerializeField] [Min(0)] private float _timeMultiClick = 1;

        [SerializeField] private UnityEvent _clicked;
        #endregion

        #region Fields
        private bool _isStartLongClick;
        private IEnumerator _pressLong;
        
        private int _clickCount;
        private IEnumerator _stoppingMultiClick;
        #endregion

        public event UnityAction Clicked
        {
            add => _clicked.AddListener(value);
            remove => _clicked.RemoveListener(value);
        }

        protected override void Awake()
        {
            base.Awake();

            if (_type == BehaviourType.LongClick &&
                _longClickTransition == LongClickTransition.Filling)
            {
                var graphic = (Image)targetGraphic;
                graphic.type = Image.Type.Filled;
                graphic.fillAmount = 0;
            }
        }

        #region Pointer functions
        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!IsLeftInputButton(eventData)) return;
            
            switch (_type)
            {
                case BehaviourType.Click: base.OnPointerDown(eventData); break;
                case BehaviourType.MultiClick: return;
                case BehaviourType.LongClick: StartLongClick(); break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            if (!IsLeftInputButton(eventData)) return;
            
            switch (_type)
            {
                case BehaviourType.Click: base.OnPointerUp(eventData); break;
                case BehaviourType.MultiClick: return;
                case BehaviourType.LongClick: StopLongClick(); break;
                default: throw new ArgumentOutOfRangeException();
            }
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsLeftInputButton(eventData)) return;

            switch (_type)
            {
                case BehaviourType.LongClick: return;
                case BehaviourType.Click: Press(); break;
                case BehaviourType.MultiClick: PressMulti(); break;
                default: throw new ArgumentOutOfRangeException();
            }
        }
        #endregion

        #region Long press functions
        private void StartLongClick()
        {
            if (_isStartLongClick) return;
            
            _isStartLongClick = true;

            CancelPressLong();
            _pressLong = PressLong();
            
            StartCoroutine(_pressLong);
        }

        private void StopLongClick()
        {
            if (!_isStartLongClick) return;
            _isStartLongClick = false;

            CancelPressLong();

            ((Image)targetGraphic).fillAmount = 0;
        }

        private IEnumerator PressLong()
        {
            StartCoroutine(Filling());
            yield return new WaitForSeconds(_timeLongClick);

            Press();
            StopLongClick();
        }

        private void CancelPressLong()
        {
            if (_pressLong == null) return;
            
            StopCoroutine(_pressLong);
            _pressLong = null;
        }
        #endregion

        #region Press multi functions
        private void PressMulti()
        {
            _clickCount++;
            
            if (_clickCount == 1)
            {
                CancelStoppingMultiClick();
                StartStoppingMultiClick();
            }
            
            if (_clickCount >= _needMultiClickCount)
            {
                CancelStoppingMultiClick();
                _clickCount = 0;
                Press();
            }
        }

        private void StartStoppingMultiClick()
        {
            if (_stoppingMultiClick != null)
                throw new Exception("StoppingMultiClick is playing already");
            
            _stoppingMultiClick = StoppingMultiClick();
            StartCoroutine(_stoppingMultiClick);
        }

        private void CancelStoppingMultiClick()
        {
            if (_stoppingMultiClick == null) return;
            
            StopCoroutine(_stoppingMultiClick);
            _stoppingMultiClick = null;
        }

        private IEnumerator StoppingMultiClick()
        {
            yield return new WaitForSeconds(_timeMultiClick);
            _clickCount = 0;
        }
        #endregion

        private void Press()
        {
            if (!IsCanClick()) return;
            _clicked?.Invoke();
        }

        private IEnumerator Filling()
        {
            if (_longClickTransition != LongClickTransition.Filling) yield break;
            
            var elapsedTime = 0f;
            var graphic = (Image)targetGraphic;
            
            while (elapsedTime < _timeLongClick)
            {
                if (!_isStartLongClick) yield break;
                
                elapsedTime += Time.unscaledDeltaTime;
                graphic.fillAmount = elapsedTime / _timeLongClick;
                yield return null;
            }
        }

        private bool IsCanClick() => !(!IsActive() || !IsInteractable());

        private static bool IsLeftInputButton(PointerEventData eventData) =>
            eventData.button == PointerEventData.InputButton.Left;
    }
}
