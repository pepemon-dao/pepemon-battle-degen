using System;
using UnityEngine;
using UnityEngine.Events;

class SelectionItem : MonoBehaviour
{
    public UnityEvent onSelected;
    public UnityEvent onDeselected;


    public void ToggleSelected()
    {
        GetComponentInParent<SelectionGroup>().ToggleSelected(this);
    }

    public void SetSelected(bool selected)
    {
        if (selected)
        {
            onSelected?.Invoke();
        }
        else
        {
            onDeselected?.Invoke();
        }
    }
}
