using UnityEngine;
using UnityEngine.Events;

namespace Hedi.me.Tools
{
    public class EntityDataListener<T> : MonoBehaviour
    {
        [SerializeField] protected EntityData<T> entityData;
        [SerializeField] protected bool triggerEventOnEnabled;
        [SerializeField] protected UnityEventData onUpdated;
        [SerializeField] protected UnityEventString onUpdatedToString;

        private void OnEnable()
        {
            entityData.OnUpdated.AddListener(TriggerOnUpdated);
            if (triggerEventOnEnabled) TriggerOnUpdated(entityData.Value);
        }

        private void OnDisable()
        {
            entityData.OnUpdated.RemoveListener(TriggerOnUpdated);
        }

        public void TriggerOnUpdated(T value)
        {
            onUpdated?.Invoke(value);
            onUpdatedToString?.Invoke(value.ToString());
        }


        [System.Serializable] public class UnityEventData : UnityEvent<T> { }
        [System.Serializable] public class UnityEventString : UnityEvent<string> { }
    }
}
