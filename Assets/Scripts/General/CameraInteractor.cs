using UnityEngine;

public class CameraInteractor : MonoBehaviour
{
    private IInteractable hoveredInteractable;

    private void Update()
    {
        // Raycast from the camera to the mouse position to find hovered IInteractable objects
        IInteractable newHoveredInteractable = null;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            newHoveredInteractable = hit.collider.GetComponent<IInteractable>();
        }

        // Update the hovered interactable
        if (hoveredInteractable != newHoveredInteractable)
        {
            if (hoveredInteractable != null)
            {
                hoveredInteractable.SetHovered(false);
            }

            hoveredInteractable = newHoveredInteractable;

            if (hoveredInteractable != null)
            {
                hoveredInteractable.SetHovered(true);
            }
        }

        // Handle interaction input
        if (hoveredInteractable != null && Input.GetMouseButtonDown(0))
        {
            hoveredInteractable.Interact();
        }
    }
}
