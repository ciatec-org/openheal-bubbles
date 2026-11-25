using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class TabNavigation : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("If true, Shift+Tab navigates backwards")]
    public bool allowShiftTab = true;
    
    [Tooltip("Root transform to search for elements (leave empty to search entire scene)")]
    public Transform rootTransform;

    private List<Selectable> selectableElements = new List<Selectable>();

    private void Start()
    {
        RefreshSelectableElements();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            
            if (allowShiftTab && shiftPressed)
            {
                NavigateBackward();
            }
            else
            {
                NavigateForward();
            }
        }
    }

    private void RefreshSelectableElements()
    {
        selectableElements.Clear();
        
        Transform searchRoot = rootTransform != null ? rootTransform : transform.root;
        
        // Search for all Selectable elements in hierarchy order
        Selectable[] allSelectables = searchRoot.GetComponentsInChildren<Selectable>(true);
        
        // Sort by hierarchy position (rendering order)
        selectableElements = allSelectables
            .Where(s => s.interactable && s.gameObject.activeInHierarchy)
            .OrderBy(s => GetHierarchyPath(s.transform))
            .ToList();
    }

    private string GetHierarchyPath(Transform t)
    {
        List<int> path = new List<int>();
        Transform current = t;
        Transform root = rootTransform != null ? rootTransform : transform.root;
        
        while (current != null && current != root.parent)
        {
            path.Insert(0, current.GetSiblingIndex());
            current = current.parent;
        }
        
        return string.Join("/", path.Select(i => i.ToString("D4")));
    }

    private void NavigateForward()
    {
        if (selectableElements.Count == 0)
        {
            RefreshSelectableElements();
            if (selectableElements.Count == 0) return;
        }

        Selectable current = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject?.GetComponent<Selectable>();
        
        int currentIndex = current != null ? selectableElements.IndexOf(current) : -1;
        int nextIndex = (currentIndex + 1) % selectableElements.Count;
        
        selectableElements[nextIndex].Select();
    }

    private void NavigateBackward()
    {
        if (selectableElements.Count == 0)
        {
            RefreshSelectableElements();
            if (selectableElements.Count == 0) return;
        }

        Selectable current = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject?.GetComponent<Selectable>();
        
        int currentIndex = current != null ? selectableElements.IndexOf(current) : -1;
        int nextIndex = currentIndex <= 0 ? selectableElements.Count - 1 : currentIndex - 1;
        
        selectableElements[nextIndex].Select();
    }

    // Call this method if adding/removing elements dynamically
    public void RefreshNavigation()
    {
        RefreshSelectableElements();
    }
}

