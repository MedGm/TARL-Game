using UnityEngine;

public class BubbleWizardNPC : MonoBehaviour
{
    public DialogueCanvas dialogueCanvas;
    public OblivionPortalTrigger portalTrigger;

    private bool playerNearby = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;
            if (portalTrigger != null)
                portalTrigger.SetWizardNearby(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;
            if (portalTrigger != null)
                portalTrigger.SetWizardNearby(false);
        }
    }
}
