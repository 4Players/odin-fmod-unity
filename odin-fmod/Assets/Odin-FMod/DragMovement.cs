using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DragMovement : MonoBehaviour
{
    private Camera mainCamera;
    
    private bool _isDragActive = false;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void OnMouseDown()
    {
        _isDragActive = true;
        transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
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
            transform.localScale = new Vector3(1f, 1f, 1f);
        }

        if (_isDragActive)
        {
            transform.position = GetWorldMousePos();
        }
    }
}