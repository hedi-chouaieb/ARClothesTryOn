
using System;
using UnityEngine;
using UnityEngine.Events;

namespace ARClothesTryOn
{
    public class TriggerHandler : MonoBehaviour
    {
        [SerializeField] private string tagTarget;
        [SerializeField] private UnityEvent onTrigger;
        [SerializeField] private UnityEvent onExit;


        void OnTriggerEnter(Collider other)
        {
            if (!other.tag.Equals(tagTarget)) return;

            onTrigger?.Invoke();
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.tag.Equals(tagTarget)) return;

            onExit?.Invoke();
        }
    }

}