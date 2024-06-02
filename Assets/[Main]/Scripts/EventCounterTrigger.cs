
using UnityEngine;
using UnityEngine.Events;

namespace ARClothesTryOn
{
    public class EventCounterTrigger : MonoBehaviour
    {
        [SerializeField] private UnityEvent onEmpty;
        [SerializeField] private UnityEvent onNotEmpty;

        private int counter;

        public void Increment()
        {
            counter++;
            if (counter == 1)
                onNotEmpty?.Invoke();
        }

        public void Decrement()
        {
            counter--;
            if (counter < 0) counter = 0;
            if (counter == 0)
                onEmpty?.Invoke();
        }
    }

}