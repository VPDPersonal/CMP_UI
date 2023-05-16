using System;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.EventSystems;

// Code by VPDInc
// Email: vpd-2000@yandex.com
// Version: 1.1.0
namespace VPDUI
{
    [AddComponentMenu("UI/Improved button", 30)]
    public sealed class ImprovedButton : Selectable, IPointerClickHandler
    {
        public event UnityAction Clicked
        {
            add => _clicked.AddListener(value);
            remove => _clicked.RemoveListener(value);
        }
        
        #region Inspector Fields
        [SerializeField] private Behaviour[] _behaviours;
        
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

        #region Unity Methods
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            
            var clickCount = _behaviours.Count(b => b == Behaviour.Click);
            var longClickCount = _behaviours.Count(b => b == Behaviour.LongClick);
            var multiClickCount = _behaviours.Count(b => b == Behaviour.MultiClick);

            if (clickCount > 1)
            {
                if (longClickCount > 0)
                {
                    if (multiClickCount > 0)
                        Array.Resize(ref _behaviours, _behaviours.Length - 1);
                    else _behaviours[^1] = Behaviour.MultiClick;
                }
                else _behaviours[^1] = Behaviour.LongClick;
            }

            if (longClickCount > 1)
            {
                if (multiClickCount > 0)
                {
                    if (clickCount > 0)
                        Array.Resize(ref _behaviours, _behaviours.Length - 1);
                    else _behaviours[^1] = Behaviour.Click;
                }
                else _behaviours[^1] = Behaviour.MultiClick;
            }
            
            if (multiClickCount > 1 && _behaviours.Length > 1)
            {
                if (longClickCount > 0)
                {
                    if (clickCount > 0)
                        Array.Resize(ref _behaviours, _behaviours.Length - 1);
                    else _behaviours[^1] = Behaviour.Click;
                }
                else _behaviours[^1] = Behaviour.LongClick;
            }
        }
#endif

        protected override void Awake()
        {
            base.Awake();

            if (!ExistBehaviour(Behaviour.LongClick) ||
                _longClickTransition != LongClickTransition.Filling) return;
                
            var graphic = (Image)targetGraphic;
            graphic.type = Image.Type.Filled;
            graphic.fillAmount = 0;
        }
#endregion

        #region Pointer Methods
        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!IsLeftInputButton(eventData)) return;

            foreach (var behaviour in _behaviours)
            {
                switch (behaviour)
                {
                    case Behaviour.Click: base.OnPointerDown(eventData); break;
                    case Behaviour.MultiClick: break;
                    case Behaviour.LongClick: StartLongClick(); break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            if (!IsLeftInputButton(eventData)) return;
            
            foreach (var behaviour in _behaviours)
            {
                switch (behaviour)
                {
                    case Behaviour.Click: base.OnPointerUp(eventData); break;
                    case Behaviour.MultiClick: break;
                    case Behaviour.LongClick: StopLongClick(); break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsLeftInputButton(eventData)) return;

            foreach (var behaviour in _behaviours)
            {
                switch (behaviour)
                {
                    case Behaviour.LongClick: break;
                    case Behaviour.Click: Press(); break;
                    case Behaviour.MultiClick: PressMulti(); break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }
        #endregion

        #region Long Press Methods
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

        #region Press Multi Methods
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

        #region Check Methods
        private bool ExistBehaviour(Behaviour behaviour) =>
            _behaviours.Any(b => b == behaviour);

        private bool IsCanClick() => !(!IsActive() || !IsInteractable());

        private static bool IsLeftInputButton(PointerEventData eventData) =>
            eventData.button == PointerEventData.InputButton.Left;
        #endregion
        
        #region Enums
        public enum Behaviour
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
    }
}
