using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class FloatingText : MonoBehaviour {

    public Animator animator;
    private Text damageText;

     void OnEnable()
    {
        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        Destroy(gameObject, 2);
        damageText = animator.GetComponent<Text>();


    }
    public void SetText(string text)
    {
        damageText.text = text;
    }
}
