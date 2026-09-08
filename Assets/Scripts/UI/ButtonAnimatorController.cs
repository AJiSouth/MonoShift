using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonAnimatorController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        anim.SetBool("IsHighlighted", true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        anim.SetBool("IsHighlighted", false);
    }
}