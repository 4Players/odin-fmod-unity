using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DragMovement : MonoBehaviour
{
    [SerializeField] private float dragSizeIncrease = 1.1f;
    
    private Camera mainCamera;
    private bool _isDragActive = false;

    private Vector3 _initialScale;

    private void Start()
    {
        mainCamera = Camera.main;
        _initialScale = transform.localScale;
    }

    private void OnMouseDown()
    {
        _isDragActive = true;
        transform.localScale = dragSizeIncrease * _initialScale;
    }

    private Vector3 GetWorldMousePos()
    {
        Vector3 objectToScreenPoint = mainCamera.WorldToScreenPoint(transform.position);
        Vector3 mousePosition = Input.mousePosition;
        return mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, objectToScreenPoint.z));
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            _isDragActive = false;
            transform.localScale = _initialScale;
        }

        if (_isDragActive)
        {
            transform.position = GetWorldMousePos();
        }
    }
}