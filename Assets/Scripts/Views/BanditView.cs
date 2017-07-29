using UnityEngine;

public class BanditView : MonoBehaviour
{
    [SerializeField]
    TextMesh HPText;

    public int id;

    public void UpdateHP(int hp)
    {
        HPText.text = hp.ToString();
    }
}
