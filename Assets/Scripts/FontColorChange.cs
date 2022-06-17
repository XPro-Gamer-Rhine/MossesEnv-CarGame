using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class FontColorChange : MonoBehaviour
{
    // Start is called before the first frame update
    private TextMeshPro m_TextMeshPro;
    public string m_Text;
    public Color m_fontColor;
    void Awake()
    {
        m_TextMeshPro = gameObject.GetComponent<TextMeshPro>()??gameObject.AddComponent<TextMeshPro>();
        m_TextMeshPro.text = m_Text;
        m_TextMeshPro.color = m_fontColor;
    }
}
