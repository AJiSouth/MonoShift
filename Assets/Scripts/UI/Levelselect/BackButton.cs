using UnityEngine;
using UnityEngine.UI;

public class BackButton : MonoBehaviour
{
    private Button btn;

    void Start()
    {
        btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(() => LevelManager.GoToMainMenu());
    }
}