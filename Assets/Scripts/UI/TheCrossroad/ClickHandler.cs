using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using Unity.VisualScripting;

public class ClickHandler : MonoBehaviour, IPointerClickHandler
{
    public event Action ClickEmptySpace;

    public void OnPointerClick(PointerEventData eventData)
    {
        ClickEmptySpace?.Invoke();
    }

}
