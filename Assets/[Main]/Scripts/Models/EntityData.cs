using UnityEngine;
using UnityEngine.Events;

namespace Hedi.me.Tools
{
    public class EntityData<T> : ScriptableObject
    {
        [SerializeField] protected T initialValue;
        [SerializeField] protected UnityEventData onUpdated = new UnityEventData();

        protected T value;

        public T InitialValue { get => initialValue; private set => initialValue = value; }
        public T Value
        {
            get => value;
            set
            {
                this.value = value;
                OnUpdated?.Invoke(this.value);
            }
        }

        public UnityEventData OnUpdated { get => onUpdated; set => onUpdated = value; }

        public void TriggerOnUpdated()
        {
            onUpdated?.Invoke(this.value);
        }

        public virtual void ResetValue(bool triggerOnUpdate)
        {
            if(triggerOnUpdate)
            {
                this.Value = this.InitialValue;
                return;
            }
            this.value = initialValue;
        }

        private void OnEnable()
        {
            ResetValue(false);
        }

        [System.Serializable] public class UnityEventData : UnityEvent<T> { }
    }
}